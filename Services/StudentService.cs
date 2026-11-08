using HtmlAgilityPack;
using MauiApp1tesst.Models;
using System.Diagnostics;
using System.Text.Json;

namespace MauiApp1tesst.Services;

public class StudentService
{
    private readonly HttpClient _httpClient;

    public StudentService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Linux; Android 10) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.120 Mobile Safari/537.36");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<StudentInfo> GetStudentInfoAsync(string url)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentException("Nội dung mã QR không được để trống.");
            }

            var trimmedInput = url.Trim();
            if (trimmedInput.StartsWith("{") && trimmedInput.EndsWith("}"))
            {
                var directStudent = ParseJsonResponse(trimmedInput);
                if (directStudent != null && !string.IsNullOrEmpty(directStudent.HoTen))
                {
                    return directStudent;
                }
            }

            var qrLink = ExtractQrLink(url);
            if (string.IsNullOrEmpty(qrLink))
            {
                throw new ArgumentException("QR Link không hợp lệ.");
            }

            Debug.WriteLine($"📥 QR Link: {qrLink}");

            // Try direct API calls first (faster and more reliable)
            var apiEndpoints = new[]
            {
                $"https://sinhvien.hactech.edu.vn/api/students/{qrLink}/info-with-qr-link?_qrLink={qrLink}",
                $"https://sinhvien.hactech.edu.vn/api/student/card?qrLink={qrLink}",
                $"https://sinhvien.hactech.edu.vn/api/student-info?qrLink={qrLink}",
                $"https://sinhvien.hactech.edu.vn/api/v1/student?qrLink={qrLink}",
                $"https://sinhvien.hactech.edu.vn/api/getStudentCard?qrLink={qrLink}",
                $"https://sinhvien.hactech.edu.vn/api/StudentCard?qrLink={qrLink}",
            };

            StudentInfo student = null;

            foreach (var endpoint in apiEndpoints)
            {
                student = await TryApiEndpoint(endpoint);
                if (student != null && !string.IsNullOrEmpty(student.HoTen))
                {
                    Debug.WriteLine($"✓ Got data from API!");
                    return student;
                }
            }

            // Fallback to direct HTTP request parsing HTML
            Debug.WriteLine("⚠️ No API endpoints worked, parsing HTML directly...");
            try
            {
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();

                // Check if this is a rendered page with data or just an empty shell
                if (content.Length < 1000 || !content.Contains("Họ và tên") && !content.Contains("Ho va ten") && !content.Contains("Ngày sinh") && !content.Contains("Ngay sinh"))
                {
                    Debug.WriteLine("⚠️ Received page appears to be JavaScript-rendered (content is too small or missing expected fields)");
                    Debug.WriteLine("📄 The page likely loads data via JavaScript which we cannot execute on Android");
                    Debug.WriteLine("💡 Solution: Look for REST API endpoint or use browser-based loading");
                    throw new Exception("HTML page requires JavaScript rendering which is not supported on this platform. The website uses client-side rendering. Please check if there's a REST API endpoint available.");
                }

                student = ParseStudentInfo(content);

                if (string.IsNullOrEmpty(student?.HoTen))
                {
                    throw new Exception("Không thể lấy dữ liệu sinh viên. Vui lòng kiểm tra QR Link.");
                }

                return student;
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Lỗi kết nối: {ex.Message}");
            }
            catch (Exception ex) when (!ex.Message.Contains("Lỗi kết nối") && !ex.Message.Contains("REST API"))
            {
                throw new Exception($"Lỗi: {ex.Message}");
            }
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Lỗi kết nối: {ex.Message}");
        }
        catch (Exception ex)
        {
            throw new Exception($"Lỗi: {ex.Message}");
        }
    }

    private string ExtractQrLink(string url)
    {
        if (url.Contains("qrLink="))
        {
            var startIndex = url.IndexOf("qrLink=") + 7;
            var endIndex = url.IndexOf("&", startIndex);
            if (endIndex == -1) endIndex = url.Length;
            return url.Substring(startIndex, endIndex - startIndex);
        }
        return url;
    }

    private async Task<StudentInfo> TryApiEndpoint(string apiUrl)
    {
        try
        {
            var endpoint = apiUrl.Split('/').Last();
            Debug.WriteLine($"📡 Trying: {endpoint}");

            var response = await _httpClient.GetAsync(apiUrl);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();

            // Check if response is JSON or HTML
            if (content.TrimStart().StartsWith("{") || content.TrimStart().StartsWith("["))
            {
                var student = ParseJsonResponse(content);
                if (student != null && !string.IsNullOrEmpty(student.HoTen))
                {
                    return student;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ {ex.Message}");
            return null;
        }
    }

    private StudentInfo ParseJsonResponse(string jsonContent)
    {
        try
        {
            using (JsonDocument doc = JsonDocument.Parse(jsonContent))
            {
                var root = doc.RootElement;
                var student = new StudentInfo();

                if (root.TryGetProperty("data", out var dataElement))
                {
                    student = ExtractStudentFromJson(dataElement);
                }
                else if (root.TryGetProperty("student", out var studentElement))
                {
                    student = ExtractStudentFromJson(studentElement);
                }
                else
                {
                    student = ExtractStudentFromJson(root);
                }

                if (!string.IsNullOrEmpty(student?.HoTen))
                {
                    Debug.WriteLine($"✓ Parsed: {student.HoTen}");
                    return student;
                }
                return null;
            }
        }
        catch
        {
            return null;
        }
    }

    private StudentInfo ExtractStudentFromJson(JsonElement element)
    {
        var student = new StudentInfo();

        var nameProperties = new[] { "name", "hoTen", "ho_ten", "fullName", "fullname", "studentName", "ten" };
        var dobProperties = new[] { "birthday", "ngaySinh", "ngay_sinh", "dateOfBirth", "dob", "birthDate", "birth_date" };
        var idProperties = new[] { "code", "maSinhVien", "ma_sinh_vien", "studentId", "id", "ma" };
        var facultyProperties = new[] { "courseCode", "course_code", "khoa", "faculty", "department", "khoaHoc" };
        var specialtyProperties = new[] { "majors", "nghe", "specialty", "major", "nganh", "chuyen_nganh" };
        var statusProperties = new[] { "status", "trangThai", "trang_thai" };

        student.HoTen = GetStringValue(element, nameProperties);
        student.NgaySinh = GetStringValue(element, dobProperties);
        student.MaSinhVien = GetStringValue(element, idProperties);
        student.Khoa = GetStringValue(element, facultyProperties);
        student.Nghe = GetStringValue(element, specialtyProperties);
        student.TrangThai = ParseStatusValue(element, statusProperties);

        return student;
    }

    private string ParseStatusValue(JsonElement element, string[] propertyNames)
    {
        foreach (var prop in propertyNames)
        {
            try
            {
                if (element.TryGetProperty(prop, out var value))
                {
                    if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int statusInt))
                    {
                        return statusInt switch
                        {
                            3 => "Đã tốt nghiệp",
                            1 => "Đang học",
                            2 => "Bảo lưu",
                            _ => $"Trạng thái {statusInt}"
                        };
                    }
                    else if (value.ValueKind == JsonValueKind.String)
                    {
                        var str = value.GetString()?.Trim();
                        if (!string.IsNullOrEmpty(str))
                        {
                            if (int.TryParse(str, out int parsedInt))
                            {
                                return parsedInt switch
                                {
                                    3 => "Đã tốt nghiệp",
                                    1 => "Đang học",
                                    2 => "Bảo lưu",
                                    _ => str
                                };
                            }
                            return str;
                        }
                    }
                }
            }
            catch { }
        }
        return string.Empty;
    }

    private string GetStringValue(JsonElement element, string[] propertyNames)
    {
        foreach (var prop in propertyNames)
        {
            try
            {
                if (element.TryGetProperty(prop, out var value))
                {
                    var stringValue = value.GetString();
                    if (!string.IsNullOrEmpty(stringValue))
                    {
                        return CleanText(stringValue);
                    }
                }
            }
            catch { }
        }
        return string.Empty;
    }

    private async Task<StudentInfo> GetStudentInfoWithPuppeteer(string url)
    {
        // Puppeteer is not supported on Android, skip this method
        Debug.WriteLine("⚠️ Puppeteer is not supported on Android");
        return null;
    }

    private StudentInfo ParseStudentInfo(string htmlContent)
    {
        var student = new StudentInfo();
        var doc = new HtmlDocument();
        doc.LoadHtml(htmlContent);

        Debug.WriteLine("🔍 Parsing HTML...");
        Debug.WriteLine($"📏 HTML length: {htmlContent.Length} characters");

        // Log first 500 chars of content to understand structure
        var htmlPreview = htmlContent.Length > 500 ? htmlContent.Substring(0, 500) : htmlContent;
        Debug.WriteLine($"📄 HTML preview: {htmlPreview.Replace("\n", " ").Replace("\r", "")}");

        // Try multiple parsing strategies

        // Strategy 1: Find rows with 'mb-3 row' class
        var rows = doc.DocumentNode.SelectNodes("//div[contains(@class, 'mb-3') and contains(@class, 'row')]");
        if (rows != null && rows.Count > 0)
        {
            Debug.WriteLine($"✓ Strategy 1: Found {rows.Count} data rows with 'mb-3 row'");
            foreach (var row in rows)
            {
                ParseRowData(row, student);
            }
        }
        else
        {
            Debug.WriteLine("ℹ️ Strategy 1: No rows with 'mb-3 row' found");
        }

        // Strategy 2: Find rows with just 'row' class
        if (string.IsNullOrEmpty(student.HoTen))
        {
            rows = doc.DocumentNode.SelectNodes("//div[contains(@class, 'row')]");
            if (rows != null && rows.Count > 5)
            {
                Debug.WriteLine($"✓ Strategy 2: Found {rows.Count} rows, trying each...");
                foreach (var row in rows)
                {
                    if (ParseRowData(row, student) && !string.IsNullOrEmpty(student.HoTen))
                    {
                        break;
                    }
                }
            }
            else
            {
                Debug.WriteLine($"ℹ️ Strategy 2: Not enough rows (found {rows?.Count ?? 0})");
            }
        }

        // Strategy 3: Find any elements with text content that looks like labels
        if (string.IsNullOrEmpty(student.HoTen))
        {
            Debug.WriteLine("✓ Strategy 3: Finding by text content matching...");
            var allElements = doc.DocumentNode.SelectNodes("//*[text()]");
            if (allElements != null && allElements.Count > 0)
            {
                Debug.WriteLine($"   Found {allElements.Count} elements with text");
                for (int i = 0; i < allElements.Count - 1; i++)
                {
                    var labelText = CleanText(allElements[i].InnerText);
                    var valueText = CleanText(allElements[i + 1].InnerText);

                    if (string.IsNullOrWhiteSpace(labelText) || string.IsNullOrWhiteSpace(valueText))
                        continue;

                    // Try to match Vietnamese labels
                    if (labelText.Contains("Họ và tên") || labelText.Contains("Ho va ten") || labelText.Contains("Name"))
                    {
                        if (!string.IsNullOrWhiteSpace(valueText) && valueText.Length < 100)
                        {
                            student.HoTen = valueText;
                            Debug.WriteLine($"   ✓ Found Name: {valueText}");
                        }
                    }
                    else if (labelText.Contains("Ngày sinh") || labelText.Contains("Ngay sinh") || labelText.Contains("Birth"))
                    {
                        if (!string.IsNullOrWhiteSpace(valueText) && valueText.Length < 50)
                        {
                            student.NgaySinh = valueText;
                            Debug.WriteLine($"   ✓ Found DOB: {valueText}");
                        }
                    }
                    else if (labelText.Contains("Mã sinh viên") || labelText.Contains("Ma sinh vien") || labelText.Contains("Student ID"))
                    {
                        if (!string.IsNullOrWhiteSpace(valueText) && valueText.Length < 50)
                        {
                            student.MaSinhVien = valueText;
                            Debug.WriteLine($"   ✓ Found ID: {valueText}");
                        }
                    }
                    else if (labelText.Contains("Khóa") || labelText.Contains("Khoa") || labelText.Contains("Faculty") || labelText.Contains("Khóa"))
                    {
                        if (!string.IsNullOrWhiteSpace(valueText) && valueText.Length < 100)
                        {
                            student.Khoa = valueText;
                            Debug.WriteLine($"   ✓ Found Faculty: {valueText}");
                        }
                    }
                    else if (labelText.Contains("Nghề") || labelText.Contains("Nghe") || labelText.Contains("Chuyên ngành") || labelText.Contains("Chuyen nganh") || labelText.Contains("Specialty"))
                    {
                        if (!string.IsNullOrWhiteSpace(valueText) && valueText.Length < 100)
                        {
                            student.Nghe = valueText;
                            Debug.WriteLine($"   ✓ Found Specialty: {valueText}");
                        }
                    }
                }
            }
        }

        // Strategy 4: Extract all text content and use simple splitting
        if (string.IsNullOrEmpty(student.HoTen))
        {
            Debug.WriteLine("✓ Strategy 4: Using text extraction...");
            var allText = doc.DocumentNode.InnerText;
            var lines = allText.Split(new[] { "\r\n", "\r", "\n", "\t" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l) && l.Length > 0)
                .ToList();

            Debug.WriteLine($"   Found {lines.Count} text lines");

            // Search for patterns in the text
            for (int i = 0; i < lines.Count - 1; i++)
            {
                var line = lines[i];
                var nextLine = i + 1 < lines.Count ? lines[i + 1] : "";

                if (string.IsNullOrWhiteSpace(nextLine) || nextLine.Length > 150)
                    continue;

                if (line.Contains("Họ và tên") || line.Contains("Ho va ten") || (line.Trim() == "Name" && nextLine.Length < 50))
                {
                    student.HoTen = nextLine;
                    Debug.WriteLine($"   ✓ Found Name: {nextLine}");
                }
                else if (line.Contains("Ngày sinh") || line.Contains("Ngay sinh") || (line.Contains("Birth") && nextLine.Length < 20))
                {
                    student.NgaySinh = nextLine;
                    Debug.WriteLine($"   ✓ Found DOB: {nextLine}");
                }
                else if (line.Contains("Mã sinh viên") || line.Contains("Ma sinh vien") || (line.Contains("Student ID") && nextLine.Length < 20))
                {
                    student.MaSinhVien = nextLine;
                    Debug.WriteLine($"   ✓ Found ID: {nextLine}");
                }
                else if ((line.Contains("Khóa") || line.Contains("Khoa") || line.Trim() == "Faculty") && nextLine.Length < 50)
                {
                    student.Khoa = nextLine;
                    Debug.WriteLine($"   ✓ Found Faculty: {nextLine}");
                }
                else if ((line.Contains("Nghề") || line.Contains("Nghe") || line.Contains("Chuyên ngành") || line.Contains("Specialty")) && nextLine.Length < 100)
                {
                    student.Nghe = nextLine;
                    Debug.WriteLine($"   ✓ Found Specialty: {nextLine}");
                }
            }
        }

        if (!string.IsNullOrEmpty(student.HoTen))
        {
            Debug.WriteLine($"✅ Success: HoTen={student.HoTen}, NgaySinh={student.NgaySinh}, MaSinhVien={student.MaSinhVien}, Khoa={student.Khoa}, Nghe={student.Nghe}");
        }
        else
        {
            Debug.WriteLine("❌ Failed to extract any student data - all strategies failed");
        }

        return student;
    }

    private bool ParseRowData(HtmlNode row, StudentInfo student)
    {
        try
        {
            var children = row.ChildNodes.Where(n => n.NodeType == HtmlNodeType.Element).ToList();

            if (children.Count >= 2)
            {
                var labelDiv = children[0];
                var valueDiv = children[children.Count - 1];

                var label = CleanText(labelDiv.InnerText);
                var value = CleanText(valueDiv.InnerText);

                if (string.IsNullOrEmpty(label) || string.IsNullOrEmpty(value) || label == value)
                    return false;

                Debug.WriteLine($"  {label}: {value}");

                bool found = false;
                if (label.Contains("Họ và tên", StringComparison.OrdinalIgnoreCase) || 
                    label.Contains("Ho va ten", StringComparison.OrdinalIgnoreCase) ||
                    label.Contains("Name", StringComparison.OrdinalIgnoreCase))
                {
                    student.HoTen = value;
                    found = true;
                }
                else if (label.Contains("Ngày sinh", StringComparison.OrdinalIgnoreCase) || 
                         label.Contains("Ngay sinh", StringComparison.OrdinalIgnoreCase) ||
                         label.Contains("Birth", StringComparison.OrdinalIgnoreCase))
                {
                    student.NgaySinh = value;
                    found = true;
                }
                else if (label.Contains("Mã sinh viên", StringComparison.OrdinalIgnoreCase) || 
                         label.Contains("Ma sinh vien", StringComparison.OrdinalIgnoreCase) ||
                         label.Contains("ID", StringComparison.OrdinalIgnoreCase))
                {
                    student.MaSinhVien = value;
                    found = true;
                }
                else if (label.Contains("Khóa", StringComparison.OrdinalIgnoreCase) || 
                         label.Contains("Khoa", StringComparison.OrdinalIgnoreCase) ||
                         label.Contains("Faculty", StringComparison.OrdinalIgnoreCase))
                {
                    student.Khoa = value;
                    found = true;
                }
                else if (label.Contains("Nghề", StringComparison.OrdinalIgnoreCase) || 
                         label.Contains("Nghe", StringComparison.OrdinalIgnoreCase) ||
                         label.Contains("Chuyên ngành", StringComparison.OrdinalIgnoreCase) ||
                         label.Contains("Chuyen nganh", StringComparison.OrdinalIgnoreCase) ||
                         label.Contains("Specialty", StringComparison.OrdinalIgnoreCase))
                {
                    student.Nghe = value;
                    found = true;
                }

                return found;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error parsing row: {ex.Message}");
        }

        return false;
    }

    private StudentInfo ExtractUsingRegex(string text)
    {
        // This method is deprecated - using Strategy 4 inline instead
        return new StudentInfo();
    }

    private string CleanText(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
    }

    public async Task<DiplomaInfo> LookupDiplomaAsync(string shb, string msv)
    {
        if (string.IsNullOrWhiteSpace(shb) && string.IsNullOrWhiteSpace(msv))
        {
            throw new ArgumentException("Vui lòng nhập Số hiệu bằng hoặc Mã sinh viên.");
        }

        shb = shb?.Trim() ?? string.Empty;
        msv = msv?.Trim() ?? string.Empty;

        // Try direct API endpoints first
        var apiUrls = new[]
        {
            $"https://www.hactech.edu.vn/api/tra-cuu-van-bang?shb={shb}&msv={msv}",
            $"https://www.hactech.edu.vn/api/v1/diploma?shb={shb}&msv={msv}",
            $"https://www.hactech.edu.vn/api/diploma/search?shb={shb}&msv={msv}"
        };

        foreach (var apiUrl in apiUrls)
        {
            try
            {
                var apiResponse = await _httpClient.GetAsync(apiUrl);
                if (apiResponse.IsSuccessStatusCode)
                {
                    var jsonContent = await apiResponse.Content.ReadAsStringAsync();
                    if (jsonContent.TrimStart().StartsWith("{") || jsonContent.TrimStart().StartsWith("["))
                    {
                        var diploma = ParseDiplomaJson(jsonContent);
                        if (diploma != null && (!string.IsNullOrEmpty(diploma.HoTen) || !string.IsNullOrEmpty(diploma.SoHieuBang)))
                        {
                            return diploma;
                        }
                    }
                }
            }
            catch { }
        }

        // Main HTTP GET request to website
        var targetUrl = $"https://www.hactech.edu.vn/tin-tuc/tra-cuu-van-bang?shb={Uri.EscapeDataString(shb)}&msv={Uri.EscapeDataString(msv)}";
        Debug.WriteLine($"📥 Querying Diploma: {targetUrl}");

        var response = await _httpClient.GetAsync(targetUrl);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        if (html.Contains("g-recaptcha") && (html.Contains("Xác nhận không phải máy") || html.Contains("g-recaptcha-response")))
        {
            var parsedDiploma = ParseDiplomaHtml(html);
            if (parsedDiploma != null && !string.IsNullOrEmpty(parsedDiploma.HoTen))
            {
                return parsedDiploma;
            }
            throw new Exception("Trang web yêu cầu xác minh reCAPTCHA. Vui lòng thử tra cứu trên web hoặc sử dụng WebView.");
        }

        var resultDiploma = ParseDiplomaHtml(html);
        if (resultDiploma == null || (string.IsNullOrEmpty(resultDiploma.HoTen) && string.IsNullOrEmpty(resultDiploma.SoHieuBang)))
        {
            throw new Exception("Không tìm thấy thông tin văn bằng phù hợp.");
        }

        return resultDiploma;
    }

    private DiplomaInfo ParseDiplomaJson(string jsonContent)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;
            JsonElement target = root;

            if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
            {
                target = root[0];
            }
            else if (root.TryGetProperty("data", out var dataElem))
            {
                if (dataElem.ValueKind == JsonValueKind.Array && dataElem.GetArrayLength() > 0)
                    target = dataElem[0];
                else
                    target = dataElem;
            }

            var diploma = new DiplomaInfo
            {
                SoHieuBang = GetStringValue(target, new[] { "soHieuBang", "so_hieu_bang", "shb", "soHieu", "diplomaCode" }),
                MaSinhVien = GetStringValue(target, new[] { "maSinhVien", "ma_sinh_vien", "msv", "studentCode", "code" }),
                HoTen = GetStringValue(target, new[] { "hoTen", "ho_ten", "name", "fullName", "fullname" }),
                GioiTinh = GetStringValue(target, new[] { "gioiTinh", "gioi_tinh", "gender", "sex" }),
                NgaySinh = GetStringValue(target, new[] { "ngaySinh", "ngay_sinh", "dateOfBirth", "dob", "birthday" }),
                NoiSinh = GetStringValue(target, new[] { "noiSinh", "noi_sinh", "placeOfBirth", "pob" }),
                Nghe = GetStringValue(target, new[] { "nghe", "specialty", "major", "nganh", "chuyen_nganh" }),
                DiemTBTN = GetStringValue(target, new[] { "diemTBTN", "diem_tbtn", "gpa", "score" }),
                XepLoaiTN = GetStringValue(target, new[] { "xepLoaiTN", "xep_loai_tn", "xepLoai", "rank", "grade" }),
                GhiChu = GetStringValue(target, new[] { "ghiChu", "ghi_chu", "note", "notes" })
            };

            return diploma;
        }
        catch
        {
            return null;
        }
    }

    private DiplomaInfo ParseDiplomaHtml(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var diploma = new DiplomaInfo();

        var tableRows = doc.DocumentNode.SelectNodes("//table//tr");
        if (tableRows != null && tableRows.Count > 1)
        {
            foreach (var row in tableRows)
            {
                var cells = row.SelectNodes("td|th");
                if (cells != null && cells.Count >= 7)
                {
                    var cellTexts = cells.Select(c => CleanText(c.InnerText)).ToList();
                    
                    if (cellTexts[0].Contains("Số hiệu") || cellTexts[1].Contains("Mã sinh viên"))
                        continue;

                    diploma.SoHieuBang = cellTexts[0];
                    diploma.MaSinhVien = cellTexts.Count > 1 ? cellTexts[1] : "";
                    diploma.HoTen = cellTexts.Count > 2 ? cellTexts[2] : "";
                    diploma.GioiTinh = cellTexts.Count > 3 ? cellTexts[3] : "";
                    diploma.NgaySinh = cellTexts.Count > 4 ? cellTexts[4] : "";
                    diploma.NoiSinh = cellTexts.Count > 5 ? cellTexts[5] : "";
                    diploma.Nghe = cellTexts.Count > 6 ? cellTexts[6] : "";
                    diploma.DiemTBTN = cellTexts.Count > 7 ? cellTexts[7] : "";
                    diploma.XepLoaiTN = cellTexts.Count > 8 ? cellTexts[8] : "";
                    diploma.GhiChu = cellTexts.Count > 9 ? cellTexts[9] : "";

                    return diploma;
                }
            }
        }

        return diploma;
    }
}

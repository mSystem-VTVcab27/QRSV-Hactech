using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using MauiApp1tesst.Models;
using MauiApp1tesst.Services;

namespace MauiApp1tesst.ViewModels
{
    public class StudentViewModel : INotifyPropertyChanged
    {
        private readonly StudentService _studentService;
        private StudentInfo? _studentInfo;
        private bool _isLoading;
        private string _errorMessage = string.Empty;
        private string _statusMessage = "Hướng máy ảnh về phía mã QR để quét thông tin.";

        public StudentViewModel(StudentService studentService)
        {
            _studentService = studentService;
            ScanQRCommand = new Command(async () => await ScanQRCode());
            OpenDiplomaWebCommand = new Command(async () => await OpenDiplomaWeb());
        }

        public StudentInfo? StudentInfo
        {
            get => _studentInfo;
            set
            {
                if (_studentInfo != value)
                {
                    _studentInfo = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (_errorMessage != value)
                {
                    _errorMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand ScanQRCommand { get; }
        public ICommand OpenDiplomaWebCommand { get; }

        private async Task ScanQRCode()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.Camera>();
                }

                if (status != PermissionStatus.Granted)
                {
                    ErrorMessage = "Quyền truy cập camera bị từ chối.";
                    return;
                }

                await Shell.Current.GoToAsync("camera");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi: {ex.Message}";
            }
        }

        private async Task OpenDiplomaWeb()
        {
            try
            {
                string url = "https://www.hactech.edu.vn/tin-tuc/tra-cuu-van-bang";
                if (StudentInfo != null && !string.IsNullOrWhiteSpace(StudentInfo.MaSinhVien))
                {
                    url = $"https://www.hactech.edu.vn/tin-tuc/tra-cuu-van-bang?msv={Uri.EscapeDataString(StudentInfo.MaSinhVien)}";
                }
                await Launcher.Default.OpenAsync(new Uri(url));
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Không thể mở trình duyệt: {ex.Message}";
            }
        }

        public async Task ProcessQrCodeScanAsync(string qrCode)
        {
            if (IsLoading) return;
            IsLoading = true;
            ErrorMessage = string.Empty;
            StatusMessage = "Đang tải thông tin sinh viên...";
            StudentInfo = null;

            try
            {
                string url = qrCode;
                if (!qrCode.Trim().StartsWith("{") && !qrCode.StartsWith("http"))
                {
                    url = $"https://sinhvien.hactech.edu.vn/student-card/?qrLink={qrCode}";
                }

                var info = await _studentService.GetStudentInfoAsync(url);
                
                if (info == null || (string.IsNullOrEmpty(info.HoTen) && string.IsNullOrEmpty(info.MaSinhVien)))
                {
                    ErrorMessage = "Không thể lấy thông tin sinh viên từ mã QR.";
                    StatusMessage = "Quét không thành công.";
                    return;
                }

                StudentInfo = info;
                StatusMessage = $"Đã tải thành công thông tin sinh viên!";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi xử lý QR: {ex.Message}";
                StatusMessage = "Có lỗi xảy ra khi đọc mã QR.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}

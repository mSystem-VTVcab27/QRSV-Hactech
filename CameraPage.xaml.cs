using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;
using MauiApp1tesst.Services;
using MauiApp1tesst.ViewModels;
using SkiaSharp;
using ZXing;

namespace MauiApp1tesst;

public partial class CameraPage : ContentPage
{
    private bool _isTorchOn = false;
    private bool _isProcessing = false;
    private int _zoomLevel = 1;
    private readonly StudentViewModel _viewModel;

    public CameraPage(StudentViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Request camera permission
        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.Camera>();
        }

        if (status != PermissionStatus.Granted)
        {
            await DisplayAlert("Lỗi", "Quyền truy cập camera bị từ chối", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }

        // Initialize barcode reader options & start detecting
        InitBarcodeReaderOptions();
        _isProcessing = false;
        barcodeReader.IsDetecting = true;
        Services.StatusBarHelper.SetLightStatusBarIcons();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        barcodeReader.IsDetecting = false;
    }

    private void InitBarcodeReaderOptions()
    {
        barcodeReader.Options = new BarcodeReaderOptions
        {
            Formats = ZXing.Net.Maui.BarcodeFormat.QrCode,
            AutoRotate = true,
            Multiple = false
        };
    }

    /// <summary>
    /// Handle live barcode detection from camera
    /// </summary>
    private void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        if (_isProcessing)
            return;

        try
        {
            var result = e.Results?.FirstOrDefault();
            if (result != null && !string.IsNullOrWhiteSpace(result.Value))
            {
                _isProcessing = true;

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        string qrCode = result.Value;
                        
                        // Process the QR code check-in/out in ViewModel
                        _ = _viewModel.ProcessQrCodeScanAsync(qrCode);

                        // Navigate to MainPage to show student info and status
                        await Shell.Current.GoToAsync("///MainPage");
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Lỗi", $"Lỗi khi xử lý mã QR: {ex.Message}", "OK");
                        _isProcessing = false;
                    }
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Barcode detection error: {ex.Message}");
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Lỗi", $"Lỗi: {ex.Message}", "OK");
                _isProcessing = false;
            });
        }
    }

    private async void OnPickImageClicked(object? sender, EventArgs e)
    {
        if (_isProcessing)
            return;

        try
        {
            var photo = await MediaPicker.Default.PickPhotoAsync();
            if (photo == null)
                return;

            _isProcessing = true;

            using var stream = await photo.OpenReadAsync();
            using var bitmap = SKBitmap.Decode(stream);

            if (bitmap == null)
            {
                await DisplayAlert("Lỗi", "Không thể đọc file ảnh", "OK");
                _isProcessing = false;
                return;
            }

            using var convertedBitmap = bitmap.ColorType == SKColorType.Rgba8888 
                ? bitmap.Copy() 
                : bitmap.Copy(SKColorType.Rgba8888);

            var pixels = convertedBitmap.Bytes;
            var luminanceSource = new RGBLuminanceSource(pixels, convertedBitmap.Width, convertedBitmap.Height, RGBLuminanceSource.BitmapFormat.RGBA32);
            
            var reader = new BarcodeReaderGeneric
            {
                AutoRotate = true,
                Options = new ZXing.Common.DecodingOptions
                {
                    TryHarder = true,
                    PossibleFormats = new[] { ZXing.BarcodeFormat.QR_CODE }
                }
            };

            var zxingResult = reader.Decode(luminanceSource);

            if (zxingResult != null && !string.IsNullOrWhiteSpace(zxingResult.Text))
            {
                string qrCode = zxingResult.Text;
                _ = _viewModel.ProcessQrCodeScanAsync(qrCode);
                await Shell.Current.GoToAsync("///MainPage");
            }
            else
            {
                await DisplayAlert("Thông báo", "Không tìm thấy mã QR trong hình ảnh đã chọn", "OK");
                _isProcessing = false;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", $"Lỗi khi chọn/đọc ảnh: {ex.Message}", "OK");
            _isProcessing = false;
        }
    }

    private void OnToggleTorch(object? sender, EventArgs e)
    {
        try
        {
            _isTorchOn = !_isTorchOn;
            barcodeReader.IsTorchOn = _isTorchOn;

            if (lblTorchIcon != null && btnTorch != null)
            {
                lblTorchIcon.Text = "⚡";
                btnTorch.BackgroundColor = _isTorchOn ? Color.FromArgb("#FEF08A") : Color.FromArgb("#70000000");
                lblTorchIcon.TextColor = _isTorchOn ? Colors.Black : Colors.White;
            }
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Lỗi", $"Không thể bật/tắt đèn: {ex.Message}", "OK");
            });
        }
    }

    private async void OnOpenDiplomaClicked(object? sender, EventArgs e)
    {
        try
        {
            await Navigation.PushAsync(new DiplomaPage());
        }
        catch
        {
            await Shell.Current.GoToAsync("///DiplomaPage");
        }
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        _isProcessing = false;
        await Shell.Current.GoToAsync("..");
    }
}

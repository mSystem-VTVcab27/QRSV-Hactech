using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;
using MauiApp1tesst.Services;
using MauiApp1tesst.ViewModels;

namespace MauiApp1tesst;

public partial class CameraPage : ContentPage
{
    private bool _isTorchOn = false;
    private bool _isProcessing = false;
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

        // Initialize barcode reader options
        InitBarcodeReaderOptions();
    }

    private void InitBarcodeReaderOptions()
    {
        barcodeReader.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormat.QrCode,
            AutoRotate = true,
            Multiple = false
        };
    }

    /// <summary>
    /// Handle live barcode detection from camera
    /// </summary>
    private void OnBarcodesDetected(object sender, BarcodeDetectionEventArgs e)
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

    private void OnToggleTorch(object sender, EventArgs e)
    {
        try
        {
            _isTorchOn = !_isTorchOn;
            barcodeReader.IsTorchOn = _isTorchOn;
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Lỗi", $"Không thể bật/tắt đèn: {ex.Message}", "OK");
            });
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        _isProcessing = false;
        await Shell.Current.GoToAsync("..");
    }
}

using System;
using Microsoft.Maui.Controls;

namespace MauiApp1tesst
{
    [QueryProperty(nameof(StudentCode), "msv")]
    public partial class DiplomaPage : ContentPage
    {
        private string _studentCode = string.Empty;

        public string StudentCode
        {
            get => _studentCode;
            set
            {
                _studentCode = Uri.UnescapeDataString(value ?? string.Empty);
                LoadWebViewUrl();
            }
        }

        public DiplomaPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Services.StatusBarHelper.SetDarkStatusBarIcons();
            if (diplomaWebView.Source == null)
            {
                LoadWebViewUrl();
            }
        }

        private void LoadWebViewUrl()
        {
            string url = "https://www.hactech.edu.vn/tin-tuc/tra-cuu-van-bang";
            if (!string.IsNullOrWhiteSpace(_studentCode))
            {
                url = $"https://www.hactech.edu.vn/tin-tuc/tra-cuu-van-bang?msv={Uri.EscapeDataString(_studentCode)}";
            }
            diplomaWebView.Source = url;
        }

        private void OnWebViewNavigating(object sender, WebNavigatingEventArgs e)
        {
            loadingIndicator.IsRunning = true;
            loadingIndicator.IsVisible = true;
        }

        private void OnWebViewNavigated(object sender, WebNavigatedEventArgs e)
        {
            loadingIndicator.IsRunning = false;
            loadingIndicator.IsVisible = false;
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }

        private void OnReloadClicked(object sender, EventArgs e)
        {
            diplomaWebView.Reload();
        }
    }
}

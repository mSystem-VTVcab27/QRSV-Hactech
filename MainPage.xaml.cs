using System;
using MauiApp1tesst.ViewModels;

namespace MauiApp1tesst
{
    public partial class MainPage : ContentPage
    {
        private readonly StudentViewModel _viewModel;

        public MainPage(StudentViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Services.StatusBarHelper.SetDarkStatusBarIcons();
        }
    }
}

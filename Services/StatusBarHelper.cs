namespace MauiApp1tesst.Services;

public static class StatusBarHelper
{
    public static void SetDarkStatusBarIcons()
    {
        try
        {
#if ANDROID
            var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
            if (activity?.Window != null)
            {
                if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.R)
                {
                    var controller = activity.Window.InsetsController;
                    if (controller != null)
                    {
                        controller.SetSystemBarsAppearance(
                            (int)Android.Views.WindowInsetsControllerAppearance.LightStatusBars,
                            (int)Android.Views.WindowInsetsControllerAppearance.LightStatusBars);
                    }
                }
                else
                {
#pragma warning disable CS0618
                    var decorView = activity.Window.DecorView;
                    decorView.SystemUiVisibility = (Android.Views.StatusBarVisibility)Android.Views.SystemUiFlags.LightStatusBar;
#pragma warning restore CS0618
                }
            }
#elif IOS
            if (UIKit.UIApplication.SharedApplication != null)
            {
                UIKit.UIApplication.SharedApplication.SetStatusBarStyle(UIKit.UIStatusBarStyle.DarkContent, false);
            }
#endif
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"StatusBarHelper error: {ex.Message}");
        }
    }

    public static void SetLightStatusBarIcons()
    {
        try
        {
#if ANDROID
            var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
            if (activity?.Window != null)
            {
                if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.R)
                {
                    var controller = activity.Window.InsetsController;
                    if (controller != null)
                    {
                        controller.SetSystemBarsAppearance(
                            0,
                            (int)Android.Views.WindowInsetsControllerAppearance.LightStatusBars);
                    }
                }
                else
                {
#pragma warning disable CS0618
                    var decorView = activity.Window.DecorView;
                    decorView.SystemUiVisibility = (Android.Views.StatusBarVisibility)0;
#pragma warning restore CS0618
                }
            }
#elif IOS
            if (UIKit.UIApplication.SharedApplication != null)
            {
                UIKit.UIApplication.SharedApplication.SetStatusBarStyle(UIKit.UIStatusBarStyle.LightContent, false);
            }
#endif
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"StatusBarHelper error: {ex.Message}");
        }
    }
}

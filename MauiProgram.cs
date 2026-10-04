using Microsoft.Extensions.Logging;
using MauiApp1tesst.Services;
using ZXing.Net.Maui.Controls;

namespace MauiApp1tesst
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseBarcodeReader()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("Roboto-Regular.ttf", "Roboto");
                    fonts.AddFont("Roboto-Bold.ttf", "RobotoBold");
                })
                .RegisterServices();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }

        private static MauiAppBuilder RegisterServices(this MauiAppBuilder builder)
        {
            builder.Services.AddSingleton<StudentService>();
            builder.Services.AddSingleton<MauiApp1tesst.ViewModels.StudentViewModel>();
            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddTransient<CameraPage>();
            builder.Services.AddTransient<DiplomaPage>();
            return builder;
        }
    }
}

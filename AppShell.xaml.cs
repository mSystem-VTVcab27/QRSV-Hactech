namespace MauiApp1tesst
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("camera", typeof(CameraPage));
            Routing.RegisterRoute("diploma", typeof(DiplomaPage));
        }
    }
}

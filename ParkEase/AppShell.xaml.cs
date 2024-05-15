using Microsoft.Maui.Controls;
using ParkEase.Page;

namespace ParkEase
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(Routes.SignUpPage, typeof(SignUpPage));
            Routing.RegisterRoute(Routes.LogInPage, typeof(LogInPage));
            Routing.RegisterRoute(Routes.MapPage, typeof(MapPage));
            Routing.RegisterRoute(Routes.ForgotPasswordPage, typeof(ForgotPasswordPage));
            
        }
    }
}

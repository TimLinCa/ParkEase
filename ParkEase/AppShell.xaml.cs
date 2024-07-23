using Microsoft.Maui.Controls;
using ParkEase.Page;
using ParkEase.ViewModel;

namespace ParkEase
{
    public partial class AppShell : Shell
    {
        public AppShell(AppShellViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;

            Routing.RegisterRoute(Routes.SignUpPage, typeof(SignUpPage));
            Routing.RegisterRoute(Routes.ForgotPasswordPage, typeof(ForgotPasswordPage));
            Routing.RegisterRoute(Routes.ResetPasswordPage, typeof(ResetPasswordPage));
            Routing.RegisterRoute(Routes.LogInPage, typeof(LogInPage));

        }
    }
}

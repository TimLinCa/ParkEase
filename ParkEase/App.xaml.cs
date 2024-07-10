using Microsoft.Extensions.DependencyInjection;
using ParkEase.Page;
using ParkEase.ViewModel;
using ParkEase.Core.Contracts.Services;
using ParkEase.Core.Services;


namespace ParkEase
{
    public partial class App : Application
    {
        private static IServiceProvider serviceProvider;

        public App()
        {
            InitializeComponent();

            Application.Current.UserAppTheme = AppTheme.Light;

            // Configure services
            ConfigureServices();

            // Set the main page based on platform
            SetMainPage();
        }

        private async void ConfigureServices()
        {
            var services = new ServiceCollection();

            // Add ViewModels
            services.AddTransient<AppShellMobileViewModel>();
            services.AddTransient<AppShellViewModel>();

            // Build the service provider
            serviceProvider = services.BuildServiceProvider();
		}

        private void SetMainPage()
        {
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                var appShellMobileViewModel = serviceProvider.GetRequiredService<AppShellMobileViewModel>();
                MainPage = new AppShellMobile(appShellMobileViewModel);
            }
            else if (DeviceInfo.Platform == DevicePlatform.WinUI)
            {
                var appShellViewModel = serviceProvider.GetRequiredService<AppShellViewModel>();
                MainPage = new AppShell(appShellViewModel);
            }
        }
    }
}

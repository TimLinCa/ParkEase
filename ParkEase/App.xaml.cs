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

            // Register Syncfusion license
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("YOUR_SYNCFUSION_LICENSE_KEY");

            // Configure services
            ConfigureServices();

            // Set the main page based on platform
            SetMainPage();
        }

        private void ConfigureServices()
        {
            var services = new ServiceCollection();

            // Add ViewModels
            services.AddTransient<AppShellMobileViewModel>();
            services.AddTransient<AppShellViewModel>();
            services.AddTransient<AppShellMobileViewModel>();

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

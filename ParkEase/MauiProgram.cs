using Microsoft.Extensions.Logging;
using ParkEase.Contracts.Services;
using ParkEase.Core.Contracts.Services;
using ParkEase.Core.Services;
using ParkEase.Page;
using ParkEase.Services;
using ParkEase.ViewModel;
using UraniumUI;
using epj.RouteGenerator;

namespace ParkEase
{
    [AutoRoutes("Page")]
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseUraniumUI()
                .UseUraniumUIMaterial()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });
            builder.Services.AddSingleton<MainViewModel>();
            builder.Services.AddSingleton(provider => new MainPage
            {
                BindingContext = provider.GetRequiredService<MainViewModel>()
            });

            builder.Services.AddTransient<SignUpViewModel>();
            builder.Services.AddTransient(provider => new SignUpPage
            {
                BindingContext = provider.GetRequiredService<SignUpViewModel>()
            });

            builder.Services.AddTransient<ForgotPasswordViewModel>();
            builder.Services.AddTransient(provider => new ForgotPasswordPage
            {
                BindingContext = provider.GetRequiredService<ForgotPasswordViewModel>()
            });

            builder.Services.AddSingleton<IMongoDBService, MongoDBService>();

            builder.Services.AddSingleton<LogInViewModel>();
            builder.Services.AddSingleton(provider => new LogInPage
            {
                BindingContext = provider.GetRequiredService<LogInViewModel>()
            });

            builder.Services.AddSingleton<IDialogService, DialogService>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}

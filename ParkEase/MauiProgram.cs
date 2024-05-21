using Microsoft.Extensions.Logging;
using ParkEase.Contracts.Services;
using ParkEase.Core.Contracts.Services;
using ParkEase.Core.Services;
using ParkEase.Page;
using ParkEase.Services;
using ParkEase.ViewModel;
using UraniumUI;
using epj.RouteGenerator;
using ParkEase.Core.Model;
using CommunityToolkit.Maui;

namespace ParkEase
{
    [AutoRoutes("Page")]
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            bool developerMode = true;

            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseUraniumUI()
                .UseUraniumUIMaterial()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            #region page

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

            builder.Services.AddSingleton<LogInViewModel>();
            builder.Services.AddSingleton(provider => new LogInPage
            {
                BindingContext = provider.GetRequiredService<LogInViewModel>()
            });

        
            builder.Services.AddSingleton<MapViewModel>();
            builder.Services.AddSingleton(provider => new MapPage
            {
                BindingContext = provider.GetRequiredService<MapViewModel>()
            });

            builder.Services.AddSingleton<CreateMapViewModel>();
            builder.Services.AddSingleton(provider => new CreateMapPage
            {
                BindingContext = provider.GetRequiredService<CreateMapViewModel>()
            });
            #endregion

            builder.Services.AddSingleton(provider => new ParkEaseModel(developerMode));

            #region service
            builder.Services.AddSingleton<IDialogService, DialogService>();
            builder.Services.AddSingleton<IMongoDBService, MongoDBService>();
            #endregion

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}

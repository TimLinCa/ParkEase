using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.Controls.PlatformConfiguration;
using ParkEase.Contracts.Services;
using ParkEase.Core.Contracts.Services;
using ParkEase.Core.Data;
using ParkEase.Core.Model;
using ParkEase.Core.Services;
using ParkEase.Messages;
using ParkEase.Page;
using ParkEase.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using User = ParkEase.Core.Data.User;

namespace ParkEase.ViewModel
{
    public partial class LogInViewModel : ObservableObject
    {
        //if this varable need to bind to the UI, it should be an ObservableProperty
        [ObservableProperty]
        private string email;

        [ObservableProperty]
        private string password;

        [ObservableProperty]
        private bool rememberMe;

        private IMongoDBService mongoDBService;

        private IDialogService dialogService;

        private ParkEaseModel parkEaseModel;


        public LogInViewModel(IMongoDBService mongoDBService, IDialogService dialogService, ParkEaseModel model)
        {
            this.dialogService = dialogService;
            this.mongoDBService = mongoDBService;
            this.parkEaseModel = model;
            Email = "";
            Password = "";
            //ForgotPasswordCommand = new RelayCommand(async () => await ExecuteForgotPasswordCommand());
        }

        public async Task<bool> AccountExists(string email, string password)
        {
            List<User> users = await mongoDBService.GetData<User>(CollectionName.Users);
            User user = users.FirstOrDefault(u => u.Email == email);
 
            if (user != null && user.Email == email && PasswordHasher.VerifyPassword(password, user.Password))
            {
                parkEaseModel.User = user;
                return true;
            } 

            return false;
        }

        public ICommand InitCommand => new RelayCommand(async () =>
        {
            if (parkEaseModel.developerMode)
            {
                User user = new User();
                user.Email = "Test123@gmail.com";
                user.Role = Roles.Engineer;
                parkEaseModel.User = user;
                await DirectToMainPage();
            }
        });

        /// <summary>
        /// Example of a command that can be binded to a button in the UI
        /// </summary>
        public ICommand LogInCommand => new RelayCommand(async () =>
        {
            //try catch block is nessary to catch any exception that might occur to prevent the app from crashing
            try
            {
                bool accountExists = await AccountExists(Email, Password);

                if (accountExists)
                {
                    await DirectToMainPage();
                }
                else
                {
                    await dialogService.ShowAlertAsync("Error", $"Check your email or password!", "OK");
                }

            }
            catch (Exception ex)
            {
                await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }

        });

        public ICommand SignUpCommand => new RelayCommand(async () =>
        {
            //Navigate to SignUpPage
            await Shell.Current.GoToAsync(nameof(SignUpPage));
        });

        public ICommand ForgotPasswordCommand => new RelayCommand(async () =>
        {
            // Implement the logic to navigate to the Forgot Password Page
            await Shell.Current.GoToAsync(nameof(ForgotPasswordPage));
        });

        private async Task SaveLoginPreferenceAsync()
        {
            await SecureStorage.SetAsync("RememberMe", RememberMe.ToString());
            if (RememberMe)
            {
                // Optionally store username or other non-sensitive data
                await SecureStorage.SetAsync("Username", Email);
            }
            else
            {
                // Clear stored data if Remember Me is not checked
                SecureStorage.Remove("Username");
            }
        }

        public async Task LoadLoginPreferenceAsync()
        {
            var rememberMeStored = await SecureStorage.GetAsync("RememberMe");
            if (rememberMeStored != null && bool.TryParse(rememberMeStored, out var remembered))
            {
                RememberMe = remembered;
                if (RememberMe)
                {
                    string? emailInStoreage = await SecureStorage.GetAsync("Username");
                    if (emailInStoreage != string.Empty)
                    {
                        Email = emailInStoreage;
                    }
                }
            }
        }

        private async Task DirectToMainPage()
        {
            WeakReferenceMessenger.Default.Send<UserChangedMessage>(new UserChangedMessage(parkEaseModel.User));

            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                await Shell.Current.GoToAsync($"//{nameof(UserMapPage)}");
            }
            else if (DeviceInfo.Platform == DevicePlatform.WinUI)
            {
                if (parkEaseModel.User.Role == Roles.User)
                {
                    await dialogService.ShowAlertAsync("Error", "Sorry you don not have permission to use this application");
                    return;
                }

                if(parkEaseModel.User.Role == Roles.Developer)
                {
                    await Shell.Current.GoToAsync($"//{nameof(MapPage)}");
                    return;
                }

                if(parkEaseModel.User.Role == Roles.Engineer)
                {
                    await Shell.Current.GoToAsync($"//{nameof(MapPage)}");
                    return;
                }
                
                if(parkEaseModel.User.Role == Roles.Administrator)
                {
                    await Shell.Current.GoToAsync($"//{nameof(CreateMapPage)}");
                    return;
                }

            }
        }

    }
}

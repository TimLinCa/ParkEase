using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ParkEase.Contracts.Services;
using ParkEase.Core.Contracts.Services;
using ParkEase.Core.Data;
using ParkEase.Core.Services;
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


        public LogInViewModel(IMongoDBService mongoDBService, IDialogService dialogService)
        {
            this.dialogService = dialogService;
            this.mongoDBService = mongoDBService;
            Email = "";
            Password = "";
            //ForgotPasswordCommand = new RelayCommand(async () => await ExecuteForgotPasswordCommand());
        }

        /// <summary>
        /// Example of a command that can be binded to a button in the UI
        /// </summary>
        public ICommand TestCommand => new RelayCommand(async () =>
        {
            //try catch block is nessary to catch any exception that might occur to prevent the app from crashing
            try
            {
                //Hashing a password
                string hashedPassword = PasswordHasher.HashPassword(Password);
                //Inseting a user to the database
                await mongoDBService.InsertData(CollectionName.Users, new User { Email = "Tim@gmail.com", Password = hashedPassword });
                //Getting all users from the database
                List<User> users = await mongoDBService.GetData<User>(CollectionName.Users);
                //Verifying a password
                bool test = PasswordHasher.VerifyPassword(Password, "test");
            }
            catch (Exception ex)
            {
                await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }

        });

        public ICommand LogInCommand => new RelayCommand(async () =>
        {
            //Navigate to MainPage
            await Shell.Current.GoToAsync(nameof(MainPage),
                new Dictionary<string, object>
                {
                    {"Email",Email }
                });
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
                    Email = await SecureStorage.GetAsync("Username");
                }
            }
        }

    }
}

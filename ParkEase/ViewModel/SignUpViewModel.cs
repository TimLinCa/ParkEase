using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ParkEase.Contracts.Services;
using ParkEase.Core.Contracts.Services;
using ParkEase.Core.Data;
using ParkEase.Core.Services;
using ParkEase.Page;
using ParkEase.Services;
using ParkEase.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ParkEase.ViewModel
{
    public partial class SignUpViewModel : ObservableObject
    {
        [ObservableProperty]
        private string fullName;

        [ObservableProperty]
        private string email;

        [ObservableProperty]
        private string password;

        [ObservableProperty]
        private string repeatPassword;

        [ObservableProperty]
        private string emailExistsMessage;

        [ObservableProperty]
        private string unMatchingPasswordMessage;

        [ObservableProperty]
        private bool isTermsAndConditionsAccepted;


        private readonly IMongoDBService mongoDBService;

        private readonly IDialogService dialogService;

        public SignUpViewModel(IMongoDBService mongoDBService, IDialogService dialogService)
        {
            this.mongoDBService = mongoDBService;
            this.dialogService = dialogService;
            FullName = "";
            Email = "";
            Password = "";
            RepeatPassword = "";
            UnMatchingPasswordMessage = "";
            EmailExistsMessage = "";
            IsTermsAndConditionsAccepted = false;
        }

        // Check if the email user register exists or not
        public async Task<bool> EmailExists(string email)
        {
            List<User> users = await mongoDBService.GetData<User>(CollectionName.Users);
            foreach (User user in users)
            {
                if (user.Email == email)
                {
                    return true;
                }
            }

            return false;
        }

        public ICommand SignUpCommand => new RelayCommand(async () =>
        {
            try
            {
                if (!string.IsNullOrEmpty(FullName) && !string.IsNullOrEmpty(Email) && !string.IsNullOrEmpty(Password) && !IsTermsAndConditionsAccepted)
                {
                    bool emailExists = await EmailExists(Email);
                    if (emailExists)
                    {
                        EmailExistsMessage = "This email address already exists";
                        await dialogService.ShowAlertAsync("", "This email address already exists", "OK");
                    }
                    else
                    {
                        string hashedPassword = PasswordHasher.HashPassword(Password);

                        await mongoDBService.InsertData<User>(CollectionName.Users, new User { FullName = FullName, Email = Email, Password = hashedPassword }); ;

                        await dialogService.ShowAlertAsync("", "Your account is created. Please sign in.", "OK");
                        await Shell.Current.GoToAsync($"///{nameof(LogInPage)}");
                    }
                }
            }
            catch (Exception ex)
            {
                await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }
        });

        public ICommand ConfirmPasswordCommand => new RelayCommand(() =>
        {
            if (!string.IsNullOrEmpty(Password) && !string.IsNullOrEmpty(RepeatPassword))
            {
                if (Password != RepeatPassword)
                {
                    UnMatchingPasswordMessage = "Password does not match!";

                }
                else
                {
                    UnMatchingPasswordMessage = "";
                }
            }
        });
    }
}
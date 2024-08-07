﻿
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

        partial void OnEmailChanged(string? value)
        {
            _ = EmailExists();
        }

        private async Task EmailExists()
        {
            try
            {
                List<User> users = await mongoDBService.GetData<User>(CollectionName.Users);
                if (!string.IsNullOrEmpty(Email))
                {
                    if(users.Any(u => u.Email == Email))
                    {
                        EmailExistsMessage = "This email address already exists";
                    }
                    else
                    {
                        EmailExistsMessage = string.Empty;
                    }
                }
                else
                {
                    EmailExistsMessage = string.Empty;
                }
            }
            catch (Exception ex)
            {
                await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }
        }


        public ICommand SignUpCommand => new RelayCommand(async () =>
        {
            try
            {
                if (!string.IsNullOrEmpty(FullName) && !string.IsNullOrEmpty(Email) && !string.IsNullOrEmpty(Password) && IsTermsAndConditionsAccepted)
                {
                    string hashedPassword = PasswordHasher.HashPassword(Password);

                    await mongoDBService.InsertData<User>(CollectionName.Users, new User { FullName = FullName, Email = Email, Password = hashedPassword }); ;

                    await dialogService.ShowAlertAsync("", "Your account is created. Please sign in.", "OK");
                    await Shell.Current.GoToAsync(nameof(LogInPage));
                }
                else
                {
                    await dialogService.ShowAlertAsync("", "Please check your information again.", "OK");
                }
            }
            catch (Exception ex)
            {
                await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }
        });

        partial void OnRepeatPasswordChanged(string? oldValue, string? value)
        {
            ConfirmPasswordCommand();
        }

        private async Task ConfirmPasswordCommand()
        {
            try
            {
                if (!string.IsNullOrEmpty(Password) && !string.IsNullOrEmpty(RepeatPassword))
                {
                    if (Password != RepeatPassword)
                    {
                        UnMatchingPasswordMessage = "Password does not match!";

                    }
                    else
                    {
                        UnMatchingPasswordMessage = string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }
        }

        public ICommand BackToLogInPage => new RelayCommand(async () =>
        {
            try
            {
                await Shell.Current.GoToAsync(nameof(LogInPage));
            }
            catch (Exception ex)
            {
                await dialogService.ShowAlertAsync(ex.Message, "OK");
            }
        });

       

    }
}
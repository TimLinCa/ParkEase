using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MailKit.Search;
using Microsoft.Maui.Controls.PlatformConfiguration;
using MongoDB.Bson;
using MongoDB.Driver;
using ParkEase.Contracts.Services;
using ParkEase.Core.Contracts.Services;
using ParkEase.Core.Data;
using ParkEase.Core.Model;
using ParkEase.Core.Services;
using ParkEase.Messages;
using ParkEase.Page;
using ParkEase.Utilities;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ParkEase.ViewModel
{
    public partial class ResetPasswordViewModel : ObservableObject
    {

        private IMongoDBService mongoDBService;

        private IDialogService dialogService;

        private ParkEaseModel parkEaseModel;

        private IAWSService awsService;

        [ObservableProperty]
        private bool errorMessageVisable;

        [ObservableProperty]
        private bool resetPasswordEnable;

        [ObservableProperty]
        private string errorMessage;

        [ObservableProperty] 
        private string errorMessageColor;

        [ObservableProperty]
        private string userCode;

        private string actualCode;

        [ObservableProperty]
        private string password;

        [ObservableProperty]
        private string repeatPassword;

        [ObservableProperty]
        private string unMatchingPasswordMessage;

        private string email;

        public ResetPasswordViewModel(IMongoDBService mongoDBService, IDialogService dialogService)
        {
            this.mongoDBService = mongoDBService;
            this.dialogService = dialogService;
            this.errorMessageVisable = false;
            this.errorMessage = string.Empty;
            this.resetPasswordEnable = false;
            actualCode = DataService.GetCode();
            email = DataService.GetEmail();


        }

        partial void OnUserCodeChanged(string? value)
        {
            CheckCodeValidation();
        }


        private async Task CheckCodeValidation()
        {
            try
            {
                if (!string.IsNullOrEmpty(UserCode))
                {
                    if(UserCode == actualCode)
                    {
                        ErrorMessageVisable = true;
                        ErrorMessageColor = "green";
                        ErrorMessage = "Valid code";
                        ResetPasswordEnable = true;
                    }
                    else
                    {
                        ErrorMessageVisable = true;
                        ErrorMessageColor = "red";
                        ErrorMessage = "Invalid code";
                        ResetPasswordEnable = false;

                    }
                }
                else
                {
                    ErrorMessageVisable = false;
                    ResetPasswordEnable = false;

                }

            }
            catch (Exception ex)
            {
                
            }
        }


        public ICommand ChangePassword => new RelayCommand(async () =>
        {
            try
            {
                string hashedPassword = PasswordHasher.HashPassword(Password);
                var builder = Builders<User>.Filter;
                var filter = builder.Eq(p => p.Email, email);
                var update = Builders<User>.Update.Set(p => p.Password, hashedPassword);

                await mongoDBService.UpdateData(CollectionName.Users, filter, update);
                await dialogService.ShowAlertAsync("Success", "Your password is successfully updated!" ,"OK");
                await Shell.Current.GoToAsync(nameof(LogInPage));

            }
            catch (Exception ex) 
            {
                await dialogService.ShowAlertAsync(ex.Message, "OK");
            }

        });

        public ICommand GoToLoginCommand => new RelayCommand(async () =>
        {
            try
            {
                // Navigate to the Login Page using the route defined in AppShell.xaml
                await Shell.Current.GoToAsync(nameof(LogInPage));
            }
            catch (Exception ex)
            {
                await dialogService.ShowAlertAsync(ex.Message, "OK");
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
    }
}

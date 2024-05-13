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
    public partial class ForgotPasswordViewModel : ObservableObject
    {
        [ObservableProperty]
        private string email;

        

        private readonly IMongoDBService mongoDBService;
        private readonly IDialogService dialogService;

        public ForgotPasswordViewModel(IMongoDBService mongoDBService, IDialogService dialogService)
        {
            this.dialogService = dialogService;
            this.mongoDBService = mongoDBService;
            Email = "";
            
        }

        public ICommand GoToLoginCommand => new RelayCommand(async () =>
        {
            // Navigate to the Login Page using the route defined in AppShell.xaml
            await Shell.Current.GoToAsync(nameof(LogInPage));
        });



        public ICommand ResetCommand => new AsyncRelayCommand(async () =>
        {
            if (string.IsNullOrEmpty(Email) || !IsValidEmail(Email))
            {
                // Prompt user with error message
                await dialogService.ShowAlertAsync("Invalid Email", "Please enter a valid email address.", "OK");
                return;
            }

            try
            {
                // Simulate password reset operation
                await Task.Delay(1000);  // Simulate network delay

                // Inform user about the password reset email
                await dialogService.ShowAlertAsync("Password Reset", "If the email you entered is associated with an account, we've sent a password reset link to it.", "OK");
            }
            catch (System.Exception ex)
            {
                await dialogService.ShowAlertAsync("Error", $"An error occurred: {ex.Message}", "OK");
            }
        });

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return false;

            return System.Text.RegularExpressions.Regex.IsMatch(email,
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
    }
}
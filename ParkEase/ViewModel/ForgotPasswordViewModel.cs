using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ParkEase.Page;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ParkEase.ViewModel
{
    public partial class ForgotPasswordViewModel : ObservableObject
    {
        [ObservableProperty]
        private string email;

        public ICommand ResetCommand { get; }

        public ForgotPasswordViewModel()
        {
            ResetCommand = new AsyncRelayCommand(ResetPasswordAsync);
        }

        public ICommand GoToLoginCommand => new RelayCommand(async () =>
        {
            // Navigate to the Login Page using the route defined in AppShell.xaml
            await Shell.Current.GoToAsync(nameof(LogInPage));
        });



        private async Task ResetPasswordAsync()
        {
            if (string.IsNullOrEmpty(Email) || !IsValidEmail(Email))
            {
                // Prompt user with error message
                await App.Current.MainPage.DisplayAlert("Invalid Email", "Please enter a valid email address.", "OK");
                return;
            }

            try
            {
                // Simulate password reset operation
                await Task.Delay(1000);  // Simulate network delay

                // Inform user about the password reset email
                await App.Current.MainPage.DisplayAlert("Password Reset", "If the email you entered is associated with an account, we've sent a password reset link to it.", "OK");
            }
            catch (System.Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return false;

            return System.Text.RegularExpressions.Regex.IsMatch(email,
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
    }
}

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
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Security.Cryptography;
//using System.Net.Mail;
using Microsoft.Maui.ApplicationModel.Communication;
using MongoDB.Driver;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using ParkEase.Messages;



namespace ParkEase.ViewModel
{
    public partial class ForgotPasswordViewModel : ObservableObject
    {
        [ObservableProperty]
        private string email;

        private string googleKey;

        private readonly IConfiguration configuration;

        private readonly IMongoDBService mongoDBService;
        private readonly IDialogService dialogService;


        public ForgotPasswordViewModel(IMongoDBService mongoDBService, IDialogService dialogService, IConfiguration configuration)
        {
            this.dialogService = dialogService;
            this.mongoDBService = mongoDBService;
            configuration = configuration;
            googleKey = configuration["GoogleAppPassword"];


        }

        /*        public ICommand SentEmail => new RelayCommand(async () =>
                {
                    // Implement the logic to navigate to the Forgot Password Page
                    await Shell.Current.GoToAsync(nameof(ResetPasswordPage));
                });*/


        // Using MailKit Library
        public ICommand SentEmail => new RelayCommand(async () =>
        {
            try
            {
                if (!string.IsNullOrEmpty(Email) && IsValidEmail(Email))
                {
                    var userList = await mongoDBService.GetData<User>(CollectionName.Users);
                    var user = userList.Where(data => data.Email == Email).FirstOrDefault();
                    if (user != null)
                    {
                        string code = GenerateRandomPassword(8);
                        string hashedPassword = PasswordHasher.HashPassword(code);
                        var message = new MimeMessage();
                        message.From.Add(new MailboxAddress("ParkEase Team", "tkdgus2233440@gmail.com"));
                        message.To.Add(new MailboxAddress("", Email));
                        message.Subject = "ParkEase Password Reset Code";

                        message.Body = new TextPart("html")
                        {
                            Text = $"Dear our user,<br><br>Your password reset code is <strong>{code}</strong><br><br>Thank you.<br><br>ParkEase Team",
                        };

                        using (var client = new SmtpClient())
                        {
                            try
                            {
                                await client.ConnectAsync("smtp.gmail.com", 465, SecureSocketOptions.SslOnConnect);
                                await client.AuthenticateAsync("tkdgus2233440@gmail.com", googleKey);
                                await client.SendAsync(message);
                                await client.DisconnectAsync(true);
                                DataService.SetCode(code);
                                DataService.SetEmail(Email);

                                Debug.WriteLine("Email sent successfully.");
/*                                var builder = Builders<User>.Filter;
                                var filter = builder.Eq(p => p.Email, Email);
                                var update = Builders<User>.Update.Set(p => p.Password, hashedPassword);

                                await mongoDBService.UpdateData(CollectionName.Users, filter, update);*/
                                await dialogService.ShowAlertAsync("Email Sent", "We sent a code to your email address.", "OK");
                                await Shell.Current.GoToAsync(nameof(ResetPasswordPage));
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error sending email: {ex.Message}");
                                Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                            }
                        }
                    }
                    else
                    {
                        await dialogService.ShowAlertAsync("Error", "We couldn't find your email address\nPlease check your email again.", "OK");
                    }
                }
                else
                {
                    await dialogService.ShowAlertAsync("Error", "Invalid email address type\nPlease check your email again.", "OK");
                }

            }
            catch (Exception ex)
            {
                await dialogService.ShowAlertAsync($"Error: {ex.Message}", "OK");
            }
        });


        // Using SMTP
        /*public ICommand SentEmail => new RelayCommand(async () =>
        {
            try
            {
                if (!string.IsNullOrEmpty(Email) && IsValidEmail(Email))
                {
                    var userList = await mongoDBService.GetData<User>(CollectionName.Users);
                    var user = userList.Where(data => data.Email == Email).FirstOrDefault();
                    if (user != null)
                    {
                        string tempPassword = GenerateRandomPassword(8);
                        string hashedPassword = PasswordHasher.HashPassword(tempPassword);

                        var msg = new MailMessage
                        {
                            From = new MailAddress("tkdgus2233440@gmail.com", "ParkEase Team"),
                            Subject = "ParkEase Temporary Password",
                            Body = $"Dear our user,<br><br>Your temporary password is <strong>{tempPassword}</strong><br><br>Thank you.<br><br>ParkEase Team",
                            Priority = MailPriority.High,
                            IsBodyHtml = true
                        };

                        msg.To.Add(Email);

                        var client = new SmtpClient
                        {
                            Host = "smtp.gmail.com",
                            Port = 587,
                            EnableSsl = true,
                            UseDefaultCredentials = false,
                            Credentials = new NetworkCredential("tkdgus2233440@gmail.com", googleKey) // app password
                        };

                        try
                        {
                            Debug.WriteLine("Attempting to send email...");
                            await client.SendMailAsync(msg);
                            Debug.WriteLine("Email sent successfully.");
                        }
                        catch (SmtpException smtpEx)
                        {
                            Debug.WriteLine($"SMTP Exception: {smtpEx.Message}");
                            await dialogService.ShowAlertAsync($"SMTP Error: {smtpEx.Message}", "OK");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"General Exception: {ex.Message}");
                            await dialogService.ShowAlertAsync($"Error: {ex.Message}", "OK");
                        }

                        var builder = Builders<User>.Filter;
                        var filter = builder.Eq(p => p.Email, Email);
                        var update = Builders<User>.Update.Set(p => p.Password, hashedPassword);

                        await mongoDBService.UpdateData(CollectionName.Users, filter, update);
                        await dialogService.ShowAlertAsync("Email Sent", "We sent a temporary password to your email address.", "OK");
                    }
                    else
                    {
                        await dialogService.ShowAlertAsync("Error", "We couldn't find your email address\nPlease check your email again.", "OK");
                    }
                }
                else
                {
                    await dialogService.ShowAlertAsync("Error", "Invalid email address type\nPlease check your email again.", "OK");
                }
            }
            catch (Exception ex)
            {
                await dialogService.ShowAlertAsync($"Error: {ex.Message}", "OK");
            }
        });*/


        public ICommand GoToLoginCommand => new RelayCommand(async () =>
        {
            try
            {
                // Navigate to the Login Page using the route defined in AppShell.xaml
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await dialogService.ShowAlertAsync(ex.Message, "OK");
            }
            
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

        public string GenerateRandomPassword(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            using (var rng = new RNGCryptoServiceProvider())
            {
                var data = new byte[length];
                rng.GetBytes(data);
                return new string(data.Select(b => chars[b % chars.Length]).ToArray());
            }
        }

    }


}
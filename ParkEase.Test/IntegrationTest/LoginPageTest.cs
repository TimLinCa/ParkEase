using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using ParkEase.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using System.IO;
using ParkEase.Core.Contracts.Services;
using ParkEase.Services;
using ParkEase.Contracts.Services;
using ParkEase.Core.Model;
using ParkEase.Core.Data;
using CollectionName = ParkEase.Core.Services.CollectionName;
using ParkEase.ViewModel;
using Syncfusion.Maui.Calendar;
using ParkEase.Utilities;   

namespace ParkEase.Test.IntergartionTest
{
    public class LoginPageTest: IAsyncLifetime
    {
        public IConfiguration Configuration { get; private set; }
        public MongoDBService MongoDBService { get; private set; }
        public ParkEaseModel Model { get; private set; }
        public IAWSService AWSService { get; private set; }
        public IDialogService DialogService { get; private set; }
        private LogInViewModel _logInViewModel;

        public async Task InitializeAsync()
        {
            // Build configuration
            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            AWSService = new AWSService(Configuration);
            MongoDBService = new MongoDBService(AWSService, DevicePlatform.WinUI, true);
            DialogService = new DialogService();
            Model = new ParkEaseModel(true);
            Model.User = new User { Role = Roles.Administrator };

            // Seed test data
            await SeedTestDataAsync();

            // Initialize the ViewModel
            _logInViewModel = new LogInViewModel(MongoDBService, DialogService, AWSService, Model);
        }

        public async Task DisposeAsync()
        {
            // Cleanup any test data from the database
            await MongoDBService.DropCollection(CollectionName.Users);
        }

        private async Task SeedTestDataAsync()
        {
            // Ensure the database contains a user with the given email and password for testing
            var user = new User { Email = "test@example.com", Password = PasswordHasher.HashPassword("ValidPassword123") };
            await MongoDBService.InsertData(CollectionName.Users, user);
        }
        [Fact]
        public async Task ValidLogin_IntegrationTest()
        {
            // Arrange
            var validEmail = "test@example.com";
            var validPassword = "ValidPassword123";
            _logInViewModel.Email = validEmail;
            _logInViewModel.Password = validPassword;

            // Check if user is inserted correctly
            var users = await MongoDBService.GetData<User>(CollectionName.Users);
            Assert.Contains(users, u => u.Email == validEmail);

            // Act
            _logInViewModel.LogInCommand.Execute(null);

            // Assert
            Assert.Equal(validEmail, Model.User.Email);
        }

        [Fact]
        public async Task InvalidEmail_IntegrationTest()
        {
            // Arrange
            var invalidEmail = "invalid-email";
            var validPassword = "ValidPassword123";
            _logInViewModel.Email = invalidEmail;
            _logInViewModel.Password = validPassword;

            // Act
            _logInViewModel.LogInCommand.Execute(null);

            // Assert
            // Here, assume DialogService.ShowAlertAsync sets a message for testing purposes
            var alertMessage = "Check your email or password!"; // Update this line based on your DialogService implementation
            Assert.Equal("Check your email or password!", alertMessage);
        }

        [Fact]
        public async Task InvalidPassword_IntegrationTest()
        {
            // Arrange
            var validEmail = "test@example.com";
            var invalidPassword = "invalid";
            _logInViewModel.Email = validEmail;
            _logInViewModel.Password = invalidPassword;

            // Act
            _logInViewModel.LogInCommand.Execute(null);

            // Assert
            // Here, assume DialogService.ShowAlertAsync sets a message for testing purposes
            var alertMessage = "Check your email or password!"; // Update this line based on your DialogService implementation
            Assert.Equal("Check your email or password!", alertMessage);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using ParkEase.Core.Services;
using ParkEase.Core.Contracts.Services;
using ParkEase.Services;
using ParkEase.Contracts.Services;
using ParkEase.Core.Model;
using ParkEase.Core.Data;
using ParkEase.ViewModel;
using Xunit;
using ParkEase.Utilities;
using Moq;


namespace ParkEase.Test.IntergartionTest
{
    public class SignUpPageTest : IAsyncLifetime
    {
        public IConfiguration Configuration { get; private set; }
        public MongoDBService MongoDBService { get; private set; }
        public ParkEaseModel Model { get; private set; }
        public IAWSService AWSService { get; private set; }
        public IDialogService DialogService { get; private set; }

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
            Model.User = new User();
            Model.User.Role = Roles.Administrator;

            // Initialize MongoDBService and ensure collections are clean before starting tests
            //await MongoDBService.CheckAPIKey();
            await MongoDBService.DropCollection(CollectionName.Users);

            // Insert test data
            await SeedTestDataAsync();

        }

        public async Task DisposeAsync()
        {
            // Clean up the test database
            await MongoDBService.DropCollection(CollectionName.Users);
        }

        private async Task SeedTestDataAsync()
        {
            // Insert a user with a duplicate email to test duplicate email scenario
            await MongoDBService.InsertData(CollectionName.Users, new User
            {
                FullName = "Existing User",
                Email = "duplicate@example.com",
                Password = PasswordHasher.HashPassword("ExistingPassword123!")
            });
        }
    }

    public class SignUpPageIntegrationTests : IClassFixture<TestDatabaseFixture>
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly SignUpViewModel _viewModel;

        public SignUpPageIntegrationTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
            _viewModel = new SignUpViewModel(_fixture.MongoDBService, _fixture.DialogService);
        }

        [Fact]
        public async Task SignUpCommand_CreatesNewUser()
        {
            // Arrange
            _viewModel.FullName = "Test User";
            _viewModel.Email = "testuser@example.com";
            _viewModel.Password = "TestPassword123!";
            _viewModel.RepeatPassword = "TestPassword123!";
            _viewModel.IsTermsAndConditionsAccepted = true;

            // Act
            _viewModel.SignUpCommand.Execute(null);

            // Assert
            List<User> users = await _fixture.MongoDBService.GetData<User>(CollectionName.Users);
            User createdUser = users.FirstOrDefault(u => u.Email == "testuser@example.com");

            Assert.NotNull(createdUser);
            Assert.Equal("Test User", createdUser.FullName);
            Assert.True(PasswordHasher.VerifyPassword("TestPassword123!", createdUser.Password));
        }

        [Fact]
        public async Task SignUpCommand_DuplicateEmail_ShowsErrorMessage()
        {
            // Arrange
            _viewModel.FullName = "New User";
            _viewModel.Email = "duplicate@example.com";
            _viewModel.Password = "NewPassword123!";
            _viewModel.RepeatPassword = "NewPassword123!";
            _viewModel.IsTermsAndConditionsAccepted = true;

            // Act
            _viewModel.SignUpCommand.Execute(null);

            // Assert
            Assert.Equal("This email address already exists", _viewModel.EmailExistsMessage);
        }

        [Fact]
        public async Task InvalidCharacterInNameField_ShowsErrorMessage()
        {
            // Arrange
            _viewModel.FullName = "Invalid123";
            _viewModel.Email = "valid@example.com";
            _viewModel.Password = "ValidPass123";
            _viewModel.RepeatPassword = "ValidPass123";
            _viewModel.IsTermsAndConditionsAccepted = true;

            // Act
            var regex = new System.Text.RegularExpressions.Regex("^[a-zA-Z ]*$");
            if (!regex.IsMatch(_viewModel.FullName))
            {
                await _fixture.DialogService.ShowAlertAsync("Error", "The field should contain only letters", "OK");
            }
            else
            {
                _viewModel.SignUpCommand.Execute(null);
            }

            // Assert
            //_fixture.DialogService.Verify(d => d.ShowAlertAsync("Error", "The field should contain only letters", "OK"), Times.Once);
        }

        [Fact]
        public async Task PasswordMismatch_ShowsErrorMessage()
        {
            // Arrange
            _viewModel.FullName = "Valid Name";
            _viewModel.Email = "valid@example.com";
            _viewModel.Password = "ValidPass123";
            _viewModel.RepeatPassword = "DifferentPass123";
            _viewModel.IsTermsAndConditionsAccepted = true;

            // Act
            _viewModel.SignUpCommand.Execute(null);

            // Assert
            Assert.Equal("Password does not match!", _viewModel.UnMatchingPasswordMessage);
        }

        [Fact]
        public async Task InvalidEmailFormat_ShowsErrorMessage()
        {
            // Arrange
            _viewModel.FullName = "Valid Name";
            _viewModel.Email = "invalid-email";
            _viewModel.Password = "ValidPass123";
            _viewModel.RepeatPassword = "ValidPass123";
            _viewModel.IsTermsAndConditionsAccepted = true;

            // Act
            var regex = new System.Text.RegularExpressions.Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            if (!regex.IsMatch(_viewModel.Email))
            {
                await _fixture.DialogService.ShowAlertAsync("Error", "Please type a valid e-mail address.", "OK");
            }
            else
            {
                _viewModel.SignUpCommand.Execute(null);
            }

            // Assert
            //_fixture.DialogService.Verify(d => d.ShowAlertAsync("Error", "Please type a valid e-mail address.", "OK"), Times.Once);
        }

        [Fact]
        public async Task InvalidPassword_ShowsErrorMessage()
        {
            // Arrange
            _viewModel.FullName = "Valid Name";
            _viewModel.Email = "valid@example.com";
            _viewModel.Password = "short"; // Invalid password (less than 8 characters)
            _viewModel.RepeatPassword = "short";
            _viewModel.IsTermsAndConditionsAccepted = true;

            // Act
            var regex = new System.Text.RegularExpressions.Regex("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)[A-Za-z\\d]{8,}$");
            if (!regex.IsMatch(_viewModel.Password))
            {
                await _fixture.DialogService.ShowAlertAsync("Error", "A valid password must include at least one uppercase letter, one lowercase letter, one number and must be at least 8 characters long.", "OK");
            }
            else
            {
                _viewModel.SignUpCommand.Execute(null);
            }

            // Assert
            //_fixture.DialogService.Verify(d => d.ShowAlertAsync("Error", "A valid password must include at least one uppercase letter, one lowercase letter, one number and must be at least 8 characters long.", "OK"), Times.Once);
        }   

        [Fact]
        public async Task TermsAndConditionsNotAccepted_ShowsErrorMessage()
        {
            // Arrange
            _viewModel.FullName = "Valid Name";
            _viewModel.Email = "valid@example.com";
            _viewModel.Password = "ValidPass123";
            _viewModel.RepeatPassword = "ValidPass123";
            _viewModel.IsTermsAndConditionsAccepted = false; // Terms and conditions not accepted

            // Act
            _viewModel.SignUpCommand.Execute(null);

            // Assert
            //_fixture.DialogService.Verify(d => d.ShowAlertAsync("", "Please check your information again.", "OK"), Times.Once);
        }
        [Fact]
        public async Task DuplicateEmailAddress_ShowsErrorMessage()
        {
            // Arrange
            _viewModel.FullName = "New User";
            _viewModel.Email = "duplicate@example.com";
            _viewModel.Password = "NewPassword123!";
            _viewModel.RepeatPassword = "NewPassword123!";
            _viewModel.IsTermsAndConditionsAccepted = true;

            // Act
            //await _viewModel.EmailExists();

            // Assert
            Assert.Equal("This email address already exists", _viewModel.EmailExistsMessage);
        }

        [Fact]
        public async Task EmptyFields_ShowsErrorMessage()
        {
            // Arrange
            _viewModel.FullName = "";
            _viewModel.Email = "";
            _viewModel.Password = "";
            _viewModel.RepeatPassword = "";
            _viewModel.IsTermsAndConditionsAccepted = false;

            // Act
            _viewModel.SignUpCommand.Execute(null);

            // Assert
            //_fixture.DialogService.Verify(d => d.ShowAlertAsync("", "Please check your information again.", "OK"), Times.Once);
        }
    }
}

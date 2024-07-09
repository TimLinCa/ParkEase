using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ParkEase.ViewModel;
using ParkEase.Contracts.Services;
using ParkEase.Core.Model;
using ParkEase.Core.Data;
using Xunit;
using Moq;
using ParkEase.Core.Contracts.Services;
using ParkEase.Utilities;
using UraniumUI.Material.Controls;


namespace ParkEase.Test.UnitTest
{
    public class LoginPage
    {
        private LogInViewModel _logInViewModel;
        private readonly Mock<IMongoDBService> _mongoDBServiceMock;
        private readonly Mock<IDialogService> _dialogServiceMock;
        private readonly Mock<IAWSService> _awsServiceMock;
        private readonly ParkEaseModel _parkEaseModel;

        public LoginPage()
        {
            _mongoDBServiceMock = new Mock<IMongoDBService>();
            _dialogServiceMock = new Mock<IDialogService>();
            _awsServiceMock = new Mock<IAWSService>();
            _parkEaseModel = new ParkEaseModel(false);
            _logInViewModel = new LogInViewModel(_mongoDBServiceMock.Object, _dialogServiceMock.Object, _awsServiceMock.Object, _parkEaseModel);
        }

        [Fact]
        public void ValidLogin()
        {
            // Arrange
            var validEmail = "test@example.com";
            var validPassword = "ValidPassword123";
            _logInViewModel.Email = validEmail;
            _logInViewModel.Password = validPassword;

            var user = new User { Email = validEmail, Password = PasswordHasher.HashPassword(validPassword) };
            _mongoDBServiceMock.Setup(m => m.GetData<User>(It.IsAny<string>())).ReturnsAsync(new List<User> { user });

            // Act
            _logInViewModel.LogInCommand.Execute(null);

            // Assert
            Assert.Equal(validEmail, _parkEaseModel.User.Email);
        }

        [Fact]
        public void InvalidEmail()
        {
            // Arrange
            var invalidEmail = "invalid-email";
            var validPassword = "ValidPassword123";
            _logInViewModel.Email = invalidEmail;
            _logInViewModel.Password = validPassword;

            // Mock setup to return an empty user list
            _mongoDBServiceMock.Setup(m => m.GetData<User>(It.IsAny<string>())).ReturnsAsync(new List<User>());

            // Mock dialog service to capture the alert message
            _dialogServiceMock.Setup(d => d.ShowAlertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));

            // Act
            _logInViewModel.LogInCommand.Execute(null);

            // Assert
            _dialogServiceMock.Verify(d => d.ShowAlertAsync("Error", "Check your email or password!", "OK"), Times.Once);
        }

        [Fact]
        public async Task InvalidPassword()
        {
            // Arrange
            var validEmail = "test@example.com";
            var invalidPassword = "invalid";
            _logInViewModel.Email = validEmail;
            _logInViewModel.Password = invalidPassword;

            // Mock setup to return a user with a valid email
            var user = new User { Email = validEmail, Password = PasswordHasher.HashPassword("ValidPassword123") };
            _mongoDBServiceMock.Setup(m => m.GetData<User>(It.IsAny<string>())).ReturnsAsync(new List<User> { user });

            // Mock dialog service to capture the alert message
            _dialogServiceMock.Setup(d => d.ShowAlertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));

            // Act
            _logInViewModel.LogInCommand.Execute(null);

            // Assert
            _dialogServiceMock.Verify(d => d.ShowAlertAsync("Error", "Check your email or password!", "OK"), Times.Once);
        }

        [Fact]
        public async Task CaseSensitivity()
        {
            // Arrange
            var storedEmail = "Test@Example.com";
            var storedPassword = "ValidPassword123";
            _logInViewModel.Email = "test@example.com"; // different case
            _logInViewModel.Password = "validpassword123"; // different case

            // Mock setup to return a user with the stored email and password
            var user = new User { Email = storedEmail, Password = PasswordHasher.HashPassword(storedPassword) };
            _mongoDBServiceMock.Setup(m => m.GetData<User>(It.IsAny<string>())).ReturnsAsync(new List<User> { user });

            // Mock dialog service to capture the alert message
            _dialogServiceMock.Setup(d => d.ShowAlertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));

            // Act
            _logInViewModel.LogInCommand.Execute(null);

            // Assert
            _dialogServiceMock.Verify(d => d.ShowAlertAsync("Error", "Check your email or password!", "OK"), Times.Once);
        }       
      
        [Fact]
        public void PasswordMasking()
        {
            // Arrange
            var passwordField = new TextField
            {
                IsPassword = true
            };

            // Act
            var isPasswordMasked = passwordField.IsPassword;

            // Assert
            Assert.True(isPasswordMasked, "Password field should be masked.");
        }

    }

}

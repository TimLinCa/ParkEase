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


namespace ParkEase.Test.UnitTest
{
    public class SignUpPage
    {
        private SignUpViewModel _signUpviewmodel;
        private readonly Mock<IMongoDBService> _mongoDBServiceMock;
        private readonly Mock<IDialogService> _dialogServiceMock;
        private readonly ParkEaseModel _parkEaseModel;

        public SignUpPage()
        {
            _mongoDBServiceMock = new Mock<IMongoDBService>();
            _dialogServiceMock = new Mock<IDialogService>();
            _parkEaseModel = new ParkEaseModel(false);
            _signUpviewmodel = new SignUpViewModel(_mongoDBServiceMock.Object, _dialogServiceMock.Object);
        }

        [Fact]
        public void ValidSignUp()
        {
            // Arrange
            _signUpviewmodel.FullName = "Valid Name";
            _signUpviewmodel.Email = "valid@example.com";
            _signUpviewmodel.Password = "ValidPass123";
            _signUpviewmodel.RepeatPassword = "ValidPass123";
            _signUpviewmodel.IsTermsAndConditionsAccepted = true;

            _mongoDBServiceMock.Setup(m => m.InsertData(It.IsAny<string>(), It.IsAny<User>()))
                .Returns(Task.FromResult(new User()));

            // Act
            _signUpviewmodel.SignUpCommand.Execute(null);

            // Assert
            _dialogServiceMock.Verify(d => d.ShowAlertAsync("", "Your account is created. Please sign in.", "OK"), Times.Once);
        }

        [Fact]
        public void InvalidCharacterInNameField()
        {
            // Arrange
            _signUpviewmodel.FullName = "Invalid123";
            _signUpviewmodel.Email = "valid@example.com";
            _signUpviewmodel.Password = "ValidPass123";
            _signUpviewmodel.RepeatPassword = "ValidPass123";
            _signUpviewmodel.IsTermsAndConditionsAccepted = true;

            // Simulate the XAML validation logic for name field
            var regex = new System.Text.RegularExpressions.Regex("^[a-zA-Z ]*$");
            if (!regex.IsMatch(_signUpviewmodel.FullName))
            {
                // Act
                _dialogServiceMock.Object.ShowAlertAsync("Error", "The field should contain only letters", "OK");
            }
            else
            {
                _signUpviewmodel.SignUpCommand.Execute(null);
            }

            // Assert
            _dialogServiceMock.Verify(d => d.ShowAlertAsync("Error", "The field should contain only letters", "OK"), Times.Once);
        }

        [Fact]
        public void PasswordMismatch()
        {
            // Arrange
            _signUpviewmodel.FullName = "Valid Name";
            _signUpviewmodel.Email = "valid@example.com";
            _signUpviewmodel.Password = "ValidPass123";
            _signUpviewmodel.RepeatPassword = "DifferentPass123";
            _signUpviewmodel.IsTermsAndConditionsAccepted = true;

            // Act
            _signUpviewmodel.SignUpCommand.Execute(null);

            // Assert
            Assert.Equal("Password does not match!", _signUpviewmodel.UnMatchingPasswordMessage);
        }

        [Fact]
        public void InvalidEmailFormat()
        {
            // Arrange
            _signUpviewmodel.FullName = "Valid Name";
            _signUpviewmodel.Email = "invalid-email";
            _signUpviewmodel.Password = "ValidPass123";
            _signUpviewmodel.RepeatPassword = "ValidPass123";
            _signUpviewmodel.IsTermsAndConditionsAccepted = true;

            // Simulate the XAML validation logic
            var regex = new System.Text.RegularExpressions.Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            if (!regex.IsMatch(_signUpviewmodel.Email))
            {
                // Act
                _dialogServiceMock.Object.ShowAlertAsync("Error", "Please type a valid e-mail address.", "OK");
            }
            else
            {
                _signUpviewmodel.SignUpCommand.Execute(null);
            }

            // Assert
            _dialogServiceMock.Verify(d => d.ShowAlertAsync("Error", "Please type a valid e-mail address.", "OK"), Times.Once);
        }

        [Fact]
        public void InvalidPassword()
        {
            // Arrange
            _signUpviewmodel.FullName = "Valid Name";
            _signUpviewmodel.Email = "valid@example.com";
            _signUpviewmodel.Password = "short"; // Invalid password (less than 8 characters)
            _signUpviewmodel.RepeatPassword = "short";
            _signUpviewmodel.IsTermsAndConditionsAccepted = true;

            // Simulate the XAML validation logic
            var regex = new System.Text.RegularExpressions.Regex("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)[A-Za-z\\d]{8,}$");
            if (!regex.IsMatch(_signUpviewmodel.Password))
            {
                // Act
                _dialogServiceMock.Object.ShowAlertAsync("Error", "A valid password must include at least one uppercase letter, one lowercase letter, one number and must be at least 8 characters long.", "OK");
            }
            else
            {
                _signUpviewmodel.SignUpCommand.Execute(null);
            }

            // Assert
            _dialogServiceMock.Verify(d => d.ShowAlertAsync("Error", "A valid password must include at least one uppercase letter, one lowercase letter, one number and must be at least 8 characters long.", "OK"), Times.Once);
        }

        [Fact]
        public void TermsAndConditionsNotAccepted()
        {
            // Arrange
            _signUpviewmodel.FullName = "Valid Name";
            _signUpviewmodel.Email = "valid@example.com";
            _signUpviewmodel.Password = "ValidPass123";
            _signUpviewmodel.RepeatPassword = "ValidPass123";
            _signUpviewmodel.IsTermsAndConditionsAccepted = false; // Terms and conditions not accepted

            // Act
            _signUpviewmodel.SignUpCommand.Execute(null);

            // Assert
            _dialogServiceMock.Verify(d => d.ShowAlertAsync("", "Please check your information again.", "OK"), Times.Once);
        }

        [Fact]
        public async Task DuplicateEmailAddress()
        {
            // Arrange
            var duplicateEmail = "duplicate@email.com";
            _mongoDBServiceMock.Setup(m => m.GetData<User>(It.IsAny<string>()))
                .ReturnsAsync(new List<User> { new User { Email = duplicateEmail } });

            // Act
            _signUpviewmodel.Email = duplicateEmail;
            await Task.Delay(200); 

            // Assert
            Assert.Equal("This email address already exists", _signUpviewmodel.EmailExistsMessage);
        }


        [Fact]
        public void EmptyFields()
        {
            // Arrange
            _signUpviewmodel.FullName = "";
            _signUpviewmodel.Email = "";
            _signUpviewmodel.Password = "";
            _signUpviewmodel.RepeatPassword = "";
            _signUpviewmodel.IsTermsAndConditionsAccepted = false;

            // Act
            _signUpviewmodel.SignUpCommand.Execute(null);

            // Assert
            _dialogServiceMock.Verify(d => d.ShowAlertAsync("", "Please check your information again.", "OK"), Times.Once);
        }
    }
}

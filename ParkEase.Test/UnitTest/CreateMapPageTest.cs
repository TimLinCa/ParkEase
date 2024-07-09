using Xunit;
using Moq;
using ParkEase.ViewModel;
using ParkEase.Services;
using ParkEase.Contracts.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using ParkEase.Core.Contracts.Services;
using ParkEase.Core.Model;
using Microsoft.Maui.Graphics.Platform;
using Microsoft.Maui.Storage;
using System.Collections.ObjectModel;
using ParkEase.Core.Data;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Layouts;

namespace ParkEase.Test.UnitTest
{
    public class CreateMapPageTest
    {
        private readonly CreateMapViewModel viewModel;
        private readonly Mock<IMongoDBService> mongoDBService;
        private readonly Mock<IDialogService> dialogService;
        private readonly ParkEaseModel parkEaseModel;
        private readonly bool addNewFloorClicked = false;

        public CreateMapPageTest()
        {
            mongoDBService = new Mock<IMongoDBService>();
            dialogService = new Mock<IDialogService>();
            parkEaseModel = new ParkEaseModel
            {
                User = new User
                {
                    Email = "testuser@example.com"
                }
            };
            viewModel = new CreateMapViewModel(mongoDBService.Object, dialogService.Object, parkEaseModel);
        }

        [Fact]
        public void DrawRectangleTest()
        {
            // Arrange
            viewModel.ListRectangle = new ObservableCollection<Rectangle>();

            // Act
            var rect = new RectF(10, 10, 50, 50);
            var rectangle = new Rectangle(1, rect);
            viewModel.ListRectangle.Add(rectangle);

            // Assert
            Assert.Single(viewModel.ListRectangle);
            Assert.Equal(rectangle, viewModel.ListRectangle[0]);
            Assert.Equal(1, viewModel.ListRectangle[0].Index);
            Assert.Equal("#009D00", viewModel.ListRectangle[0].Color);
            Assert.Equal(rect, viewModel.ListRectangle[0].Rect);
        }

        [Fact]
        public void RemoveClearRectangleTest()
        {
            // Arrange
            viewModel.ListRectangle = new ObservableCollection<Rectangle>
            {
                new Rectangle(1, new RectF(10, 10, 50, 50)),
                new Rectangle(2, new RectF(20, 20, 60, 60)),
                new Rectangle(3, new RectF(30, 30, 70, 70))
            };

            // Act
            // Remove a rectangle
            var rectangleToDelete = viewModel.ListRectangle[2];
            
            var command = (RelayCommand)viewModel.RemoveRectangleClick;
            command.Execute(null);

            // Assert deletion
            Assert.Equal(2, viewModel.ListRectangle.Count);
            Assert.DoesNotContain(rectangleToDelete, viewModel.ListRectangle);
        }

        [Fact]
        public void ClearRectangleTest()
        {
            // Arrange
            viewModel.ListRectangle = new ObservableCollection<Rectangle>
            {
                new Rectangle(1, new RectF(10, 10, 50, 50)),
                new Rectangle(2, new RectF(20, 20, 60, 60)),
                new Rectangle(3, new RectF(30, 30, 70, 70))
            };

            // Act
            // Clear all rectangles
            viewModel.ListRectangle.Clear();

            // Assert clearing
            Assert.Empty(viewModel.ListRectangle);
        }

        [Fact]
        public async Task AddExistingFloorNameTest()
        {
            // Arrange
            var existingFloorName = "F1";
            viewModel.FloorNames = new ObservableCollection<string> { existingFloorName };
            viewModel.Floor = existingFloorName;

            // Setup dialogService to capture the message
            string capturedTitle = null;
            string capturedMessage = null;
            string capturedButton = null;

            dialogService.Setup(d => d.ShowAlertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                         .Callback<string, string, string>((title, message, button) =>
                         {
                             capturedTitle = title;
                             capturedMessage = message;
                             capturedButton = button;
                         })
                         .Returns(Task.CompletedTask);

            // Act
            var command = (RelayCommand)viewModel.AddNewFloorCommand;
            command.Execute(null);

            // Wait for any asynchronous operations to complete
            await Task.Delay(500);

            // Assert
            Assert.Equal("Warning", capturedTitle);
            Assert.Equal("This floor name already existed in database.\nPlease enter another one!", capturedMessage);
            Assert.Equal("OK", capturedButton);
        }

    }
}

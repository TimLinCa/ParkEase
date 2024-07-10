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
using Castle.Core.Resource;
using NSubstitute;
using System;
using MongoDB.Driver;
using System.Reflection;
using MongoDB.Bson.Serialization;

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

        [Fact]
        public async Task ClickAddButtonBeforeSaveTest_success()
        {
            // Arrange
            var newFloorName = "F1";
            viewModel.Floor = newFloorName;
            viewModel.ListRectangle = new ObservableCollection<Rectangle>
            {
                new Rectangle(1, new RectF(10, 10, 50, 50)),
                new Rectangle(2, new RectF(20, 20, 60, 60)),
                new Rectangle(3, new RectF(30, 30, 70, 70))
            };
            viewModel.SetPrivate<byte[]>("imageData", new byte[] { 0x20, 0x20 }); // Mock image data
            string capturedMessage = null;

            // Act
            // Set addNewFloorClicked to true
            viewModel.SetPrivate<bool>("addNewFloorClicked", true);
            // Try to save without clicking Add button
            var command = (RelayCommand)viewModel.SaveFloorInfoCommand;
            command.Execute(null);

            // Wait for any asynchronous operations to complete
            await Task.Delay(500);

            // Assert
            Assert.Empty(viewModel.Floor);
            Assert.Null(viewModel.ImgSourceData);
            Assert.Equal(100, viewModel.RectWidth);
            Assert.Equal(50, viewModel.RectHeight);
            Assert.Empty(viewModel.ListRectangle);
            Assert.False(addNewFloorClicked);
        }

        [Fact]
        public async Task ClickAddButtonBeforeSaveTest_fail()
        {
            // Arrange
            var newFloorName = "F1";
            viewModel.Floor = newFloorName;
            viewModel.ListRectangle = new ObservableCollection<Rectangle>
            {
                new Rectangle(1, new RectF(10, 10, 50, 50)),
                new Rectangle(2, new RectF(20, 20, 60, 60)),
                new Rectangle(3, new RectF(30, 30, 70, 70))
            };
            viewModel.SetPrivate<byte[]>("imageData", new byte[] { 0x20, 0x20 }); // Mock image data

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
            // Set addNewFloorClicked to true
            viewModel.SetPrivate<bool>("addNewFloorClicked", false);
            // Try to save without clicking Add button
            var command = (RelayCommand)viewModel.SaveFloorInfoCommand;
            command.Execute(null);

            // Wait for any asynchronous operations to complete
            await Task.Delay(500);

            // Assert
            Assert.Equal("You may forget clicking Add button.", capturedMessage);
        }

        [Fact]
        public async Task SaveFloorMap_Success()
        {
            // Arrange
            var newFloorName = "F1";
            viewModel.Floor = newFloorName;
            viewModel.ListRectangle = new ObservableCollection<Rectangle>
            {
                new Rectangle(1, new RectF(10, 10, 50, 50)),
                new Rectangle(2, new RectF(20, 20, 60, 60)),
                new Rectangle(3, new RectF(30, 30, 70, 70))
            };
            viewModel.SetPrivate<byte[]>("imageData", new byte[] { 0x20, 0x20 }); // Mock image data

            // Set addNewFloorClicked to true
            viewModel.SetPrivate<bool>("addNewFloorClicked", true);

            // Act
            var command = (RelayCommand)viewModel.SaveFloorInfoCommand;
            command.Execute(null);

            // Wait for any asynchronous operations to complete
            await Task.Delay(500);

            // Assert
            // Verify that the new floor name is added to FloorNames
            Assert.Contains(newFloorName, viewModel.FloorNames);

            // Verify that listFloorInfos contains the new floor info
            var floorInfo = viewModel.GetPrivate<List<FloorInfo>>("listFloorInfos").FirstOrDefault(fi => fi.Floor == newFloorName);
            Assert.NotNull(floorInfo);
            Assert.Equal(newFloorName, floorInfo.Floor);
            Assert.Equal(viewModel.GetPrivate<byte[]>("imageData"), floorInfo.ImageData);

            // Verify that info is cleared
            Assert.Empty(viewModel.Floor); // Check if Floor is cleared
            Assert.Null(viewModel.ImgSourceData); // Check if ImgSourceData is cleared
            Assert.Equal(100, viewModel.RectWidth); // Check if RectWidth is reset
            Assert.Equal(50, viewModel.RectHeight); // Check if RectHeight is reset
            Assert.Empty(viewModel.ListRectangle); // Check if ListRectangle is cleared
        }

        [Fact]
        public void EmptyFieldTest()
        {
            // Arrange
            viewModel.CompanyName = "Test Company";
            viewModel.Address = "";

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
            var command = (RelayCommand)viewModel.SubmitCommand;
            command.Execute(null);

            // Assert
            Assert.Equal("Please check if all fields is filled up.", capturedMessage);
        }

        [Fact]
        public async Task EditParkingDataBeforeSubmit_Success()
        {
            // Arrange
            var existingFloorName = "F1";
            var updatedRectangles = new ObservableCollection<Rectangle>
    {
        new Rectangle(1, new RectF(10, 10, 50, 50)),
        new Rectangle(2, new RectF(20, 20, 60, 60)),
        new Rectangle(3, new RectF(30, 30, 70, 70))
    };
            var updatedImageData = new byte[] { 0x30, 0x30 }; // Mock updated image data

            // Add initial floor info to listFloorInfos
            var initialFloorInfo = new FloorInfo(existingFloorName, new List<Rectangle>(), new byte[] { 0x20, 0x20 });
            viewModel.GetPrivate<List<FloorInfo>>("listFloorInfos").Add(initialFloorInfo);
            viewModel.FloorNames.Add(existingFloorName);

            // Set the viewModel to edit mode
            viewModel.SelectedFloorName = existingFloorName;
            viewModel.ListRectangle = updatedRectangles;
            viewModel.SetPrivate<byte[]>("imageData", updatedImageData);

            // Act
            var command = (RelayCommand)viewModel.SaveFloorInfoCommand;
            command.Execute(null);

            // Wait for any asynchronous operations to complete
            await Task.Delay(500);

            // Assert
            var editedFloorInfo = viewModel.GetPrivate<List<FloorInfo>>("listFloorInfos").FirstOrDefault(fi => fi.Floor == existingFloorName);
            Assert.NotNull(editedFloorInfo);
            Assert.Equal(updatedImageData, editedFloorInfo.ImageData);
        }

        [Fact]
        public async Task SubmitCommand_AddNewParkingData()
        {
            // Arrange
            var newFloorName = "F1";
            var newRectangles = new ObservableCollection<Rectangle>
        {
            new Rectangle(1, new RectF(10, 10, 50, 50)),
            new Rectangle(2, new RectF(20, 20, 60, 60)),
            new Rectangle(3, new RectF(30, 30, 70, 70))
        };
            var newImageData = new byte[] { 0x20, 0x20 }; // Mock image data

            viewModel.CompanyName = "Test Company";
            viewModel.Address = "123 Test Street";
            viewModel.SetPrivate("latitude", 51.066669);
            viewModel.SetPrivate("longitude", -114.08989);
            viewModel.Fee = 10;
            viewModel.LimitHour = 2;

            var listFloorInfos = new List<FloorInfo>
        {
            new FloorInfo(newFloorName, newRectangles.ToList(), newImageData)
        };
            viewModel.SetPrivate("listFloorInfos", listFloorInfos);

            string capturedMessage = null;
            var mockPrivateParkingId = "123";
            string expectedMessage = $"Your data is saved.\nGenerate QR Code to use this parking lot\n{mockPrivateParkingId}";

            // Mock the dialog service to capture the message
            dialogService
                .Setup(ds => ds.ShowAlertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string, string>((title, message, button) => capturedMessage = message)
                .Returns(Task.CompletedTask);

            // Create mock private parking data
            var privateParking = new PrivateParking
            {
                Id = mockPrivateParkingId,
                CompanyName = viewModel.CompanyName,
                Address = viewModel.Address,
                Latitude = viewModel.GetPrivate<double>("latitude"),
                Longitude = viewModel.GetPrivate<double>("longitude"),
                CreatedBy = "testuser@example.com",
                ParkingInfo = new ParkingInfo { Fee = viewModel.Fee, LimitedHour = viewModel.LimitHour },
                FloorInfo = listFloorInfos
            };

            // Mock the database save method
            mongoDBService
                .Setup(db => db.InsertData(It.IsAny<string>(), It.IsAny<PrivateParking>()))
                .ReturnsAsync(privateParking);

            mongoDBService
                .Setup(db => db.GetData<PrivateParking>(It.IsAny<string>()))
                .ReturnsAsync(new List<PrivateParking> { privateParking });

            // Act
            var command = (RelayCommand)viewModel.SubmitCommand;
            command.Execute(null);

            // Wait for any asynchronous operations to complete
            await Task.Delay(500);

            // Assert
            Assert.NotNull(capturedMessage);
            Assert.Equal(expectedMessage, capturedMessage);

            mongoDBService.Verify(db => db.InsertData(It.IsAny<string>(), It.Is<PrivateParking>(p =>
                p.CompanyName == privateParking.CompanyName &&
                p.Address == privateParking.Address &&
                p.Latitude == privateParking.Latitude &&
                p.Longitude == privateParking.Longitude &&
                p.CreatedBy == privateParking.CreatedBy &&
                p.ParkingInfo.Fee == privateParking.ParkingInfo.Fee &&
                p.ParkingInfo.LimitedHour == privateParking.ParkingInfo.LimitedHour &&
                p.FloorInfo.SequenceEqual(listFloorInfos)
            )), Times.Once);
        }

        [Fact]
        public async Task SubmitCommand_UpdateExistingParkingData()
        {
            // Arrange
            var mockRectangles = new List<Rectangle>
        {
            new Rectangle(1, new RectF(10, 10, 50, 50)),
            new Rectangle(2, new RectF(20, 20, 60, 60)),
            new Rectangle(3, new RectF(30, 30, 70, 70))
        };
            var mockImageData = new byte[] { 0x20, 0x20 };

            var mockListFloorInfos = new List<FloorInfo>
        {
            new FloorInfo("F1", mockRectangles, mockImageData),
            new FloorInfo("F2", mockRectangles, mockImageData),
            new FloorInfo("Ground", mockRectangles, mockImageData)
        };

            var privateParking1 = new PrivateParking
            {
                Id = "6673068ab36704987b214633",
                CompanyName = "Test Company",
                Address = "Address",
                Latitude = 5.3,
                Longitude = 1.2,
                CreatedBy = "testuser@example.com",
                ParkingInfo = new ParkingInfo { Fee = 0.75, LimitedHour = 4 },
                FloorInfo = mockListFloorInfos
            };

            var mockUserData = new List<PrivateParking> { privateParking1 };

            var mongoDBService = new Mock<IMongoDBService>();
            var dialogService = new Mock<IDialogService>();
            var parkEaseModel = new ParkEaseModel { User = new User { Email = "testuser@example.com" } };

            mongoDBService
                .Setup(m => m.GetData<PrivateParking>(It.IsAny<string>()))
                .ReturnsAsync(mockUserData);

            var viewModel = new CreateMapViewModel(mongoDBService.Object, dialogService.Object, parkEaseModel)
            {
                SelectedAddress = "Address",
                CompanyName = "Updated Company",
                Address = "Updated Address",
                Fee = 1.0, // Updated fee
                LimitHour = 3, // Updated limit hour
            };

            // Set private fields using reflection
            SetPrivateField(viewModel, "selectedPropertyId", "6673068ab36704987b214633");
            SetPrivateField(viewModel, "latitude", 6.5);
            SetPrivateField(viewModel, "longitude", 2.3);
            SetPrivateField(viewModel, "listFloorInfos", mockListFloorInfos);

            mongoDBService
                .Setup(m => m.UpdateData(
                    It.IsAny<string>(),
                    It.IsAny<FilterDefinition<PrivateParking>>(),
                    It.IsAny<UpdateDefinition<PrivateParking>>()))
                .Returns(Task.CompletedTask);

            // Act
            viewModel.SubmitCommand.Execute(null);

            // Assert
            mongoDBService.Verify(m => m.UpdateData(
                It.IsAny<string>(),
                It.Is<FilterDefinition<PrivateParking>>(f => FilterDefinitionMatches(f, "6673068ab36704987b214633")),
                It.Is<UpdateDefinition<PrivateParking>>(u => UpdateDefinitionMatches(u, "Updated Company", "Updated Address", 1.0, 3))
            ), Times.Once);

            dialogService.Verify(d => d.ShowAlertAsync("", "Your data is updated.", "OK"), Times.Once);
        }

        // Helper method to set private fields using reflection
        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var fieldInfo = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (fieldInfo != null)
            {
                fieldInfo.SetValue(obj, value);
            }
            else
            {
                throw new ArgumentException($"Field '{fieldName}' not found in type '{obj.GetType().Name}'.");
            }
        }

        // Helper method to match FilterDefinition with expected ID
        private bool FilterDefinitionMatches(FilterDefinition<PrivateParking> filter, string id)
        {
            var bsonDocument = filter.Render(BsonSerializer.SerializerRegistry.GetSerializer<PrivateParking>(), BsonSerializer.SerializerRegistry);
            var filterId = bsonDocument["_id"].AsObjectId.ToString();
            return filterId == id;
        }

        // Helper method to match UpdateDefinition with expected values
        private bool UpdateDefinitionMatches(UpdateDefinition<PrivateParking> update, string companyName, string address, double fee, int limitHour)
        {
            var bsonDocument = update.Render(BsonSerializer.SerializerRegistry.GetSerializer<PrivateParking>(), BsonSerializer.SerializerRegistry);
            var updateDocument = bsonDocument["$set"].AsBsonDocument;
            return updateDocument["CompanyName"] == companyName
                && updateDocument["Address"] == address
                && updateDocument["ParkingInfo"]["Fee"].ToDouble() == fee
                && updateDocument["ParkingInfo"]["LimitedHour"].ToInt32() == limitHour;
        }
    

        [Fact]
        public async Task LoadMapDataTest()
        {
            // Arrange
            var mockRectangles = new List<Rectangle>
        {
            new Rectangle(1, new RectF(10, 10, 50, 50)),
            new Rectangle(2, new RectF(20, 20, 60, 60)),
            new Rectangle(3, new RectF(30, 30, 70, 70))
        };
            var mockImageData = new byte[] { 0x20, 0x20 };

            var mockListFloorInfos = new List<FloorInfo>
        {
            new FloorInfo("F1", mockRectangles, mockImageData)
        };

            var privateParking1 = new PrivateParking
            {
                Id = "123",
                CompanyName = "Test Company 1",
                Address = "Address1",
                Latitude = 5.3,
                Longitude = 1.2,
                CreatedBy = "testuser@example.com",
                ParkingInfo = new ParkingInfo {Fee = 0.75, LimitedHour = 4 },
                FloorInfo = mockListFloorInfos
            };

            var privateParking2 = new PrivateParking
            {
                Id = "123",
                CompanyName = "Test Company 2",
                Address = "Address2",
                Latitude = 5.3,
                Longitude = 1.2,
                CreatedBy = "testuser@example.com",
                ParkingInfo = new ParkingInfo { Fee = 0.75, LimitedHour = 4 },
                FloorInfo = mockListFloorInfos
            };

            var privateParking3 = new PrivateParking
            {
                Id = "123",
                CompanyName = "Test Company 3",
                Address = "Address3",
                Latitude = 5.3,
                Longitude = 1.2,
                CreatedBy = "testuser@example.com",
                ParkingInfo = new ParkingInfo { Fee = 0.75, LimitedHour = 4 },
                FloorInfo = mockListFloorInfos
            };

            // Mock setup to return the mockUserData when GetDataFilter is called
            mongoDBService
            .Setup(m => m.GetDataFilter<PrivateParking>(It.IsAny<string>(), It.IsAny<FilterDefinition<PrivateParking>>()))
            .ReturnsAsync(new List<PrivateParking> { privateParking1, privateParking2, privateParking3 });

            // Invoke private method "GetUserDataFromDatabase"
            typeof(CreateMapViewModel)
                .GetMethod("GetUserDataFromDatabase", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(viewModel, null);

            // Assert: Check that PropertyAddresses contains the expected addresses
            Assert.Collection(viewModel.PropertyAddresses,
                address => Assert.Contains("Address1", viewModel.PropertyAddresses),
                address => Assert.Contains("Address2", viewModel.PropertyAddresses),
                address => Assert.Contains("Address3", viewModel.PropertyAddresses));
        }

        [Fact]
        public async Task LoadFloorDataTest()
        {
            // Arrange
            var mockRectangles = new List<Rectangle>
            {
                new Rectangle(1, new RectF(10, 10, 50, 50)),
                new Rectangle(2, new RectF(20, 20, 60, 60)),
                new Rectangle(3, new RectF(30, 30, 70, 70))
            };
            var mockImageData = new byte[] { 0x20, 0x20 };

            var mockListFloorInfos = new List<FloorInfo>
            {
                new FloorInfo("F1", mockRectangles, mockImageData),
                new FloorInfo("F2", mockRectangles, mockImageData),
                new FloorInfo("Ground", mockRectangles, mockImageData)
            };

            var privateParking = new PrivateParking
            {
                Id = "123",
                CompanyName = "Test Company",
                Address = "Address",
                Latitude = 5.3,
                Longitude = 1.2,
                CreatedBy = "testuser@example.com",
                ParkingInfo = new ParkingInfo { Fee = 0.75, LimitedHour = 4 },
                FloorInfo = mockListFloorInfos
            };

            viewModel.SetPrivate<List<PrivateParking>>("userData", new List<PrivateParking> { privateParking });

            // Act: Trigger the LoadParkingInfoCommand
            viewModel.SelectedAddress = "Address"; // Set the selected address to trigger command execution
            viewModel.LoadParkingInfoCommand.Execute(null);

            // Assert: Check that listFloorInfos and FloorNames are populated correctly
            Assert.Collection(viewModel.FloorNames,
                floorName => Assert.Contains("F1", viewModel.FloorNames),
                floorName => Assert.Contains("F2", viewModel.FloorNames),
                floorName => Assert.Contains("Ground", viewModel.FloorNames));
        }

    }
}

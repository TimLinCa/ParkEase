using Moq;
using ParkEase.Core.Contracts.Services;
using ParkEase.Core.Model;
using ParkEase.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ParkEase.Contracts.Services;
using ParkEase.Core.Data;
using MongoDB.Driver;
using CommunityToolkit.Mvvm.Input;
using Syncfusion.Maui.Calendar;
using CollectionName = ParkEase.Core.Services.CollectionName;
using Syncfusion.Maui.Core.Carousel;
namespace ParkEase.Test.UnitTest
{
    public class AnalysisPageTest
    {
        private Mock<IMongoDBService> mongodbServiceMock;
        private Mock<IDialogService> dialogServiceMock;
        private ParkEaseModel model;
        private AnalysisViewModel viewModel;
        public AnalysisPageTest()
        {
            model = new ParkEaseModel();
            model.User = new User();
            mongodbServiceMock = new Mock<IMongoDBService>();
            dialogServiceMock = new Mock<IDialogService>();
            viewModel = new AnalysisViewModel(model, mongodbServiceMock.Object, dialogServiceMock.Object);
        }

        [Fact]
        public void LoadedCommand_AdministratorRole_AddsPrivateAreaType()
        {
            // Arrange
            model.User.Role = Roles.Administrator;

            // Act
            viewModel.LoadedCommand.Execute(null);

            // Assert
            Assert.Single(viewModel.AreaTypeItemSource);
            Assert.Contains(AreaType.Private.ToString(), viewModel.AreaTypeItemSource);
        }

        [Fact]
        public void LoadedCommand_EngineerRole_AddsPublicAreaType()
        {
            // Arrange
            model.User.Role = Roles.Engineer;

            // Act
            viewModel.LoadedCommand.Execute(null);

            // Assert
            Assert.Single(viewModel.AreaTypeItemSource);
            Assert.Contains(AreaType.Public.ToString(), viewModel.AreaTypeItemSource);
        }

        [Fact]
        public async Task ApplyCommand_EndTimeBeforeStartTime_ShowsErrorDialog()
        {
            // Arrange
            viewModel.StartTime = new TimeSpan(10, 0, 0);
            viewModel.EndTime = new TimeSpan(9, 0, 0);

            // Act
            await viewModel.ApplyCommand.ExecuteAsync(null);

            // Assert
            dialogServiceMock.Verify(d => d.ShowAlertAsync("Error", "End time should be greater than start time", "OK"), Times.Once);
        }

        [Fact]
        public async Task ApplyCommand_NoDateRangeSelected_ShowsErrorDialog()
        {
            // Arrange
            viewModel.IsCurrentDayCheck = false;
            viewModel.SelectedDateRange = null;

            // Act
            await viewModel.ApplyCommand.ExecuteAsync(null);

            // Assert
            dialogServiceMock.Verify(d => d.ShowAlertAsync("Error", "Please select a date range", "OK"), Times.Once);
        }

        [Fact]
        public void OnAreaTypeSelectedChanged_PublicAreaType_UpdatesAreaNameItemSource()
        {
            // Arrange
            var parkingDatas = new List<ParkingData>
        {
            new ParkingData { ParkingSpot = "Spot1" },
            new ParkingData { ParkingSpot = "Spot2" }
        };
            mongodbServiceMock.Setup(m => m.GetData<ParkingData>(ParkEase.Core.Services.CollectionName.ParkingData))
                .ReturnsAsync(parkingDatas);

            // Act
            viewModel.AreaTypeSelected = AreaType.Public.ToString();

            // Assert
            Assert.Equal(2, viewModel.AreaNameItemSource.Count);
            Assert.Contains("Spot1", viewModel.AreaNameItemSource);
            Assert.Contains("Spot2", viewModel.AreaNameItemSource);
            Assert.False(viewModel.IsFloowSelectedVisible);
        }

        [Fact]
        public async Task ApplyCommand_ValidInputs_UpdatesGraphs()
        {
            // Arrange
            var parkingData = new ParkingData { Id = "1", ParkingSpot = "ParkingSpot1" };
            var parkingLogs = new List<PublicLog>
        {
            new PublicLog { AreaId = "1", Timestamp = DateTime.Now, Status = true },
            new PublicLog { AreaId = "1", Timestamp = DateTime.Now.AddHours(1), Status = false }
        };

            mongodbServiceMock.Setup(m => m.GetData<ParkingData>(CollectionName.ParkingData))
                .ReturnsAsync(new List<ParkingData> { parkingData });
            mongodbServiceMock.Setup(m => m.GetData<PublicLog>(CollectionName.PublicLogs))
                .ReturnsAsync(parkingLogs);

            viewModel.StartTime = new TimeSpan(8, 0, 0);
            viewModel.EndTime = new TimeSpan(18, 0, 0);
            viewModel.IsCurrentDayCheck = true;
            viewModel.AreaTypeSelected = "Public";
            viewModel.AreaNameSelected = "ParkingSpot1";

            // Act
            viewModel.ApplyCommand.Execute(null);

            // Assert
            Assert.NotNull(viewModel.UsageSeriesCollection);
            Assert.NotNull(viewModel.ParkingTimeSeriesCollection);
            Assert.NotEmpty(viewModel.AverageUsage);
            Assert.NotEmpty(viewModel.AverageParkingTime);
        }

        [Fact]
        public async Task ApplyCommand_PrivateParking_UpdatesGraphs()
        {
            // Arrange
            var privateParking = new PrivateParking { Id = "1", CompanyName = "Company", Address = "Address" };
            var parkingLogs = new List<PrivateLog>
        {
            new PrivateLog { AreaId = "1", Timestamp = DateTime.Now, Status = true, Floor = "Floor1" },
            new PrivateLog { AreaId = "1", Timestamp = DateTime.Now.AddHours(1), Status = false, Floor = "Floor1" }
        };

            mongodbServiceMock.Setup(m => m.GetDataFilter<PrivateParking>(CollectionName.PrivateParking, It.IsAny<FilterDefinition<PrivateParking>>()))
                .ReturnsAsync(new List<PrivateParking> { privateParking });
            mongodbServiceMock.Setup(m => m.GetData<PrivateLog>(CollectionName.PrivateLogs))
                .ReturnsAsync(parkingLogs);

            viewModel.StartTime = new TimeSpan(8, 0, 0);
            viewModel.EndTime = new TimeSpan(18, 0, 0);
            viewModel.IsCurrentDayCheck = true;
            viewModel.AreaTypeSelected = "Private";
            viewModel.AreaNameSelected = "Company(Address)";
            viewModel.IsAllFloorCheck = true;

            // Act
            viewModel.ApplyCommand.Execute(null);

            // Assert
            Assert.NotNull(viewModel.UsageSeriesCollection);
            Assert.NotNull(viewModel.ParkingTimeSeriesCollection);
            Assert.NotEmpty(viewModel.AverageUsage);
            Assert.NotEmpty(viewModel.AverageParkingTime);
        }

        [Fact]
        public async Task ApplyCommand_DateRangeSelected_UpdatesGraphs()
        {
            // Arrange
            var parkingData = new ParkingData { Id = "1", ParkingSpot = "ParkingSpot1" };
            var parkingLogs = new List<PublicLog>
        {
            new PublicLog { AreaId = "1", Timestamp = DateTime.Now.AddDays(-5), Status = true },
            new PublicLog { AreaId = "1", Timestamp = DateTime.Now.AddDays(-3), Status = false }
        };

            mongodbServiceMock.Setup(m => m.GetData<ParkingData>(CollectionName.ParkingData))
                .ReturnsAsync(new List<ParkingData> { parkingData });
            mongodbServiceMock.Setup(m => m.GetData<PublicLog>(CollectionName.PublicLogs))
                .ReturnsAsync(parkingLogs);

            viewModel.StartTime = new TimeSpan(8, 0, 0);
            viewModel.EndTime = new TimeSpan(18, 0, 0);
            viewModel.IsCurrentDayCheck = false;
            viewModel.SelectedDateRange = new CalendarDateRange(DateTime.Now.AddDays(-7), DateTime.Now);
            viewModel.AreaTypeSelected = "Public";
            viewModel.AreaNameSelected = "ParkingSpot1";

            // Act
            viewModel.ApplyCommand.Execute(null);

            // Assert
            Assert.NotNull(viewModel.UsageSeriesCollection);
            Assert.NotNull(viewModel.ParkingTimeSeriesCollection);
            Assert.NotEmpty(viewModel.AverageUsage);
            Assert.NotEmpty(viewModel.AverageParkingTime);
        }

        [Fact]
        public async Task ApplyCommand_NoData_ShowsAlert()
        {
            // Arrange
            mongodbServiceMock.Setup(m => m.GetData<ParkingData>(CollectionName.ParkingData))
             .ReturnsAsync(new List<ParkingData>());
            mongodbServiceMock.Setup(m => m.GetData<PublicLog>(CollectionName.PublicLogs))
                .ReturnsAsync(new List<PublicLog>());

            viewModel.StartTime = new TimeSpan(8, 0, 0);
            viewModel.EndTime = new TimeSpan(18, 0, 0);
            viewModel.IsCurrentDayCheck = true;
            viewModel.AreaTypeSelected = "Public";
            viewModel.AreaNameSelected = "ParkingSpot1";

            // Act
            viewModel.ApplyCommand.Execute(null);

            // Assert
            dialogServiceMock.Verify(d => d.ShowAlertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        private byte[] GetByteArray(int sizeInKb)
        {
            Random rnd = new Random();
            byte[] b = new byte[sizeInKb * 1024]; // convert kb to byte
            rnd.NextBytes(b);
            return b;
        }
    }
}

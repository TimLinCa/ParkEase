using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using ParkEase.Core.Services;
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
using Syncfusion.Maui.Core.Carousel;
using Moq;
using System.Collections.ObjectModel;
using Xunit;
using Microsoft.Maui.Controls;
using ParkEase.Controls;

namespace ParkEase.Test.IntergartionTest
{
    public class MapPageTestsFixture : IAsyncLifetime
    {
        public IConfiguration Configuration { get; private set; }
        public MongoDBService MongoDBService { get; private set; }
        public ParkEaseModel Model { get; private set; }
        public IAWSService AWSService { get; private set; }
        public DialogService DialogService { get; private set; }
        public GeocodingService GeocodingService { get; private set; }

        public async Task InitializeAsync()
        {
            // Build configuration
            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            AWSService = new AWSService(Configuration);

            Environment.SetEnvironmentVariable("GoogleAKYKey", await AWSService.GetParamenter("/ParkEase/APIKeys/google"));

            MongoDBService = new MongoDBService(AWSService, DevicePlatform.WinUI, true);
            DialogService = new DialogService();
            GeocodingService = new GeocodingService();
            Model = new ParkEaseModel(true);
            Model.User = new User();
            Model.User.Role = Roles.User;

            // Seed test data
            await SeedTestDataAsync();
        }

        public async Task DisposeAsync()
        {
            // Clean up the test database
            await MongoDBService.DropCollection(CollectionName.ParkingData);
            await MongoDBService.DropCollection(CollectionName.PrivateParking);
            await MongoDBService.DropCollection(CollectionName.PublicStatus);
            await MongoDBService.DropCollection(CollectionName.PrivateStatus);
        }

        private async Task SeedTestDataAsync()
        {
            var mapLineInside = new MapLine(new List<MapPoint> {
                new MapPoint { Lat = "51.06922604963283", Lng = "-114.09428749140609" },
                new MapPoint { Lat = "51.06923953322363", Lng = "-114.08988866862167" }
            })
            { Id = "mapLineInside" };

            var mapLineInsideA = new MapLine(new List<MapPoint> {
                new MapPoint { Lat = "51.074223986352735", Lng = "-114.09974241563248" },
                new MapPoint { Lat = "51.073286968369274", Lng = "-114.09974241563248" }
            })
            { Id = "mapLineInsideA" };

            var mapLineOutside = new MapLine(new List<MapPoint> {
                new MapPoint { Lat = "49.012", Lng = "-100.099" },
                new MapPoint { Lat = "48.011", Lng = "-100.099" }
            })
            { Id = "mapLineOutside" };

            var parkingDataInside = new ParkingData
            {
                Id = mapLineInside.Id,
                ParkingSpot = "Inside Spot",
                ParkingTime = "Mon to Fri: 7am to 6pm",
                ParkingFee = "$2.00 per hour",
                Points = mapLineInside.Points
            };

            var parkingDataInsideA = new ParkingData
            {
                Id = mapLineInsideA.Id,
                ParkingSpot = "Inside Spot A",
                ParkingTime = "Mon to Fri: 7am to 6pm",
                ParkingFee = "$2.00 per hour",
                Points = mapLineInsideA.Points
            };

            var parkingDataOutside = new ParkingData
            {
                Id = mapLineOutside.Id,
                ParkingSpot = "Outside Spot",
                ParkingTime = "Mon to Fri: 7am to 6pm",
                ParkingFee = "$2.00 per hour",
                Points = mapLineOutside.Points
            };

            // Insert ParkingData
            await MongoDBService.InsertData(CollectionName.ParkingData, parkingDataInside);
            await MongoDBService.InsertData(CollectionName.ParkingData, parkingDataInsideA);
            await MongoDBService.InsertData(CollectionName.ParkingData, parkingDataOutside);

            var parkingDatas = await MongoDBService.GetData<ParkingData>(CollectionName.ParkingData);

            var publicStatusInside = new PublicStatus
            {
                Id = parkingDataInside.Id,
                AreaId = parkingDataInside.Id,
                Status = true
            };

            var publicStatusInsideA = new PublicStatus
            {
                Id = parkingDataInsideA.Id,
                AreaId = parkingDataInsideA.Id,
                Status = false
            };

            var publicStatusOutside = new PublicStatus
            {
                Id = parkingDataOutside.Id,
                AreaId = parkingDataOutside.Id,
                Status = false
            };

            // Insert PublicStatus
            await MongoDBService.InsertData(CollectionName.PublicStatus, publicStatusInside);
            await MongoDBService.InsertData(CollectionName.PublicStatus, publicStatusInsideA);
            await MongoDBService.InsertData(CollectionName.PublicStatus, publicStatusOutside);

            var privateParkingDataInside = new PrivateParking
            {
                Id = "mapPointInside",
                CompanyName = "A",
                Address = "AA",
                Latitude = 51.074223986452735,
                Longitude = -114.09974241563248,
                CreatedBy = "Inside Spot",
                ParkingInfo = new ParkingInfo
                {
                    Fee = 10,
                    LimitedHour = 2
                }
            };


            var privateParkingDataInsideA = new PrivateParking
            {
                Id = "mapPointInsideA",
                CompanyName = "B",
                Address = "BB",
                Latitude = 51.073286968469274,
                Longitude = -114.09974241563248,
                CreatedBy = "Inside Spot A",
                ParkingInfo = new ParkingInfo
                {
                    Fee = 8,
                    LimitedHour = 3
                }
            };

            // Insert PrivateParking
            await MongoDBService.InsertData(CollectionName.PrivateParking, privateParkingDataInside);
            await MongoDBService.InsertData(CollectionName.PrivateParking, privateParkingDataInsideA);

            var privateStatusInside = new PrivateStatus
            {
                AreaId = privateParkingDataInside.Id,
                Status = true
            };

            var privateStatusInsideA = new PrivateStatus
            {
                AreaId = privateParkingDataInsideA.Id,
                Status = false
            };

            // Insert PrivateStatus
            await MongoDBService.InsertData(CollectionName.PrivateStatus, privateStatusInside);
            await MongoDBService.InsertData(CollectionName.PrivateStatus, privateStatusInsideA);
        }
    }

    public class MapPageTests : IClassFixture<MapPageTestsFixture>
    {
        private readonly MapPageTestsFixture _fixture;
        private readonly MapViewModel _viewModel;

        public MapPageTests(MapPageTestsFixture fixture)
        {
            _fixture = fixture;
            _viewModel = new MapViewModel(_fixture.MongoDBService, _fixture.DialogService);
        }

        [Fact]
        public async Task DrawLineOnMap()
        {
            // Arrange
            _viewModel.DrawingLine = false;

            // Act
            _viewModel.DrawCommand.Execute(null);

            // Assert
            Assert.True(_viewModel.DrawingLine);
        }

        [Fact]
        public async Task ClearLineFromMap()
        {
            // Arrange
            var mapLine = new MapLine(new List<MapPoint> { new MapPoint { Lat = "0", Lng = "0" }, new MapPoint { Lat = "1", Lng = "1" } });
            _viewModel.MapLines = new ObservableCollection<MapLine> { mapLine };
            _viewModel.SelectedMapLine = mapLine;

            var deleteResult = new DeleteDataResult { Success = true, DeleteCount = 1 };

            // Act
            _viewModel.DeletedLineCommand.Execute(null);

            // Assert
            Assert.DoesNotContain(mapLine, _viewModel.MapLines);
            var deletedData = await _fixture.MongoDBService.GetData<ParkingData>(CollectionName.ParkingData);
            Assert.False(deletedData.Any(d => d.Points.SequenceEqual(mapLine.Points)));
        }

        [Fact]
        public async Task InputParkingData()
        {
            // Arrange
            var mapLine = new MapLine(new List<MapPoint> { new MapPoint { Lat = "0", Lng = "0" }, new MapPoint { Lat = "1", Lng = "1" } });
            _viewModel.MapLines = new ObservableCollection<MapLine> { mapLine };
            _viewModel.SelectedMapLine = mapLine;
            _viewModel.ParkingSpot = "Test Spot";
            _viewModel.SelectedParkingTime = "Mon to Fri: 7am to 6pm";
            _viewModel.SelectedParkingFee = "$2.00 per hour";

            var parkingData = new ParkingData
            {
                ParkingSpot = "Test Spot",
                ParkingTime = "Mon to Fri: 7am to 6pm",
                ParkingFee = "$2.00 per hour",
                Points = mapLine.Points
            };

            // Act
            _viewModel.SubmitCommand.Execute(null);

            // Assert
            var insertedData = await _fixture.MongoDBService.GetData<ParkingData>(CollectionName.ParkingData);
            Assert.Contains(insertedData, pd =>
                pd.ParkingSpot == parkingData.ParkingSpot &&
                pd.ParkingTime == parkingData.ParkingTime &&
                pd.ParkingFee == parkingData.ParkingFee &&
                pd.Points.SequenceEqual(parkingData.Points));
        }

        [Fact]
        public async Task EmptyFields()
        {
            // Arrange
            var mapLine = new MapLine(new List<MapPoint> { new MapPoint { Lat = "0", Lng = "0" }, new MapPoint { Lat = "1", Lng = "1" } });
            _viewModel.MapLines = new ObservableCollection<MapLine> { mapLine };
            _viewModel.SelectedMapLine = mapLine;

            // Act & Assert for empty ParkingSpot
            _viewModel.ParkingSpot = "";
            _viewModel.SelectedParkingTime = "Mon to Fri: 7am to 6pm";
            _viewModel.SelectedParkingFee = "$2.00 per hour";
            _viewModel.SubmitCommand.Execute(null);
            //Assert.True(_fixture.DialogService.ShowAlertAsyncCalled);

            // Act & Assert for empty SelectedParkingTime
            _viewModel.ParkingSpot = "Test Spot";
            _viewModel.SelectedParkingTime = "";
            _viewModel.SelectedParkingFee = "$2.00 per hour";
            _viewModel.SubmitCommand.Execute(null);
            //Assert.True(((DialogService)_fixture.DialogService).ShowAlertAsyncCalled);

            // Act & Assert for empty SelectedParkingFee
            _viewModel.ParkingSpot = "Test Spot";
            _viewModel.SelectedParkingTime = "Mon to Fri: 7am to 6pm";
            _viewModel.SelectedParkingFee = "";
            _viewModel.SubmitCommand.Execute(null);
            //Assert.True(((DialogService)_fixture.DialogService).ShowAlertAsyncCalled);

            // Act & Assert for empty SelectedMapLine
            _viewModel.ParkingSpot = "Test Spot";
            _viewModel.SelectedParkingTime = "Mon to Fri: 7am to 6pm";
            _viewModel.SelectedParkingFee = "$2.00 per hour";
            _viewModel.SelectedMapLine = null;
            _viewModel.SubmitCommand.Execute(null);
            //Assert.True(((DialogService)_fixture.DialogService).ShowAlertAsyncCalled);
        }


        [Fact]
        public async Task SubmitParkingData()
        {
            // Arrange
            var mapLine = new MapLine(new List<MapPoint> { new MapPoint { Lat = "0", Lng = "0" }, new MapPoint { Lat = "1", Lng = "1" } });
            _viewModel.MapLines = new ObservableCollection<MapLine> { mapLine };
            _viewModel.SelectedMapLine = mapLine;
            _viewModel.ParkingSpot = "Test Spot";
            _viewModel.SelectedParkingTime = "Mon to Fri: 7am to 6pm";
            _viewModel.SelectedParkingFee = "$2.00 per hour";

            var parkingData = new ParkingData
            {
                ParkingSpot = "Test Spot",
                ParkingTime = "Mon to Fri: 7am to 6pm",
                ParkingFee = "$2.00 per hour",
                Points = mapLine.Points
            };

            // Act
            _viewModel.SubmitCommand.Execute(null);

            // Assert
            var insertedData = await _fixture.MongoDBService.GetData<ParkingData>(CollectionName.ParkingData);
            Assert.Contains(insertedData, pd =>
                pd.ParkingSpot == parkingData.ParkingSpot &&
                pd.ParkingTime == parkingData.ParkingTime &&
                pd.ParkingFee == parkingData.ParkingFee &&
                pd.Points.SequenceEqual(parkingData.Points));
        }

        [Fact]
        public async Task EditExistingParkingData()
        {
            // Arrange
            var mapLine = new MapLine(new List<MapPoint> { new MapPoint { Lat = "0", Lng = "0" }, new MapPoint { Lat = "1", Lng = "1" } });
            _viewModel.MapLines = new ObservableCollection<MapLine> { mapLine };
            _viewModel.SelectedMapLine = mapLine;
            _viewModel.ParkingSpot = "Edited Spot";
            _viewModel.SelectedParkingTime = "Mon to Fri: 7am to 6pm";
            _viewModel.SelectedParkingFee = "$2.00 per hour";

            var existingParkingData = new ParkingData
            {
                ParkingSpot = "Original Spot",
                ParkingTime = "Mon to Fri: 7am to 6pm",
                ParkingFee = "$2.00 per hour",
                Points = mapLine.Points
            };

            // Add the existing data to the database
            await _fixture.MongoDBService.InsertData(CollectionName.ParkingData, existingParkingData);

            // Act
            _viewModel.SubmitCommand.Execute(null);

            // Assert
            var updatedData = await _fixture.MongoDBService.GetData<ParkingData>(CollectionName.ParkingData);
            Assert.Contains(updatedData, pd =>
                pd.ParkingSpot == "Edited Spot" &&
                pd.ParkingTime == "Mon to Fri: 7am to 6pm" &&
                pd.ParkingFee == "$2.00 per hour" &&
                pd.Points.SequenceEqual(mapLine.Points));
        }

        [Fact]
        public async Task DeleteCorrespondingParkingData()
        {
            // Arrange
            var mapLine = new MapLine(new List<MapPoint> { new MapPoint { Lat = "0", Lng = "0" }, new MapPoint { Lat = "1", Lng = "1" } });
            _viewModel.MapLines = new ObservableCollection<MapLine> { mapLine };
            _viewModel.SelectedMapLine = mapLine;

            var parkingData = new ParkingData
            {
                ParkingSpot = "Test Spot",
                ParkingTime = "Mon to Fri: 7am to 6pm",
                ParkingFee = "$2.00 per hour",
                Points = mapLine.Points
            };

            // Add the data to the database
            await _fixture.MongoDBService.InsertData(CollectionName.ParkingData, parkingData);

            // Act
            _viewModel.DeletedLineCommand.Execute(null);

            // Assert
            Assert.DoesNotContain(mapLine, _viewModel.MapLines);
            var remainingData = await _fixture.MongoDBService.GetData<ParkingData>(CollectionName.ParkingData);
            Assert.False(remainingData.Any(d => d.Points.SequenceEqual(mapLine.Points)));
        }

        [Fact]
        public async Task LoadMapData_OnPageRefresh()
        {
            // Arrange
            var mapLine = new MapLine(new List<MapPoint>
            {
                new MapPoint { Lat = "0", Lng = "0" },
                new MapPoint { Lat = "1", Lng = "1" }
            });

            var parkingData = new ParkingData
            {
                ParkingSpot = "Test Spot",
                ParkingTime = "Mon to Fri: 7am to 6pm",
                ParkingFee = "$2.00 per hour",
                Points = mapLine.Points
            };

            // Add the data to the database
            await _fixture.MongoDBService.InsertData(CollectionName.ParkingData, parkingData);

            // Act
            _viewModel.MapNavigatedCommand.Execute(null);

            // Assert
            Assert.Contains(_viewModel.MapLines, line => line.Points.SequenceEqual(mapLine.Points));
        }
    }

    //public class DialogService : IDialogService
    //{
    //    public bool ShowAlertAsyncCalled { get; private set; } = false;

    //    public Task ShowAlertAsync(string title, string message, string cancel = "OK")
    //    {
    //        ShowAlertAsyncCalled = true;
    //        return Task.CompletedTask;
    //    }

    //    public Task ShowBottomSheet(string address, string parkingFee, string limitHour, string availability, bool ShowButton, string lat, string lng)
    //    {
    //        // Implement if needed for your tests
    //        return Task.CompletedTask;
    //    }

    //    public Task DismissBottomSheetAsync()
    //    {
    //        // Implement if needed for your tests
    //        return Task.CompletedTask;
    //    }

    //    public void ResetAlertFlag()
    //    {
    //        ShowAlertAsyncCalled = false;
    //    }
    //}
}

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
using ParkEase.Controls;
using Syncfusion.Maui.Core.Carousel;
using Moq;
using System.Collections.ObjectModel;
namespace ParkEase.Test.IntergartionTest
{
    public class UserMapPageTestsFixture : IAsyncLifetime
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
            // Use MongoDBService to insert test data
            var mapLineInside = new MapLine(new List<MapPoint> {
                new MapPoint { Lat = "51.06922604963283", Lng = "-114.09428749140609" },
                new MapPoint { Lat = "51.06923953322363", Lng = "-114.08988866862167" }
            });
            mapLineInside.Id = "mapLineInside";

            var mapLineInsideA = new MapLine(new List<MapPoint> {
                new MapPoint { Lat = "51.074223986352735", Lng = "-114.09974241563248" },
                new MapPoint { Lat = "51.073286968369274", Lng = "-114.09974241563248" }
            });
            mapLineInsideA.Id = "mapLineInsideA";

            var mapLineOutside = new MapLine(new List<MapPoint> {
                new MapPoint { Lat = "49.012", Lng = "-100.099" },
                new MapPoint { Lat = "48.011", Lng = "-100.099" }
            });
            mapLineOutside.Id = "mapLineOutside";

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

            List<ParkingData> parkingDatas = await MongoDBService.GetData<ParkingData>(CollectionName.ParkingData);
            parkingDataInside = parkingDatas.FirstOrDefault(p => p.ParkingSpot == "Inside Spot");
            parkingDataInsideA = parkingDatas.FirstOrDefault(p => p.ParkingSpot == "Inside Spot A");
            parkingDataOutside = parkingDatas.FirstOrDefault(p => p.ParkingSpot == "Outside Spot");

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
                ParkingInfo = new ParkingInfo()
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
                ParkingInfo = new ParkingInfo()
                {
                    Fee = 8,
                    LimitedHour = 3
                }
            };

            // Insert PrivateParking
            await MongoDBService.InsertData(CollectionName.PrivateParking, privateParkingDataInside);
            await MongoDBService.InsertData(CollectionName.PrivateParking, privateParkingDataInsideA);
            List<PrivateParking> privateParkingDatas = await MongoDBService.GetData<PrivateParking>(CollectionName.PrivateParking);
            privateParkingDataInside = privateParkingDatas.FirstOrDefault(p => p.CreatedBy == "Inside Spot");
            privateParkingDataInsideA = privateParkingDatas.FirstOrDefault(p => p.CreatedBy == "Inside Spot A");

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

    public class UserMapPageTests : IClassFixture<UserMapPageTestsFixture>
    {
        private readonly UserMapPageTestsFixture _fixture;
        private readonly UserMapViewModel _viewModel;
        private readonly FakeDialogService _dialogService;
        private readonly GeocodingService _geocodingService;

        public UserMapPageTests(UserMapPageTestsFixture fixture)
        {
            _fixture = fixture;
            _dialogService = new FakeDialogService();
            _viewModel = new UserMapViewModel(_fixture.MongoDBService, _dialogService, _fixture.GeocodingService);
            // _viewModel.LoadedCommand.Execute(null);
            // _dialogService = _fixture.DialogService;
            _geocodingService = _fixture.GeocodingService;
        }

        [Fact]
        public async Task ShowPublicParkingTest()
        {
            // Arrange
            double selectedRadius = 1000;  // Example selected radius in meters
            double locationLatitude = 51.0661481;
            double locationLongitude = -114.0997;

            _viewModel.SelectedRadius = selectedRadius;
            _viewModel.ShowPublicParking = true;
            _viewModel.LocationLatitude = locationLatitude;
            _viewModel.LocationLongitude = locationLongitude;

            // Act
            _viewModel.LoadedEventCommand.Execute(null);

            await Task.Delay(2000);

            _viewModel.UpdateRangeCommand.Execute(null);

            _viewModel.LoadedCommand.Execute(null);

            await Task.Delay(5000);

            // Assert
            Assert.Equal(selectedRadius / 1000.0, _viewModel.Radius);

            Assert.True(_viewModel.MapLines.Count == 2);
        }

        [Fact]
        public async Task ShowAvailableParkingTest()
        {
            // Arrange
            double selectedRadius = 1000;  // Example selected radius in meters
            double locationLatitude = 51.0661481;
            double locationLongitude = -114.0997;

            _viewModel.SelectedRadius = selectedRadius;
            _viewModel.ShowPublicParking = true;
            _viewModel.ShowAvailableParking = true;
            _viewModel.LocationLatitude = locationLatitude;
            _viewModel.LocationLongitude = locationLongitude;

            // Act
            _viewModel.LoadedEventCommand.Execute(null);

            await Task.Delay(2000);

            _viewModel.UpdateRangeCommand.Execute(null);

            _viewModel.LoadedCommand.Execute(null);

            await Task.Delay(5000);

            // Assert
            Assert.Equal(selectedRadius / 1000.0, _viewModel.Radius);

            Assert.True(_viewModel.MapLines.Count == 1);
        }

        [Fact]
        public async void SearchAddressTest()
        {
            // Arrange
            var searchText = "North Hill Centre";

            _viewModel.SearchText = searchText;

            Location location = await _geocodingService.GetLocationAsync(searchText);

            // Act
            _viewModel.SearchCommand.Execute(null);

            await Task.Delay(5000);

            // Assert
            Assert.Equal(_viewModel.CenterLocation, location);
        }

        [Fact]
        public async void SearchAddressNoLocationTest()
        {
            // Arrange
            var searchText = "No this location";
            Location expectedLocation = null;

            _viewModel.SearchText = searchText;

            // Act
            _viewModel.SearchCommand.Execute(null);
            await Task.Delay(5000);

            // Assert
            Assert.True(_viewModel.IsSearchInProgress);
            Assert.True(_dialogService.ShowAlertAsyncCalled, "ShowAlertAsync should have been called.");
        }

        [Fact]
        public async void BackToTheCurrentLocationTest()
        {
            // Arrange
            var searchText = "North Hill Centre";

            _viewModel.SearchText = searchText;
            _viewModel.SearchCommand.Execute(null);
            Location searchLocation = await _geocodingService.GetLocationAsync(searchText);

            var latitude = 51.074223986352735;
            var longitude = -114.09974241563248;
            Location userLocation = new Location
            {
                Latitude = latitude,
                Longitude = longitude
            };

            _viewModel.LocationLatitude = latitude;
            _viewModel.LocationLongitude = longitude;

            // Act
            _viewModel.BackToCurrentLocationCommand.Execute(null);
            await Task.Delay(5000);

            // Assert
            Assert.Equal(_viewModel.CenterLocation, userLocation);
            Assert.NotEqual(_viewModel.CenterLocation, searchLocation);
        }

        [Fact]
        public async Task RangeChangeParkingSpotUpdateTest()
        {
            // Arrange
            double selectedRadius = 700;  // Example selected radius in meters
            double locationLatitude = 51.0661481;
            double locationLongitude = -114.0997;

            _viewModel.SelectedRadius = selectedRadius;
            _viewModel.ShowPublicParking = true;
            _viewModel.LocationLatitude = locationLatitude;
            _viewModel.LocationLongitude = locationLongitude;

            // Act
            _viewModel.LoadedEventCommand.Execute(null);

            await Task.Delay(2000);

            _viewModel.UpdateRangeCommand.Execute(null);

            _viewModel.LoadedCommand.Execute(null);

            await Task.Delay(5000);

            // Assert
            Assert.Single(_viewModel.MapLines);

            // Arrange
            double newSelectedRadius = 1000;
            _viewModel.SelectedRadius = newSelectedRadius;

            // Act
            _viewModel.LoadedEventCommand.Execute(null);

            await Task.Delay(2000);

            _viewModel.UpdateRangeCommand.Execute(null);

            _viewModel.LoadedCommand.Execute(null);

            await Task.Delay(5000);

            // Assert
            Assert.True(_viewModel.MapLines.Count == 2);
        }

        [Fact]
        public async void GetParkingSpotsInformationTest()
        {
            // Arrange
            double selectedRadius = 1000;  // Example selected radius in meters
            double locationLatitude = 51.0661481;
            double locationLongitude = -114.0997;

            _viewModel.SelectedRadius = selectedRadius;
            _viewModel.ShowPublicParking = true;
            _viewModel.ShowAvailableParking = true;
            _viewModel.LocationLatitude = locationLatitude;
            _viewModel.LocationLongitude = locationLongitude;

            // Act
            _viewModel.LoadedEventCommand.Execute(null);

            await Task.Delay(2000);

            _viewModel.UpdateRangeCommand.Execute(null);

            _viewModel.LoadedCommand.Execute(null);

            await Task.Delay(5000);

            _viewModel.SelectedMapLine = _viewModel.MapLines[0];  // Select one of the lines

            await Task.Delay(5000);
            Assert.True(_dialogService.ShowBottomSheetCalled, "ShowBottomSheet should have been called.");
        }

        [Fact]
        public async void GetParkingSpotsInformationNoParkingDataTest()
        {
            // Arrange
            double selectedRadius = 100;  // Example selected radius in meters
            double locationLatitude = 51.0661481;
            double locationLongitude = -114.0997;

            var mapLineOutside = new MapLine(new List<MapPoint> {
                new MapPoint { Lat = "0", Lng = "0" },
                new MapPoint { Lat = "1", Lng = "1" }
            });
            mapLineOutside.Id = "mapLineOutside";

            _viewModel.SelectedRadius = selectedRadius;
            _viewModel.ShowPublicParking = true;
            _viewModel.ShowAvailableParking = true;
            _viewModel.LocationLatitude = locationLatitude;
            _viewModel.LocationLongitude = locationLongitude;

            // Act
            _viewModel.LoadedEventCommand.Execute(null);

            await Task.Delay(2000);

            _viewModel.UpdateRangeCommand.Execute(null);
            _viewModel.LoadedCommand.Execute(null);

            await Task.Delay(3000);

            _viewModel.SelectedMapLine = mapLineOutside;
            await Task.Delay(5000);

            Assert.True(_dialogService.ShowAlertAsyncCalled, "ShowAlertAsync should have been called.");
        }

        [Fact]
        public async Task GetPrivateParkingSpotsInformationTest()
        {
            // Arrange
            double selectedRadius = 100;  // Example selected radius in meters
            double locationLatitude = 51.0661481;
            double locationLongitude = -114.0997;

            _viewModel.SelectedRadius = selectedRadius;
            _viewModel.ShowPublicParking = true;
            _viewModel.ShowAvailableParking = true;
            _viewModel.ShowPrivateParking = true;
            _viewModel.LocationLatitude = locationLatitude;
            _viewModel.LocationLongitude = locationLongitude;

            var privateParkingDataInside = new PrivateParking
            {
                Id = "mapPointInside",
                CompanyName = "A",
                Address = "AA",
                Latitude = 51.074223986452735,
                Longitude = -114.09974241563248,
                CreatedBy = "Inside Spot",
                ParkingInfo = new ParkingInfo()
                {
                    Fee = 10,
                    LimitedHour = 2
                }
            };

            // Act
            _viewModel.LoadedEventCommand.Execute(null);

            await Task.Delay(2000);

            _viewModel.UpdateRangeCommand.Execute(null);
            _viewModel.LoadedCommand.Execute(null);

            await _viewModel.ShowPrivateParkingBottomSheet(privateParkingDataInside);

            await Task.Delay(2000);

            // Assert
            await Task.Delay(5000);
            Assert.True(_dialogService.ShowBottomSheetCalled, "ShowBottomSheet should have been called.");
        }

        [Fact]
        public async Task GetPrivateParkingSpotsInformationNoDataTest()
        {
            // Arrange
            double selectedRadius = 1000;  // Example selected radius in meters
            double locationLatitude = 51.0661481;
            double locationLongitude = -114.0997;

            _viewModel.SelectedRadius = selectedRadius;
            _viewModel.ShowPublicParking = true;
            _viewModel.ShowAvailableParking = true;
            _viewModel.ShowPrivateParking = true;
            _viewModel.LocationLatitude = locationLatitude;
            _viewModel.LocationLongitude = locationLongitude;

            // Act
            _viewModel.LoadedEventCommand.Execute(null);

            await Task.Delay(2000);

            _viewModel.UpdateRangeCommand.Execute(null);
            _viewModel.LoadedCommand.Execute(null);

            await _viewModel.ShowPrivateParkingBottomSheet(null);

            await Task.Delay(5000);

            // Assert
            Assert.True(_dialogService.ShowBottomSheetCalled, "ShowBottomSheet should have been called.");
        }
    }

    public class FakeDialogService : IDialogService
    {
        public bool ShowAlertAsyncCalled { get; private set; }
        public bool ShowBottomSheetCalled { get; private set; }
        public bool DismissBottomSheetAsyncCalled { get; private set; }

        public Task ShowAlertAsync(string title, string message, string cancel = "OK")
        {
            ShowAlertAsyncCalled = true;
            return Task.CompletedTask;
        }

        public Task ShowBottomSheet(string address, string parkingFee, string limitHour, string availability, bool ShowButton, string lat, string lng)
        {
            ShowBottomSheetCalled = true;
            return Task.CompletedTask;
        }

        public Task DismissBottomSheetAsync()
        {
            DismissBottomSheetAsyncCalled = true;
            return Task.CompletedTask;
        }
    }
}

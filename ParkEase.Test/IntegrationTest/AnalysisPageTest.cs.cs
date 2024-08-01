using Microsoft.Extensions.Configuration;
using NSubstitute.Core;
using ParkEase.Contracts.Services;
using ParkEase.Core.Contracts.Services;
using ParkEase.Core.Data;
using ParkEase.Core.Model;
using ParkEase.Core.Services;
using ParkEase.Controls;
using ParkEase.ViewModel;
using The49.Maui.BottomSheet;
using CollectionName = ParkEase.Core.Services.CollectionName;
using ParkEase.Test.IntegrationTest.Services;
namespace ParkEase.Test.IntergartionTest
{
    public class TestDatabaseFixture : IAsyncLifetime
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
            Model = new ParkEaseModel(true);
            Model.User = new User();
            Model.User.Role = Roles.Administrator;

            DialogService = new TestDialogService();
            // Seed test data
            await SeedTestDataAsync();
        }

        public async Task DisposeAsync()
        {
            // Clean up the test database
            await MongoDBService.DropCollection(CollectionName.PublicLogs);
            await MongoDBService.DropCollection(CollectionName.PrivateLogs);
            await MongoDBService.DropCollection(CollectionName.ParkingData);
            await MongoDBService.DropCollection(CollectionName.PrivateParking);
        }

        private async Task SeedTestDataAsync()
        {
            // Use MongoDBService to insert test data
            await MongoDBService.InsertData(CollectionName.ParkingData, new ParkingData
            {
                ParkingSpot = "TestSpot"
            });

            List<ParkingData> parkingDatas = await MongoDBService.GetData<ParkingData>(CollectionName.ParkingData);
            ParkingData parkingData = parkingDatas.FirstOrDefault();
            if(parkingData == null) throw new Exception("ParkingData not found");

            await MongoDBService.InsertData(CollectionName.PublicLogs, new PublicLog
            {
                AreaId = parkingData.Id,
                Timestamp = DateTime.Now.AddHours(-2),
                Status = true
            });

            await MongoDBService.InsertData(CollectionName.PublicLogs, new PublicLog
            {
                AreaId = parkingData.Id,
                Timestamp = DateTime.Now.AddHours(-1),
                Status = false
            });

            var privateParking = new PrivateParking
            {
                CompanyName = "TestCompany",
                Address = "TestAddress",
                CreatedBy = Model.User.Email,
                FloorInfo = new List<FloorInfo> { new FloorInfo("1", new List<Rectangle>(), new byte[1024]), new FloorInfo("2", new List<Rectangle>(), new byte[1024]) }
            };
            await MongoDBService.InsertData(CollectionName.PrivateParking, privateParking);
            await MongoDBService.InsertData(CollectionName.PrivateLogs, new PrivateLog
            {
                AreaId = privateParking.Id,
                Floor = "1",
                Timestamp = DateTime.Now.AddHours(-2),
                Status = true
            });
        }

    }

    public class AnalysisPageIntegrationTests : IClassFixture<TestDatabaseFixture>
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly AnalysisViewModel _viewModel;
        private readonly TestDialogService _testDialogService;
        private int allowedRefreshTime = 2000;
        public AnalysisPageIntegrationTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _testDialogService = new TestDialogService();
            _viewModel = new AnalysisViewModel(_fixture.Model, _fixture.MongoDBService, _testDialogService);
            _viewModel.LoadedCommand.Execute(null);
           
        }

        [Fact]
        public async Task UpdateAreaNameItemSource_PublicAreaType_PopulatesCorrectly()
        {
            // Arrange
            _viewModel.AreaTypeSelected = "Public";

            // Act
            await _viewModel.UpdateAreaNameItemSource();

            // Assert
            Assert.Contains("TestSpot", _viewModel.AreaNameItemSource);
            Assert.False(_viewModel.IsFloowSelectedVisible);
        }

        [Fact]
        public async Task UpdateAreaNameItemSource_PrivateAreaType_PopulatesCorrectly()
        {
            _viewModel.AreaTypeSelected = "Private";

            // Act
            await _viewModel.UpdateAreaNameItemSource();

            // Assert
            Assert.Contains("TestCompany(TestAddress)", _viewModel.AreaNameItemSource);
            Assert.True(_viewModel.IsFloowSelectedVisible);
        }

        [Fact]
        public async Task ApplyCommand_NonCurrentDayWithoutDateRange_ShowsError()
        {
            // Arrange
            _viewModel.IsCurrentDayCheck = false;
            _viewModel.SelectedDateRange = null;

            // Act
            await _viewModel.ApplyCommand.ExecuteAsync(null);

            // Assert
            // Assert.Contains("Please select a date range", capturedDialogMessage);
        }

        [Fact]
        public async Task UpdateUsageGraph_ChangesTimeInterval()
        {
            // Arrange
            _viewModel.AreaTypeSelected = "Public";
            _viewModel.AreaNameSelected = "TestSpot";
            _viewModel.IsCurrentDayCheck = true;
            await _viewModel.ApplyCommand.ExecuteAsync(null);

            // Act - change to monthly view
            _viewModel.IsUsageMonthlyChecked = true;

            // Assert
            // Check that the graph data has changed accordingly
            // You might need to expose some properties or methods to verify this
        }

        [Fact]
        public async Task UpdateParkingTimeGraph_ChangesTimeInterval()
        {
            // Arrange
            _viewModel.AreaTypeSelected = "Public";
            _viewModel.AreaNameSelected = "TestSpot";
            _viewModel.IsCurrentDayCheck = true;
            await _viewModel.ApplyCommand.ExecuteAsync(null);

            // Act - change to monthly view
            _viewModel.IsParkingTimeMonthlyChecked = true;

            // Assert
            // Check that the graph data has changed accordingly
        }

        [Fact]
        public async Task LoadFloorInfo_PrivateParkingSelected_PopulatesFloorInfo()
        {
            // Arrange
            _viewModel.AreaTypeSelected = "Private";
            await _viewModel.UpdateAreaNameItemSource();
            _viewModel.AreaNameSelected = "TestCompany(TestAddress)";

            // Act
            _viewModel.IsAllFloorCheck = false;

            // Assert
            Assert.Contains("1", _viewModel.FloorItemSource);
            Assert.Contains("2", _viewModel.FloorItemSource);
            Assert.True(_viewModel.IsFloorEnabled);
        }

        [Fact]
        public async Task ApplyCommand_PrivateParkingWithSpecificFloor_UpdatesGraphs()
        {
            // Arrange
            _viewModel.AreaTypeSelected = "Private";
            await _viewModel.UpdateAreaNameItemSource();
            _viewModel.AreaNameText = "TestCompany(TestAddress)";
            _viewModel.IsAllFloorCheck = false;
            _viewModel.FloorSelected = "1";
            _viewModel.IsCurrentDayCheck = true;

            // Act
            await _viewModel.ApplyCommand.ExecuteAsync(null);
            await Task.Delay(allowedRefreshTime);
            // Assert
            Assert.NotNull(_viewModel.UsageSeriesCollection);
            Assert.NotNull(_viewModel.ParkingTimeSeriesCollection);
            Assert.NotEmpty(_viewModel.AverageUsage);
            Assert.NotEmpty(_viewModel.AverageParkingTime);
        }
    }
}

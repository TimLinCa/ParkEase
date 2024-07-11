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
            DialogService = new DialogService();
            Model = new ParkEaseModel(true);
            Model.User = new User();
            Model.User.Role = Roles.Administrator;
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
        }

    }

    public class AnalysisPageIntegrationTests : IClassFixture<TestDatabaseFixture>
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly AnalysisViewModel _viewModel;

        public AnalysisPageIntegrationTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _viewModel = new AnalysisViewModel(_fixture.Model, _fixture.MongoDBService, _fixture.DialogService);
            _viewModel.LoadedCommand.Execute(null);
        }

        [Fact]
        public async Task ApplyCommand_PublicParkingData_UpdatesGraphs()
        {
            // Arrange
            _viewModel.AreaTypeSelected = "Public";
            _viewModel.AreaNameSelected = "TestSpot";
            _viewModel.IsCurrentDayCheck = true;
            _viewModel.StartTime = new TimeSpan(0, 0, 0);
            _viewModel.EndTime = new TimeSpan(23, 59, 59);

            // Act
            await _viewModel.ApplyCommand.ExecuteAsync(null);

            // Assert
            Assert.NotNull(_viewModel.UsageSeriesCollection);
            Assert.NotNull(_viewModel.ParkingTimeSeriesCollection);
            Assert.NotEmpty(_viewModel.AverageUsage);
            Assert.NotEmpty(_viewModel.AverageParkingTime);

            // Additional assertions based on known test data
        }

        // Additional test methods...
    }
}

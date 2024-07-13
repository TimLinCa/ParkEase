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
using Syncfusion.Maui.Core.Carousel;
using ParkEase.Test.IntergartionTest;
using System.Collections.ObjectModel;
using LiveChartsCore.Drawing;
using ParkEase.Messages;
using System.Net;
using CommunityToolkit.Maui.Storage;
namespace ParkEase.Test.IntergartionTest
{
    public class PrivateMapPageTestsFixture : IAsyncLifetime
    {
        public IConfiguration Configuration { get; private set; }
        public MongoDBService MongoDBService { get; private set; }
        public ParkEaseModel Model { get; private set; }
        public IAWSService AWSService { get; private set; }
        public IDialogService DialogService { get; private set; }
        public List<PrivateParking> parkingLotDatas { get; private set; }
        public PrivateMapViewModel ViewModel { get; set; }
        
        IFileSaver fileSaver;


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
            Model = new ParkEaseModel(true)
            {
                User = new User { Role = Roles.User }
            };

            ViewModel = new PrivateMapViewModel(MongoDBService, DialogService, Model, fileSaver);
            // Seed test data
            await SeedTestDataAsync();
        }

        private async Task SeedTestDataAsync()
        {
            var TestSample1 = new PrivateParking
            {
                CompanyName = "First Company",
                Address = "Maple street 123",
                Latitude = 51.064330,
                Longitude = -114.092650,
                CreatedBy = "Testing@test.com",
                ParkingInfo = new ParkingInfo
                {
                    Fee = 5,
                    LimitedHour = 2
                }
            };
            var TestSample2 = new PrivateParking
            {
                CompanyName = "Second Company",
                Address = "CrowFoot 567",
                Latitude = 51.091987,
                Longitude = -114.129066,
                CreatedBy = "Testing@test.com",
                ParkingInfo = new ParkingInfo
                {
                    Fee = 3,
                    LimitedHour = 5
                }
            };


            await MongoDBService.InsertData(CollectionName.PrivateParking, TestSample1);
            await MongoDBService.InsertData(CollectionName.PrivateParking, TestSample2);
            parkingLotDatas = await MongoDBService.GetData<PrivateParking>(CollectionName.PrivateParking);
            
            //DataService.SetId(userLocation);
            await ViewModel.LoadedCommand.ExecuteAsync(null);

        }

        public async Task DisposeAsync()
        {
            // Clean up the test database
            await MongoDBService.DropCollection(CollectionName.PrivateParking);
        }
    }

    public class PrivateMapPageIntegrationTests : IClassFixture<PrivateMapPageTestsFixture>
    {
        private readonly PrivateMapPageTestsFixture _fixture;
        private readonly PrivateMapViewModel _viewModel;

        private readonly int allowedRefreshTime = 2000;

        public PrivateMapPageIntegrationTests(PrivateMapPageTestsFixture fixture)
        {
            _fixture = fixture;
            _viewModel = fixture.ViewModel;
            InitializeViewModel();
        }
        private void InitializeViewModel()
        {
            var parkingLotDatas = _fixture.parkingLotDatas;

        }
        [Fact]
        public async Task DisplayAddressTest()
        {
            var privateParkingData = await _fixture.MongoDBService.GetData<PrivateParking>(CollectionName.PrivateParking);
            var test1Id = privateParkingData.Where(data => data.Address == "Maple street 123").FirstOrDefault().Id;
            var test1Address = privateParkingData.Where(data => data.Address == "Maple street 123").FirstOrDefault().Address;
            var test1Fee = privateParkingData.Where(data => data.Address == "Maple street 123").FirstOrDefault().ParkingInfo.Fee;
            var test1LimitedHour = privateParkingData.Where(data => data.Address == "Maple street 123").FirstOrDefault().ParkingInfo.LimitedHour;


            DataService.SetId(test1Id);
            await _viewModel.LoadedCommand.ExecuteAsync(null);
            Assert.Equal(test1Address, _viewModel.Address);
            Assert.Equal(test1Fee, _viewModel.Fee);
            Assert.Equal(test1LimitedHour.ToString(), _viewModel.LimitHour);

            var test2Id = privateParkingData.Where(data => data.Address == "CrowFoot 567").FirstOrDefault().Id;
            var test2Address = privateParkingData.Where(data => data.Address == "CrowFoot 567").FirstOrDefault().Address;
            var test2Fee = privateParkingData.Where(data => data.Address == "CrowFoot 567").FirstOrDefault().ParkingInfo.Fee;
            var test2LimitedHour = privateParkingData.Where(data => data.Address == "CrowFoot 567").FirstOrDefault().ParkingInfo.LimitedHour;


            DataService.SetId(test2Id);
            await _viewModel.LoadedCommand.ExecuteAsync(null);
            Assert.Equal(test2Address, _viewModel.Address);
            Assert.Equal(test2Fee, _viewModel.Fee);
            Assert.Equal(test2LimitedHour.ToString(), _viewModel.LimitHour);

        }
    }

}

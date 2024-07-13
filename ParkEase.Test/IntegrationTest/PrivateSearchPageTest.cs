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
namespace ParkEase.Test.IntergartionTest
{
    public class PrivateSearchPageTestsFixture : IAsyncLifetime
    {
        public IConfiguration Configuration { get; private set; }
        public MongoDBService MongoDBService { get; private set; }
        public ParkEaseModel Model { get; private set; }
        public IAWSService AWSService { get; private set; }
        public IDialogService DialogService { get; private set; }
        public List<PrivateParking> parkingLotDatas { get; private set; }
        public PrivateSearchViewModel ViewModel { get; set; }

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

            ViewModel = new PrivateSearchViewModel(MongoDBService, DialogService, Model);
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
            var TestSample3 = new PrivateParking
            {
                CompanyName = "Third Company",
                Address = "Dragon 17",
                Latitude = 51.118502,
                Longitude = -114.067337,
                CreatedBy = "Testing@test.com",
                ParkingInfo = new ParkingInfo
                {
                    Fee = 6,
                    LimitedHour = 2
                }
            };
            var TestSample4 = new PrivateParking
            {
                CompanyName = "Fourth Company",
                Address = "Church 20",
                Latitude = 51.070760,
                Longitude = -114.100040,
                CreatedBy = "Testing@test.com",
                ParkingInfo = new ParkingInfo
                {
                    Fee = 6,
                    LimitedHour = 2
                }
            };

            await MongoDBService.InsertData(CollectionName.PrivateParking, TestSample1);
            await MongoDBService.InsertData(CollectionName.PrivateParking, TestSample2);
            await MongoDBService.InsertData(CollectionName.PrivateParking, TestSample3);
            await MongoDBService.InsertData(CollectionName.PrivateParking, TestSample4);
            parkingLotDatas = await MongoDBService.GetData<PrivateParking>(CollectionName.PrivateParking);
            Location userLocation = new Location(51.067970, -114.098839);
            DataService.SetLocation(userLocation);
            await ViewModel.LoadedCommand.ExecuteAsync(null);

        }

        public async Task DisposeAsync()
        {
            // Clean up the test database
            await MongoDBService.DropCollection(CollectionName.PrivateParking);
        }
    }

    public class PrivateSearchPageIntegrationTests : IClassFixture<PrivateSearchPageTestsFixture>
    {
        private readonly PrivateSearchPageTestsFixture _fixture;
        private readonly PrivateSearchViewModel _viewModel;

        private readonly int allowedRefreshTime = 2000;

        public PrivateSearchPageIntegrationTests(PrivateSearchPageTestsFixture fixture)
        {
            _fixture = fixture;
            _viewModel = fixture.ViewModel;
            InitializeViewModel();
        }
        private void InitializeViewModel()
        {
            var parkingLotDatas = _fixture.parkingLotDatas;

            // Initialize the list if it's null
            if (_viewModel.addressDistanceFullList == null)
            {
                _viewModel.addressDistanceFullList = new List<AddressDistance>();
            }

       
        }

        [Fact]
        public async Task DisplayAddressTest()
        {
            // Assert
            Assert.Contains(_viewModel.addressDistanceFullList, ad => ad.Address == "Maple street 123");
            Assert.Contains(_viewModel.addressDistanceFullList, ad => ad.Address == "CrowFoot 567");
            Assert.Contains(_viewModel.addressDistanceFullList, ad => ad.Address == "Dragon 17");
            Assert.Contains(_viewModel.addressDistanceFullList, ad => ad.Address == "Church 20");
        }

        [Fact]
        public async Task SearchBarTest()
        {
            // Arrange


            // Act
            var parkingLotDatas = await _fixture.MongoDBService.GetData<PrivateParking>(CollectionName.PrivateParking);

            _viewModel.AddressDistanceList = new ObservableCollection<AddressDistance>(_viewModel.addressDistanceFullList);

            // Simulate user input and filter the list
            _viewModel.SearchText = "Maple";

            foreach (var row in _viewModel.AddressDistanceList)
            {
                Assert.Equal("Maple street 123", row.Address);
            }
            _viewModel.SearchText = "17";
            foreach (var row in _viewModel.AddressDistanceList)
            {
                Assert.Equal("Dragon 17", row.Address);
            }
            Assert.DoesNotContain(_viewModel.AddressDistanceList, a => a.Address == "CrowFoot 567");

            _viewModel.SearchText = "ajbkfjbifj";
            Assert.Equal(_viewModel.AddressDistanceList.Count, 0);
            Assert.Equal(_viewModel.AddressMessage, "No matching addresses found");
            

        }

        [Fact]
        public async Task SelectAddressTest()
        {
            // user selects address
            var privateParkingData = await _fixture.MongoDBService.GetData<PrivateParking>(CollectionName.PrivateParking);
            var parkingData = privateParkingData.Where(data => data.Address == "Maple street 123").First();

            var selectedAddress = _viewModel.addressDistanceFullList
                            .FirstOrDefault(ad => ad.Address == "Maple street 123");
            _viewModel.SelectedAddress = selectedAddress;
            await Task.Delay(allowedRefreshTime);
            Assert.Equal(_viewModel.IdResult, parkingData.Id);
        }
        [Fact]
        public async Task AddressSortTest()
        {
            var privateParkingData = await _fixture.MongoDBService.GetData<PrivateParking>(CollectionName.PrivateParking);
            var firstDistance = ConvertDistanceStringToDouble(_viewModel.addressDistanceFullList.First().Distance); 
            foreach (var row in _viewModel.AddressDistanceList)
            {
                var distance = ConvertDistanceStringToDouble(row.Distance);
                Assert.True(firstDistance <= distance);

            }

            double ConvertDistanceStringToDouble(string distanceString)
            {
                // Remove the " km" part
                string numericString = distanceString.Replace(" km", "").Trim();

                // Convert the numeric string to double
                if (double.TryParse(numericString, out double distance))
                {
                    return distance;
                }
                else
                {
                    throw new FormatException("The input string is not in a correct format.");
                }
            }
        }
    }
    
}

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
using System.Diagnostics;
using System.Linq;



namespace ParkEase.Test.UnitTest
{
    public class PrivateSearchPageTest
    {
        private readonly PrivateSearchViewModel viewModel;
        private readonly Mock<IMongoDBService> mongoDBService;
        private readonly Mock<IDialogService> dialogService;
        private readonly ParkEaseModel parkEaseModel;
        Location userLocation = new Location(51.067970, -114.098839);


        public PrivateSearchPageTest()
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
            viewModel = new PrivateSearchViewModel(mongoDBService.Object, dialogService.Object, parkEaseModel);
        }

        //helper
        double CoordinateDistance(double latitude, double longtitude)
        {
            Location newLocation = new Location(latitude, longtitude);
            double distance = Location.CalculateDistance(userLocation, newLocation, DistanceUnits.Kilometers);
            return distance;
        }

        [Fact]
        public async Task DisplayAddressTest()
        {
            // Arrange
            var sampleParkingLots = new List<PrivateParking>
            {
                new PrivateParking { Address = "Test Address 1", Latitude = 51.064330, Longitude = -114.092650 },
                new PrivateParking { Address = "Test Address 2", Latitude = 51.091987, Longitude = -114.129066 },
                new PrivateParking { Address = "Test Address 3", Latitude = 51.118502, Longitude = -114.067337 }
            };
            mongoDBService.Setup(m => m.GetData<PrivateParking>(It.IsAny<string>())).ReturnsAsync(sampleParkingLots);

            // Act
            var parkingLotData = await mongoDBService.Object.GetData<PrivateParking>("PrivateParking");

            viewModel.addressDistanceFullList = parkingLotData.Select(parkingLot => new AddressDistance
            {
                Address = parkingLot.Address,
                Distance = $"{CoordinateDistance(parkingLot.Latitude, parkingLot.Longitude).ToString("F2")} km"
            }).OrderBy(a => a.Distance).ToList();

            // Initialize the list if it's null
            if (viewModel.addressDistanceFullList == null)
            {
                viewModel.addressDistanceFullList = new List<AddressDistance>();
            }


            // Assert
            Assert.Contains(viewModel.addressDistanceFullList, ad => ad.Address == "Test Address 1");
            Assert.Contains(viewModel.addressDistanceFullList, ad => ad.Address == "Test Address 2");
            Assert.Contains(viewModel.addressDistanceFullList, ad => ad.Address == "Test Address 3");

        }

        [Fact]
        public async Task SearchBarTest()
        {
            // Arrange
            var sampleParkingLots = new List<PrivateParking>
            {
                new PrivateParking { Address = "Maple street 123", Latitude = 51.064330, Longitude = -114.092650 },
                new PrivateParking { Address = "CrowFoot 567", Latitude = 51.091987, Longitude = -114.129066 },
                new PrivateParking { Address = "Dragon 17", Latitude = 51.118502, Longitude = -114.067337 }
            };
            mongoDBService.Setup(m => m.GetData<PrivateParking>(It.IsAny<string>())).ReturnsAsync(sampleParkingLots);

            // Act
            var parkingLotData = await mongoDBService.Object.GetData<PrivateParking>("PrivateParking");
            viewModel.addressDistanceFullList = parkingLotData.Select(parkingLot => new AddressDistance
            {
                Address = parkingLot.Address,
                Distance = $"{CoordinateDistance(parkingLot.Latitude, parkingLot.Longitude).ToString("F2")} km"
            }).OrderBy(a => a.Distance).ToList();

            viewModel.AddressDistanceList = new ObservableCollection<AddressDistance>(viewModel.addressDistanceFullList);

            // Simulate user input and filter the list
            viewModel.SearchText = "Maple";

            foreach (var row in viewModel.AddressDistanceList)
            {
                Assert.Equal("Maple street 123", row.Address);
            }
            viewModel.SearchText = "17";
            foreach (var row in viewModel.AddressDistanceList)
            {
                Assert.Equal("Dragon 17", row.Address);
            }

            Assert.DoesNotContain(viewModel.AddressDistanceList, a => a.Address == "CrowFoot 567");

        }



    }
}


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
using ParkEase.Messages;
using System.Net;




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
            var sampleParkingLots = new List<PrivateParking>
            {
                new PrivateParking { Address = "Test Address 1", Latitude = 51.064330, Longitude = -114.092650 },
                new PrivateParking { Address = "Test Address 2", Latitude = 51.091987, Longitude = -114.129066 },
                new PrivateParking { Address = "Test Address 3", Latitude = 51.118502, Longitude = -114.067337 }
            };
            mongoDBService.Setup(m => m.GetData<PrivateParking>(It.IsAny<string>())).ReturnsAsync(sampleParkingLots);
            DataService.SetLocation(userLocation);
            viewModel = new PrivateSearchViewModel(mongoDBService.Object, dialogService.Object, parkEaseModel);

        }

        [Fact]
        public void CalculateDistanceTest()
        {
            viewModel.LoadedCommand.Execute(null);

            var test1 = new Location(51.064330, -114.092650);
            var test2 = new Location(51.091987, -114.129066);
            var test3 = new Location(51.118502, -114.067337);

            var address1Distance = viewModel.addressDistanceFullList.FirstOrDefault(ad => ad.Address == "Test Address 1").Distance;
            var address2Distance = viewModel.addressDistanceFullList.FirstOrDefault(ad => ad.Address == "Test Address 2").Distance;
            var address3Distance = viewModel.addressDistanceFullList.FirstOrDefault(ad => ad.Address == "Test Address 3").Distance;

            var actural1 = CalculateDistance(userLocation, test1);
            var actural2 = CalculateDistance(userLocation, test2);
            var actural3 = CalculateDistance(userLocation, test3);
            Assert.Equal(address1Distance, actural1);
            Assert.Equal(address2Distance, actural2);
            Assert.Equal(address3Distance, actural3);

            string CalculateDistance(Location location1, Location location2)
            {
                const double EarthRadiusKm = 6371.0;

                double dLat = DegreesToRadians(location2.Latitude - location1.Latitude);
                double dLon = DegreesToRadians(location2.Longitude - location1.Longitude);

                double lat1 = DegreesToRadians(location1.Latitude);
                double lat2 = DegreesToRadians(location2.Latitude);

                double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                           Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1) * Math.Cos(lat2);
                double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
                double distance = EarthRadiusKm * c;
                string result = Math.Round(distance, 2).ToString("F2") + " km";
                return result;
            }

            double DegreesToRadians(double degrees)
            {
                return degrees * Math.PI / 180.0;
            }


        }

        





    }
}


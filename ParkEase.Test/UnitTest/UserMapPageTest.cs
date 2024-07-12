using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Moq;
using ParkEase.ViewModel;
using ParkEase.Controls;
using ParkEase.Services;
using ParkEase.Page;
using Xunit;
using ParkEase.Core.Contracts.Services;
using ParkEase.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;
using ParkEase.Core.Model;
using ParkEase.Core.Data;
using System.Collections.ObjectModel;
using System.Net;
using Syncfusion.Maui.Core.Carousel;
using LiveChartsCore.Drawing;
using MongoDB.Driver;




namespace ParkEase.Test.UnitTest
{
    public class UserMapPageTest
    {
        private readonly UserMapViewModel viewModel;
        private readonly Mock<IMongoDBService> mongoDBService;
        private readonly Mock<IDialogService> dialogService;
        private readonly Mock<IGeocodingService> geocodingService;
        private readonly Mock<IMessagingCenter> messagingCenter;
        private readonly ParkEaseModel parkEaseModel;
        private readonly bool addNewFloorClicked = false;

        public UserMapPageTest()
        {
            mongoDBService = new Mock<IMongoDBService>();
            dialogService = new Mock<IDialogService>();
            geocodingService = new Mock<IGeocodingService>();
            messagingCenter = new Mock<IMessagingCenter>();
            parkEaseModel = new ParkEaseModel
            {
                User = new User
                {
                    Email = "testuser@example.com"
                }
            };
            viewModel = new UserMapViewModel(mongoDBService.Object, dialogService.Object, geocodingService.Object);
        }

        [Fact]
        public async void SearchAddressTest()
        {
            // Arrange
            var searchText = "Test";
            // Define the expected location result
            Location expectedLocation = new Location
            {
                Latitude = 51.074223986352735,
                Longitude = -114.09974241563248
            };

            viewModel.SearchText = searchText;
            geocodingService.Setup(x => x.GetLocationAsync(searchText)).ReturnsAsync(expectedLocation);

            Location location = await geocodingService.Object.GetLocationAsync(searchText);

            // Act
            viewModel.SearchCommand.Execute(null);

            // Assert
            Assert.Equal(viewModel.CenterLocation, location);
        }

        [Fact]
        public void SearchAddressNoLocationTest()
        {
            // Arrange
            var searchText = "No this location";
            Location expectedLocation = null;

            viewModel.SearchText = searchText;
            geocodingService.Setup(x => x.GetLocationAsync(searchText)).ReturnsAsync(expectedLocation);

            // Act
            viewModel.SearchCommand.Execute(null);

            // Assert
            Assert.Equal(viewModel.IsSearchInProgress, true);
            dialogService.Verify(d => d.ShowAlertAsync("Location not found", "Unable to find the specified location.", "OK"), Times.Once);
        }

        [Fact]
        public async void BackToTheCurrentLocationTest()
        {
            // Arrange
            var searchText = "Test";
            Location searchLocation = new Location
            {
                Latitude = 37.422,
                Longitude = -122.084
            };

            viewModel.SearchText = searchText;
            geocodingService.Setup(x => x.GetLocationAsync(searchText)).ReturnsAsync(searchLocation);
            viewModel.SearchCommand.Execute(null);

            var latitude = 51.074223986352735;
            var longitude = -114.09974241563248;
            Location userLocation = new Location
            {
                Latitude = latitude,
                Longitude = longitude
            };

            // var lastCenterLocation = viewModel.CenterLocation;

            viewModel.LocationLatitude = latitude;
            viewModel.LocationLongitude = longitude;

            // Act
            viewModel.BackToCurrentLocationCommand.Execute(null);

            // Assert
            Assert.Equal(viewModel.CenterLocation, userLocation);
            Assert.NotEqual(viewModel.CenterLocation, searchLocation);
        }

        [Fact]
        public async Task ShowPublicParkingTest()
        {
            // Arrange
            double selectedRadius = 500;  // Example selected radius in meters
            double locationLatitude = 51.074223986352735;
            double locationLongitude = -114.09974241563248;

            // Create map lines with points inside and outside the radius
            var mapLineInside = new MapLine(new List<MapPoint> {
                new MapPoint { Lat = "51.074223986352735", Lng = "-114.09974241563248" },
                new MapPoint { Lat = "51.073286968369274", Lng = "-114.09974241563248" }
            });
            mapLineInside.Id = "mapLineInside";

            var mapLineOutside = new MapLine(new List<MapPoint> {
                new MapPoint { Lat = "49.012", Lng = "-100.099" },
                new MapPoint { Lat = "48.011", Lng = "-100.099" }
            });
            mapLineOutside.Id = "mapLineOutside";

            viewModel.SelectedRadius = selectedRadius;
            viewModel.ShowPublicParking = true;
            viewModel.LocationLatitude = locationLatitude;
            viewModel.LocationLongitude = locationLongitude;

            viewModel.MapLines = new ObservableCollection<MapLine> { mapLineInside, mapLineOutside };
            viewModel.SelectedMapLine = mapLineInside;  // Select one of the lines

            // Mock MongoDB service to return parking data
            var parkingDataInside = new ParkingData
            {
                Id = mapLineInside.Id,
                ParkingSpot = "Inside Spot",
                ParkingTime = "Mon to Fri: 7am to 6pm",
                ParkingFee = "$2.00 per hour",
                Points = mapLineInside.Points
            };

            var parkingDataOutside = new ParkingData
            {
                Id = mapLineOutside.Id,
                ParkingSpot = "Outside Spot",
                ParkingTime = "Mon to Fri: 7am to 6pm",
                ParkingFee = "$2.00 per hour",
                Points = mapLineOutside.Points
            };

            mongoDBService
                .Setup(m => m.GetData<ParkingData>(It.IsAny<string>()))
                .ReturnsAsync(new List<ParkingData> { parkingDataInside, parkingDataOutside });

            var publicStatusInside = new PublicStatus
            {
                AreaId = mapLineInside.Id,
                Status = true
            };

            var publicStatusOutside = new PublicStatus
            {
                AreaId = mapLineOutside.Id,
                Status = false
            };

            mongoDBService
                .Setup(m => m.GetData<PublicStatus>(It.IsAny<string>()))
                .ReturnsAsync(new List<PublicStatus> { publicStatusInside, publicStatusOutside });

            // Act
            viewModel.LoadedEventCommand.Execute(null);
            viewModel.UpdateRangeCommand.Execute(null);

            viewModel.LoadedCommand.Execute(null);

            await Task.Delay(2000);

            // Assert
            Assert.Equal(selectedRadius / 1000.0, viewModel.Radius);

            Assert.Single(viewModel.MapLines);
            Assert.Equal(viewModel.MapLines[0], mapLineInside);
            Assert.NotEqual(viewModel.MapLines[0], mapLineOutside);
        }

        [Fact]
        public async Task ShowAvailableParkingTest()
        {
            // Arrange
            double selectedRadius = 500;  // Example selected radius in meters
            double locationLatitude = 51.074223986352735;
            double locationLongitude = -114.09974241563248;

            // Create map lines with points inside and outside the radius
            var mapLineInside = new MapLine(new List<MapPoint> {
                new MapPoint { Lat = "51.074223986352735", Lng = "-114.09974241563248" },
                new MapPoint { Lat = "51.073286968369274", Lng = "-114.09974241563248" }
            });
            mapLineInside.Id = "mapLineInside";

            var mapLineInsideA = new MapLine(new List<MapPoint> {
                new MapPoint { Lat = "51.07423", Lng = "-114.09974241563248" },
                new MapPoint { Lat = "51.07327", Lng = "-114.09974241563248" }
            });
            mapLineInsideA.Id = "mapLineInsideA";

            var mapLineOutside = new MapLine(new List<MapPoint> {
                new MapPoint { Lat = "49.012", Lng = "-100.099" },
                new MapPoint { Lat = "48.011", Lng = "-100.099" }
            });
            mapLineOutside.Id = "mapLineOutside";

            viewModel.SelectedRadius = selectedRadius;
            viewModel.ShowPublicParking = true;
            viewModel.ShowAvailableParking = true;
            viewModel.LocationLatitude = locationLatitude;
            viewModel.LocationLongitude = locationLongitude;

            viewModel.MapLines = new ObservableCollection<MapLine> { mapLineInside, mapLineInsideA, mapLineOutside };
            viewModel.SelectedMapLine = mapLineInside;  // Select one of the lines

            // Mock MongoDB service to return parking data
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
                ParkingSpot = "Inside Spot",
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

            mongoDBService
                .Setup(m => m.GetData<ParkingData>(It.IsAny<string>()))
                .ReturnsAsync(new List<ParkingData> { parkingDataInside, parkingDataInsideA, parkingDataOutside });

            var publicStatusInside = new PublicStatus
            {
                AreaId = mapLineInside.Id,
                Status = true
            };

            var publicStatusInsideA = new PublicStatus
            {
                AreaId = mapLineInsideA.Id,
                Status = false
            };

            var publicStatusOutside = new PublicStatus
            {
                AreaId = mapLineOutside.Id,
                Status = false
            };

            mongoDBService
                .Setup(m => m.GetData<PublicStatus>(It.IsAny<string>()))
                .ReturnsAsync(new List<PublicStatus> { publicStatusInside, publicStatusInsideA, publicStatusOutside });

            // Act
            viewModel.LoadedEventCommand.Execute(null);
            viewModel.UpdateRangeCommand.Execute(null);

            viewModel.LoadedCommand.Execute(null);

            await Task.Delay(2000);

            // Assert
            Assert.Equal(selectedRadius / 1000.0, viewModel.Radius);

            Assert.Single(viewModel.MapLines);
            Assert.Equal(viewModel.MapLines[0], mapLineInsideA);
            Assert.NotEqual(viewModel.MapLines[0], mapLineInside);
            Assert.NotEqual(viewModel.MapLines[0], mapLineOutside);
        }

        [Fact]
        public async Task ParkingSpotAvailabilityUpdateTest()
        {
            // Arrange
            double selectedRadius = 500;  // Example selected radius in meters
            double locationLatitude = 51.074223986352735;
            double locationLongitude = -114.09974241563248;

            // Create map lines with points inside and outside the radius
            var mapLineInside = new MapLine(new List<MapPoint> {
                new MapPoint { Lat = "51.074223986352735", Lng = "-114.09974241563248" },
                new MapPoint { Lat = "51.073286968369274", Lng = "-114.09974241563248" }
            });
            mapLineInside.Id = "mapLineInside";

            var mapLineOutside = new MapLine(new List<MapPoint> {
                new MapPoint { Lat = "49.012", Lng = "-100.099" },
                new MapPoint { Lat = "48.011", Lng = "-100.099" }
            });
            mapLineOutside.Id = "mapLineOutside";

            viewModel.SelectedRadius = selectedRadius;
            viewModel.ShowPublicParking = true;
            viewModel.LocationLatitude = locationLatitude;
            viewModel.LocationLongitude = locationLongitude;

            // Mock MongoDB service to return parking data
            var parkingDataInside = new ParkingData
            {
                Id = mapLineInside.Id,
                ParkingSpot = "Inside Spot",
                ParkingTime = "Mon to Fri: 7am to 6pm",
                ParkingFee = "$2.00 per hour",
                Points = mapLineInside.Points
            };

            var parkingDataOutside = new ParkingData
            {
                Id = mapLineOutside.Id,
                ParkingSpot = "Outside Spot",
                ParkingTime = "Mon to Fri: 7am to 6pm",
                ParkingFee = "$2.00 per hour",
                Points = mapLineOutside.Points
            };

            mongoDBService
                .Setup(m => m.GetData<ParkingData>(It.IsAny<string>()))
                .ReturnsAsync(new List<ParkingData> { parkingDataInside, parkingDataOutside });

            var publicStatusInside = new PublicStatus
            {
                AreaId = mapLineInside.Id,
                Status = true
            };

            var publicStatusOutside = new PublicStatus
            {
                AreaId = mapLineOutside.Id,
                Status = false
            };

            mongoDBService
                .Setup(m => m.GetData<PublicStatus>(It.IsAny<string>()))
                .ReturnsAsync(new List<PublicStatus> { publicStatusInside, publicStatusOutside });

            // Act
            viewModel.LoadedEventCommand.Execute(null);
            viewModel.UpdateRangeCommand.Execute(null);

            viewModel.LoadedCommand.Execute(null);

            await Task.Delay(2000);

            // Assert
            Assert.Single(viewModel.MapLines);
            Assert.Equal(viewModel.MapLines[0], mapLineInside);

            // Add new line
            var mapLineInsideA = new MapLine(new List<MapPoint> {
                new MapPoint { Lat = "51.07423", Lng = "-114.09974241563248" },
                new MapPoint { Lat = "51.07327", Lng = "-114.09974241563248" }
            });
            mapLineInsideA.Id = "mapLineInsideA";

            var parkingDataInsideA = new ParkingData
            {
                Id = mapLineInsideA.Id,
                ParkingSpot = "Inside Spot",
                ParkingTime = "Mon to Fri: 7am to 6pm",
                ParkingFee = "$2.00 per hour",
                Points = mapLineInsideA.Points
            };

            var publicStatusInsideA = new PublicStatus
            {
                AreaId = mapLineInsideA.Id,
                Status = false
            };

            mongoDBService
                .Setup(m => m.GetData<ParkingData>(It.IsAny<string>()))
                .ReturnsAsync(new List<ParkingData> { parkingDataInside, parkingDataInsideA, parkingDataOutside });

            mongoDBService
                .Setup(m => m.GetData<PublicStatus>(It.IsAny<string>()))
                .ReturnsAsync(new List<PublicStatus> { publicStatusInside, publicStatusInsideA, publicStatusOutside });

            viewModel.LoadedEventCommand.Execute(null);
            viewModel.UpdateRangeCommand.Execute(null);
            viewModel.LoadedCommand.Execute(null);
            await Task.Delay(3000);

            // Assert
            Assert.Equal(viewModel.MapLines[0], mapLineInside);
            Assert.Equal(viewModel.MapLines[1], mapLineInsideA);
        }

        [Fact]
        public async void GetParkingSpotsInformationTest()
        {
            // Arrange
            double selectedRadius = 500;  // Example selected radius in meters
            double locationLatitude = 51.074223986352735;
            double locationLongitude = -114.09974241563248;

            // Create map lines with points inside and outside the radius
            var mapLineInside = new MapLine(new List<MapPoint> {
                new MapPoint { Lat = "51.074223986352735", Lng = "-114.09974241563248" },
                new MapPoint { Lat = "51.073286968369274", Lng = "-114.09974241563248" }
            });
            mapLineInside.Id = "mapLineInside";

            var mapLineInsideA = new MapLine(new List<MapPoint> {
                new MapPoint { Lat = "51.07423", Lng = "-114.09974241563248" },
                new MapPoint { Lat = "51.07327", Lng = "-114.09974241563248" }
            });
            mapLineInsideA.Id = "mapLineInsideA";

            var mapLineOutside = new MapLine(new List<MapPoint> {
                new MapPoint { Lat = "49.012", Lng = "-100.099" },
                new MapPoint { Lat = "48.011", Lng = "-100.099" }
            });
            mapLineOutside.Id = "mapLineOutside";

            viewModel.SelectedRadius = selectedRadius;
            viewModel.ShowPublicParking = true;
            viewModel.ShowAvailableParking = true;
            viewModel.LocationLatitude = locationLatitude;
            viewModel.LocationLongitude = locationLongitude;

            viewModel.MapLines = new ObservableCollection<MapLine> { mapLineInside, mapLineInsideA, mapLineOutside };

            // Mock MongoDB service to return parking data
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

            mongoDBService
                .Setup(m => m.GetData<ParkingData>(It.IsAny<string>()))
                .ReturnsAsync(new List<ParkingData> { parkingDataInside, parkingDataInsideA, parkingDataOutside });

            mongoDBService
                .Setup(m => m.GetDataFilter<ParkingData>(It.IsAny<string>(), It.IsAny<FilterDefinition<ParkingData>>()))
                .ReturnsAsync(new List<ParkingData> { parkingDataInside });

            var publicStatusInside = new PublicStatus
            {
                AreaId = mapLineInside.Id,
                Status = true
            };

            var publicStatusInsideA = new PublicStatus
            {
                AreaId = mapLineInsideA.Id,
                Status = false
            };

            var publicStatusOutside = new PublicStatus
            {
                AreaId = mapLineOutside.Id,
                Status = false
            };

            mongoDBService
                .Setup(m => m.GetData<PublicStatus>(It.IsAny<string>()))
                .ReturnsAsync(new List<PublicStatus> { publicStatusInside, publicStatusInsideA, publicStatusOutside });

            // Act
            viewModel.LoadedEventCommand.Execute(null);
            viewModel.UpdateRangeCommand.Execute(null);

            viewModel.LoadedCommand.Execute(null);

            await Task.Delay(2000);

            viewModel.SelectedMapLine = mapLineInside;  // Select one of the lines

            var address = parkingDataInside.ParkingSpot;
            var parkingFee = parkingDataInside.ParkingFee;
            var limitedHour = parkingDataInside.ParkingTime;
            var parkingDataId = parkingDataInside.Id;
            var lat = parkingDataInside.Points[1].Lat;
            var lng = parkingDataInside.Points[1].Lng;

            dialogService.Verify(d => d.ShowBottomSheet(address, parkingFee, limitedHour, $"{0} Available Spots", true, lat, lng), Times.Once);
        }

        [Fact]
        public async void GetParkingSpotsInformationNoParkingDataTest()
        {
            // Arrange
            double selectedRadius = 500;  // Example selected radius in meters
            double locationLatitude = 51.074223986352735;
            double locationLongitude = -114.09974241563248;

            // Create map lines with points inside and outside the radius
            var mapLineInside = new MapLine(new List<MapPoint> {
                new MapPoint { Lat = "51.074223986352735", Lng = "-114.09974241563248" },
                new MapPoint { Lat = "51.073286968369274", Lng = "-114.09974241563248" }
            });
            mapLineInside.Id = "mapLineInside";

            var mapLineInsideA = new MapLine(new List<MapPoint> {
                new MapPoint { Lat = "51.07423", Lng = "-114.09974241563248" },
                new MapPoint { Lat = "51.07327", Lng = "-114.09974241563248" }
            });
            mapLineInsideA.Id = "mapLineInsideA";

            var mapLineOutside = new MapLine(new List<MapPoint> {
                new MapPoint { Lat = "49.012", Lng = "-100.099" },
                new MapPoint { Lat = "48.011", Lng = "-100.099" }
            });
            mapLineOutside.Id = "mapLineOutside";

            viewModel.SelectedRadius = selectedRadius;
            viewModel.ShowPublicParking = true;
            viewModel.ShowAvailableParking = true;
            viewModel.LocationLatitude = locationLatitude;
            viewModel.LocationLongitude = locationLongitude;

            viewModel.MapLines = new ObservableCollection<MapLine> { mapLineInside, mapLineInsideA, mapLineOutside };

            // Mock MongoDB service to return parking data
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

            mongoDBService
                .Setup(m => m.GetData<ParkingData>(It.IsAny<string>()))
                .ReturnsAsync(new List<ParkingData> { parkingDataInside, parkingDataInsideA, parkingDataOutside });

            mongoDBService
                .Setup(m => m.GetDataFilter<ParkingData>(It.IsAny<string>(), It.IsAny<FilterDefinition<ParkingData>>()))
                .ReturnsAsync(new List<ParkingData> { });

            var publicStatusInside = new PublicStatus
            {
                AreaId = mapLineInside.Id,
                Status = true
            };

            var publicStatusInsideA = new PublicStatus
            {
                AreaId = mapLineInsideA.Id,
                Status = false
            };

            var publicStatusOutside = new PublicStatus
            {
                AreaId = mapLineOutside.Id,
                Status = false
            };

            mongoDBService
                .Setup(m => m.GetData<PublicStatus>(It.IsAny<string>()))
                .ReturnsAsync(new List<PublicStatus> { publicStatusInside, publicStatusInsideA, publicStatusOutside });

            // Act
            viewModel.LoadedEventCommand.Execute(null);
            viewModel.UpdateRangeCommand.Execute(null);

            viewModel.LoadedCommand.Execute(null);

            await Task.Delay(2000);

            viewModel.SelectedMapLine = mapLineInside;  // Select one of the lines

            dialogService.Verify(d => d.ShowAlertAsync("No Data Found", "No parking data found for the selected line.", "OK"), Times.Once);
        }

        [Fact]
        public async Task GetPrivateParkingSpotsInformationTest()
        {
            // Arrange
            double selectedRadius = 500;  // Example selected radius in meters
            double locationLatitude = 51.074223986352735;
            double locationLongitude = -114.09974241563248;

            // Create map lines with points inside
            var mapLineInside = new MapLine(new List<MapPoint> {
                new MapPoint { Lat = "51.074223986352735", Lng = "-114.09974241563248" },
                new MapPoint { Lat = "51.073286968369274", Lng = "-114.09974241563248" }
            });
            mapLineInside.Id = "mapLineInside";

            var parkingLineDataInside = new ParkingData
            {
                Id = mapLineInside.Id,
                ParkingSpot = "Inside Spot",
                ParkingTime = "Mon to Fri: 7am to 6pm",
                ParkingFee = "$2.00 per hour",
                Points = mapLineInside.Points
            };

            var mapPointInside = new MapPoint { Lat = "51.074223986352735", Lng = "-114.09974241563248" };
            var mapLineInsideA = new MapPoint { Lat = "51.073286968369274", Lng = "-114.09974241563248" };

            viewModel.SelectedRadius = selectedRadius;
            viewModel.ShowPublicParking = true;
            viewModel.ShowAvailableParking = true;
            viewModel.ShowPrivateParking = true;
            viewModel.LocationLatitude = locationLatitude;
            viewModel.LocationLongitude = locationLongitude;

            var parkingDataInside = new PrivateParking
            {
                Id = "mapLineInside",
                CompanyName = "A",
                Address = "AA",
                Latitude = 51.074223986352735,
                Longitude = -114.09974241563248,
                CreatedBy = "A",
                ParkingInfo = new ParkingInfo()
                {
                    Fee = 10,
                    LimitedHour = 2
                }
            };

            var parkingDataInsideA = new PrivateParking
            {
                Id = "mapLineInsideA",
                CompanyName = "B",
                Address = "BB",
                Latitude = 51.073286968369274,
                Longitude = -114.09974241563248,
                CreatedBy = "B",
                ParkingInfo = new ParkingInfo()
                {
                    Fee = 8,
                    LimitedHour = 3
                }
            };

            mongoDBService
                .Setup(m => m.GetData<PrivateParking>(It.IsAny<string>()))
                .ReturnsAsync(new List<PrivateParking> { parkingDataInside, parkingDataInsideA });

            mongoDBService
                .Setup(m => m.GetData<ParkingData>(It.IsAny<string>()))
                .ReturnsAsync(new List<ParkingData> { parkingLineDataInside });

            var publicStatusInside = new PublicStatus
            {
                AreaId = mapLineInside.Id,
                Status = true
            };

            mongoDBService
                .Setup(m => m.GetData<PublicStatus>(It.IsAny<string>()))
                .ReturnsAsync(new List<PublicStatus> { publicStatusInside });


            var privateStatusInside = new PrivateStatus
            {
                AreaId = "mapLineInside",
                Status = true
            };

            var privateStatusInsideA = new PrivateStatus
            {
                AreaId = "mapLineInsideA",
                Status = false
            };

            mongoDBService
                .Setup(m => m.GetData<PrivateStatus>(It.IsAny<string>()))
                .ReturnsAsync(new List<PrivateStatus> { privateStatusInside, privateStatusInsideA });

            // Act
            viewModel.LoadedEventCommand.Execute(null);
            viewModel.UpdateRangeCommand.Execute(null);

            viewModel.LoadedCommand.Execute(null);

            await viewModel.ShowPrivateParkingBottomSheet(parkingDataInside);

            await Task.Delay(2000);

            // Assert
            dialogService.Verify(d => d.ShowBottomSheet(
                parkingDataInside.Address,
                $"{parkingDataInside.ParkingInfo.Fee:C}/hour",
                $"{parkingDataInside.ParkingInfo.LimitedHour} hours",
                $"{0} Available Spots",
                true,
                parkingDataInside.Latitude.ToString(),
                parkingDataInside.Longitude.ToString()
            ), Times.Once);
        }

        [Fact]
        public async Task GetPrivateParkingSpotsInformationNoDataTest()
        {
            // Arrange
            double selectedRadius = 500;  // Example selected radius in meters
            double locationLatitude = 51.074223986352735;
            double locationLongitude = -114.09974241563248;

            // Create map lines with points inside
            var mapLineInside = new MapLine(new List<MapPoint> {
                new MapPoint { Lat = "51.074223986352735", Lng = "-114.09974241563248" },
                new MapPoint { Lat = "51.073286968369274", Lng = "-114.09974241563248" }
            });
            mapLineInside.Id = "mapLineInside";

            var parkingLineDataInside = new ParkingData
            {
                Id = mapLineInside.Id,
                ParkingSpot = "Inside Spot",
                ParkingTime = "Mon to Fri: 7am to 6pm",
                ParkingFee = "$2.00 per hour",
                Points = mapLineInside.Points
            };

            var mapPointInside = new MapPoint { Lat = "51.074223986352735", Lng = "-114.09974241563248" };
            var mapLineInsideA = new MapPoint { Lat = "51.073286968369274", Lng = "-114.09974241563248" };

            viewModel.SelectedRadius = selectedRadius;
            viewModel.ShowPublicParking = true;
            viewModel.ShowAvailableParking = true;
            viewModel.ShowPrivateParking = true;
            viewModel.LocationLatitude = locationLatitude;
            viewModel.LocationLongitude = locationLongitude;

            var parkingDataInside = new PrivateParking
            {
                Id = "mapLineInside",
                CompanyName = "A",
                Address = "AA",
                Latitude = 51.074223986352735,
                Longitude = -114.09974241563248,
                CreatedBy = "A",
                ParkingInfo = new ParkingInfo()
                {
                    Fee = 10,
                    LimitedHour = 2
                }
            };

            var parkingDataInsideA = new PrivateParking
            {
                Id = "mapLineInsideA",
                CompanyName = "B",
                Address = "BB",
                Latitude = 51.073286968369274,
                Longitude = -114.09974241563248,
                CreatedBy = "B",
                ParkingInfo = new ParkingInfo()
                {
                    Fee = 8,
                    LimitedHour = 3
                }
            };

            mongoDBService
                .Setup(m => m.GetData<PrivateParking>(It.IsAny<string>()))
                .ReturnsAsync(new List<PrivateParking> { parkingDataInside, parkingDataInsideA });

            mongoDBService
                .Setup(m => m.GetData<ParkingData>(It.IsAny<string>()))
                .ReturnsAsync(new List<ParkingData> { parkingLineDataInside });

            var publicStatusInside = new PublicStatus
            {
                AreaId = mapLineInside.Id,
                Status = true
            };

            mongoDBService
                .Setup(m => m.GetData<PublicStatus>(It.IsAny<string>()))
                .ReturnsAsync(new List<PublicStatus> { publicStatusInside });


            var privateStatusInside = new PrivateStatus
            {
                AreaId = "mapLineInside",
                Status = true
            };

            var privateStatusInsideA = new PrivateStatus
            {
                AreaId = "mapLineInsideA",
                Status = false
            };

            mongoDBService
                .Setup(m => m.GetData<PrivateStatus>(It.IsAny<string>()))
                .ReturnsAsync(new List<PrivateStatus> { privateStatusInside, privateStatusInsideA });

            // Act
            viewModel.LoadedEventCommand.Execute(null);
            viewModel.UpdateRangeCommand.Execute(null);

            viewModel.LoadedCommand.Execute(null);

            await viewModel.ShowPrivateParkingBottomSheet(null);

            await Task.Delay(2000);

            // Assert
            dialogService.Verify(d => d.ShowBottomSheet(
                "No Data",
                "N/A",
                "N/A",
                "N/A",
                false,
                "0",
                "0"
            ), Times.Once);
        }
    }
}

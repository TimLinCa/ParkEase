using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using ParkEase.Contracts.Services;
using ParkEase.Core.Contracts.Services;
using ParkEase.Core.Model;
using ParkEase.Core.Services;
using ParkEase.ViewModel;
using ParkEase.Services;
using MongoDB.Driver;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.Configuration;
using Moq;
using Microsoft.Extensions.Primitives;
using Syncfusion.Maui.Core.Carousel;
using ParkEase.Core.Data;
using System;
using Microsoft.Maui.Platform;
using ParkEase.PerformanceTest.ServiceForTest;
using ParkEase.Controls;
namespace ParkEase.PerformanceTest.Benchmarks
{

    [SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.Mono80)]
    [RPlotExporter]
    [MemoryDiagnoser]
    public class UserMapViewModelBenchmarks
    {
        static readonly Random random = new Random();

        [Params(1, 10)]
        public int NumberOfInstances { get; set; }

        private List<UserMapViewModel> _viewModels;
        private ParkEaseModel _model;
        private IMongoDBService _mongoDBService;
        private IDialogService _dialogService;
        private IGeocodingService _geocodingService;

        [GlobalSetup]
        public async Task Setup()
        {
            // Initialize services
            Console.WriteLine("Setting up...");
            var awsService = new FakeAWSClient();
            _model = new ParkEaseModel();
            _mongoDBService = new MongoDBService(awsService, DevicePlatform.WinUI, true, true);
            _dialogService = new DialogService(); // Use a mock service
            _geocodingService = new GeocodingService();
            _model.User = new User();
            _model.User.Role = Roles.Developer;
            _model.User.Email = "test@gmail.com";
            // Create multiple instances
            _viewModels = new List<UserMapViewModel>();
            for (int i = 0; i < NumberOfInstances; i++)
            {
                UserMapViewModel vm = new UserMapViewModel(_mongoDBService, _dialogService, _geocodingService);
                vm.LoadedCommand.Execute(null);
                _viewModels.Add(vm);
            }
            await SeedTestDataAsync();
            Console.WriteLine("Setup complete.");
        }

        [GlobalCleanup]
        public async Task Cleanup()
        {
            // Clean up the test database
            await _mongoDBService.DropCollection(CollectionName.ParkingData);
            await _mongoDBService.DropCollection(CollectionName.PrivateParking);
            await _mongoDBService.DropCollection(CollectionName.PublicStatus);
            await _mongoDBService.DropCollection(CollectionName.PrivateStatus);
        }

        #region UserMap Page
        [Benchmark]
        public async Task SearchCommand()
        {
            var tasks = _viewModels.Select(vm =>
            {
                vm.SearchText = "North Hill Centre";
                return vm.SearchCommand.ExecuteAsync(null);
            });
            await Task.WhenAll(tasks);
        }

        [Benchmark]
        public async Task BackToCurrentLocationCommand()
        {
            var tasks = _viewModels.Select(vm =>
            {
                vm.LocationLatitude = 51.0661481;
                vm.LocationLongitude = -114.0997;
                return vm.BackToCurrentLocationCommand.ExecuteAsync(null);
            });
            await Task.WhenAll(tasks);
        }

        [Benchmark]
        public async Task LoadedCommand()
        {
            var tasks = _viewModels.Select(vm =>
            {
                return vm.LoadedCommand.ExecuteAsync(null);
            });
            await Task.WhenAll(tasks);
        }

        [Benchmark]
        public async Task UnLoadedCommand()
        {
            var tasks = _viewModels.Select(vm =>
            {
                return vm.UnLoadedCommand.ExecuteAsync(null);
            });
            await Task.WhenAll(tasks);
        }

        [Benchmark]
        public async Task UpdateRangeCommand()
        {
            var tasks = _viewModels.Select(vm =>
            {
                vm.LocationLatitude = 51.0661481;
                vm.LocationLongitude = -114.0997;
                vm.SelectedRadius = 1000;
                return vm.UpdateRangeCommand.ExecuteAsync(null);
            });
            await Task.WhenAll(tasks);
        }

        [Benchmark]
        public async Task ClearSavedSpotCommand()
        {
            var tasks = _viewModels.Select(vm =>
            {
                vm.LocationLatitude = 51.0661481;
                vm.LocationLongitude = -114.0997;
                vm.SelectedRadius = 1000;
                return vm.ClearSavedSpotCommand.ExecuteAsync(null);
            });
            await Task.WhenAll(tasks);
        }

        [Benchmark]
        public async Task WalkNavigationCommand()
        {
            var tasks = _viewModels.Select(vm =>
            {
                vm.LocationLatitude = 51.0661481;
                vm.LocationLongitude = -114.0997;
                vm.SelectedRadius = 1000;
                return vm.WalkNavigationCommand.ExecuteAsync(null);
            });
            await Task.WhenAll(tasks);
        }
        #endregion

        private async Task SeedTestDataAsync()
        {
            Console.WriteLine("Seeding test data...");
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
            await _mongoDBService.InsertData(CollectionName.ParkingData, parkingDataInside);
            await _mongoDBService.InsertData(CollectionName.ParkingData, parkingDataInsideA);
            await _mongoDBService.InsertData(CollectionName.ParkingData, parkingDataOutside);

            List<ParkingData> parkingDatas = await _mongoDBService.GetData<ParkingData>(CollectionName.ParkingData);
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
            await _mongoDBService.InsertData(CollectionName.PublicStatus, publicStatusInside);
            await _mongoDBService.InsertData(CollectionName.PublicStatus, publicStatusInsideA);
            await _mongoDBService.InsertData(CollectionName.PublicStatus, publicStatusOutside);

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
            await _mongoDBService.InsertData(CollectionName.PrivateParking, privateParkingDataInside);
            await _mongoDBService.InsertData(CollectionName.PrivateParking, privateParkingDataInsideA);
            List<PrivateParking> privateParkingDatas = await _mongoDBService.GetData<PrivateParking>(CollectionName.PrivateParking);
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
            await _mongoDBService.InsertData(CollectionName.PrivateStatus, privateStatusInside);
            await _mongoDBService.InsertData(CollectionName.PrivateStatus, privateStatusInsideA);
            Console.WriteLine("Test data seeded.");
        }
    }
}


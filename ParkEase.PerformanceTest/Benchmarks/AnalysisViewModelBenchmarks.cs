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
namespace ParkEase.PerformanceTest.Benchmarks
{

    [SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.Mono80)]
    [RPlotExporter]
    [MemoryDiagnoser]
    public class AnalysisViewModelBenchmarks
    {
        static readonly Random random = new Random();

        [Params(1, 10)]
        public int NumberOfInstances { get; set; }

        private List<AnalysisViewModel> _viewModels;
        private ParkEaseModel _model;
        private IMongoDBService _mongoDBService;
        private IDialogService _dialogService;

        [GlobalSetup]
        public async Task Setup()
        {
            // Initialize services
            Console.WriteLine("Setting up...");
            var awsService = new FakeAWSClient();
            _model = new ParkEaseModel();
            _mongoDBService = new MongoDBService(awsService, DevicePlatform.WinUI, true, true);
            _dialogService = new DialogService(); // Use a mock service
            _model.User = new User();
            _model.User.Role = Roles.Developer;
            _model.User.Email = "test@gmail.com";
            // Create multiple instances
            _viewModels = new List<AnalysisViewModel>();
            for (int i = 0; i < NumberOfInstances; i++)
            {
                AnalysisViewModel vm = new AnalysisViewModel(_model, _mongoDBService, _dialogService);
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
            await _mongoDBService.DropCollection(CollectionName.PublicLogs);
            await _mongoDBService.DropCollection(CollectionName.PrivateLogs);
            await _mongoDBService.DropCollection(CollectionName.ParkingData);
            await _mongoDBService.DropCollection(CollectionName.PrivateParking);
        }

        #region Analysis Page
        [Benchmark]
        public async Task ConcurrentPublicApplyCommand()
        {
            var tasks = _viewModels.Select(vm =>
            {
                vm.AreaTypeSelected = "Public";
                vm.AreaNameSelected = "TestSpot";
                vm.AreaNameText = "TestSpot";
                vm.IsCurrentDayCheck = true;
                vm.IsAllDayCheck = true;
                return vm.ApplyCommand.ExecuteAsync(null);
            });
            await Task.WhenAll(tasks);
        }

        [Benchmark]
        public async Task ConcurrentPrivateApplyCommand()
        {
            var tasks = _viewModels.Select(vm =>
            {
                vm.AreaTypeSelected = "Private";
                vm.AreaNameSelected = "TestCompany(TestAddress)";
                vm.AreaNameText = "TestCompany(TestAddress)";
                vm.FloorSelected = "Ground";
                vm.IsCurrentDayCheck = true;
                vm.IsAllDayCheck = true;
                return vm.ApplyCommand.ExecuteAsync(null);
            });
            await Task.WhenAll(tasks);
        }
        #endregion

        private async Task SeedTestDataAsync()
        {
            Console.WriteLine("Seeding test data...");
            await _mongoDBService.DropCollection(CollectionName.PublicLogs);
            await _mongoDBService.DropCollection(CollectionName.PrivateLogs);
            await _mongoDBService.DropCollection(CollectionName.ParkingData);
            await _mongoDBService.DropCollection(CollectionName.PrivateParking);
            var startDate = new DateTime(2024, 7, 1);
            var endDate = new DateTime(2024, 7, 15);
            await _mongoDBService.InsertData(CollectionName.ParkingData, new ParkingData
            {
                ParkingSpot = "TestSpot"
            });

            List<ParkingData> parkingDatas = await _mongoDBService.GetData<ParkingData>(CollectionName.ParkingData);
            ParkingData parkingData = parkingDatas.FirstOrDefault();
            var publicLogs = GeneratePublicMonthData(startDate, endDate, 8, parkingData.Id);
            await _mongoDBService.InsertMany(CollectionName.PublicLogs, publicLogs);

            var privateParking = new PrivateParking
            {
                CompanyName = "TestCompany",
                Address = "TestAddress",
                CreatedBy = "test@gmail.com",
                FloorInfo = new List<FloorInfo> { new FloorInfo("Ground", new List<Rectangle>(), new byte[1024]), new FloorInfo("2", new List<Rectangle>(), new byte[1024]) }
            };
            await _mongoDBService.InsertData(CollectionName.PrivateParking, privateParking);
            List<PrivateParking> privateParks = await _mongoDBService.GetData<PrivateParking>(CollectionName.PrivateParking);
            PrivateParking privatePark = privateParks.FirstOrDefault();
            var privateLogs = GeneratePrivateMonthData(startDate, endDate, 10, privatePark.Id);
            await _mongoDBService.InsertMany(CollectionName.PrivateLogs, privateLogs);
            Console.WriteLine("Test data seeded.");
        }

        static List<PublicLog> GeneratePublicMonthData(DateTime startDate, DateTime endDate, int lotIdNumber, string areaId)
        {
            var data = new List<PublicLog>();

            for (int lotId = 1; lotId <= lotIdNumber; lotId++)
            {
                DateTime currentTime = startDate;

                while (currentTime < endDate)
                {
                    int eventsPerDay = random.Next(3, 9); // 3 to 8 events per day
                    for (int i = 0; i < eventsPerDay; i++)
                    {
                        var arrivalTimeInterval = TimeSpan.FromHours(24 / eventsPerDay);
                        var arrivalTime = GenerateTimestamp(currentTime, currentTime + arrivalTimeInterval);
                        var parkingDuration = TimeSpan.FromMinutes(random.Next(30, 481)); // 0.5 to 4 hours
                        var departureTime = arrivalTime + parkingDuration;

                        // Ensure departure time doesn't go beyond the end of the month
                        if (departureTime > endDate)
                        {
                            departureTime = endDate;
                        }

                        // Arrival (status true)
                        data.Add(CreatePulbicParkingEvent(areaId, lotId, true, arrivalTime));

                        // Departure (status false)
                        data.Add(CreatePulbicParkingEvent(areaId, lotId, false, departureTime));

                        currentTime = departureTime;
                    }

                    // Move to the next day if we haven't already
                    if (currentTime.Date == startDate.Date)
                    {
                        currentTime = currentTime.Date.AddDays(1);
                    }
                }
            }

            return data.OrderBy(e => e.Timestamp).ToList();
        }

        static List<PrivateLog> GeneratePrivateMonthData(DateTime startDate, DateTime endDate, int lotIdNumber, string areaId)
        {
            var data = new List<PrivateLog>();

            for (int lotId = 1; lotId <= lotIdNumber; lotId++)
            {
                DateTime currentTime = startDate;

                while (currentTime < endDate)
                {
                    int eventsPerDay = random.Next(3, 9); // 3 to 8 events per day
                    for (int i = 0; i < eventsPerDay; i++)
                    {
                        var arrivalTimeInterval = TimeSpan.FromHours(24 / eventsPerDay);
                        var arrivalTime = GenerateTimestamp(currentTime, currentTime + arrivalTimeInterval);
                        var parkingDuration = TimeSpan.FromMinutes(random.Next(30, 481)); // 0.5 to 4 hours
                        var departureTime = arrivalTime + parkingDuration;

                        // Ensure departure time doesn't go beyond the end of the month
                        if (departureTime > endDate)
                        {
                            departureTime = endDate;
                        }

                        // Arrival (status true)
                        data.Add(CreatePrivateLog(lotId, true, arrivalTime, areaId));

                        // Departure (status false)
                        data.Add(CreatePrivateLog(lotId, false, departureTime, areaId));

                        currentTime = departureTime;
                    }

                    // Move to the next day if we haven't already
                    if (currentTime.Date == startDate.Date)
                    {
                        currentTime = currentTime.Date.AddDays(1);
                    }
                }
            }

            return data.OrderBy(e => e.Timestamp).ToList();
        }

        static DateTime GenerateTimestamp(DateTime start, DateTime end)
        {
            return start.AddSeconds(random.Next(0, (int)(end - start).TotalSeconds));
        }

        static PrivateLog CreatePrivateLog(int lotId, bool status, DateTime timestamp, string areaId)
        {
            return new PrivateLog
            {
                AreaId = areaId,
                Index = lotId - 1,
                CamName = "test",
                LotId = lotId,
                Status = status,
                Floor = "Ground",
                Timestamp = timestamp
            };
        }

        static PublicLog CreatePulbicParkingEvent(string areaId, int lotId, bool status, DateTime timestamp)
        {
            return new PublicLog
            {
                AreaId = areaId,
                Index = lotId - 1,
                CamName = "test",
                Status = status,
                Timestamp = timestamp
            };
        }
    }

    public class FakeConfiguration : IConfiguration
    {
        private readonly Dictionary<string, string> _data;

        string? IConfiguration.this[string key] { get => _data.ContainsKey(key) ? _data[key] : null; set => _data.Add(key, ""); }

        public FakeConfiguration()
        {
            _data = new Dictionary<string, string>
        {
            {"AWSAccessKey", "fakeAccessKey"},
            {"AWSSecretKey", "fakeSecretKey"}
        };
        }

        // Implement other IConfiguration members as needed (return null or empty implementations)
        public IConfigurationSection GetSection(string key) => null;
        public IEnumerable<IConfigurationSection> GetChildren() => new List<IConfigurationSection>();
        public IChangeToken GetReloadToken() => null;
    }

   
}

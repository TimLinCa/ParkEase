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
using ParkEase.Messages;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace ParkEase.PerformanceTest.Benchmarks
{
    [SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.Mono80)]
    [RPlotExporter]
    [MemoryDiagnoser]
    public class PrivateMapViewModelBenchmarks
    {
        [Params(1, 10)]
        public int NumberOfInstances { get; set; }
        private List<PrivateMapViewModel> _viewModels;
        private ParkEaseModel _model;
        private IMongoDBService _mongoDBService;
        private IDialogService _dialogService;

        [GlobalSetup]
        public async Task Setup()
        {
            try
            {
                // Initialize services
                Console.WriteLine("Setting up...");
                var awsService = new FakeAWSClient();
                _model = new ParkEaseModel();

                // Ensure you replace this with your actual MongoDB connection string
                var connectionString = "mongodb://localhost:27017";
            

                // Validate the connection string
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new ArgumentException("MongoDB connection string is not provided.");
                }

                var mongoClientSettings = MongoClientSettings.FromConnectionString(connectionString);
                mongoClientSettings.MaxConnectionPoolSize = 200; // Increase the connection pool size

                _mongoDBService = new MongoDBService(awsService, DevicePlatform.WinUI, true, true);

                _dialogService = new DialogService(); // Use a mock service
                _model.User = new User();
                _model.User.Role = Roles.User;
                _model.User.Email = "test@gmail.com";

                // Create multiple instances
                _viewModels = new List<PrivateMapViewModel>();
                await SeedTestDataAsync();
                var privateParkingData = await _mongoDBService.GetData<PrivateParking>(CollectionName.PrivateParking);
                var IdResult = privateParkingData.Where(data => data.Address == "Maple street 123").FirstOrDefault()?.Id;
                DataService.SetId(IdResult);

                for (int i = 0; i < NumberOfInstances; i++)
                {
                    PrivateMapViewModel vm = new PrivateMapViewModel(_mongoDBService, _dialogService, _model);
                    _viewModels.Add(vm);
                }

                Console.WriteLine("Setup complete.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Setup failed: {ex.Message}");
                throw;
            }
        }

        private async Task SeedTestDataAsync()
        {
            Console.WriteLine("Seeding test data...");
            await _mongoDBService.DropCollection(CollectionName.PrivateParking);
            await _mongoDBService.InsertData(CollectionName.PrivateParking, new PrivateParking
            {
                CompanyName = "First Company",
                Address = "Maple street 123",
                Latitude = 51.064330,
                Longitude = -114.092650,
                CreatedBy = _model.User.Email,
                ParkingInfo = new ParkingInfo { Fee = 5, LimitedHour = 2 },
            });
            await _mongoDBService.InsertData(CollectionName.PrivateParking, new PrivateParking
            {
                CompanyName = "Second Company",
                Address = "CrowFoot 567",
                Latitude = 51.091987,
                Longitude = -114.129066,
                CreatedBy = _model.User.Email,
                ParkingInfo = new ParkingInfo { Fee = 3, LimitedHour = 5 },
            });
            await _mongoDBService.InsertData(CollectionName.PrivateParking, new PrivateParking
            {
                CompanyName = "Third Company",
                Address = "Dragon 17",
                Latitude = 51.118502,
                Longitude = -114.067337,
                CreatedBy = _model.User.Email,
                ParkingInfo = new ParkingInfo { Fee = 6, LimitedHour = 2 },
            });

            Console.WriteLine("Test data seeded.");
        }

        [GlobalCleanup]
        public async Task Cleanup()
        {
            // Clean up the test database
            await _mongoDBService.DropCollection(CollectionName.PrivateParking);
        }

        [Benchmark]
        public async Task PrivateMapExecuteLoadedCommand()
        {
            var tasks = _viewModels.Select(vm => vm.LoadedCommand.ExecuteAsync(null));
            await Task.WhenAll(tasks);
        }

        [Benchmark]
        public async Task PrivateMapShowSelectedMap()
        {
            var tasks = _viewModels.Select(vm => vm.ShowSelectedMap());
            await Task.WhenAll(tasks);
        }

        [Benchmark]
        public async Task PrivateMapLoadParkingData()
        {
            var tasks = _viewModels.Select(vm => vm.LoadParkingData());
            await Task.WhenAll(tasks);
        }

/*        [Benchmark]
        public async Task PrivateMapRun()
        {
            var tasks = _viewModels.Select(vm =>
            {
                var cts = new System.Threading.CancellationTokenSource();
                return vm.Run(cts.Token).ContinueWith(_ => cts.Cancel());
            });
            await Task.WhenAll(tasks);
        }*/
    }

    public static class CommandExtensions
    {
        public static Task ExecuteAsync(this ICommand command, object parameter)
        {
            var tcs = new TaskCompletionSource<object>();
            if (command.CanExecute(parameter))
            {
                command.Execute(parameter);
                tcs.SetResult(null);
            }
            else
            {
                tcs.SetException(new InvalidOperationException("Command cannot be executed."));
            }
            return tcs.Task;
        }
    }
}

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Maui.Controls;
using ParkEase.Contracts.Services;
using ParkEase.Core.Contracts.Services;
using ParkEase.Core.Model;
using ParkEase.Core.Services;
using ParkEase.ViewModel;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
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
using ParkEase.Utilities;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Controls.Maps;
using System.Linq;
using Microsoft.Maui.Controls.Internals;
using ParkEase.Controls;

namespace ParkEase.PerformanceTest.Benchmarks
{
    [SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.Mono80)]
    [RPlotExporter]
    [MemoryDiagnoser]
    public class MapViewModelBenchmarks
    {
        private List<MapViewModel> _viewModels;
        private IMongoDBService _mongoDBService;
        private IDialogService _dialogService;

        [Params(1, 10)]
        public int NumberOfInstances { get; set; }

        [GlobalSetup]
        public async Task Setup()
        {
            Console.WriteLine("Setting up...");

            // Initialize services
            var awsService = new FakeAWSClient(); // Using fake AWS client
            _mongoDBService = new MongoDBService(awsService, DevicePlatform.WinUI, true, true);
            _dialogService = new DialogService();

            _viewModels = new List<MapViewModel>();
            for (int i = 0; i < NumberOfInstances; i++)
            {
                var viewModel = new MapViewModel(_mongoDBService, _dialogService);
                _viewModels.Add(viewModel);
            }

            // Seed test data in MongoDB
            await SeedTestDataAsync();

            Console.WriteLine("Setup complete.");
        }

        [GlobalCleanup]
        public async Task Cleanup()
        {
            // Clean up the test database
            await _mongoDBService.DropCollection(CollectionName.ParkingData);
        }

        [Benchmark]
        public async Task SubmitCommand()
        {
            var tasks = _viewModels.Select(async vm =>
            {
                try
                {
                    vm.ParkingSpot = "Test Spot";
                    vm.SelectedParkingTime = "Mon to Fri: 7am to 6pm";
                    vm.SelectedParkingFee = "$1.50 per hour";
                    vm.SelectedMapLine = new MapLine(new List<MapPoint>
                    {
                        new MapPoint { Lat = "51.0447", Lng = "-114.0719" },
                        new MapPoint { Lat = "51.0447", Lng = "-114.0718" }
                    });

                    if (vm.SubmitCommand is IAsyncRelayCommand asyncCommand)
                    {
                        await asyncCommand.ExecuteAsync(null);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"SubmitCommand exception: {ex.Message}");
                }
            });

            await Task.WhenAll(tasks);
        }

        [Benchmark]
        public void DrawCommand()
        {
            var tasks = _viewModels.Select(vm =>
            {
                try
                {
                    vm.DrawCommand.Execute(null);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"DrawCommand exception: {ex.Message}");
                }
                return Task.CompletedTask;
            });

            Task.WaitAll(tasks.ToArray());
        }

        [Benchmark]
        public async Task DeletedLineCommand()
        {
            var tasks = _viewModels.Select(async vm =>
            {
                try
                {
                    vm.SelectedMapLine = new MapLine(new List<MapPoint>
                    {
                        new MapPoint { Lat = "51.0447", Lng = "-114.0719" },
                        new MapPoint { Lat = "51.0447", Lng = "-114.0718" }
                    });

                    if (vm.DeletedLineCommand is IAsyncRelayCommand asyncCommand)
                    {
                        await asyncCommand.ExecuteAsync(null);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"DeletedLineCommand exception: {ex.Message}");
                }
            });

            await Task.WhenAll(tasks);
        }

        [Benchmark]
        public async Task LoadData()
        {
            var tasks = _viewModels.Select(async vm =>
            {
                try
                {
                    await vm.LoadParkingData(new List<MapPoint>
                    {
                        new MapPoint { Lat = "51.0447", Lng = "-114.0719" },
                        new MapPoint { Lat = "51.0447", Lng = "-114.0718" }
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"LoadData exception: {ex.Message}");
                }
            });

            await Task.WhenAll(tasks);
        }

        private async Task SeedTestDataAsync()
        {
            Console.WriteLine("Seeding test data...");
            await _mongoDBService.DropCollection(CollectionName.ParkingData);

            var testData = new ParkingData
            {
                ParkingSpot = "Test Spot",
                ParkingTime = "Mon to Fri: 7am to 6pm",
                ParkingFee = "$1.50 per hour",
                Points = new List<MapPoint>
                {
                    new MapPoint { Lat = "51.0447", Lng = "-114.0719" },
                    new MapPoint { Lat = "51.0447", Lng = "-114.0718" }
                }
            };

            await _mongoDBService.InsertData(CollectionName.ParkingData, testData);
            Console.WriteLine("Test data seeded.");
        }
    }
}
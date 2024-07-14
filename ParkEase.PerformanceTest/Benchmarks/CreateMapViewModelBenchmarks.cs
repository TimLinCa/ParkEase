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
using ParkEase.Core.Data;
using System;
using ParkEase.PerformanceTest.ServiceForTest;
using System.Reflection;

namespace ParkEase.PerformanceTest.Benchmarks
{
    [SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.Mono80)]
    [RPlotExporter]
    [MemoryDiagnoser]
    public class CreateMapViewModelBenchmarks
    {
        [Params(1, 10)]
        public int NumberOfInstances { get; set; }

        private List<CreateMapViewModel> _viewModels;
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
            _dialogService = new DialogService();
            _model.User = new User { Role = Roles.Developer, Email = "test@gmail.com" };
            // Create multiple instances
            _viewModels = new List<CreateMapViewModel>();
            for (int i = 0; i < NumberOfInstances; i++)
            {
                CreateMapViewModel vm = new CreateMapViewModel(_mongoDBService, _dialogService, _model);
                vm.LoadedCommand.Execute(null);
                _viewModels.Add(vm);
            }

            await SeedTestDataAsync();
            Console.WriteLine("Setup complete.");
        }

        [GlobalCleanup]
        public async Task Cleanup()
        {
            await _mongoDBService.DropCollection(CollectionName.PrivateParking);
        }

        [Benchmark]
        public async Task ConcurrentAddNewFloorCommand()
        {
            var tasks = _viewModels.Select(vm =>
            {
                vm.Floor = "TestFloor";
                return vm.AddNewFloorCommand.ExecuteAsync(null);
            });
            await Task.WhenAll(tasks);
        }

        [Benchmark]
        public async Task ConcurrentSaveFloorInfoCommand()
        {
            var tasks = _viewModels.Select(vm =>
            {
                vm.Floor = "TestFloor";
                SetPrivateField(vm, "addNewFloorClicked", true);
                vm.ListRectangle.Add(new Rectangle(1, new RectF(0, 0, 100, 50)));
                SetPrivateField(vm, "imageData", new byte[] { 0x20, 0x20 });
                return vm.SaveFloorInfoCommand.ExecuteAsync(null);
            });
            await Task.WhenAll(tasks);
        }

        [Benchmark]
        public async Task ConcurrentSubmitCommand()
        {
                var tasks = _viewModels.Select(vm =>
                {
                    SetupViewModelForSubmit(vm);
                    return vm.SubmitCommand.ExecuteAsync(null);
                });
                await Task.WhenAll(tasks);
        }

        private async Task SeedTestDataAsync()
        {
            Console.WriteLine("Seeding test data...");
            await _mongoDBService.DropCollection(CollectionName.PrivateParking);

            // Floor 1
            var rectangles = new List<Rectangle>
            {
                new Rectangle(1, new RectF(10, 10, 50, 50)),
                new Rectangle(2, new RectF(20, 20, 60, 60)),
                new Rectangle(3, new RectF(30, 30, 70, 70))
            };
            var privateParking1 = new PrivateParking
            {
                CompanyName = "TestCompany1",
                Address = "TestAddress1",
                CreatedBy = "test@gmail.com",
                FloorInfo = new List<FloorInfo> { new FloorInfo("Ground", rectangles, new byte[1024]) }
            };
            var privateParking2 = new PrivateParking
            {
                CompanyName = "TestCompany2",
                Address = "TestAddress2",
                CreatedBy = "test@gmail.com",
                FloorInfo = new List<FloorInfo> { new FloorInfo("Ground", rectangles, new byte[1024]), new FloorInfo("2", rectangles, new byte[1024]) }
            };

            // Insert PrivateParking
            await _mongoDBService.InsertData(CollectionName.PrivateParking, privateParking1);
            await _mongoDBService.InsertData(CollectionName.PrivateParking, privateParking2);

            Console.WriteLine("Test data seeded.");
        }

        private void SetupViewModelForSubmit(CreateMapViewModel vm)
        {
            SetPrivateField(vm, "selectedPropertyId", "");
            vm.SelectedAddress = "";
            vm.CompanyName = "NewTestCompany";
            vm.Address = "NewTestAddress";
            SetPrivateField(vm, "latitude", 51.066669);
            SetPrivateField(vm, "longitude", -114.08989);
            vm.Fee = 10;
            vm.LimitHour = 2;
            var rectangles = new List<Rectangle>
            {
                new Rectangle(1, new RectF(10, 10, 50, 50)),
                new Rectangle(2, new RectF(20, 20, 60, 60)),
                new Rectangle(3, new RectF(30, 30, 70, 70))
            };
            var newListFloorInfo = new List<FloorInfo> { new FloorInfo("Ground", rectangles, new byte[1024]) };
            SetPrivateField(vm, "listFloorInfos", newListFloorInfo);
        }

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(obj, value);
            }
            else
            {
                Console.WriteLine($"Field {fieldName} not found");
            }
        }
    }
}
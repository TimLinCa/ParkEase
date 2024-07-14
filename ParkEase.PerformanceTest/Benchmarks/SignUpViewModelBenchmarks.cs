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

namespace ParkEase.PerformanceTest.Benchmarks
{
    [SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.Mono80)]
    [RPlotExporter]
    [MemoryDiagnoser]
    public class SignUpViewModelBenchmarks
    {
        private List<SignUpViewModel> _viewModels;
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

            _viewModels = new List<SignUpViewModel>();
            for (int i = 0; i < NumberOfInstances; i++)
            {
                var viewModel = new SignUpViewModel(_mongoDBService, _dialogService);
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
            await _mongoDBService.DropCollection(CollectionName.Users);
        }

        [Benchmark]
        public async Task SignUpCommand()
        {
            var tasks = _viewModels.Select(async vm =>
            {
                try
                {
                    vm.FullName = "Test User";
                    vm.Email = "test@gmail.com";
                    vm.Password = "Password123";
                    vm.RepeatPassword = "Password123";
                    vm.IsTermsAndConditionsAccepted = true;

                    if (vm.SignUpCommand is IAsyncRelayCommand asyncCommand)
                    {
                        await asyncCommand.ExecuteAsync(null);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"SignUpCommand exception: {ex.Message}");
                }
            });

            await Task.WhenAll(tasks);
        }

        private async Task SeedTestDataAsync()
        {
            Console.WriteLine("Seeding test data...");
            await _mongoDBService.DropCollection(CollectionName.Users);

            var testUser = new User
            {
                Email = "test@gmail.com",
                Password = PasswordHasher.HashPassword("Password123")
            };

            await _mongoDBService.InsertData(CollectionName.Users, testUser);
            Console.WriteLine("Test data seeded.");
        }
    }
}
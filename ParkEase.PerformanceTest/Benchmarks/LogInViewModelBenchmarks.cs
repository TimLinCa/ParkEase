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


[SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.Mono80)]
[RPlotExporter]
[MemoryDiagnoser]
public class LogInViewModelBenchmarks
{
    private List<LogInViewModel> _viewModels;
    private IMongoDBService _mongoDBService;
    private IDialogService _dialogService;
    private ParkEaseModel _model;

    [Params(1, 10)]
    public int NumberOfInstances { get; set; }

    [GlobalSetup]
    public async Task Setup()
    {
        Console.WriteLine("Setting up...");

        // Initialize services
        var awsService = new FakeAWSClient(); // Using fake AWS client
        _model = new ParkEaseModel();
        _mongoDBService = new MongoDBService(awsService, DevicePlatform.WinUI, true, true);
        _dialogService = new DialogService();

        _viewModels = new List<LogInViewModel>();
        for (int i = 0; i < NumberOfInstances; i++)
        {
            var viewModel = new LogInViewModel(_mongoDBService, _dialogService, awsService, _model);
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
    public async Task LogInCommand()
    {
        var tasks = _viewModels.Select(async vm =>
        {
            try
            {
                vm.Email = "test@gmail.com";
                vm.Password = "Password123";

                if (vm.LogInCommand is IAsyncRelayCommand asyncCommand)
                {
                    await asyncCommand.ExecuteAsync(null);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LogInCommand exception: {ex.Message}");
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

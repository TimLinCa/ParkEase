using ParkEase.Services;
using ParkEase.Contracts.Services;
using ParkEase.Core.Model;
using ParkEase.Core.Data;
using CollectionName = ParkEase.Core.Services.CollectionName;
using ParkEase.ViewModel;
using ParkEase.Core.Services;
using ParkEase.Core.Contracts.Services;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration;
using ParkEase.Test.IntergartionTest;
using Microsoft.Maui.Graphics.Platform;
using IImage = Microsoft.Maui.Graphics.IImage;
using System.Windows.Input;

namespace ParkEase.Test.IntegrationTest
{
    public class CreateMapPageTestFixture : IAsyncLifetime
    {
        public IConfiguration Configuration { get; private set; }
        public MongoDBService MongoDBService { get; private set; }
        public ParkEaseModel Model { get; private set; }
        public IAWSService AWSService { get; private set; }
        public IDialogService DialogService { get; private set; }

        public async Task InitializeAsync()
        {
            // Build configuration
            Configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

            AWSService = new AWSService(Configuration);
            MongoDBService = new MongoDBService(AWSService, DevicePlatform.WinUI, true);
            //DialogService = new DialogService();
            Model = new ParkEaseModel(true);
            Model.User = new User { Email = "testEmail@gmail.com" };
            Model.User.Role = Roles.Administrator;
            // Seed test data
            await SeedTestDataAsync();
        }

        public async Task DisposeAsync()
        {
            // Clean up the test database
            await MongoDBService.DropCollection(CollectionName.PublicLogs);
            await MongoDBService.DropCollection(CollectionName.PrivateLogs);
            await MongoDBService.DropCollection(CollectionName.ParkingData);
            await MongoDBService.DropCollection(CollectionName.PrivateParking);
        }

        private async Task SeedTestDataAsync()
        {
            // Use MongoDBService to insert test data
            // Floor 1
            var rectangleF1 = new List<Rectangle>
            {
                new Rectangle(1, new RectF(10, 10, 50, 50)),
                new Rectangle(2, new RectF(20, 20, 60, 60)),
                new Rectangle(3, new RectF(30, 30, 70, 70))
            };
            var imageDataF1 = new byte[] { 0x20, 0x20 };

            var listFloorInfos1 = new List<FloorInfo>
            {
                new FloorInfo("F1", rectangleF1, imageDataF1)
            };

            // Floor 2
            var rectangleF2 = new List<Rectangle>
            {
                new Rectangle(1, new RectF(15, 15, 55, 55)),
                new Rectangle(2, new RectF(25, 25, 65, 65)),
                new Rectangle(3, new RectF(35, 35, 75, 75))
            };
            var imageDataF2 = new byte[] { 0x30, 0x30 };

            var listFloorInfos2 = new List<FloorInfo>
            {
                new FloorInfo("F1", rectangleF1, imageDataF1),
                new FloorInfo("F2", rectangleF2, imageDataF2)
            };

            var privateParkingMock1 = new PrivateParking
            {
                Id = "privateParkingId1",
                CompanyName = "Test Company 1",
                Address = "Address1",
                Latitude = 51.066669,
                Longitude = -114.08989,
                CreatedBy = Model.User.Email,
                ParkingInfo = new ParkingInfo { Fee = 2, LimitedHour = 6 },
                FloorInfo = listFloorInfos1
            };

            var privateParkingMock2 = new PrivateParking
            {
                Id = "privateParkingId2",
                CompanyName = "Test Company 2",
                Address = "Address2",
                Latitude = 51.045415,
                Longitude = -114.074862,
                CreatedBy = Model.User.Email,
                ParkingInfo = new ParkingInfo { Fee = 0.75, LimitedHour = 4 },
                FloorInfo = listFloorInfos2
            };

            // Insert PrivateParking
            await MongoDBService.InsertData(CollectionName.ParkingData, privateParkingMock1);
            await MongoDBService.InsertData(CollectionName.ParkingData, privateParkingMock2);

            // Get PrivateParking
            List<PrivateParking> privateParkingDatas = await MongoDBService.GetData<PrivateParking>(CollectionName.PrivateParking);
            PrivateParking privateParking1 = privateParkingDatas.FirstOrDefault(p => p.Address == "Address1");
            PrivateParking privateParking2 = privateParkingDatas.FirstOrDefault(p => p.Address == "Address2");
        }
    }

    public class CreateMapPageTests : IClassFixture<CreateMapPageTestFixture>
    {
        private readonly CreateMapPageTestFixture _fixture;
        private readonly CreateMapViewModel _viewModel;
        //private readonly DialogService _dialogService;

        public CreateMapPageTests(CreateMapPageTestFixture fixture)
        {
            _fixture = fixture;
            _viewModel = new CreateMapViewModel(_fixture.MongoDBService, _fixture.DialogService, _fixture.Model);
            _ = PrepareImage();
        }

        private async Task PrepareImage()
        {
            var imageData = new byte[] { 0x20, 0x20 };
            using (MemoryStream ms = new MemoryStream(imageData))
            {
                _viewModel.ImgSourceData = await Task.Run(() => PlatformImage.FromStream(ms));
            }
        }

        [Fact]
        public void DrawRectangleTest()
        {
            // Arrange
            _viewModel.ImgSourceData = new MockImage();
            _viewModel.RectWidth = 100;
            _viewModel.RectHeight = 50;
            var startPoint = new PointF(50, 50);

            // Act
            _viewModel.AddRectangle(startPoint);

            // Assert
            Assert.Single(_viewModel.ListRectangle);
            var addedRectangle = _viewModel.ListRectangle.First();
            Assert.Equal(1, addedRectangle.Index);
            Assert.Equal(startPoint.X, addedRectangle.Rect.X);
            Assert.Equal(startPoint.Y, addedRectangle.Rect.Y);
            Assert.Equal(100, addedRectangle.Rect.Width);
            Assert.Equal(50, addedRectangle.Rect.Height);
        }

        [Fact]
        public void DeleteRectangle_ShouldRemoveLastRectangleFromList()
        {
            // Arrange
            _viewModel.ImgSourceData = new MockImage();
            _viewModel.RectWidth = 100;
            _viewModel.RectHeight = 50;

            // Add three rectangles
            _viewModel.AddRectangle(new PointF(10, 10));
            _viewModel.AddRectangle(new PointF(20, 20));
            _viewModel.AddRectangle(new PointF(30, 30));

            Assert.Equal(3, _viewModel.ListRectangle.Count);

            var lastRectangleBeforeRemoval = _viewModel.ListRectangle.Last();

            // Act
            _viewModel.RemoveRectangleClick.Execute(null);

            // Assert
            Assert.Equal(2, _viewModel.ListRectangle.Count);
            Assert.DoesNotContain(lastRectangleBeforeRemoval, _viewModel.ListRectangle);
            Assert.Equal(2, _viewModel.ListRectangle.Last().Index);
        }





    }

    public class MockImage : IImage
    {
        public float Width => 100;
        public float Height => 100;

        public IImage Downsize(float maxWidthOrHeight, bool disposeOriginal = false)
        {
            throw new NotImplementedException();
        }

        public IImage Resize(float width, float height, ResizeMode resizeMode = ResizeMode.Fit)
        {
            throw new NotImplementedException();
        }

        public void Save(Stream stream, ImageFormat format = ImageFormat.Png, float quality = 1)
        {
            throw new NotImplementedException();
        }

        public Task SaveAsync(Stream stream, ImageFormat format = ImageFormat.Png, float quality = 1)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            // No-op for testing
        }

        // If Draw method is still required, keep it
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            // No-op for testing
        }

        public IImage Downsize(float maxWidth, float maxHeight, bool disposeOriginal = false)
        {
            throw new NotImplementedException();
        }

        public IImage Resize(float width, float height, ResizeMode resizeMode = ResizeMode.Fit, bool disposeOriginal = false)
        {
            throw new NotImplementedException();
        }

        IImage IImage.ToPlatformImage()
        {
            throw new NotImplementedException();
        }
    }
}

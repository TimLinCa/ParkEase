using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using Microsoft.VisualBasic;
using ParkEase.Contracts.Services;
using ParkEase.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using IImage = Microsoft.Maui.Graphics.IImage;
using ParkEase.Page;
using ParkEase.Core.Contracts.Services;
using ParkEase.Core.Data;
using ParkEase.Core.Services;
using Microsoft.Maui.Media;
using Microsoft.Maui.Graphics.Platform;
using System.Reflection;
using Microsoft.Maui.Graphics;


namespace ParkEase.ViewModel
{
    public partial class CreateMapViewModel : ObservableObject
    {
        [ObservableProperty]
        private string companyName;

        [ObservableProperty]
        private string address;

        [ObservableProperty]
        private string city;

        [ObservableProperty]
        private double fee;

        [ObservableProperty]
        private double limitHour;

        [ObservableProperty]
        private int numberOfLot;

        [ObservableProperty]
        private string floor;

        [ObservableProperty]
        private int recCount;

        [ObservableProperty]
        private IImage imgSourceData;

        [ObservableProperty]
        private float rectWidth;

        [ObservableProperty]
        private float rectHeight;

        [ObservableProperty]
        private ObservableCollection<RectF> rectangles;

        private List<Rectangle> ListRectangles { get; set; }

        private List<FloorInfo> ListfloorInfos { get; set; }

        private byte[] imageData;

        private readonly IMongoDBService mongoDBService;

        private readonly IDialogService dialogService;

        private Task drawTask = null;

        public CreateMapViewModel(IMongoDBService mongoDBService, IDialogService dialogService)
        {
            this.mongoDBService = mongoDBService;
            this.dialogService = dialogService;
            companyName = string.Empty;
            address = string.Empty;
            city = string.Empty;
            fee = 0;
            limitHour = 0;
            numberOfLot = 0;
            rectWidth = 100;
            rectHeight = 50;
            rectangles = new ObservableCollection<RectF>();

            ListRectangles = new List<Rectangle>();
            ListfloorInfos = new List<FloorInfo>();
        }

        public ICommand UploadImageClick => new RelayCommand(async () =>
            {
                try
                {
                    if (MediaPicker.Default.IsCaptureSupported)
                    {
                        // Load photo
                        FileResult myPhoto = await MediaPicker.PickPhotoAsync();
                        if (myPhoto != null)
                        {
                            string imgPath = myPhoto.FullPath;
                            Microsoft.Maui.Graphics.IImage image;
                            Assembly assembly = GetType().GetTypeInfo().Assembly;
                            using (Stream fileStream = File.OpenRead(imgPath))
                            {
                                ImgSourceData = PlatformImage.FromStream(fileStream);
                            }
                            using var stream = await myPhoto.OpenReadAsync();
                            using var memoryStream = new MemoryStream();
                            await stream.CopyToAsync(memoryStream);
                            var imageBytes = memoryStream.ToArray();

                            // Convert byte array to Base64 string and assign to ImageData
                            imageData = imageBytes;
                        }
                    }
                    else
                    {
                        await dialogService.ShowAlertAsync("OOPS", "Your device isn't supported", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
                }

            });

        public void AddRectangle(PointF point)
        {
            try
            {
                if (ImgSourceData != null)
                {
                    var rect = new RectF(point.X, point.Y, RectWidth, RectHeight);
                    Rectangles.Add(rect);
                    RecCount = RecCount + 1;
                }
            }
            catch (Exception)
            {

                throw;
            }


        }

        public ICommand RemoveRectangleClick => new RelayCommand(async () =>
        {
            try
            {
                if (RecCount > 0)
                {
                    Rectangles.RemoveAt(RecCount - 1);
                    RecCount--;
                }
                else
                {
                    await dialogService.ShowAlertAsync("Error", "There is nothing to deldete.\nPlease draw the map first!", "OK");
                }
            }
            catch (Exception ex)
            {
                await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }

        });

        public ICommand ClearAllRectangleClick => new RelayCommand(async () =>
        {
            try
            {
                if (RecCount > 0)
                {
                    Rectangles.Clear();
                    RecCount = 0;
                }
                else
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }

        });

        // Save Floor Information Command

        // Submit Command
        public ICommand AddParkingInfoAsync => new RelayCommand(async () =>

        {
            try
            {
                for (int i = 0; i < RecCount; i++)
                {
                    var insertRect = new Rectangle(i + 1, Rectangles[i]);
                    ListRectangles.Add(insertRect);
                };

                /*var rect1 = new Rectangle(1, new RectF(10, 10, 100, 50));
                var rect2 = new Rectangle(2, new RectF(50, 30, 100, 50));
                ListRectangles.Add(rect1);
                ListRectangles.Add(rect2);*/

                var floor1 = new FloorInfo("b1", ListRectangles, imageData);
                ListfloorInfos.Add(floor1);


                var privateParkingInfo = new PrivateParking
                {
                    CompanyName = CompanyName,
                    Address = Address,
                    City = City,
                    ParkingInfo = new ParkingInfo
                    {
                        Fee = Fee,
                        LimitedHour = LimitHour,
                        NumberOfLot = RecCount
                    },

                    FloorInfo = ListfloorInfos

                    /*FloorInfo = new List<FloorInfo>
                    {
                        Floor = floor,
                        Rectangles = listRectangles,
                        imageData = imageData,
                    }*/


                };
                await mongoDBService.InsertData(CollectionName.PrivateParking, privateParkingInfo);
                var TEST = await mongoDBService.GetData<PrivateParking>(CollectionName.PrivateParking);
                await dialogService.ShowAlertAsync("", "Your data is saved.", "OK");
            }
            catch (Exception ex)
            {
                await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }
        });

    }

}

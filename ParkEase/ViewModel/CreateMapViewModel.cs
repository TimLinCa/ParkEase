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
        private string floor;

        [ObservableProperty]
        private IImage imgSourceData;

        [ObservableProperty]
        private float rectWidth;

        [ObservableProperty]
        private float rectHeight;

        [ObservableProperty]
        private ObservableCollection<RectF> rectangles;

        private List<Rectangle> ListRectangles;

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
            rectWidth = 100;
            rectHeight = 50;
            rectangles = new ObservableCollection<RectF>();

            ListfloorInfos = new List<FloorInfo>();
        }

        public ICommand UploadImageClick => new RelayCommand(async () =>
            {
                try
                {
                    if (MediaPicker.Default.IsCaptureSupported)
                    {
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
                    //RectCount = RectCount + 1;
                }
            }
            catch (Exception ex)
            {
                dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }


        }

        public ICommand RemoveRectangleClick => new RelayCommand(async () =>
        {
            try
            {
                if (Rectangles.Count > 0)
                {
                    Rectangles.RemoveAt(Rectangles.Count - 1);
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
                if (Rectangles.Count > 0)
                {
                    Rectangles.Clear();
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
        public ICommand SaveFloorInfoCommand => new RelayCommand(async () =>
        {
            try
            {
                if (Rectangles.Count > 0)
                {
                    ListRectangles = new List<Rectangle>();
                    for (int i = 0; i < Rectangles.Count; i++)
                    {
                        var insertedRect = new Rectangle(i + 1, Rectangles[i]);
                        ListRectangles.Add(insertedRect);
                    };
                }

                if (Floor != null && ListRectangles.Count > 0 && imageData != null)
                {
                    var floorInfo = new FloorInfo(Floor, ListRectangles, Rectangles.Count, imageData);
                    ListfloorInfos.Add(floorInfo);

                    ResetFloorInfo();
                }
                else
                {
                    await dialogService.ShowAlertAsync("Warning", "One of these information is missing. Please check the following:\n1. Is floor typed?\n2. Do you upload Image?\n3. Do you create at least one rectangle?", "OK");
                }
            } catch (Exception ex)
            {
                await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }
        }
        );

        // Submit Command
        public ICommand AddParkingInfoAsync => new RelayCommand(async () =>
        {
            try
            {
                if (IsValid())
                {
                    var privateParkingInfo = new PrivateParking
                    {
                        CompanyName = CompanyName,
                        Address = Address,
                        City = City,
                        ParkingInfo = new ParkingInfo
                        {
                            Fee = Fee,
                            LimitedHour = LimitHour
                        },

                        FloorInfo = ListfloorInfos

                    };
                    await mongoDBService.InsertData(CollectionName.PrivateParking, privateParkingInfo);
                    var TEST = await mongoDBService.GetData<PrivateParking>(CollectionName.PrivateParking);
                    await dialogService.ShowAlertAsync("", "Your data is saved.", "OK");

                    ResetAfterSubmit();
                }
                else
                {
                    await dialogService.ShowAlertAsync("Warning", "Please fill in all fields.", "OK");
                }
            }
            catch (Exception ex)
            {
                await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }
        });

        // Edit Command
        public ICommand EditMapCommand => new RelayCommand(async () =>
        {
            // 1. In CreateMapPage, they can edit foor they already save  -> click EDIT
            // 2. Admid can have many different parking places -> in Manage Property, they can select a specific property to edit from a list
            try
            {
                var data = await mongoDBService.GetData<PrivateParking>(CollectionName.PrivateParking);
                var loadedData = data.FirstOrDefault(data => data.Address == "16 Ave NW");
                if (loadedData != null)
                {
                    CompanyName = loadedData.CompanyName;
                    Address = loadedData.Address;
                    City = loadedData.City;
                    Fee = loadedData.ParkingInfo.Fee;
                    LimitHour = loadedData.ParkingInfo.LimitedHour;
                    ListfloorInfos = loadedData.FloorInfo;
                    
                    
                    foreach (FloorInfo floorInfo in ListfloorInfos)
                    {
                        if (floorInfo != null)
                        {
                            Floor = floorInfo.Floor;
                            ListRectangles = floorInfo.Rectangles;
                            foreach (Rectangle rectangle in ListRectangles)
                            {
                                float pointX = rectangle.Rect.X;
                                float pointY = rectangle.Rect.Y;
                                var rect = new RectF(pointX, pointY, rectangle.Rect.Width, rectangle.Rect.Height);
                                Rectangles.Add(rect);

                                // Can Delete, but cannot Clear (maybe after Clear, it doesn't re-draw bc Rectangles = 0) -> need to be fixed
                            }
                            
                            // <Not completed> change byte[] to IImage
                            //ImgSourceData = (IImage)ImageSource.FromStream(() => new MemoryStream(floorInfo.ImageData));
                            /*try
                            {
                                byte[] imageByte = floorInfo.ImageData;
                                IImage ImgSourceData;
                                Assembly assembly = GetType().GetTypeInfo().Assembly;
                                using (Stream stream = assembly.GetManifestResourceStream(imageByte.ToString()))
                                {
                                    if (stream == null) await dialogService.ShowAlertAsync("Error", "Stream is null", "OK");
                                    ImgSourceData = PlatformImage.FromStream(stream);
                                }
                            } catch (Exception ex)
                            {
                                await dialogService.ShowAlertAsync("Error in Load edit image", ex.Message, "OK");
                            }*/

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }
        });

            private bool IsValid()
        {
            return !string.IsNullOrEmpty(CompanyName) &&
                    !string.IsNullOrEmpty(Address) &&
                    !string.IsNullOrEmpty(City) &&
                    ListfloorInfos.Count() > 0;
        }

        private void ResetAfterSubmit()
        {
            CompanyName = string.Empty;
            Address = string.Empty;
            City = string.Empty;
            Fee = 0;
            LimitHour = 0;
            ListfloorInfos.Clear();
        }

        private void ResetFloorInfo()
        {
            Floor = string.Empty;
            ImgSourceData = null;
            RectWidth = 100;
            RectHeight = 50;
            Rectangles.Clear();
        }

    }

}

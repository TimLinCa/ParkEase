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
using ParkEase.Core.Model;
using Amazon.SecurityToken.Model;
using MongoDB.Driver;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
        private ObservableCollection<string> floorNames;

        [ObservableProperty]
        private IImage imgSourceData;

        [ObservableProperty]
        private float rectWidth;

        [ObservableProperty]
        private float rectHeight;

        [ObservableProperty]
        private ObservableCollection<RectF> rectangles;

        [ObservableProperty]
        private ObservableCollection<string> propertyAddresses;

        [ObservableProperty]
        private string selectedAddress;

        [ObservableProperty]
        private string selectedFloorName;

        private string selectedPropertyId;

        private List<PrivateParking> userData;

        private List<Rectangle> listRectangles;

        private List<FloorInfo> listfloorInfos;

        private byte[] imageData;

        private readonly IMongoDBService mongoDBService;

        private readonly IDialogService dialogService;

        private Task drawTask = null;

        private ParkEaseModel parkEaseModel;

        public CreateMapViewModel(IMongoDBService mongoDBService, IDialogService dialogService, ParkEaseModel model)
        {
            this.mongoDBService = mongoDBService;
            this.dialogService = dialogService;
            this.parkEaseModel = model;
            companyName = string.Empty;
            address = string.Empty;
            city = string.Empty;
            fee = 0;
            limitHour = 0;
            rectWidth = 100;
            rectHeight = 50;
            rectangles = new ObservableCollection<RectF>();

            listfloorInfos = new List<FloorInfo>();
            FloorNames = new ObservableCollection<string>();
            PropertyAddresses = new ObservableCollection<string>();

            _ = GetUserDataFromDatabase();
            
        }

        // Get User's data from database
        private async Task GetUserDataFromDatabase()
        {
            try
            {
                var filter = Builders<PrivateParking>.Filter.Eq(data => data.CreatedBy, parkEaseModel.User.Email);
                userData = await mongoDBService.GetDataFilter<PrivateParking>(CollectionName.PrivateParking, filter);

                _ = GetPropertyAddress();
            }
            catch (Exception ex)
            {
                await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }
        }


        // List of User's parking property address
        private async Task GetPropertyAddress()
        {
            try
            {
                if (userData != null)
                {
                    PropertyAddresses.Clear();
                    foreach (var item in userData)
                    {
                        PropertyAddresses.Add(item.Address);
                    }
                }
            } catch (Exception ex)
            {
                await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }

        }

        // Load Parking Information base on Address
        public ICommand LoadParkingInfoCommand => new RelayCommand(async () =>
        {
            try
            {
                if (userData != null)
                {
                    var selectedProperty = userData.FirstOrDefault(data => data.Address == SelectedAddress);
                    if (selectedProperty != null)
                    {
                        selectedPropertyId = selectedProperty.Id;
                        CompanyName = selectedProperty.CompanyName;
                        Address = selectedProperty.Address;
                        City = selectedProperty.City;
                        Fee = selectedProperty.ParkingInfo.Fee;
                        LimitHour = selectedProperty.ParkingInfo.LimitedHour;
                        listfloorInfos = selectedProperty.FloorInfo;
                    }
                    _ = GetFloorNames();
                }
            }
            catch (Exception ex)
            {
                await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }

        });

        // Load list of Floor name to display in dropdown selection
        private async Task GetFloorNames()
        {
            try
            {
                if (!string.IsNullOrEmpty(Address))
                {
                    foreach (FloorInfo item in  listfloorInfos)
                    {
                        FloorNames.Add(item.Floor);
                    }
                }

            } catch (Exception ex)
            {
                await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }
        }

        // Load Floor Information from database
        public ICommand LoadFloorInfoCommand => new RelayCommand(async () =>
        {
            try
            {
                if (listfloorInfos != null)
                {
                    var selectedFloor = listfloorInfos.FirstOrDefault(data => data.Floor == SelectedFloorName);
                    if (selectedFloor != null)
                    {
                        // Load image
                        byte[] imageByte = selectedFloor.ImageData;
                        using (MemoryStream ms = new MemoryStream(imageByte))
                        {
                            ImgSourceData = await Task.Run(() => PlatformImage.FromStream(ms));
                        }

                        // Load rectangles
                        listRectangles = selectedFloor.Rectangles;
                        foreach (Rectangle rectangle in listRectangles)
                        {
                            float pointX = rectangle.Rect.X;
                            float pointY = rectangle.Rect.Y;
                            var rect = new RectF(pointX, pointY, rectangle.Rect.Width, rectangle.Rect.Height);
                            Rectangles.Add(rect);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }

        });

        // Upload parking map
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

        // Save Floor Information Command
        public ICommand SaveFloorInfoCommand => new RelayCommand(async () =>
        {
            try
            {
                if (Rectangles.Count > 0)
                {
                    listRectangles = new List<Rectangle>();
                    for (int i = 0; i < Rectangles.Count; i++)
                    {
                        var insertedRect = new Rectangle(i + 1, Rectangles[i]);
                        listRectangles.Add(insertedRect);
                    };
                }

                if (Floor != null && listRectangles.Count > 0 && imageData != null)
                {
                    var floorInfo = new FloorInfo(Floor, listRectangles, Rectangles.Count, imageData);
                    listfloorInfos.Add(floorInfo);
                    FloorNames.Add(floorInfo.Floor);

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
        public ICommand SubmitCommand => new RelayCommand(async () =>
        {
            try
            {
                if (IsValid())
                {
                    // if UPDATING existing data
                    if (selectedPropertyId != null && (SelectedAddress != null || SelectedFloorName != null)) 
                    {
                        var filter = Builders<PrivateParking>.Filter.Eq(data => data.Id, selectedPropertyId);
                        var update = Builders<PrivateParking>.Update
                                        .Set(i => i.CompanyName, CompanyName)
                                        .Set(i => i.Address, Address)
                                        .Set(i => i.City, City)
                                        .Set(i => i.CreatedBy, parkEaseModel.User.Email)
                                        .Set(i => i.ParkingInfo, new ParkingInfo { Fee = Fee, LimitedHour = LimitHour })
                                        .Set(i => i.FloorInfo, listfloorInfos);
                        await mongoDBService.UpdateData<PrivateParking>(CollectionName.PrivateParking, filter, update);
                        await dialogService.ShowAlertAsync("", "Your data is updated.", "OK");

                        ResetAfterSubmit();
                    }
                    // if INSERT new data
                    else
                    {
                        var privateParkingInfo = new PrivateParking
                        {
                            CompanyName = CompanyName,
                            Address = Address,
                            City = City,
                            CreatedBy = parkEaseModel.User.Email,
                            ParkingInfo = new ParkingInfo
                            {
                                Fee = Fee,
                                LimitedHour = LimitHour
                            },

                            FloorInfo = listfloorInfos

                        };
                        await mongoDBService.InsertData(CollectionName.PrivateParking, privateParkingInfo);
                        await dialogService.ShowAlertAsync("", "Your data is saved.", "OK");

                        ResetAfterSubmit();
                    }
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


        // Control rectangle drawing
        // Add rectangle
        public void AddRectangle(PointF point)
        {
            try
            {
                if (ImgSourceData != null)
                {
                    var rect = new RectF(point.X, point.Y, RectWidth, RectHeight);
                    Rectangles.Add(rect);
                }
            }
            catch (Exception ex)
            {
                dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }
        }

        // Remove a rectangle
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

        // Clear all rectangle
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

        private bool IsValid()
        {
            return !string.IsNullOrEmpty(CompanyName) &&
                    !string.IsNullOrEmpty(Address) &&
                    !string.IsNullOrEmpty(City) &&
                    listfloorInfos.Count() > 0;
        }

        private void ResetAfterSubmit()
        {
            CompanyName = string.Empty;
            Address = string.Empty;
            City = string.Empty;
            Fee = 0;
            LimitHour = 0;
            listfloorInfos.Clear();
        }

        private void ResetFloorInfo()
        {
            Floor = string.Empty;
            ImgSourceData = null;
            RectWidth = 100;
            RectHeight = 50;
            Rectangles.Clear();
        }

        /*// Edit Command
        public ICommand EditMapCommand => new RelayCommand(async () =>
        {
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
                    listfloorInfos = loadedData.FloorInfo;

                    foreach (FloorInfo floorInfo in listfloorInfos)
                    {
                        if (floorInfo != null)
                        {
                            Floor = floorInfo.Floor;

                            // Load image
                            byte[] imageByte = floorInfo.ImageData;
                            using (MemoryStream ms = new MemoryStream(imageByte))
                            {
                                ImgSourceData = await Task.Run(() => PlatformImage.FromStream(ms));
                            }

                            // Load rectangles
                            listRectangles = floorInfo.Rectangles;
                            foreach (Rectangle rectangle in listRectangles)
                            {
                                float pointX = rectangle.Rect.X;
                                float pointY = rectangle.Rect.Y;
                                var rect = new RectF(pointX, pointY, rectangle.Rect.Width, rectangle.Rect.Height);
                                Rectangles.Add(rect);

                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }
        });*/

    }

}

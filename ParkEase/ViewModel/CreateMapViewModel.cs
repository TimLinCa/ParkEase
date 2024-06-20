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
using ParkEase.Core.Model;
using MongoDB.Driver;
using MongoDB.Bson;
using ZXing.Net.Maui;
using System.Runtime.InteropServices;

namespace ParkEase.ViewModel
{
    public partial class CreateMapViewModel : ObservableObject
    {
        [ObservableProperty]
        private string companyName;

        [ObservableProperty]
        private string address;

        private double latitude;

        private double longitude;

        [ObservableProperty]
        private double fee;

        [ObservableProperty]
        private int limitHour;

        [ObservableProperty]
        private string floor;

        [ObservableProperty]
        private IImage imgSourceData;

        //[ObservableProperty]
        private float rectWidth;

        //[ObservableProperty]
        private float rectHeight;

        //[ObservableProperty]
        //private ObservableCollection<RectF> rectangles;

        [ObservableProperty]
        private ObservableCollection<Rectangle> listRectangle;

        [ObservableProperty]
        private ObservableCollection<string> propertyAddresses;

        [ObservableProperty]
        private ObservableCollection<string> floorNames;

        [ObservableProperty]
        private string selectedAddress;

        [ObservableProperty]
        private string selectedFloorName;

        private string selectedPropertyId;

        private string selectedCompanyName;

        private List<PrivateParking> userData;

        //private List<Rectangle> listRectangles;

        private List<FloorInfo> listFloorInfos;

        private byte[] imageData;

        private readonly IMongoDBService mongoDBService;

        private readonly IDialogService dialogService;

        private Task drawTask = null;

        private ParkEaseModel parkEaseModel;

        private bool addNewFloorClicked;

        public CreateMapViewModel(IMongoDBService mongoDBService, IDialogService dialogService, ParkEaseModel model)
        {
            this.mongoDBService = mongoDBService;
            this.dialogService = dialogService;
            this.parkEaseModel = model;
            companyName = string.Empty;
            address = string.Empty;
            fee = 0;
            limitHour = 0;
            rectWidth = 100;
            rectHeight = 50;
            //rectangles = new ObservableCollection<RectF>();
            ListRectangle = new ObservableCollection<Rectangle>();
            listFloorInfos = new List<FloorInfo>();
            FloorNames = new ObservableCollection<string>();
            PropertyAddresses = new ObservableCollection<string>();
            addNewFloorClicked = false;

            _ = GetUserDataFromDatabase();
            
        }

        public float RectWidth
        {
            get => rectWidth;
            set
            {
                if(rectWidth != value)
                {
                    rectWidth = value;
                    OnPropertyChanged(nameof(RectWidth));
                }
            }
        }
        public float RectHeight
        {
            get => rectHeight;
            set
            {
                if (rectHeight != value)
                {
                    rectHeight = value;
                    OnPropertyChanged(nameof(RectHeight));
                }
            }
        }

        public event PropertyChangingEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangingEventArgs(propertyName));
        }
        //https://stackoverflow.com/questions/76846770/how-to-useonpropertychanged-net-maui

        // Get User's data from database
        private async Task GetUserDataFromDatabase()
        {
            try
            {
                var filter = Builders<PrivateParking>.Filter.Eq(p => p.CreatedBy, parkEaseModel.User.Email);
                userData = await mongoDBService.GetDataFilter<PrivateParking>(CollectionName.PrivateParking, filter);

                if (userData == null || userData.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("No data found.");
                    return;
                }

                /*List<PrivateParking> privateData = await mongoDBService.GetData<PrivateParking>(CollectionName.PrivateParking);

                if (privateData == null || privateData.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("No parking data found.");
                    return;
                }
                userData = privateData.Where(data => data.CreatedBy == parkEaseModel.User.Email).ToList();*/

                _ = GetPropertyAddress();
            }
            catch (Exception ex)
            {
                await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }
        }
        //https://www.mongodb.com/docs/drivers/csharp/current/usage-examples/updateOne/


        // List of User's parking property 
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

        // Load list of Floor name to display in dropdown selection
        private async Task GetFloorNames()
        {
            try
            {
                if (!string.IsNullOrEmpty(Address))
                {
                    foreach (FloorInfo item in listFloorInfos)
                    {
                        FloorNames.Add(item.Floor);
                    }
                }

            }
            catch (Exception ex)
            {
                await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }
        }

        // verify address and save latitude, longitude
        public ICommand AddressCommand => new RelayCommand(async () =>
        {
            if (string.IsNullOrWhiteSpace(Address))
            {
                // Handle empty address case
                return;
            }

            try
            {
                // https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/device/geocoding?view=net-maui-8.0&tabs=android
                IEnumerable<Location> locations = await Geocoding.Default.GetLocationsAsync(Address);
                Location location = locations?.FirstOrDefault();

                if (location != null)
                {
                    // Perform reverse geocoding to verify the address
                    IEnumerable<Placemark> placemarks = await Geocoding.Default.GetPlacemarksAsync(location.Latitude, location.Longitude);
                    Placemark placemark = placemarks?.FirstOrDefault();
                    bool test = NormalizeAddress(placemark.FeatureName).Contains(NormalizeAddress(Address));

                    if (test)
                    {
                        // Compare the input address with the reverse geocoded address

                        latitude = location.Latitude;
                        longitude = location.Longitude;
                    }
                    else
                    {
                        await dialogService.ShowAlertAsync("error", "Invalid Address", "OK");

                    }
                }
                else
                {
                    await dialogService.ShowAlertAsync("error", "Invalid Address", "OK");
                }
            }
            catch (Exception ex)
            {
                await dialogService.ShowAlertAsync("error", ex.Message, "OK");
            }
        });

        // AddressCommand helper
        private string NormalizeAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return string.Empty;
            }
            return new string(address.ToLower().Where(char.IsLetterOrDigit).ToArray());
        }

        // Load Parking Information base on Address
        public ICommand LoadParkingInfoCommand => new RelayCommand(async () =>
        {
            try
            {
                if (userData != null)
                {
                    listFloorInfos.Clear();
                    FloorNames.Clear();
                    var selectedProperty = userData.FirstOrDefault(data => data.Address == SelectedAddress);
                    if (selectedProperty != null)
                    {
                        selectedPropertyId = selectedProperty.Id;
                        selectedCompanyName = selectedProperty.CompanyName;
                        CompanyName = selectedProperty.CompanyName;
                        Address = selectedProperty.Address;
                        Fee = selectedProperty.ParkingInfo.Fee;
                        LimitHour = selectedProperty.ParkingInfo.LimitedHour;
                        listFloorInfos = selectedProperty.FloorInfo;
                    }
                    _ = GetFloorNames();
                }
            }
            catch (Exception ex)
            {
                await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }
        });

        

        // LoadFloorInfoCommand Floor Information from database
        public ICommand LoadFloorInfoCommand => new RelayCommand(async () =>
        {
            try
            {
                if (listFloorInfos != null)
                {
                    var selectedFloor = listFloorInfos.FirstOrDefault(data => data.Floor == SelectedFloorName);
                    if (selectedFloor != null)
                    {
                        ListRectangle?.Clear();
                        // Load image
                        imageData = selectedFloor.ImageData;
                        using (MemoryStream ms = new MemoryStream(imageData))
                        {
                            ImgSourceData = await Task.Run(() => PlatformImage.FromStream(ms));
                        }

                        // Load rectangles
                        foreach (var rectangle in selectedFloor.Rectangles)
                        {
                           ListRectangle.Add(rectangle);
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
                    if (!addNewFloorClicked)
                    {
                        await dialogService.ShowAlertAsync("", "Please make sure that you entered floor name and clicked Add button before uploading image.", "OK");
                        return;
                    }
                    if (MediaPicker.Default.IsCaptureSupported)
                    {
                        FileResult myPhoto = await MediaPicker.PickPhotoAsync();
                        if (myPhoto != null)
                        {
                            ListRectangle?.Clear();
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

        public ICommand AddNewFloorCommand => new RelayCommand(async () =>
        {
            try
            {
                if (!string.IsNullOrEmpty(Floor))
                {
                    if (FloorNames.Contains(Floor))
                    {
                        await dialogService.ShowAlertAsync("Warning", "This floor name already existed in database.\nPlease enter another one!", "OK");
                        addNewFloorClicked = false;
                    }
                    else
                    {
                        //await dialogService.ShowAlertAsync("", "Please upload parking map image.", "OK");
                        addNewFloorClicked = true;
                    }
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
                if (ListRectangle.Count > 0)
                {
                    // Add new floor information
                    if (addNewFloorClicked)
                    {
                        var floorInfo = new FloorInfo(Floor, ListRectangle.ToList(), imageData);
                        listFloorInfos.Add(floorInfo);
                        FloorNames.Add(floorInfo.Floor);

                        ResetFloorInfo();
                    }
                    // Edit existing floor information
                    else if (!string.IsNullOrEmpty(SelectedFloorName) && !addNewFloorClicked)
                    {
                        var existingFloorInfo = listFloorInfos.FirstOrDefault(item => item.Floor == SelectedFloorName);
                        if (existingFloorInfo != null)
                        {
                            existingFloorInfo.Rectangles = ListRectangle.ToList();
                            existingFloorInfo.ImageData = imageData;

                            ResetFloorInfo();
                        }
                    }
                }
                else
                {
                    await dialogService.ShowAlertAsync("Warning", "Something went wrong." +
                        "                               \nIf you want to edit existing map, select one floor from dropdown." +
                        "                               \nIf you want to add new map, please enter floor name and click Add button.", "OK");
                }
            }
            catch (Exception ex)
            {
                await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }
        });

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
                        var builder = Builders<PrivateParking>.Filter;
                        //var filter = builder.Eq(p => p.CompanyName, selectedCompanyName) & builder.Eq(p => p.Address, selectedAddress);
                        var filter = builder.Eq(p => p.Id, selectedPropertyId);

                        var update = Builders<PrivateParking>.Update
                                        .Set(p => p.CompanyName, CompanyName)
                                        .Set(p => p.Address, Address)
                                        .Set(p => p.Latitude, latitude)
                                        .Set(p => p.Longitude, longitude)
                                        .Set(p => p.CreatedBy, parkEaseModel.User.Email)
                                        .Set(p => p.ParkingInfo, new ParkingInfo { Fee = Fee, LimitedHour = LimitHour })
                                        .Set(p => p.FloorInfo, listFloorInfos);

                        await mongoDBService.UpdateData(CollectionName.PrivateParking, filter, update);
                        await dialogService.ShowAlertAsync("", "Your data is updated.", "OK");

                        ResetAfterSubmit();
                        _ = GetUserDataFromDatabase();
                    }
                    // if INSERT new data
                    else
                    {
                        var privateParkingInfo = new PrivateParking
                        {
                            CompanyName = CompanyName,
                            Address = Address,
                            Latitude = latitude,
                            Longitude = longitude,
                            CreatedBy = parkEaseModel.User.Email,
                            ParkingInfo = new ParkingInfo
                            {
                                Fee = Fee,
                                LimitedHour = LimitHour
                            },

                            FloorInfo = listFloorInfos
                            
                        };
                        await mongoDBService.InsertData(CollectionName.PrivateParking, privateParkingInfo);

                        var privateData = await mongoDBService.GetData<PrivateParking>(CollectionName.PrivateParking);
                        var parkingInfo = privateData.Where(data => data.Address == Address).First();

                        await dialogService.ShowAlertAsync("Success", "Your data is saved.\n" + 
                                                            "Generate QR Code to use this parking lot\n" + 
                                                            parkingInfo.Id, 
                                                            "OK");

                        ResetAfterSubmit();
                        _ = GetUserDataFromDatabase();
                    }
                }
                else
                {
                    await dialogService.ShowAlertAsync("Warning", "Please check if all fields is filled up.", "OK");
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
                    ListRectangle.Add(new Rectangle(ListRectangle.Count + 1, rect));
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
                if (ListRectangle.Count > 0)
                {
                    ListRectangle.RemoveAt(ListRectangle.Count - 1);
                }
                else
                {
                    await dialogService.ShowAlertAsync("Error", "There is nothing to deldete.\nPlease draw the map first! ", "OK");
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
                if (ListRectangle.Count > 0)
                {
                    ListRectangle.Clear();
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
                    listFloorInfos.Count() > 0;
        }

        private void ResetAfterSubmit()
        {
            CompanyName = "";
            Address = "";
            Fee = 0;
            LimitHour = 0;
            listFloorInfos.Clear();
            SelectedFloorName = "";
            FloorNames.Clear();
            ListRectangle.Clear();
            ImgSourceData = null;
            imageData = null;
        }

        private void ResetFloorInfo()
        {
            Floor = "";
            ImgSourceData = null;
            RectWidth = 100;
            RectHeight = 50;
            ListRectangle.Clear();
            addNewFloorClicked = false;
        }
    }
}

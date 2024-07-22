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
        #region ObservableProperty
        [ObservableProperty]
        private string companyName;

        [ObservableProperty]
        private string address;

        [ObservableProperty]
        private double fee;

        [ObservableProperty]
        private int limitHour;

        [ObservableProperty]
        private string floor;

        [ObservableProperty]
        private IImage imgSourceData;

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

        [ObservableProperty]
        private IAsyncRelayCommand addNewFloorCommand;

        [ObservableProperty]
        private IAsyncRelayCommand saveFloorInfoCommand;

        [ObservableProperty]
        private IAsyncRelayCommand submitCommand;

        [ObservableProperty]
        private ObservableCollection<string> deleteOptions;

        [ObservableProperty]
        private string deleteOptionSelected;

        [ObservableProperty]
        private string addressToDelete;

        [ObservableProperty]
        private ObservableCollection<string> listFloorsToDelete;

        [ObservableProperty]
        private string floorToDelete;

        [ObservableProperty]
        private bool isDeleteAddressVisible;

        [ObservableProperty]
        private bool isDeleteFloorVisible;
        #endregion

        #region PrivateProperty

        private double latitude;

        private double longitude;

        private float rectWidth;

        private float rectHeight;

        private string selectedPropertyId;

        private string selectedCompanyName;

        private List<PrivateParking> userData;

        private List<FloorInfo> listFloorInfos;

        private byte[] imageData;

        private readonly IMongoDBService mongoDBService;

        private readonly IDialogService dialogService;

        private Task drawTask = null;

        private ParkEaseModel parkEaseModel;

        private bool addNewFloorClicked;

        private PrivateParking deletedFloorAddressSelected;
        #endregion

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
            ListRectangle = new ObservableCollection<Rectangle>();
            listFloorInfos = new List<FloorInfo>();
            FloorNames = new ObservableCollection<string>();
            PropertyAddresses = new ObservableCollection<string>();
            addNewFloorClicked = false;
            DeleteOptions = ["Parking lot", "A Floor"];
            ListFloorsToDelete = new ObservableCollection<string>();
            IsDeleteAddressVisible = false;
            IsDeleteFloorVisible = false;

            AddNewFloorCommand = new AsyncRelayCommand(AddNewFloorCommandAsync);
            SaveFloorInfoCommand = new AsyncRelayCommand(SaveFloorInfoCommandAsync);
            SubmitCommand = new AsyncRelayCommand(SubmitCommandAsync);
        }

        #region OnPropertyChangedEvent
        partial void OnSelectedAddressChanged(string propertyName)
        {
            _ = LoadParkingInfo();
        }

        partial void OnSelectedFloorNameChanged(string propertyName)
        {
            _ = LoadFloorInfo();
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

        // For delete zone
        partial void OnDeleteOptionSelectedChanged(string value)
        {
            DeleteOptionCommand();
        }

        // if A Floor option is selected, load list of floors to delete when address is changed
        partial void OnAddressToDeleteChanged(string value)
        {
            if (DeleteOptionSelected == "A Floor" && !string.IsNullOrEmpty(value))
            {
                deletedFloorAddressSelected = userData.FirstOrDefault(data => data.Address == AddressToDelete);
                if (deletedFloorAddressSelected == null)
                {
                    System.Diagnostics.Debug.WriteLine("No data found.");
                    return;
                }
                ListFloorsToDelete.Clear();
                foreach (FloorInfo item in deletedFloorAddressSelected.FloorInfo)
                {
                    ListFloorsToDelete.Add(item.Floor);
                }
            }
        }
        #endregion

        public ICommand LoadedCommand => new RelayCommand(() =>
        {
            try
            {
                _ = GetUserDataFromDatabase();
                RefreshPage();
            }
            catch (Exception ex)
            {
                dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }
        });

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
                    bool test = ((RoundDownToSecondDecimal(placemark.Location.Latitude) == RoundDownToSecondDecimal(location.Latitude)) && (RoundDownToSecondDecimal(placemark.Location.Longitude) == RoundDownToSecondDecimal(location.Longitude)));

                    if (test)
                    {
                        // Compare the input address with the reverse geocoded address
                        latitude = location.Latitude;
                        longitude = location.Longitude;
                        await dialogService.ShowAlertAsync("Success", "Valid Address", "OK");
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

        double RoundDownToSecondDecimal(double value)
        {
            return Math.Floor(value * 100) / 100;
        }

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
        private async Task LoadParkingInfo()
        {
            try
            {
                if (SelectedAddress == null)
                {
                    RefreshPage();
                }
                else if (userData != null)
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
                        latitude = selectedProperty.Latitude;
                        longitude = selectedProperty.Longitude;
                    }
                    _ = GetFloorNames();
                }
            }
            catch (Exception ex)
            {
                await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }
        }

        // LoadFloorInfoCommand Floor Information from database
        private async Task LoadFloorInfo()
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
        }

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

        private async Task AddNewFloorCommandAsync()
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
                        addNewFloorClicked = true;
                    }
                }
            }
            catch (Exception ex)
            {
                await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }
        }

        // Save Floor Information Command
        private async Task SaveFloorInfoCommandAsync()
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
                    else
                    {
                        await dialogService.ShowAlertAsync("Warning", "You may forget clicking Add button.", "OK");
                    }
                }
                else
                {
                    await dialogService.ShowAlertAsync("Warning", "Something went wrong." +
                        "                               \nIf you want to edit existing map, select one floor from dropdown." +
                        "                               \nIf you want to add new map, please enter floor name, click Add button, upload image and create map.", "OK");
                }
            }
            catch (Exception ex)
            {
                await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }
        }

        // Submit Command
        private async Task SubmitCommandAsync()
        {
            try
            {
                if (IsValid())
                {
                    // if UPDATING existing data
                    if (selectedPropertyId != null && (SelectedAddress != null || SelectedFloorName != null)) 
                    {
                        var builder = Builders<PrivateParking>.Filter;
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
                        await dialogService.ShowAlertAsync("Success", "Your data is updated.", "OK");

                        RefreshPage();
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
                        RefreshPage();
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
        }



        private void DeleteOptionCommand()
        {
            // Delete Parking lot
            if (DeleteOptionSelected == "Parking lot")
            {
                IsDeleteAddressVisible = true;
                IsDeleteFloorVisible = false;

            }
            // Delete a Floor
            else if (DeleteOptionSelected == "A Floor")
            {
                IsDeleteAddressVisible = true;
                IsDeleteFloorVisible = true;
            }
        }



        // Delete Command
        public ICommand DeteleCommand => new RelayCommand(async () =>
        {
            try
            {
                if (DeleteOptionSelected == "Parking lot" && !string.IsNullOrEmpty(AddressToDelete))
                {
                    bool answer = await dialogService.ShowConfirmAsync("Delete parking lot", $"Are you sure you want to delete the parking lot at address \'{AddressToDelete}\'?", "Yes", "No");
                    if (answer)
                    {
                        // Create a filter to find the data to delete based on the address
                        var filter = Builders<PrivateParking>.Filter.Eq(p => p.Address, AddressToDelete);

                        // Delete the private parking data from MongoDB
                        var result = await mongoDBService.DeleteData(CollectionName.PrivateParking, filter);

                        if (result.Success && result.DeleteCount > 0)
                        {
                            // Reset UI, PropertyAddresses picker after successful deletion
                            RefreshPage();
                            PropertyAddresses.Remove(AddressToDelete);
                            AddressToDelete = null;

                            await dialogService.ShowAlertAsync("Success", "The parking lot is deleted successfully.", "OK");
                        }
                    }
                }
                else if (DeleteOptionSelected == "A Floor" && !string.IsNullOrEmpty(AddressToDelete) && !string.IsNullOrEmpty(FloorToDelete))
                {
                    bool answer = await dialogService.ShowConfirmAsync("Delete floor", $"Are you sure you want to delete the floor \'{FloorToDelete}\' at address \'{AddressToDelete}\'?", "Yes", "No");
                    if (answer)
                    {
                        // Search for FloorInfo to delete
                        var floorToRemove = deletedFloorAddressSelected.FloorInfo.FirstOrDefault(f => f.Floor == FloorToDelete);
                        if (floorToRemove != null)
                        {
                            // Update the PrivateParking object in MongoDB
                            var filter = Builders<PrivateParking>.Filter.Eq(p => p.Id, deletedFloorAddressSelected.Id);
                            var update = Builders<PrivateParking>.Update.Set(p => p.FloorInfo, deletedFloorAddressSelected.FloorInfo);

                            await mongoDBService.UpdateData(CollectionName.PrivateParking, filter, update);

                            // Remove the floor from selected address, listFloorInfos (if users loaded it) and ListFloorsToDelete
                            deletedFloorAddressSelected.FloorInfo.Remove(floorToRemove);
                            listFloorInfos.Remove(floorToRemove);
                            ListFloorsToDelete.Remove(FloorToDelete);
                            FloorToDelete = null;
                            ResetFloorInfo();

                            await dialogService.ShowAlertAsync("Success", $"Floor \'{FloorToDelete}\' is deleted successfully.", "OK");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Floor {FloorToDelete} not found at address {AddressToDelete}.");
                        }
                    }
                }
                else
                {
                    await dialogService.ShowAlertAsync("Warning", "Please select an option to delete.", "OK");
                }

            }
            catch (Exception ex)
            {
                await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }
        });


        #region Control rectangle drawing
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
        #endregion

        private bool IsValid()
        {
            return !string.IsNullOrEmpty(CompanyName) &&
                    !string.IsNullOrEmpty(Address) &&
                    listFloorInfos.Count > 0 &&
                    latitude != 0 &&
                    longitude != 0;
        }

       private void RefreshPage()
        {
            CompanyName = "";
            Address = "";
            latitude = 0;
            longitude = 0;
            Fee = 0;
            LimitHour = 0;
            listFloorInfos?.Clear();
            SelectedFloorName = null;
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

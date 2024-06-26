using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ParkEase.Utilities;
using System.Windows.Input;
using ParkEase.Core.Contracts.Services;
using ParkEase.Contracts.Services;
using ParkEase.Core.Model;
using ParkEase.Core.Data;
using System.Collections.ObjectModel;
using MongoDB.Driver;
using ParkEase.Core.Services;
using IImage = Microsoft.Maui.Graphics.IImage;
using Microsoft.Maui.Graphics.Platform;
using System.Reflection;
using ZXing.Net.Maui;
using ParkEase.Page;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Controls;
using CommunityToolkit.Mvvm.Messaging;
using ParkEase.Messages;
using Microsoft.Maui.Devices.Sensors;


namespace ParkEase.ViewModel
{
    public partial class PrivateSearchViewModel : ObservableObject
    {
        private List<PrivateParking> parkingLotData;

        private List<AddressDistance> addressDistanceFullList;

        private readonly Location userLocation;

        private readonly IMongoDBService mongoDBService;

        private readonly IDialogService dialogService;

        private readonly ParkEaseModel parkEaseModel;

        private string idResult;

        private bool isNavigating = false;

        [ObservableProperty]
        private AddressDistance selectedAddress;

        [ObservableProperty]
        private ObservableCollection<AddressDistance> addressDistanceList;

        [ObservableProperty]
        private string addressMessage = "No matching addresses found";

        [ObservableProperty]
        private bool errorMessageVisable;

        [ObservableProperty]
        private string searchText;

        [ObservableProperty]
        private bool enableScanner;

        [ObservableProperty]
        private bool gridVisible;

        [ObservableProperty]
        private bool barcodeButtonVisible;

        [ObservableProperty]
        private bool enableExpender;

        [ObservableProperty]
        private string scannerText;

        [ObservableProperty]
        private string scannerImage = "qr_code.png";

        [ObservableProperty]
        private string arrowBack = "arrow_icon.png";

        [ObservableProperty]
        private BarcodeDetectionEventArgs barcodeDetectionEventArgs;

        public PrivateSearchViewModel(IMongoDBService mongoDBService, IDialogService dialogService, ParkEaseModel model)
        {

            userLocation = DataService.GetLocation();
            this.mongoDBService = mongoDBService;
            this.dialogService = dialogService;
            this.parkEaseModel = model;
            EnableScanner = true;
            GridVisible = false;
            BarcodeButtonVisible = true;
            errorMessageVisable = false;
            ScannerText = "";
            addressDistanceList = new ObservableCollection<AddressDistance>();
        }

        public ICommand LoadedCommand => new RelayCommand(async () =>
        {
            await LoadAddresses();
        });

        public ICommand UnLoadedCommand => new RelayCommand(() =>
        {
            AddressDistanceList = new ObservableCollection<AddressDistance>();
        });

        // Load address from database and sort by distance
        private async Task LoadAddresses()
        {  
            try
            {
                parkingLotData = await mongoDBService.GetData<PrivateParking>(CollectionName.PrivateParking);

                addressDistanceFullList = parkingLotData.Select(parkingLot => new AddressDistance
                {
                    Address = parkingLot.Address,
                    Distance = $"{CoordinateDistance(parkingLot.Latitude, parkingLot.Longitude).ToString("F2")} km"
                }).OrderBy(a => a.Distance).ToList();

                AddressDistanceList = new ObservableCollection<AddressDistance>(addressDistanceFullList);
            }
            catch (Exception ex)
            {
                await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }
        }

        partial void OnSearchTextChanged(string? value)
        {
            MatchedAddress();
        }


        private async Task MatchedAddress()
        {
            try
            {
                if (!string.IsNullOrEmpty(SearchText))
                {
                    var matchedAddresses = AddressDistanceList
                        .Where(a => a.Address.ToLower().Contains(SearchText.ToLower()))
                        .ToList();

                    AddressDistanceList = new ObservableCollection<AddressDistance>(matchedAddresses);

                    if (AddressDistanceList?.Count == 0)
                    {
                        ErrorMessageVisable = true;
                    }
                    else
                    {
                        ErrorMessageVisable = false;
                    }
                }
                else
                {
                    ErrorMessageVisable = false;
                    AddressDistanceList = new ObservableCollection<AddressDistance>(addressDistanceFullList);
                }
            }
            catch (Exception ex)
            {
                AddressMessage = $"Error: {ex.Message}";
            }
        }

        // Calculate distance between 2 locations
        private double CoordinateDistance(double latitude, double longtitude)
        {
            Location newLocation = new Location(latitude, longtitude);
            double distance = Location.CalculateDistance(userLocation, newLocation, DistanceUnits.Kilometers);
            return distance;
        }

        partial void OnSelectedAddressChanged(AddressDistance? value)
        {
            if (!isNavigating)
            {
                AddressSelectedCommand();
            }
        }

        private async Task AddressSelectedCommand()
        {
            try
            {
                isNavigating = true;
              
                idResult = parkingLotData.FirstOrDefault(data => data.Address == SelectedAddress.Address)?.Id;
                parkEaseModel.PrivateMapId = idResult;
                SelectedAddress = null;
                isNavigating = false;
                await Shell.Current.GoToAsync(nameof(PrivateMapPage));
            }
            catch (Exception ex)
            {
                await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }
        }

        public ICommand BarcodesDetectedCommand => new RelayCommand<string>(async qrCode =>
        {
            try
            {
                idResult = qrCode;
                GridVisible = !GridVisible;
                parkEaseModel.PrivateMapId = idResult;
                MainThread.BeginInvokeOnMainThread(MyMainThreadCode);
            }
            catch(Exception ex)
            {
                await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }

        });

        void MyMainThreadCode()
        {
            Shell.Current.GoToAsync(nameof(PrivateMapPage));
        }

        /*        public ICommand CloseCameraCommand => new RelayCommand(() =>
                {
                    GridVisible = !GridVisible;
                });*/

        [RelayCommand]
        public async Task ScannerButton()
        {
            try
            {
                //BarcodeButtonVisible = !BarcodeButtonVisible;
                GridVisible = !GridVisible;
            }
            catch (Exception ex)
            {
                await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }
        }
    }

    public class AddressDistance
    {
        public string Address { get; set; }
        public string Distance { get; set; }
    }
}
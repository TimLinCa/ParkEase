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

        [ObservableProperty]
        private AddressDistance selectedAddress;

        [ObservableProperty]
        private ObservableCollection<AddressDistance> addressDistanceList;

        [ObservableProperty]
        private string addressMessage;

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
        private string scannerImage;

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
            scannerImage = "qr_code.png";
            addressDistanceList = new ObservableCollection<AddressDistance>();
            _ = LoadAddresses();
        }

        // Load address from database and sort by distance
        private async Task LoadAddresses()
        {
            try
            {
                parkingLotData = await mongoDBService.GetData<PrivateParking>(CollectionName.PrivateParking);

                addressDistanceFullList = parkingLotData.Select(parkingLot => new AddressDistance
                {
                    Address = parkingLot.Address,
                    Distance = CoordinateDistance(parkingLot.Latitude, parkingLot.Longitude)
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
                        AddressMessage = "No matching addresses found";
                    }
                    else
                    {
                        AddressMessage = string.Empty;
                    }
                }
                else
                {
                    AddressMessage = string.Empty;
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
            AddressSelectedCommand();
        }

        private async Task AddressSelectedCommand()
        {
            try
            {
                idResult = parkingLotData.FirstOrDefault(data => data.Address == SelectedAddress.Address)?.Id;
                parkEaseModel.PrivateMapId = idResult;
                WeakReferenceMessenger.Default.Send<PrivateIdChangedMessage>(new PrivateIdChangedMessage(idResult));
                await Shell.Current.GoToAsync(nameof(PrivateMapPage));
                //await Shell.Current.GoToAsync(nameof(PrivateMapPage));
            }
            catch (Exception ex)
            {
                await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }
        }

        public ICommand BarcodesDetectedCommand => new RelayCommand<string>(async qrCode =>
        {
            //var result = qrCode;
            idResult = qrCode;
            GridVisible = !GridVisible;
            parkEaseModel.PrivateMapId = idResult;
            WeakReferenceMessenger.Default.Send<PrivateIdChangedMessage>(new PrivateIdChangedMessage(idResult));
            await Shell.Current.GoToAsync(nameof(PrivateMapPage));
        });

        [RelayCommand]
        public async Task ScannerButton()
        {
            try
            {
                BarcodeButtonVisible = !BarcodeButtonVisible;
                GridVisible = !GridVisible;
            }
            catch (Exception ex)
            {
                await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }
        }


        public ICommand NavigatePrivateMapPage => new RelayCommand(async () =>
        {
            // Implement the logic to navigate to the Forgot Password Page
            //await Shell.Current.GoToAsync(nameof(PrivateMapPage));
            await Shell.Current.GoToAsync(nameof(PrivateMapPage));
        });
    }

    public class AddressDistance
    {
        public string Address { get; set; }
        public double Distance { get; set; }
    }
}

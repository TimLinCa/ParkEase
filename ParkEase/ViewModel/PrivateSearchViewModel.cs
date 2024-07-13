using Camera.MAUI;
using Camera.MAUI.ZXing;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MongoDB.Driver;
using ParkEase.Contracts.Services;
using ParkEase.Controls;
using ParkEase.Core.Contracts.Services;
using ParkEase.Core.Data;
using ParkEase.Core.Model;
using ParkEase.Core.Services;
using ParkEase.Messages;
using ParkEase.Page;
using System.Collections.ObjectModel;
using System.Windows.Input;
using ZXing.Net.Maui;
using BarcodeFormat = Camera.MAUI.BarcodeFormat;

public delegate void StartCameraAsyncHandler();
public delegate void StopCameraAsyncHandler();

namespace ParkEase.ViewModel
{
    public partial class PrivateSearchViewModel : ObservableObject
    {
        private List<PrivateParking> parkingLotData;

        public List<AddressDistance> addressDistanceFullList { get; set; }

        private Location userLocation;

        private readonly IMongoDBService mongoDBService;

        private readonly IDialogService dialogService;

        private readonly ParkEaseModel parkEaseModel;

        public string IdResult;

        private bool isNavigating = false;


        [ObservableProperty]
        private AddressDistance selectedAddress;

        [ObservableProperty]
        private ObservableCollection<AddressDistance> addressDistanceList;

        [ObservableProperty]
        public string addressMessage = "No matching addresses found";

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

        [ObservableProperty]
        private IAsyncRelayCommand loadedCommand;

        public event StartCameraAsyncHandler StartCameraAsyncEvent;

        public event StopCameraAsyncHandler StopCameraAsyncEvent;

        private bool isDetecting = false;


        public PrivateSearchViewModel(IMongoDBService mongoDBService, IDialogService dialogService, ParkEaseModel model)
        {

            this.mongoDBService = mongoDBService;
            this.dialogService = dialogService;
            this.parkEaseModel = model;
            EnableScanner = true;
            GridVisible = true;
            BarcodeButtonVisible = true;
            errorMessageVisable = false;
            ScannerText = "";
            addressDistanceList = new ObservableCollection<AddressDistance>();
            LoadedCommand = new AsyncRelayCommand(LoadedCommandAsync);
        }

        public ICommand SimplePopupClickedCommand => new RelayCommand(async () =>
        {
            var page = new ScannerPopUp();
            await Application.Current.MainPage.ShowPopupAsync(page);
        });


        private async Task LoadedCommandAsync()
        {
            userLocation = DataService.GetLocation();
            await LoadAddresses();
        }

        public ICommand UnLoadedCommand => new RelayCommand(() =>
        {
            StopCamera();
            AddressDistanceList = new ObservableCollection<AddressDistance>();
        });

        // Load address from database and sort by distance
        private async Task LoadAddresses()
        {  
            try
            {
                parkingLotData = await mongoDBService.GetData<PrivateParking>(CollectionName.PrivateParking);

                // Sort by distance
                /*addressDistanceFullList = parkingLotData.Select(parkingLot => new AddressDistance
                {
                    Address = parkingLot.Address,
                    Distance = $"{CoordinateDistance(parkingLot.Latitude, parkingLot.Longitude).ToString("F2")} km"
                }).OrderBy(a => a.Distance).ToList();*/
                
                addressDistanceFullList = parkingLotData.Select(parkingLot => new AddressDistance
                {
                    Address = parkingLot.Address,
                    Distance = $"{CoordinateDistance(parkingLot.Latitude, parkingLot.Longitude).ToString("F2")} km"
                }).ToList();

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
              
                IdResult = parkingLotData.FirstOrDefault(data => data.Address == SelectedAddress.Address)?.Id;
                DataService.SetId(IdResult);
                //parkEaseModel.PrivateMapId = idResult;
                SelectedAddress = null;
                isNavigating = false;
                if(Shell.Current != null)
                {
                    await Shell.Current.GoToAsync(nameof(PrivateMapPage));
                }
            }
            catch (Exception ex)
            {
                await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }
        }

        public ICommand BarcodeDetectEventCommand => new RelayCommand<string>(async qrCode =>
        {
            try
            {
                IdResult = qrCode;
                StopCameraAsyncEvent?.Invoke();
                GridVisible = !GridVisible;
                DataService.SetId(IdResult);

                //parkEaseModel.PrivateMapId = idResult;
                MainThread.BeginInvokeOnMainThread(MyMainThreadCode);
            }
            catch (Exception ex)
            {
                await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }
        });

        private void OpenCamera()
        {
            StartCameraAsyncEvent?.Invoke();
            isDetecting = true;
            GridVisible = true;
        }

        private void StopCamera()
        {
            StopCameraAsyncEvent?.Invoke();
            isDetecting = false;
            GridVisible = false;
        }

        void MyMainThreadCode()
        {
            Shell.Current.GoToAsync(nameof(PrivateMapPage));
        }

        [RelayCommand]
        public async Task ScannerButton()
        {
            try
            {
                var page = new ScannerPopUp();
                await Application.Current.MainPage.ShowPopupAsync(page);
                /*if (isDetecting)
                {
                    StopCamera();
                }
                else
                {
                    OpenCamera();
                }*/

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
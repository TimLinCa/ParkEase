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


namespace ParkEase.ViewModel
{
    public partial class PrivateSearchViewModel : ObservableObject
    {
        

        private List<PrivateParking> parkingLotData;

        private List<string> addressList;

        private List<PrivateStatus> privateStatusData;

        private string address;
        private string city;
        private double fee;
        private string limitHour;
        private List<FloorInfo> listFloorInfos;

        //private List<FloorInfo> listFloorInfos;

        private readonly IMongoDBService mongoDBService;

        private readonly IDialogService dialogService;

        private ParkEaseModel parkEaseModel;

        private string idResult;



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
        private string searchText;

        [ObservableProperty]
        private BarcodeDetectionEventArgs barcodeDetectionEventArgs;
        [ObservableProperty]
        private string selectedAddress;

        [ObservableProperty]
        private ObservableCollection<string> addresses;

        [ObservableProperty]
        private string emailExistsMessage;

        private Location location;


        public PrivateSearchViewModel(IMongoDBService mongoDBService, IDialogService dialogService, ParkEaseModel model)
        {

            location = DataService.GetLocation();
            this.mongoDBService = mongoDBService;
            this.dialogService = dialogService;
            this.parkEaseModel = model;
            privateStatusData = new List<PrivateStatus>();

            EnableScanner = true;
            GridVisible = false;
            BarcodeButtonVisible = true;
            ScannerText = "";
            scannerImage = "scanner_image.png";

            Addresses = new ObservableCollection<string>();
            _ = LoadAddresses();
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
                    var matchedAddresses = addressList
                        .Where(a => a.ToLower().Contains(SearchText.ToLower()))
                        .ToList();

                    Addresses = new ObservableCollection<string>(matchedAddresses);

                    if (Addresses.Count()==0)
                    {
                        EmailExistsMessage = "No matching addresses found";
                    }
                    else
                    {
                        EmailExistsMessage = string.Empty;
                    }

                }
                else
                {
                    Addresses = new ObservableCollection<string>(addressList);
                }
            }
            catch (Exception ex)
            {
                EmailExistsMessage = $"Error: {ex.Message}";
            }
        }

        partial void OnSelectedAddressChanged(string? value)
        {
            AddressSelectedCommand();
        }

        private async Task AddressSelectedCommand()
        {
            try
            {

                idResult = parkingLotData.FirstOrDefault(data => data.Address == SelectedAddress)?.Id;
                DataService.SetId(idResult);
                await Shell.Current.GoToAsync(nameof(PrivateMapPage));
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
            DataService.SetId(idResult);
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

        private async Task LoadAddresses()
        {
            try
            {

                parkingLotData = await mongoDBService.GetData<PrivateParking>(CollectionName.PrivateParking);
                addressList = parkingLotData.Select(data => data.Address).ToList();
                /*addressList = new List<string>()
                {
                    "1628 17Ave NW",
                    "530 10Ave NW",
                    "666 park SE",
                    "17st SW",
                };*/


                Addresses = new ObservableCollection<string>(addressList);
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
}

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

        [ObservableProperty]
        private string barcodeResult;

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

        private string searchAddress;
        private ObservableCollection<string> addresses;
        private string emailExistsMessage;

        [ObservableProperty]
        private string selectedAddress;
        



        public PrivateSearchViewModel(IMongoDBService mongoDBService, IDialogService dialogService, ParkEaseModel model)
        {
            this.mongoDBService = mongoDBService;
            this.dialogService = dialogService;
            this.parkEaseModel = model;
            privateStatusData = new List<PrivateStatus>();

            BarcodeResult = string.Empty;
            EnableScanner = true;
            GridVisible = false;
            BarcodeButtonVisible = true;
            ScannerText = "";
            scannerImage = "scanner_image.png";

            Addresses = new ObservableCollection<string>();
            _ = LoadAddress();
        }

        public string SearchText
        {
            get => searchAddress;
            set
            {
                if (searchAddress != value)
                {
                    searchAddress = value;
                    OnPropertyChanged();
                    _ = MatchedAddress();
                }
            }
        }

        public ObservableCollection<string> Addresses
        {
            get => addresses;
            set
            {
                addresses = value;
                OnPropertyChanged();
            }
        }

        public string EmailExistsMessage
        {
            get => emailExistsMessage;
            set
            {
                emailExistsMessage = value;
                OnPropertyChanged();
            }
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



                    Addresses.Clear();

                    foreach (var address in matchedAddresses)
                    {
                        Addresses.Add(address);
                    }

                    if (matchedAddresses.Any())
                    {
                        EmailExistsMessage = "Matching addresses found";
                    }
                    else
                    {
                        EmailExistsMessage = "No matching addresses found";
                    }
                }
                else
                {
                    Addresses.Clear();

                    foreach (var address in addressList)
                    {
                        Addresses.Add(address);
                    }

                    EmailExistsMessage = string.Empty;
                }
            }
            catch (Exception ex)
            {
                EmailExistsMessage = $"Error: {ex.Message}";
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }













        public ICommand BarcodesDetectedCommand => new RelayCommand<string>(async qrCode =>
        {
            //var result = qrCode;
            BarcodeResult = qrCode;
            GridVisible = false;
            BarcodeButtonVisible = !BarcodeButtonVisible;
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

        private async Task LoadAddress()
        {
            try
            {
                var parkingLotData = await mongoDBService.GetData<PrivateParking>(CollectionName.PrivateParking);
                addressList = parkingLotData.Select(data => data.Address).ToList();

                foreach (var address in addressList)
                {
                    Addresses.Add(address);
                }
            }
            catch (Exception ex)
            {
                await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }
        }

        private void SearchBar_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        public ICommand NavigatePrivateMapPage => new RelayCommand(async () =>
        {
            // Implement the logic to navigate to the Forgot Password Page
            //await Shell.Current.GoToAsync(nameof(PrivateMapPage));
            await Shell.Current.GoToAsync(nameof(PrivateMapPage));
        });


    }
}

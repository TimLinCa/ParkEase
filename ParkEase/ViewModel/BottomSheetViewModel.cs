using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Devices.Sensors;
using MongoDB.Driver;
using ParkEase.Contracts.Services;
using ParkEase.Controls;
using ParkEase.Core.Contracts.Services;
using ParkEase.Core.Data;
using ParkEase.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;


namespace ParkEase.ViewModel
{
    public partial class BottomSheetViewModel : ObservableObject
    {
        [ObservableProperty]
        private string address;

        [ObservableProperty]
        private string parkingFee;

        [ObservableProperty]
        private string limitHour;

        [ObservableProperty]
        private string availability;

        [ObservableProperty]
        private bool showButton;

        [ObservableProperty]
        private string lat;

        [ObservableProperty]
        private string lng;

        public ICommand GetDirectionsCommand { get; }
        public ICommand OpenInGoogleMapsCommand { get; }

        private IMongoDBService mongoDBService;
        private IDialogService dialogService;
        public BottomSheetViewModel(IMongoDBService mongoDBService, IDialogService dialogService)
        {
            this.mongoDBService = mongoDBService;
            this.dialogService = dialogService;
            GetDirectionsCommand = new AsyncRelayCommand(OnGetDirections);
            OpenInGoogleMapsCommand = new AsyncRelayCommand(OnOpenInGoogleMaps);
        }

        public BottomSheetViewModel()
        {
            address = "";
            parkingFee = "";
            limitHour = "";
            availability = "";
            showButton = false;
            lat = "";
            lng = "";

            GetDirectionsCommand = new AsyncRelayCommand(OnGetDirections);
            OpenInGoogleMapsCommand = new AsyncRelayCommand(OnOpenInGoogleMaps);
        }


        private async Task OnGetDirections()
        {
            // Send a message to trigger the JavaScript function
            MessagingCenter.Send(this, "GetDirections");
        }

        private async Task OnOpenInGoogleMaps()
        {

            // Construct the URI for Google Maps
            string uri = $"https://www.google.com/maps/dir/?api=1&destination={Lat},{Lng}&travelmode=driving"; /*https://developers.google.com/maps/documentation/urls/get-started#directions-action*/

            // Open the URI
            await Launcher.OpenAsync(new Uri(uri)); /*https://learn.microsoft.com/en-us/dotnet/api/microsoft.maui.applicationmodel.launcher.openasync?view=net-maui-8.0#microsoft-maui-applicationmodel-launcher-openasync(system-uri)*/

        }

    }
}


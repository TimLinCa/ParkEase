using ParkEase.ViewModel;
using The49.Maui.BottomSheet;

namespace ParkEase.Controls
{
    public partial class MyBottomSheet : BottomSheet
    {
        public string Lat { get; set; }
        public string Lng { get; set; }
        public bool DismissedState { get; set; } = false;

        private bool isLocationSaved = false;
        public MyBottomSheet()
        {
            InitializeComponent();
            Lat = string.Empty;
            Lng = string.Empty;
            Dismissed += MyBottomSheet_Dismissed;
        }

        private void MyBottomSheet_Dismissed(object? sender, DismissOrigin e)
        {
            DismissedState = true;
        }

        private void GetDirecitonsCommand(object sender, EventArgs e)
        {
            MessagingCenter.Send(this, "GetDirections");
        }

        private async void OpenInGoogleMapsCommand(object sender, EventArgs e)
        {
            // Construct the URI for Google Maps
            string uri = $"https://www.google.com/maps/dir/?api=1&destination={Lat},{Lng}&travelmode=driving"; /*https://developers.google.com/maps/documentation/urls/get-started#directions-action*/

            // Open the URI
            await Launcher.OpenAsync(new Uri(uri)); /*https://learn.microsoft.com/en-us/dotnet/api/microsoft.maui.applicationmodel.launcher.openasync?view=net-maui-8.0#microsoft-maui-applicationmodel-launcher-openasync(system-uri)*/
        }

        private void SaveOrRemoveParkingLocationCommand(object sender, EventArgs e)
        {
            if (!isLocationSaved)
            {
                // Save the location
                MessagingCenter.Send(this, "SaveParkingLocation", (Lat, Lng));
                ParkingLocationIcon.Source = "removecar.png"; // Change icon
                ParkingLocationLabel.Text = "Clear Spot"; // Change text
            }
            else
            {
                // Remove the location
                MessagingCenter.Send(this, "RemoveParkingLocation", (Lat, Lng));
                ParkingLocationIcon.Source = "addcar.png"; // Change icon back
                ParkingLocationLabel.Text = "Save Spot"; // Change text back
            }
            isLocationSaved = !isLocationSaved;
        }

        private async void ShareSpotButton_Clicked(object sender, EventArgs e)
        {
            string locationUrl = $"https://www.google.com/maps/search/?api=1&query={Lat},{Lng}";
            await Share.RequestAsync(new ShareTextRequest
            {
                Title = "Share Parking Spot",
                Text = $"Check out this parking spot: {locationUrl}",
                Uri = locationUrl
            });
        }

        public void SetAddress(string address)
        {
            label_address.Text = address;
        }

        public void SetParkingFee(string parkingFee)
        {
            label_parkingFee.Text = parkingFee;
        }

        public void SetLimitHour(string limitHour)
        {
            label_limitHour.Text = limitHour;
        }

        public void SetAvailability(string availability)
        {
            label_availability.Text = availability;
        }

        public void SetVisibilityNavigatedButton(bool showButton)
        {
            vs_ButtonLayout.IsVisible = showButton;
        }

        public void SetLat(string lat)
        {
            Lat = lat;
        }

        public void SetLng(string lng)
        {
            Lng = lng;
        }
    }
}
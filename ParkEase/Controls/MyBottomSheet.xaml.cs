using ParkEase.ViewModel;
using The49.Maui.BottomSheet;
using System.Timers;
using Plugin.LocalNotification;
using System.Windows.Input;

namespace ParkEase.Controls
{
    public partial class MyBottomSheet : BottomSheet
    {
        public string Lat { get; set; }
        public string Lng { get; set; }
        public bool DismissedState { get; set; } = false;

        private bool isLocationSaved = false;

        private System.Timers.Timer _timer; 

        private DateTime _endTime;
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

        private void SaveOrRemoveParkingLocationCommand(object sender, TappedEventArgs e)
        {
            if (!isLocationSaved)
            {
                // Save the location
                MessagingCenter.Send(this, "SaveParkingLocation", (Lat, Lng));
                ParkingLocationIcon.Source = "removecar.png"; // Change the icon to indicate the spot is saved
                ParkingLocationLabel.Text = "Clear Spot"; // Change the label text to indicate the spot can be cleared
                MessagingCenter.Send(this, "UpdateWalkNavigationVisibility", true); // Show the WalkNavigation button
            }
            else
            {
                // Remove the location
                MessagingCenter.Send(this, "RemoveParkingLocation", (Lat, Lng));
                ParkingLocationIcon.Source = "addcar.png"; // Change the icon back to indicate the spot can be saved
                ParkingLocationLabel.Text = "Save Spot"; // Change the label text back to indicate the spot can be saved
                MessagingCenter.Send(this, "UpdateWalkNavigationVisibility", false); // Hide the WalkNavigation button
            }
            isLocationSaved = !isLocationSaved;  // Flip the value of isLocationSaved: if it was true, make it false; if it was false, make it true
        }

        private async void ShareSpotButton_Clicked(object sender, TappedEventArgs e)
        {
            // Create a URL for the location using Google Maps
            //https://developers.google.com/maps/documentation/urls/get-started
            string locationUrl = $"https://www.google.com/maps/search/?api=1&query={Lat},{Lng}";  

            // Open the share dialog with the location URL
            //https://learn.microsoft.com/en-us/previous-versions/xamarin/essentials/share?tabs=android
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
            hs_ButtonLayout.IsVisible = showButton;
        }

        public void SetLat(string lat)
        {
            Lat = lat;
        }

        public void SetLng(string lng)
        {
            Lng = lng;
        }

        private void OnStartTimerClicked(object sender, EventArgs e)
        {
            var selectedTime = timePicker.Time;
            _endTime = DateTime.Now.Add(selectedTime);

            _timer = new System.Timers.Timer(1000); 
            _timer.Elapsed += TimerElapsed;
            _timer.Start();
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            var remainingTime = _endTime - DateTime.Now;
            if (remainingTime <= TimeSpan.Zero)
            {
                _timer.Stop();
                ShowNotification();
                Device.BeginInvokeOnMainThread(() =>
                {
                    timerLabel.Text = "Time's up!";
                });
            }
            else
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    timerLabel.Text = remainingTime.ToString(@"hh\:mm\:ss");
                });
            }
        }

        private void ShowNotification()
        {
            var notification = new NotificationRequest
            {
                BadgeNumber = 1,
                Description = "Your parking timer has expired!",
                Title = "Parking Reminder",
                ReturningData = "TimerExpired",
                NotificationId = 1337
            };

            LocalNotificationCenter.Current.Show(notification); 
        }


    }
}
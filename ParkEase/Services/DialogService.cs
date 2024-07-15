using ParkEase.Contracts.Services;
using ParkEase.Controls;
using ParkEase.Core.Data;
using ParkEase.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkEase.Services
{

    public class DialogService : IDialogService
    {
        private MyBottomSheet currentBottomSheet;

        public Task ShowAlertAsync(string title, string message, string cancel = "OK")
        {
            return Application.Current.MainPage.DisplayAlert(title, message, cancel);
        }

        public Task<bool> ShowConfirmAsync(string title, string message, string accept = "Yes", string cancel = "No") // Add this method
        {
            return Application.Current.MainPage.DisplayAlert(title, message, accept, cancel);
        }

        public async Task<MyBottomSheet> ShowBottomSheet(string address, string parkingFee, string limitHour, string availability, bool ShowButton, string lat, string lng, bool isLocationSaved = false)
        {
   
            if (currentBottomSheet != null)
            {
                //https://github.com/the49ltd/The49.Maui.BottomSheet
                if (!currentBottomSheet.DismissedState)
                {
                    await currentBottomSheet.DismissAsync(); /*https://stackoverflow.com/questions/76626365/how-do-i-dismiss-the-bottom-sheet-from-a-button-click-event-inside-the-bottom-sh*/
                }
            }

            currentBottomSheet = new MyBottomSheet();

            currentBottomSheet.SetAddress(address);
            currentBottomSheet.SetParkingFee(parkingFee);
            currentBottomSheet.SetLimitHour(limitHour);
            currentBottomSheet.SetAvailability(availability);
            currentBottomSheet.SetVisibilityNavigatedButton(ShowButton);
            currentBottomSheet.SetLat(lat);
            currentBottomSheet.SetLng(lng);
            currentBottomSheet.SetIsLocationSaved(isLocationSaved);
            await currentBottomSheet.ShowAsync();
            return currentBottomSheet;
        }

        public async Task DismissBottomSheetAsync()
        {
            if (currentBottomSheet != null)
            {
                if (!currentBottomSheet.DismissedState)
                {
                    await currentBottomSheet.DismissAsync();
                }
                currentBottomSheet = null;
            }
        }

        public void UpdateBottomSheetAvailability(string availability)
        {
            if (currentBottomSheet != null && !currentBottomSheet.DismissedState)
            {
                currentBottomSheet.UpdateAvailability(availability);
            }
        }

        public async Task OpenGoogleMap(string Lat, string Lng,TravelMode trabelMode)
        {
            // Construct the URI for Google Maps
            string uri = $"https://www.google.com/maps/dir/?api=1&destination={Lat},{Lng}&travelmode={trabelMode.ToString().ToLower()}"; /*https://developers.google.com/maps/documentation/urls/get-started#directions-action*/

            // Open the URI
            await Launcher.OpenAsync(new Uri(uri)); /*https://learn.microsoft.com/en-us/dotnet/api/microsoft.maui.applicationmodel.launcher.openasync?view=net-maui-8.0#microsoft-maui-applicationmodel-launcher-openasync(system-uri)*/
        }
    }
}

using ParkEase.Contracts.Services;
using ParkEase.Controls;
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

        public async Task ShowBottomSheet(string address, string parkingFee, string limitHour, string availability, bool ShowButton, string lat, string lng)
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

            await currentBottomSheet.ShowAsync();
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

    }
}

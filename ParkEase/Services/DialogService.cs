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

        public async Task ShowBottomSheet(string address, string parkingFee, string limitHour, string availability, bool ShowButton, string lat, string lng)
        {

            if (currentBottomSheet != null)
            {
                await currentBottomSheet.DismissAsync();
            }

            var bottomSheetViewModel = new BottomSheetViewModel
            {
                Address = address,
                ParkingFee = parkingFee,
                LimitHour = limitHour,
                Availability = availability,
                ShowButton = ShowButton,
                Lat = lat,
                Lng = lng
            };

            //bottomSheetViewModel.Lat = lat;
            //bottomSheetViewModel.Lng = lng;

            currentBottomSheet = new MyBottomSheet(bottomSheetViewModel)
            {
                HasHandle = true,
                HandleColor = Colors.Black
            };

            await currentBottomSheet.ShowAsync();
        }

    }
}

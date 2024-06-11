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

        public Task ShowAlertAsync(string title, string message, string cancel = "OK")
        {
            return Application.Current.MainPage.DisplayAlert(title, message, cancel);
        }

        public Task ShowPrivateMapBottomSheet(string address, string parkingFee, string limitHour, string parkingCapacity, bool showParkingCapacity)
        {
            var bottomSheetViewModel = new BottomSheetViewModel
            {
                Address = address,
                ParkingFee = parkingFee,
                LimitHour = limitHour,
                ParkingCapacity = parkingCapacity,
                ShowParkingCapacity = showParkingCapacity
            };

            var sheet = new MyBottomSheet(bottomSheetViewModel)
            {
                HasHandle = true,
                HandleColor = Colors.Black
            };

            return sheet.ShowAsync();
        }
    }
}

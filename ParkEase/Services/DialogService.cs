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

        public Task ShowPrivateMapBottomSheet(string address, decimal parkingFee, int limitHours)
        {
            BottomSheetViewModel bottomSheetViewModel = new BottomSheetViewModel();
            bottomSheetViewModel.Address = address;
            bottomSheetViewModel.ParkingFee = Convert.ToDecimal(parkingFee);
            bottomSheetViewModel.LimitHour = Convert.ToInt32(limitHours);
            MyBottomSheet sheet = new MyBottomSheet(bottomSheetViewModel)
            {
                HasHandle = true,
                HandleColor = Colors.Black
            };

            return sheet.ShowAsync();

        }
    }
}

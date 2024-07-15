using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ParkEase.Controls;
using ParkEase.Core.Data;
namespace ParkEase.Contracts.Services
{
    public interface IDialogService
    {
        Task ShowAlertAsync(string title, string message, string cancel = "OK");

        Task<bool> ShowConfirmAsync(string title, string message, string accept = "Yes", string cancel = "No"); 

        Task<MyBottomSheet> ShowBottomSheet(string address, string parkingFee, string limitHour, string availability, bool ShowButton, string lat, string lng,bool isLocationSaved = false);

        Task DismissBottomSheetAsync();

        void UpdateBottomSheetAvailability(string availability);

        Task OpenGoogleMap(string Lat,string Lng, TravelMode travelMode);
    }
}

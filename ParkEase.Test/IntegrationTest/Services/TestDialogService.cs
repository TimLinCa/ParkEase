using Microsoft.Extensions.Configuration;
using NSubstitute.Core;
using ParkEase.Contracts.Services;
using ParkEase.Core.Contracts.Services;
using ParkEase.Core.Data;
using ParkEase.Core.Model;
using ParkEase.Core.Services;
using ParkEase.Controls;
using ParkEase.ViewModel;
using The49.Maui.BottomSheet;
using CollectionName = ParkEase.Core.Services.CollectionName;

namespace ParkEase.Test.IntegrationTest.Services
{
    public class TestDialogService : IDialogService
    {
        private List<(string Title, string Message, string Cancel)> _shownAlerts = new List<(string, string, string)>();
        private List<(string Address, string ParkingFee, string LimitHour, string Availability, bool ShowButton, string Lat, string Lng)> _shownBottomSheets = new List<(string, string, string, string, bool, string, string)>();
        private int _dismissBottomSheetCount = 0;

        public Task ShowAlertAsync(string title, string message, string cancel = "OK")
        {
            _shownAlerts.Add((title, message, cancel));
            return Task.CompletedTask;
        }

        public Task ShowBottomSheet(string address, string parkingFee, string limitHour, string availability, bool ShowButton, string lat, string lng)
        {
            _shownBottomSheets.Add((address, parkingFee, limitHour, availability, ShowButton, lat, lng));
            return Task.CompletedTask;
        }

        public Task<MyBottomSheet> DismissBottomSheetAsync()
        {
            _dismissBottomSheetCount++;
            return Task.FromResult<MyBottomSheet>(null);
        }

        // Methods to retrieve shown messages for testing
        public List<(string Title, string Message, string Cancel)> GetShownAlerts()
        {
            return _shownAlerts;
        }

        public List<(string Address, string ParkingFee, string LimitHour, string Availability, bool ShowButton, string Lat, string Lng)> GetShownBottomSheets()
        {
            return _shownBottomSheets;
        }

        public int GetDismissBottomSheetCount()
        {
            return _dismissBottomSheetCount;
        }

        public Task<bool> ShowConfirmAsync(string title, string message, string accept = "Yes", string cancel = "No")
        {
            return Task.FromResult(true);
        }

        public Task<MyBottomSheet> ShowBottomSheet(string address, string parkingFee, string limitHour, string availability, bool ShowButton, string lat, string lng, bool isLocationSaved = false)
        {
            return Task.FromResult<MyBottomSheet>(null);
        }

        Task IDialogService.DismissBottomSheetAsync()
        {
            return Task.CompletedTask;
        }

        public void UpdateBottomSheetAvailability(string availability)
        {
            return;
        }

        public Task OpenGoogleMap(string Lat, string Lng, TravelMode travelMode)
        {
            return Task.CompletedTask;
        }
    }
}

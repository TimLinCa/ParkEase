using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkEase.Contracts.Services
{
    public interface IDialogService
    {
        Task ShowAlertAsync(string title, string message, string cancel = "OK");

        Task ShowBottomSheet(string address, string parkingFee, string limitHour, string availability, bool ShowButton, string lat, string lng);

    }
}

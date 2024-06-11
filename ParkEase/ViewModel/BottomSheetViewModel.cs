using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkEase.ViewModel
{
    public partial class BottomSheetViewModel : ObservableObject
    {
        [ObservableProperty]
        private string address;

        [ObservableProperty]
        private string parkingFee;

        [ObservableProperty]
        private string limitHour;

        [ObservableProperty]
        private string parkingCapacity;

        [ObservableProperty]
        private bool showParkingCapacity;

        public BottomSheetViewModel()
        {
            address = "";
            parkingFee = "";
            limitHour = "";
            parkingCapacity = "";
            showParkingCapacity = false;
        }
    }
}

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
        private decimal parkingFee;

        [ObservableProperty]
        private int limitHour;

        public BottomSheetViewModel()
        {
            address = "";
            parkingFee = 0;
            limitHour = 0;
        }
    }
}

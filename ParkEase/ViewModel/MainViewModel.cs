using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkEase.ViewModel
{
    [QueryProperty("Email", "Email")]
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private string title;

        [ObservableProperty]
        private string email;

        public MainViewModel()
        {
            Title = "ParkEase";
            Email = "";
        }
    }
}

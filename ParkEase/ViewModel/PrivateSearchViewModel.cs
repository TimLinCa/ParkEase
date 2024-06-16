using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ParkEase.Utilities;
using System.Windows.Input;
using ParkEase.Core.Contracts.Services;
using ParkEase.Contracts.Services;
using ParkEase.Core.Model;
using ParkEase.Core.Data;
using System.Collections.ObjectModel;
using MongoDB.Driver;
using ParkEase.Core.Services;
using IImage = Microsoft.Maui.Graphics.IImage;
using Microsoft.Maui.Graphics.Platform;
using System.Reflection;
using ZXing.Net.Maui;
using ParkEase.Page;

namespace ParkEase.ViewModel
{
    public partial class PrivateSearchViewModel : ObservableObject
    {

        public ICommand NavigatePrivateMapPage => new RelayCommand(async () =>
        {
            // Implement the logic to navigate to the Forgot Password Page
            //await Shell.Current.GoToAsync(nameof(PrivateMapPage));
            await Shell.Current.GoToAsync($"//{nameof(PrivateMapPage)}");
        });

    }
}

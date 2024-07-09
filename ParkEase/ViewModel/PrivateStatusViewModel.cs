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
using CommunityToolkit.Mvvm.Messaging;
using ParkEase.Messages;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ParkEase.Page;
using ParkEase.Controls;

namespace ParkEase.ViewModel
{
    /*
     * What information are needed
     * - Company name
     * - Address(picker)
     * - Fee
     * - LimitedHour
     * 
     * - Floor(picker)
     */

    public partial class PrivateStatusViewModel : ObservableObject
    {

        private readonly IMongoDBService mongoDBService;

        private readonly IDialogService dialogService;

        [ObservableProperty]
        private string companyName;

        [ObservableProperty]
        private List<string> addressList;

        private List<FloorInfo> listFloorInfos;

        public PrivateStatusViewModel(IMongoDBService mongoDBService, IDialogService dialogService, ParkEaseModel model)
        {
            this.mongoDBService = mongoDBService;
            this.dialogService = dialogService;
        }
    }
}

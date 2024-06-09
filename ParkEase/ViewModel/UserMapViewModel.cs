using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Platform;
using MongoDB.Driver;
using Newtonsoft.Json;
using ParkEase.Contracts.Services;
using ParkEase.Controls;
using ParkEase.Core.Contracts.Services;
using ParkEase.Core.Data;
using ParkEase.Core.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace ParkEase.ViewModel
{
    public partial class UserMapViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<MapLine> mapLines;  //list on map

        [ObservableProperty]
        private MapLine selectedMapLine;

        private IMongoDBService mongoDBService;
        private IDialogService dialogService;
        public UserMapViewModel(IMongoDBService mongoDBService, IDialogService dialogService)
        {
            this.mongoDBService = mongoDBService;
            this.dialogService = dialogService;
        }

        partial void OnSelectedMapLineChanged(MapLine? value)
        {

        }

        public ICommand LoadedEventCommand => new RelayCommand<EventArgs>(async e =>
        {
            try
            {
                List<ParkingData> parkingDatas = await mongoDBService.GetData<ParkingData>(CollectionName.ParkingData);

                if (parkingDatas == null || !parkingDatas.Any())
                {
                    System.Diagnostics.Debug.WriteLine("No parking data found.");
                    return;
                }

                MapLines = new ObservableCollection<MapLine>(parkingDatas
                    .Where(pd => pd.Points.Count > 1)
                    .Select(pd => new MapLine(pd.Points)).ToList());

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in draw_lines: {ex.Message}");
            }
        });
    }
}

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
    public partial class MapViewModel : ObservableObject
    {
        // Automatically generate property change notification - any changes to the properties update UI
        [ObservableProperty]
        private string parkingSpot;

        [ObservableProperty]
        private string parkingCapacity;

        [ObservableProperty]
        private Location? startLocation; // The starting point of the line

        [ObservableProperty]
        private ObservableCollection<MapLine> mapLines;  //list on map

        [ObservableProperty]
        private MapLine selectedMapLine;

        [ObservableProperty]
        private bool drawingLine;

        [ObservableProperty]
        private ParkingData selectedParkingData;

        [ObservableProperty]
        private string selectedParkingTime;

        [ObservableProperty]
        private string selectedParkingFee;

        private readonly IMongoDBService mongoDBService;
        private readonly IDialogService dialogService;

        public ObservableCollection<string> ParkingTimes { get; }

        public ObservableCollection<string> ParkingFees { get; }

        private object mapWebView;
        public class Polyline
        {
            public int Index { get; set; }
            public List<Location> Points { get; set; }

            public Polyline()
            {
                Points = new List<Location>();
            }
        }

        public MapViewModel(IMongoDBService mongoDBService, IDialogService dialogService)
        {
            this.mongoDBService = mongoDBService;
            this.dialogService = dialogService;
            parkingSpot = "";
            parkingCapacity = "";
            startLocation = null;

            ParkingTimes = new ObservableCollection<string>   /*https://www.calgaryparking.com/find-parking/on-street.html*/
            {
                "Mon to Fri: 7am to 6pm",
                "Sat: 9am to 6pm",
                "Sun and holidays",
                "Evening after 6pm"
            };

            ParkingFees = new ObservableCollection<string> /*https://thecityofcalgary.maps.arcgis.com/apps/instant/sidebar/index.html?appid=10fd81aba2a548d49e7731f593c36282*/
            {
                "Free",
                "$1.50 per hour",
                "$2.00 per hour"
            };
        }

        // Loads parking data for selected line
        partial void OnSelectedMapLineChanged(MapLine? value)
        {

            if (value != null)
            {
                LoadParkingData(value.Index);
            }
        }


        //Form submission and updates entry in MongoDB
        public ICommand SubmitCommand => new RelayCommand(async () =>
        {
            try
            {
                if (IsValid())
                {
                    var parkingData = new ParkingData
                    {
                        Index = SelectedMapLine.Index,
                        ParkingSpot = ParkingSpot,
                        ParkingTime = SelectedParkingTime,
                        ParkingFee = SelectedParkingFee,
                        ParkingCapacity = ParkingCapacity,
                        Points = SelectedMapLine.Points
                    };

                    var filter = Builders<ParkingData>.Filter.Eq(p => p.Index, parkingData.Index);
                    var update = Builders<ParkingData>.Update
                        .Set(p => p.ParkingSpot, parkingData.ParkingSpot)
                        .Set(p => p.ParkingTime, parkingData.ParkingTime)
                        .Set(p => p.ParkingFee, parkingData.ParkingFee)
                        .Set(p => p.ParkingCapacity, parkingData.ParkingCapacity)
                        .Set(p => p.Points, parkingData.Points);

                    var existingData = await mongoDBService.GetData<ParkingData>(CollectionName.ParkingData);
                    if (existingData.Any(d => d.Index == parkingData.Index))
                    {
                        await mongoDBService.UpdateData(CollectionName.ParkingData, filter, update);
                        await dialogService.ShowAlertAsync("Success", "Your information is updated.", "OK");
                    }
                    else
                    {
                        await mongoDBService.InsertData(CollectionName.ParkingData, parkingData);
                        await dialogService.ShowAlertAsync("Success", "Your information is submitted.", "OK");
                    }
                    await LoadParkingData(SelectedMapLine.Index);
                }
                else
                {
                    await dialogService.ShowAlertAsync("Warning", "Please fill in all fields.", "OK");
                }
            }
            catch (Exception ex)
            {
                await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }
        });

        // check if all required fields are filled
        private bool IsValid()
        {
            return !string.IsNullOrEmpty(ParkingSpot) &&
                   !string.IsNullOrEmpty(SelectedParkingTime) &&
                   !string.IsNullOrEmpty(SelectedParkingFee) &&
                   !string.IsNullOrEmpty(ParkingCapacity) &&
                   SelectedMapLine != null &&
                   SelectedMapLine.Points != null &&
                   SelectedMapLine.Points.Count == 2;
        }

        // loads parking data from MongoDB for given index and update properties
        public async Task LoadParkingData(int index)
        {
            var data = await mongoDBService.GetData<ParkingData>(CollectionName.ParkingData);
            SelectedParkingData = data.FirstOrDefault(d => d.Index == index);
            if (SelectedParkingData != null)
            {
                ParkingSpot = SelectedParkingData.ParkingSpot;
                SelectedParkingTime = SelectedParkingData.ParkingTime;
                SelectedParkingFee = SelectedParkingData.ParkingFee;
                ParkingCapacity = SelectedParkingData.ParkingCapacity;
            }

        }


        // Reset selected parking data in side panel
        public void ResetSidePanelData()
        {
            SelectedParkingData = null;
        }

        public ICommand MapNavigatedCommand => new RelayCommand<WebNavigatedEventArgs>(async e =>
        {
            try
            {
                List<ParkingData> parkingDatas = await mongoDBService.GetData<ParkingData>(CollectionName.ParkingData);

                if (parkingDatas == null || !parkingDatas.Any())
                {
                    System.Diagnostics.Debug.WriteLine("No parking data found.");
                    return;
                }

                if (parkingDatas.Count > 0)
                {
                    parkingDatas = parkingDatas.OrderBy(pd => pd.Index).ToList();
                    MapLines = new ObservableCollection<MapLine>(parkingDatas.Where(pd => pd.Points.Count > 1).Select(pd => new MapLine(pd.Points) { Index = pd.Index }).ToList());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in draw_lines: {ex.Message}");
            }
        });

        public ICommand DrawCommand => new RelayCommand(() =>
        {
            DrawingLine = !DrawingLine;
        });

        public ICommand DeletedLineCommand => new RelayCommand(async () =>
        {
            try
            {
                if (SelectedMapLine != null)
                {
                    MapLines.Remove(SelectedMapLine);
                    int lineIndex = SelectedMapLine.Index;
                    // Create a filter to match the line with the specified index
                    var filter = Builders<ParkingData>.Filter.Eq(p => p.Index, lineIndex);

                    // Delete the line data from MongoDB
                    var result = await mongoDBService.DeleteData(CollectionName.ParkingData, filter);

                    if (result.DeletedCount > 0)
                    {
                        // Reload all remaining lines from the database
                        var remainingLines = await mongoDBService.GetData<ParkingData>(CollectionName.ParkingData);

                        // Reassign indexes to be consecutive starting from 1
                        int newIndex = lineIndex;
                        foreach (var line in remainingLines.Where(line => line.Index > newIndex).OrderBy(l => l.Index))
                        {

                            var updateFilter = Builders<ParkingData>.Filter.Eq(p => p.Id, line.Id);
                            var update = Builders<ParkingData>.Update.Set(p => p.Index, newIndex);
                            await mongoDBService.UpdateData(CollectionName.ParkingData, updateFilter, update);
                            newIndex++;
                        }

                        // Reset side panel data after successful deletion
                        ResetSidePanelData();
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle any errors that occur during the deletion process
                await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }

          
        });
    }
}


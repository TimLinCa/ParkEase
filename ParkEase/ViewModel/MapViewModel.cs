using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Platform;
using MongoDB.Driver;
using ParkEase.Contracts.Services;
using ParkEase.Core.Contracts.Services;
using ParkEase.Core.Data;
using ParkEase.Core.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace ParkEase.ViewModel
{
    public partial class MapViewModel : ObservableObject
    {

        [ObservableProperty]
        private string parkingSpot;

        [ObservableProperty]
        private string parkingCapacity;

        [ObservableProperty]
        private string locationInfo;

        [ObservableProperty]
        private bool draw; // Indicates whether the line is drawn or not

        [ObservableProperty]
        private Location? startLocation; // The starting point of the line

        [ObservableProperty]
        private Line? selectedLine; // Selected line

        [ObservableProperty]
        private List<Line> lines; //list on map

        [ObservableProperty]
        private ParkingData selectedParkingData;

        [ObservableProperty]
        private string selectedParkingTime;

        [ObservableProperty]
        private string selectedParkingFee;

        private readonly IMongoDBService mongoDBService;
        private readonly IDialogService dialogService;
        private static int currentMaxIndex = 0; // Initialize the index counter

        // Action to evaluate JavaScript
        public Func<string, Task<string>> EvaluateJavaScript { get; set; }


        public ObservableCollection<string> ParkingTimes { get; }


        // New properties for Parking Fee Picker
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
            locationInfo = "";
            draw = false;
            startLocation = null;
            selectedLine = new Line();
            lines = new List<Line>();

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

            InitializeIndexCounter();
        }

        private async void InitializeIndexCounter()
        {
            // Optionally, retrieve the current max index from MongoDB
            var data = await mongoDBService.GetData<ParkingData>(CollectionName.ParkingData);
            currentMaxIndex = data.Any() ? data.Max(d => d.Index) : 0;
        }


        partial void OnSelectedLineChanged(Line? value)
        {

            if (value != null)
            {
                LoadParkingData(value.Index);
              
            }
        }

        public ICommand SubmitCommand => new RelayCommand(async () =>
        {
            try
            {
                if (IsValid())
                {
                    if (SelectedLine.Index <= 0) // New line, assign new unique index
                    {
                        SelectedLine.Index = ++currentMaxIndex;
                    }

                    var parkingData = new ParkingData
                    {
                        Index = SelectedLine.Index,
                        ParkingSpot = ParkingSpot,
                        ParkingTime = SelectedParkingTime,
                        ParkingFee = SelectedParkingFee,
                        ParkingCapacity = ParkingCapacity,
                        Points = SelectedLine.Points
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

        private bool IsValid()
        {
            return !string.IsNullOrEmpty(ParkingSpot) &&
                   !string.IsNullOrEmpty(SelectedParkingTime) &&
                   !string.IsNullOrEmpty(SelectedParkingFee) &&
                   !string.IsNullOrEmpty(ParkingCapacity) &&
                   SelectedLine != null &&
                   SelectedLine.Points != null &&
                   SelectedLine.Points.Count > 0;
        }

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

        public async Task DeleteLineDataAsync(int lineIndex)
        {
            try
            {
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

                    SelectedLine = null;
                }
            }
            catch (Exception ex)
            {
                // Handle any errors that occur during the deletion process
                await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }
        }

        private void ResetFormFields()
        {
            ParkingSpot = string.Empty;
            SelectedParkingTime = null;
            SelectedParkingFee = null;
            ParkingCapacity = string.Empty;
        }
        public void ResetSidePanelData()
        {
            SelectedParkingData = null;
        }
    }
}


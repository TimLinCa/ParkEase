using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Platform;
using MongoDB.Bson.Serialization;
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

        [ObservableProperty]
        private string availableSpots;

        private IMongoDBService mongoDBService;
        private IDialogService dialogService;
        public UserMapViewModel(IMongoDBService mongoDBService, IDialogService dialogService)
        {
            this.mongoDBService = mongoDBService;
            this.dialogService = dialogService;
        }

        //partial void OnSelectedMapLineChanged(MapLine? value)
        //{

        //}

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

                var lines = new ObservableCollection<MapLine>();

                foreach (var pd in parkingDatas)
                {
                    if (pd.Points.Count > 1)
                    {
                        var color = await GetLineColor(pd.Id); // Get the color based on parking availability
                        lines.Add(new MapLine(pd.Points, color));
                    }
                }

                MapLines = lines;

                // Load and calculate available parking spots
                await LoadAvailableSpots(null); // Load with no specific parkingDataId initially

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in draw_lines: {ex.Message}");
            }
        });

        private async Task<string> GetLineColor(string parkingDataId)
        {
            var statuses = await mongoDBService.GetData<PublicStatus>(CollectionName.PublicStatus);
            var matchingStatuses = statuses.Where(status => status.AreaId == parkingDataId);
            var availableSpots = matchingStatuses.Count(status => !status.Status);

            return availableSpots > 0 ? "green" : "red";  // Return the color based on availability
        }

        private async Task LoadAvailableSpots(string parkingDataId)
        {
            try
            {
                var statuses = await mongoDBService.GetData<PublicStatus>(CollectionName.PublicStatus);

                if (string.IsNullOrEmpty(parkingDataId))
                {
                    // If no specific parkingDataId, do a general count
                    var count = statuses.Count(status => !status.Status); // Count where status is false
                    availableSpots = count.ToString();
                }
                else
                {
                    var matchingStatuses = statuses.Where(status => status.AreaId == parkingDataId);

                    if (matchingStatuses.Any())
                    {
                        var count = matchingStatuses.Count(status => !status.Status); // Count where status is false
                        availableSpots = count.ToString();
                    }
                    else
                    {
                        AvailableSpots = "No"; // No matching areaId
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading available spots: {ex.Message}");
            }
        }

        public async Task OnLineClicked(MapLine selectedLine)
        {
            if (selectedLine != null)
            {
                try
                {
                    // Create a filter to match the selected line's points
                    var filter = Builders<ParkingData>.Filter.Eq(pd => pd.Points, selectedLine.Points);

                    // Fetch the parking data that matches the filter from MongoDB database
                    List<ParkingData> parkingDataList = await mongoDBService.GetDataFilter<ParkingData>(CollectionName.ParkingData, filter);

                    // Check if the parking data is not null and has at least one item
                    if (parkingDataList != null && parkingDataList.Count > 0)
                    {
                        // Get the first parking data from the list
                        var parkingData = parkingDataList.First();

                        // Extract necessary information from the parking data
                        var address = parkingData.ParkingSpot;
                        var parkingFee = parkingData.ParkingFee;
                        var limitedHour = parkingData.ParkingTime;
                        var parkingDataId = parkingData.Id; // Assuming `Id` is the property name for the parking data ID

                        // Load and calculate available parking spots
                        await LoadAvailableSpots(parkingDataId);

                        // Show the bottom sheet with the line's information
                        await dialogService.ShowPrivateMapBottomSheet(address, parkingFee, limitedHour, $"{availableSpots} Available Spots");
                    }
                    else
                    {
                        await dialogService.ShowAlertAsync("No Data Found", "No parking data found for the selected line.");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error retrieving parking data: {ex.Message}");
                }
            }
        }

    }
}
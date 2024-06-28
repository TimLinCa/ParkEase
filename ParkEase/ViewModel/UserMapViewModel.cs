using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
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
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace ParkEase.ViewModel
{
    public partial class UserMapViewModel : ObservableObject
    {
        private List<MapLine> dbMapLines;
        private List<PrivateParking> allPrivateParkings;

        [ObservableProperty]
        private ObservableCollection<MapLine> mapLines; 

        [ObservableProperty]
        private MapLine selectedMapLine;

        [ObservableProperty]
        private string availableSpots;

        [ObservableProperty]
        private string selectedRadius;

        [ObservableProperty]
        private double radius;

        [ObservableProperty]
        private double markerLatitude;

        [ObservableProperty]
        private double markerLongitude;

        [ObservableProperty]
        private bool showRedLines;

        private Location? location;
        private Task loadingLocationTask;

        private readonly IMongoDBService mongoDBService;
        private readonly IDialogService dialogService;
        public UserMapViewModel(IMongoDBService mongoDBService, IDialogService dialogService)
        {
            this.mongoDBService = mongoDBService;
            this.dialogService = dialogService;
            ShowRedLines = true;
        }

        public ICommand PageLoadedCommand => new RelayCommand(async() =>
        {
            
        });

        public ICommand LoadedEventCommand => new RelayCommand<EventArgs>(async e =>
        {
            await LoadMapDataAsync();
            await LoadPrivateParkingDataAsync();
            location = await Geolocation.GetLocationAsync();
        });

        private async Task LoadLocationTask()
        {
            location = await Geolocation.GetLocationAsync();
        }

        // Fetches parking data from the database and displays it on the map
        private async Task LoadMapDataAsync()
        {
            try
            {
                var parkingDatas = await mongoDBService.GetData<ParkingData>(CollectionName.ParkingData);
                if (parkingDatas == null || !parkingDatas.Any())
                {
                    System.Diagnostics.Debug.WriteLine("No parking data found.");
                    return;
                }

                var lines = new ObservableCollection<MapLine>(); // Collection of lines to be displayed on the map

                foreach (var pd in parkingDatas)
                {
                    if (pd.Points.Count > 1)
                    {
                        var color = await GetLineColorAsync(pd.Id); // Get the color of the line based on the availability of parking spots
                        lines.Add(new MapLine(pd.Points, color)); // Add the line to the collection
                    }
                }

                dbMapLines = new List<MapLine>(lines);
                await LoadAvailableSpotsAsync(null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading map data: {ex.Message}");
            }
        }

        private async Task LoadPrivateParkingDataAsync()
        {
            try
            {
                allPrivateParkings = await mongoDBService.GetData<PrivateParking>("PrivateParking");
                if (allPrivateParkings == null || !allPrivateParkings.Any())
                {
                    System.Diagnostics.Debug.WriteLine("No private parking data found.");
                    return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading private parking data: {ex.Message}");
            }
        }

        // Gets the color of the line based on the availability of parking spots
        private async Task<string> GetLineColorAsync(string parkingDataId)
        {
            var statuses = await mongoDBService.GetData<PublicStatus>(CollectionName.PublicStatus); // Get the statuses of the parking spots
            var availableSpots = statuses.Count(status => status.AreaId == parkingDataId && !status.Status); // Count the number of available spots where the status is false
            return availableSpots > 0 ? "green" : "red"; // Return green if there are available spots, red otherwise
        }

        // Loads the total number of available spots
        private async Task LoadAvailableSpotsAsync(string parkingDataId)
        {
            try
            {
                var statuses = await mongoDBService.GetData<PublicStatus>(CollectionName.PublicStatus);
                var count = string.IsNullOrEmpty(parkingDataId) // If the parkingDataId is null, count the total number of available spots
                    ? statuses.Count(status => !status.Status)  // Otherwise, count the number of available spots for the specific parkingDataId
                    : statuses.Count(status => status.AreaId == parkingDataId && !status.Status); // Count the number of available spots where the status is false

                AvailableSpots = count.ToString(); // Set the total number of available spots
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading available spots: {ex.Message}");
            }
        }

        // Handles the event when a line is clicked
        public async Task OnLineClickedAsync(MapLine selectedLine)
        {
            if (selectedLine == null) return;

            try
            {
                var filter = Builders<ParkingData>.Filter.Eq(pd => pd.Points, selectedLine.Points); // Filter the parking data based on the selected line
                var parkingDataList = await mongoDBService.GetDataFilter<ParkingData>(CollectionName.ParkingData, filter); // Get the parking data based on the filter

                if (parkingDataList == null || !parkingDataList.Any()) // If no parking data is found, show an alert
                {
                    await dialogService.ShowAlertAsync("No Data Found", "No parking data found for the selected line.");
                    return;
                }
                
                // Get the address, parking fee, limited hour, parking data id, latitude, and longitude
                var parkingData = parkingDataList.First();
                var address = parkingData.ParkingSpot;
                var parkingFee = parkingData.ParkingFee;
                var limitedHour = parkingData.ParkingTime;
                var parkingDataId = parkingData.Id;
                var lat = parkingData.Points[1].Lat;
                var lng = parkingData.Points[1].Lng;

                // Load the available spots and show the bottom sheet
                await LoadAvailableSpotsAsync(parkingDataId);
                
                // Show the bottom sheet with the address, parking fee, limited hour, available spots, and a button to show the directions
                await dialogService.ShowBottomSheet(address, parkingFee, limitedHour, $"{AvailableSpots} Available Spots", true, lat, lng);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving parking data: {ex.Message}");
            }
        }
        // Represents the command interface, used to implement the command pattern.
        //A specific class that implements ICommand, allowing you to define the logic to run when the command is executed.
        public ICommand UpdateRangeCommand => new RelayCommand(async() =>
        {
            
            if (string.IsNullOrEmpty(SelectedRadius))
            {
                System.Diagnostics.Debug.WriteLine("SelectedRadius is null or empty");
                return;
            }

            // Parse selected radius to double (meters to kilometers)
            if (!double.TryParse(SelectedRadius.Split(' ')[0], out double radius_out))
            {
                System.Diagnostics.Debug.WriteLine("Failed to parse SelectedRadius");
                return;
            }

            radius_out /= 1000.0;
            if(location == null) location = await Geolocation.GetLocationAsync(); ;

            // LINQ method to filter isPointInCircle: check if any point in the line.Points collection is within the specified radius from the given location (latitude and longitude).
            List<MapLine> linesInRange = dbMapLines.Where(line => isPointInCircle(line.Points, location.Latitude, location.Longitude, radius_out) && (ShowRedLines || line.Color != "red")).ToList();
            Radius = radius_out;
            MapLines = new ObservableCollection<MapLine>(linesInRange);

            // Clear existing markers
            MessagingCenter.Send(this, "ClearMarkers");

            // Filter private parking locations based on the distance range
            var privateParkingsInRange = allPrivateParkings.Where(pp => isPointInCircle(new List<MapPoint> { new MapPoint { Lat = pp.Latitude.ToString(), Lng = pp.Longitude.ToString() } }, location.Latitude, location.Longitude, radius_out)).ToList();

            // Update markers for the filtered private parking locations
            foreach (var privateParking in privateParkingsInRange)
            {
                System.Diagnostics.Debug.WriteLine($"Loaded private parking: {privateParking.Latitude}, {privateParking.Longitude}");
                MessagingCenter.Send(this, "AddMarker", (privateParking.Latitude, privateParking.Longitude, "Private Parking"));
            }
        });

        //From chatGPT 
        private bool isPointInCircle(List<MapPoint> points, double centerLat, double centerLng, double radius)
        {
            foreach(MapPoint point in points)
            {
                double pointLat = double.Parse(point.Lat);
                double pointLng = double.Parse(point.Lng);

                // Pythagorean theorem
                var distance = Math.Sqrt(Math.Pow(pointLat - centerLat, 2) + Math.Pow(pointLng - centerLng, 2));
                if (distance <= radius / 110.567) 
                {
                    return true;
                }
            }

            return false;
        }

    }
}
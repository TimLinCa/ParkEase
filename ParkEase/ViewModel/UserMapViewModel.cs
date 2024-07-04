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
using Microsoft.Maui.Dispatching;
using System.Linq;
using System.Net;
using ParkEase.Messages;

namespace ParkEase.ViewModel
{
    public partial class UserMapViewModel : ObservableObject
    {
        private List<MapLine> dbMapLines;
        private List<PrivateParking> allPrivateParkings;
        private string availableSpots;
        private bool isMapLoaded = false;
        private List<PublicStatus> publicStatuses;



        [ObservableProperty]
        private ObservableCollection<MapLine> mapLines;

        [ObservableProperty]
        private MapLine selectedMapLine;

        [ObservableProperty]
        private string selectedRadius;

        [ObservableProperty]
        private double radius;

        [ObservableProperty]
        private double markerLatitude;

        [ObservableProperty]
        private double markerLongitude;

        [ObservableProperty]
        private double locationLatitude;

        [ObservableProperty]
        private double locationLongitude;

        [ObservableProperty]
        private bool showPublicParking;

        [ObservableProperty]
        private bool showPrivateParking;

        [ObservableProperty]
        private bool showAvailableParking;

        [ObservableProperty]
        private string searchText;

        [ObservableProperty]
        private bool isSearchInProgress;

        [ObservableProperty]
        private Location centerLocation;


        private readonly IMongoDBService mongoDBService;
        private readonly IDialogService dialogService;
        private readonly IGeocodingService geocodingService;

        private CancellationTokenSource cts;
        //private readonly object lockObj = new object();
        //private bool stopping = false;
        public UserMapViewModel(IMongoDBService mongoDBService, IDialogService dialogService, IGeocodingService geocodingService)
        {
            this.mongoDBService = mongoDBService;
            this.dialogService = dialogService;
            this.geocodingService = geocodingService;

            // Subscribe to property changed events
            PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(ShowPublicParking) ||
                    args.PropertyName == nameof(ShowPrivateParking) ||
                    args.PropertyName == nameof(ShowAvailableParking))
                {
                    ApplyFilters();
                }
            };

        }

        public ICommand BackToCurrentLocationCommand => new RelayCommand(async () =>
        {
            IsSearchInProgress = false;
            CenterLocation = new Location { Latitude = LocationLatitude, Longitude = LocationLongitude };
        });

        public ICommand SearchCommand => new RelayCommand(async () =>
        {
            if (!string.IsNullOrEmpty(SearchText))
            {
                IsSearchInProgress = true;
                var location = await geocodingService.GetLocationAsync(SearchText);
                if (location != null)
                {
                    CenterLocation = location;
                    var locationtemp = DataService.GetLocation();
                    // Optionally, refresh the map or do other necessary updates here
                }
                else
                {
                    await dialogService.ShowAlertAsync("Location not found", "Unable to find the specified location.");
                }

            }
        });

        public ICommand LoadedCommand => new RelayCommand(async () =>
        {
            StartStatusRefreshLoop();
        });

        public ICommand UnLoadedCommand => new RelayCommand(async () =>
        {
            cts.Cancel();
        });

        private void StartStatusRefreshLoop()
        {
            cts = new CancellationTokenSource();
            var token = cts.Token;
            _ = Run(token); // Start the real-time update loop
        }

        private async Task Run(CancellationToken token)
        {
            //Run this while in another thread
            await Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    if (isMapLoaded == false) continue;
                    try
                    {
                        await ApplyFilters();

                        await Task.Delay(1000, token);

                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Exception in refresh loop: {ex.Message}");
                    }
                }
            }, token);
        }


        public ICommand LoadedEventCommand => new RelayCommand<EventArgs>(async e =>
        {
            await LoadMapDataAsync();
            await LoadPrivateParkingDataAsync();
            isMapLoaded = true;
        });

        partial void OnSelectedMapLineChanged(MapLine? value)
        {
            if (value == null)
            {
                dialogService.DismissBottomSheetAsync();
                return;
            }
            try
            {
                LoadSeletedMapLine(value);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving parking data: {ex.Message}");
            }
        }

        private async Task LoadSeletedMapLine(MapLine? value)
        {
            try
            {
                var filter = Builders<ParkingData>.Filter.Eq(pd => pd.Points, SelectedMapLine.Points); // Filter the parking data based on the selected line
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
                //await dialogService.ShowBottomSheet(address, parkingFee, limitedHour, $"{AvailableSpots} Available Spots", true, lat, lng);

                await dialogService.ShowBottomSheet(address, parkingFee, limitedHour, $"{availableSpots} Available Spots", true, lat, lng);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving parking data: {ex.Message}");
            }
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

                var lines = new ObservableCollection<MapLine>(parkingDatas.Select(pd => new MapLine(pd.Points) { Id = pd.Id }).ToList()); // Collection of lines to be displayed on the map
                dbMapLines = new List<MapLine>(lines);
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
        private string GetLineColorAsync(string parkingDataId)
        {
            var test = publicStatuses.Where(status => status.AreaId.Equals(parkingDataId) && status.Status == false).ToList();
            var availableSpots = publicStatuses.Count(status => status.AreaId == parkingDataId && status.Status == false); // Count the number of available spots where the status is false
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

                availableSpots = count.ToString(); // Set the total number of available spots
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
                //await dialogService.ShowBottomSheet(address, parkingFee, limitedHour, $"{AvailableSpots} Available Spots", true, lat, lng);

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await dialogService.ShowBottomSheet(address, parkingFee, limitedHour, $"{availableSpots} Available Spots", true, lat, lng);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving parking data: {ex.Message}");
            }
        }
        // Represents the command interface, used to implement the command pattern.
        //A specific class that implements ICommand, allowing you to define the logic to run when the command is executed.
        public ICommand UpdateRangeCommand => new RelayCommand(async () =>
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

            // LINQ method to filter isPointInCircle: check if any point in the line.Points collection is within the specified radius from the given location (latitude and longitude).
            List<MapLine> linesInRange = dbMapLines.Where(line => isPointInCircle(line.Points, LocationLatitude, LocationLongitude, radius_out)).ToList();
            Radius = radius_out;
        });

        //From chatGPT 
        private bool isPointInCircle(List<MapPoint> points, double centerLat, double centerLng, double radius)
        {
            foreach (MapPoint point in points)
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

        private async Task ApplyFilters()
        {
            try
            {
                double lat = IsSearchInProgress ? CenterLocation.Latitude : LocationLatitude;
                double lng = IsSearchInProgress ? CenterLocation.Longitude : LocationLongitude;
                if (Radius == 0) return;
                publicStatuses = await mongoDBService.GetData<PublicStatus>(CollectionName.PublicStatus); // Get the statuses of the parking spots 
                List<MapLine> filteredLines = new List<MapLine>();
                List<PrivateParking> filteredPrivateParkings = new List<PrivateParking>();

                if (ShowPublicParking)
                {
                    filteredLines = dbMapLines.Where(line => isPointInCircle(line.Points, lat, lng, Radius)).ToList();

                    foreach (var pd in filteredLines)
                    {
                        if (pd.Points.Count > 1)
                        {
                            pd.Color = GetLineColorAsync(pd.Id); // Get the color of the line based on the availability of parking spots
                        }
                    }

                    if (ShowAvailableParking)
                    {
                        filteredLines = filteredLines.Where(line => line.Color == "green").ToList();
                    }


                    List<MapLine> filteredLinesToAdd = new List<MapLine>();
                    List<MapLine> filteredLinesToUpdate = new List<MapLine>();
                    List<MapLine> filteredLinesToDelete = new List<MapLine>();

                    if (MapLines == null)
                    {
                        MapLines = new ObservableCollection<MapLine>();
                        filteredLinesToAdd = filteredLines;
                    }
                    else
                    {
                        foreach (MapLine filteredLine in filteredLines)
                        {
                            if (!MapLines.Any(mapline => mapline.Id == filteredLine.Id)) filteredLinesToAdd.Add(filteredLine);
                            else
                            {
                                if (MapLines.First(mapline => mapline.Id == filteredLine.Id).Color != filteredLine.Color) filteredLinesToUpdate.Add(filteredLine);
                            }
                        }

                        foreach (MapLine mapline in MapLines)
                        {
                            if (filteredLines.Any(line => line.Id == mapline.Id) == false) filteredLinesToDelete.Add(mapline);
                        }
                    }

                    foreach (MapLine mapLine in filteredLinesToDelete)
                    {
                        MapLine lineToRemove = MapLines.FirstOrDefault(line => line.Id == mapLine.Id);
                        if (lineToRemove != null) await RemoveMapLine(lineToRemove);

                    }

                    foreach (MapLine mapLine in filteredLinesToUpdate)
                    {
                        MapLine lineToRemove = MapLines.FirstOrDefault(line => line.Id == mapLine.Id);
                        if (lineToRemove != null) await RemoveMapLine(lineToRemove);
                        await AddMapLine(mapLine);
                    }

                    foreach (MapLine mapLine in filteredLinesToAdd)
                    {
                        await AddMapLine(mapLine);
                    }
                }
                else
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        MapLines = new ObservableCollection<MapLine>();
                    });
                }

                if (ShowPrivateParking)
                {
                    filteredPrivateParkings = allPrivateParkings.Where(pp => isPointInCircle(new List<MapPoint> { new MapPoint { Lat = pp.Latitude.ToString(), Lng = pp.Longitude.ToString() } }, lat, lng, Radius)).ToList();
                }
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    MessagingCenter.Send(this, "ClearMarkers");
                });


                foreach (var privateParking in filteredPrivateParkings)
                {
                    System.Diagnostics.Debug.WriteLine($"Loaded private parking: {privateParking.Latitude}, {privateParking.Longitude}");
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        MessagingCenter.Send(this, "AddMarker", (privateParking.Latitude, privateParking.Longitude, "Private Parking"));
                    });
                }
            }
            catch (Exception ex)
            {

                throw;
            }

        }

        private async Task RemoveMapLine(MapLine mapLine)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                MapLines.Remove(mapLine);
            });
        }

        private async Task AddMapLine(MapLine mapLine)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                MapLines.Add(mapLine);
            });
        }

    }
}
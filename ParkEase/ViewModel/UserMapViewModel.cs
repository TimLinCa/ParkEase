using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MongoDB.Driver;
using ParkEase.Contracts.Services;
using ParkEase.Controls;
using ParkEase.Core.Contracts.Services;
using ParkEase.Core.Data;
using ParkEase.Core.Services;
using ParkEase.Messages;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;

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
        private double selectedRadius;

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
        private bool showPublicParking = true;

        [ObservableProperty]
        private bool showPrivateParking;

        [ObservableProperty]
        private bool showAvailableParking = true;

        [ObservableProperty]
        private string searchText;

        [ObservableProperty]
        private ObservableCollection<SearchResultItemViewModel> searchAddress;

        [ObservableProperty]
        private SearchResultItemViewModel selectedSuggestion;

        [ObservableProperty]
        private bool isSuggestionsVisible;

        [ObservableProperty]
        private bool research = true;

        [ObservableProperty]
        private bool mapVisible;

        [ObservableProperty]
        private bool isSearchInProgress;

        [ObservableProperty]
        private Location centerLocation;

        [ObservableProperty]
        private bool isWalkNavigationVisible;

        [ObservableProperty]
        private IAsyncRelayCommand loadedEventCommand;



        private readonly IMongoDBService mongoDBService;
        private readonly IDialogService dialogService;
        private readonly IGeocodingService geocodingService;
        private bool isRangeUpdated = false;

        private CancellationTokenSource cts;

        //private readonly object lockObj = new object();
        //private bool stopping = false;
        public UserMapViewModel(IMongoDBService mongoDBService, IDialogService dialogService, IGeocodingService geocodingService)
        {
            this.mongoDBService = mongoDBService;
            this.dialogService = dialogService;
            this.geocodingService = geocodingService;

            LoadedEventCommand = new AsyncRelayCommand(ExecuteLoadedEventCommand);

            // Check and restore saved location
            RestoreSavedLocation();

            // Subscribe to messaging center messages
            MessagingCenter.Subscribe<MyBottomSheet, bool>(this, "UpdateWalkNavigationVisibility", (sender, isVisible) =>
            {
                IsWalkNavigationVisible = isVisible;
            });

        }

        partial void OnSearchTextChanged(string value)
        {
            Task.Run(async () =>
            {
                if (string.IsNullOrEmpty(value))
                {
                    SearchAddress = new ObservableCollection<SearchResultItemViewModel>();
                    IsSuggestionsVisible = false;
                    MapVisible = true;
                    return;
                }

                var list = await geocodingService.GetPredictedAddressAsync(value, LocationLatitude, LocationLongitude);

                List<SearchResultItem> searchResultItems = list;

                List<SearchResultItemViewModel> SearchResultItemViewModels = new();
                foreach (SearchResultItem item in searchResultItems)
                {
                    SearchResultItemViewModels.Add(new SearchResultItemViewModel()
                    {
                        AddressName = item.AddressName,
                        SecondaryText = item.SecondaryText,
                        Distance = item.Distance
                    });
                }

                SearchAddress = new ObservableCollection<SearchResultItemViewModel>(SearchResultItemViewModels);
                IsSuggestionsVisible = SearchAddress.Count > 0;
                MapVisible = SearchAddress.Count == 0;
            });
        }

        partial void OnSelectedSuggestionChanged(SearchResultItemViewModel? value)
        {
            AddressSelected();
        }

        private async Task AddressSelected()
        {
            MapVisible = true;
            try
            {
                if (string.IsNullOrEmpty(SelectedSuggestion.AddressName))
                {
                    await dialogService.ShowAlertAsync("Warning", "Please input search text.", "OK");
                    return;
                }

                IsSearchInProgress = true;

                var location = await geocodingService.GetLocationAsync(SelectedSuggestion.AddressName);

                if (location != null)
                {
                    CenterLocation = location;
                    var locationTemp = DataService.GetLocation();
                }
                else
                {
                    await dialogService.ShowAlertAsync("Location not found", "Unable to find the specified location.");
                }
                IsSuggestionsVisible = false;
            }
            catch (Exception ex)
            {
                await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }
            finally
            {
                IsSearchInProgress = false;
            }
        }

        private async void RestoreSavedLocation()
        {
            string savedLat = await SecureStorage.Default.GetAsync("SavedParkingLat");
            string savedLng = await SecureStorage.Default.GetAsync("SavedParkingLng");

            if (!string.IsNullOrEmpty(savedLat) && !string.IsNullOrEmpty(savedLng))
            {
                IsWalkNavigationVisible = true;
            }
        }

        public ICommand BackToCurrentLocationCommand => new RelayCommand(() =>
        {
            IsSearchInProgress = false;
            CenterLocation = new Location { Latitude = LocationLatitude, Longitude = LocationLongitude };
        });

        public ICommand MapVisibleCommand => new RelayCommand(() =>
        {
            MapVisible = !MapVisible;
        });

        public ICommand SearchCommand => new AsyncRelayCommand(async () =>
        {
            try
            {
                if (string.IsNullOrEmpty(SearchText))
                {
                    await dialogService.ShowAlertAsync("Warning", "Please input search text.", "OK");
                    return;
                }

                IsSearchInProgress = true;

                var location = await geocodingService.GetLocationAsync(SearchText);

                if (location != null)
                {
                    CenterLocation = location;
                    var locationTemp = DataService.GetLocation();
                }
                else
                {
                    await dialogService.ShowAlertAsync("Location not found", "Unable to find the specified location.");
                }
            }
            catch (Exception ex)
            {
                await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }
            finally
            {
                IsSearchInProgress = false;
            }
        });

        public ICommand LoadedCommand => new RelayCommand(() =>
        {
            StartStatusRefreshLoop();
        });

        public ICommand UnLoadedCommand => new RelayCommand(() =>
        {
            cts.Cancel();
        });

        public ICommand ClearSavedSpotCommand => new RelayCommand(async () =>
        {
            bool answer = await dialogService.ShowConfirmAsync("Clear Saved Spot", "Do you want to clear the saved spot?");
            if (answer)
            {
                // Clear the saved spot
                await ClearSavedSpotAsync();
            }
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


        private async Task ExecuteLoadedEventCommand()
        {
            await LoadMapDataAsync();
            await LoadPrivateParkingDataAsync();
            isMapLoaded = true;
        }

        private async Task ClearSavedSpotAsync()
        {
            string savedLat = await SecureStorage.Default.GetAsync("SavedParkingLat");
            string savedLng = await SecureStorage.Default.GetAsync("SavedParkingLng");

            if (!string.IsNullOrEmpty(savedLat) && !string.IsNullOrEmpty(savedLng))
            {
                MessagingCenter.Send(this, "RemoveParkingLocation", (savedLat, savedLng));
                IsWalkNavigationVisible = false;

                // Remove the location from SecureStorage
                SecureStorage.Default.Remove("SavedParkingLat");
                SecureStorage.Default.Remove("SavedParkingLng");
            }
        }

        public async Task<bool> HasSavedSpotAsync(MapLine line)
        {
            string savedLat = await SecureStorage.Default.GetAsync("SavedParkingLat");
            string savedLng = await SecureStorage.Default.GetAsync("SavedParkingLng");

            if (string.IsNullOrEmpty(savedLat) || string.IsNullOrEmpty(savedLng))
            {
                return false;
            }

            // Check if the saved spot matches any point in the line
            return line.Points.Any(p => p.Lat == savedLat && p.Lng == savedLng);
        }


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

        private async Task<string> GetPrivateParkingColorAsync(string parkingId)
        {
            var statuses = await mongoDBService.GetData<PrivateStatus>("PrivateStatus");
            var availableSpots = statuses.Count(status => status.AreaId == parkingId && !status.Status);
            return availableSpots > 0 ? "green" : "red";
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
                await dialogService.ShowBottomSheet(address, parkingFee, limitedHour, $"{availableSpots} Available Spots", true, lat, lng);

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving parking data: {ex.Message}");
            }
        }
        // Represents the command interface, used to implement the command pattern.
        //A specific class that implements ICommand, allowing you to define the logic to run when the command is executed.
        public ICommand UpdateRangeCommand => new RelayCommand(() =>
        {
            double radius_out = SelectedRadius / 1000.0;

            // LINQ method to filter isPointInCircle: check if any point in the line.Points collection is within the specified radius from the given location (latitude and longitude).
            List<MapLine> linesInRange = dbMapLines.Where(line => isPointInCircle(line.Points, LocationLatitude, LocationLongitude, radius_out)).ToList();
            Radius = radius_out;
            isRangeUpdated = true;

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
                if (dbMapLines == null) return;
                double lat = IsSearchInProgress ? CenterLocation.Latitude : LocationLatitude;
                double lng = IsSearchInProgress ? CenterLocation.Longitude : LocationLongitude;
                if (Radius == 0) return;

                publicStatuses = await mongoDBService.GetData<PublicStatus>(CollectionName.PublicStatus); // Get the statuses of the parking spots 
                var privateStatuses = await mongoDBService.GetData<PrivateStatus>("PrivateStatus"); // Get the statuses of private parking spots
                List<MapLine> filteredLines = new List<MapLine>();
                List<PrivateParking> filteredPrivateParkings = new List<PrivateParking>();

                if (ShowPublicParking)
                {
                    filteredLines = dbMapLines.Where(line => isPointInCircle(line.Points, lat, lng, Radius)).ToList();

                    for (int i = 0; i < filteredLines.Count; i++)
                    {
                        string color = GetLineColorAsync(filteredLines[i].Id);
                        if (filteredLines[i].Color != color)
                        {
                            filteredLines[i] = (MapLine)filteredLines[i].Clone();
                            filteredLines[i].Color = color;
                        }
                    }

                    if (ShowAvailableParking)
                    {
                        filteredLines = filteredLines.Where(line => line.Color == "green").ToList();
                    }

                    if (isRangeUpdated)
                    {
                        MapLines = new ObservableCollection<MapLine>(filteredLines);
                        SelectedMapLine = null;
                        isRangeUpdated = false;
                        return;
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
                                if (MapLines.First(mapline => mapline.Id == filteredLine.Id).Color.Equals(filteredLine.Color) == false) filteredLinesToUpdate.Add(filteredLine);
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
                        if (ShowAvailableParking && mapLine.Color == "green") await AddMapLine(mapLine);
                        else if (!ShowAvailableParking) await AddMapLine(mapLine);
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

                    foreach (var privateParking in filteredPrivateParkings)
                    {
                        string color = await GetPrivateParkingColorAsync(privateParking.Id);

                        System.Diagnostics.Debug.WriteLine($"Loaded private parking: {privateParking.Latitude}, {privateParking.Longitude}");
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            MessagingCenter.Send(this, "AddMarker", (privateParking.Latitude, privateParking.Longitude, "Private Parking", color));
                        });
                    }
                }
                else
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        MessagingCenter.Send(this, "ClearMarkers");
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

        public async Task ShowPrivateParkingBottomSheet(PrivateParking privateParking)
        {
            if (privateParking == null)
            {
                await dialogService.ShowBottomSheet(
                    "No Data",
                    "N/A",
                    "N/A",
                    "N/A",
                    false,
                    "0",
                    "0"
                );
                return;
            }

            var availableSpots = await GetAvailablePrivateSpots(privateParking);

            await dialogService.ShowBottomSheet(
                privateParking.Address,
                $"{privateParking.ParkingInfo.Fee:C}/hour",
                $"{privateParking.ParkingInfo.LimitedHour} hours",
                $"{availableSpots} Available Spots",
                true,
                privateParking.Latitude.ToString(),
                privateParking.Longitude.ToString()
            );
        }

        public async Task<PrivateParking> GetPrivateParkingAsync(double lat, double lng)
        {
            try
            {
                var filter = Builders<PrivateParking>.Filter.And(
                    Builders<PrivateParking>.Filter.Eq(pp => pp.Latitude, lat),
                    Builders<PrivateParking>.Filter.Eq(pp => pp.Longitude, lng)
                );

                var privateParkingList = await mongoDBService.GetDataFilter<PrivateParking>("PrivateParking", filter);
                return privateParkingList?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error retrieving private parking data: {ex.Message}");
                return null;
            }
        }

        private async Task<int> GetAvailablePrivateSpots(PrivateParking privateParking)
        {
            // Logic to calculate available spots
            var statuses = await mongoDBService.GetData<PrivateStatus>("PrivateStatus");
            var availableSpots = statuses.Count(status => status.AreaId == privateParking.Id && !status.Status);
            return availableSpots;
        }

        public ICommand WalkNavigationCommand => new RelayCommand(async () =>
        {
            try
            {
                // Retrieve the saved coordinates from SecureStorage
                var savedLat = await SecureStorage.Default.GetAsync("SavedParkingLat");
                var savedLng = await SecureStorage.Default.GetAsync("SavedParkingLng");

                if (string.IsNullOrEmpty(savedLat) || string.IsNullOrEmpty(savedLng))
                {
                    await dialogService.ShowAlertAsync("No saved location", "Please save a parking location first.");
                    return;
                }

                var destinationLatitude = double.Parse(savedLat);
                var destinationLongitude = double.Parse(savedLng);

                // Assuming you have the user's current location set
                var currentLocation = new Location(LocationLatitude, LocationLongitude);
                var destinationLocation = new Location(destinationLatitude, destinationLongitude);

                // Start walking navigation
                var uri = $"https://www.google.com/maps/dir/?api=1&origin={currentLocation.Latitude},{currentLocation.Longitude}&destination={destinationLocation.Latitude},{destinationLocation.Longitude}&travelmode=walking";
                await Launcher.OpenAsync(new Uri(uri));

                // Optionally, display a message or update the UI as needed
                //await dialogService.ShowAlertAsync("Navigation started", "Walking navigation has been started.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error starting walking navigation: {ex.Message}");
            }
        });
    }

    public class SearchResultItemViewModel : SearchResultItem
    {
    }
}
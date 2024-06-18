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

        private readonly IMongoDBService mongoDBService;
        private readonly IDialogService dialogService;
        public UserMapViewModel(IMongoDBService mongoDBService, IDialogService dialogService)
        {
            this.mongoDBService = mongoDBService;
            this.dialogService = dialogService;
        }
        public ICommand LoadedEventCommand => new RelayCommand<EventArgs>(async e =>
        {
            await LoadMapDataAsync();
        });

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

                var lines = new ObservableCollection<MapLine>();

                foreach (var pd in parkingDatas)
                {
                    if (pd.Points.Count > 1)
                    {
                        var color = await GetLineColorAsync(pd.Id);
                        lines.Add(new MapLine(pd.Points, color));
                    }
                }

                dbMapLines = new List<MapLine>(lines);
                //MapLines = lines;
                await LoadAvailableSpotsAsync(null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading map data: {ex.Message}");
            }
        }
        private async Task<string> GetLineColorAsync(string parkingDataId)
        {
            var statuses = await mongoDBService.GetData<PublicStatus>(CollectionName.PublicStatus);
            var availableSpots = statuses.Count(status => status.AreaId == parkingDataId && !status.Status);
            return availableSpots > 0 ? "green" : "red";
        }

        private async Task LoadAvailableSpotsAsync(string parkingDataId)
        {
            try
            {
                var statuses = await mongoDBService.GetData<PublicStatus>(CollectionName.PublicStatus);
                var count = string.IsNullOrEmpty(parkingDataId)
                    ? statuses.Count(status => !status.Status)
                    : statuses.Count(status => status.AreaId == parkingDataId && !status.Status);

                AvailableSpots = count.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading available spots: {ex.Message}");
            }
        }

        public async Task OnLineClickedAsync(MapLine selectedLine)
        {
            if (selectedLine == null) return;

            try
            {
                var filter = Builders<ParkingData>.Filter.Eq(pd => pd.Points, selectedLine.Points);
                var parkingDataList = await mongoDBService.GetDataFilter<ParkingData>(CollectionName.ParkingData, filter);

                if (parkingDataList == null || !parkingDataList.Any())
                {
                    await dialogService.ShowAlertAsync("No Data Found", "No parking data found for the selected line.");
                    return;
                }

                var parkingData = parkingDataList.First();
                var address = parkingData.ParkingSpot;
                var parkingFee = parkingData.ParkingFee;
                var limitedHour = parkingData.ParkingTime;
                var parkingDataId = parkingData.Id;
                var lat = parkingData.Points[1].Lat;
                var lng = parkingData.Points[1].Lng;

                await LoadAvailableSpotsAsync(parkingDataId);
                await dialogService.ShowBottomSheet(address, parkingFee, limitedHour, $"{AvailableSpots} Available Spots", true, lat, lng);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving parking data: {ex.Message}");
            }
        }

        public ICommand UpdateRangeCommand => new RelayCommand(async() =>
        {
            // parse selectedRadius to int 200 meters > 0.2
            if (string.IsNullOrEmpty(SelectedRadius))
            {
                Debug.WriteLine("SelectedRadius is null or empty");
                return;
            }

            // Parse selected radius to double (meters to kilometers)
            if (!double.TryParse(SelectedRadius.Split(' ')[0], out double radius_out))
            {
                Debug.WriteLine("Failed to parse SelectedRadius");
                return;
            }

            radius_out /= 1000.0;
            var location = await Geolocation.GetLocationAsync();

            List<MapLine> linesInRange = dbMapLines.Where(line => isPointInCircle(line.Points, location.Latitude, location.Longitude, radius_out)).ToList();
            MapLines = new ObservableCollection<MapLine>(linesInRange);
            Radius = radius_out;
        });


        private bool isPointInCircle(List<MapPoint> points, double centerLat, double centerLng, double radius)
        {
            foreach(MapPoint point in points)
            {
                double pointLat = double.Parse(point.Lat);
                double pointLng = double.Parse(point.Lng);

                var distance = Math.Sqrt(Math.Pow(pointLat - centerLat, 2) + Math.Pow(pointLng - centerLng, 2));
                if(distance <= (radius / 111))
                {
                    return true;
                }
            }

            return false;
        }

    }
}
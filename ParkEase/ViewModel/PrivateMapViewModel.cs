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

namespace ParkEase.ViewModel
{
    public partial class PrivateMapViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<string> floorNames;

        [ObservableProperty]
        private string selectedFloorName;

        [ObservableProperty]
        private ObservableCollection<RectF> rectangles;

        [ObservableProperty]
        private ObservableCollection<Rectangle> listRectangle;

        private string selectedPropertyId;

        private string rectStrokeColor;

        //private List<Rectangle> listRectangles;

        private List<PrivateParking> parkingLotData;

        private List<PrivateStatus> privateStatusData;

        private string address;
        private string city;
        private double fee;
        private string limitHour;
        private List<FloorInfo> listFloorInfos;

        //private List<FloorInfo> listFloorInfos;

        private readonly IMongoDBService mongoDBService;

        private readonly IDialogService dialogService;

        private ParkEaseModel parkEaseModel;

        public PrivateMapViewModel(IMongoDBService mongoDBService, IDialogService dialogService, ParkEaseModel model)
        {
            this.mongoDBService = mongoDBService;
            this.dialogService = dialogService;
            this.parkEaseModel = model;
            selectedFloorName = string.Empty;
            FloorNames = new ObservableCollection<string>();
            ListRectangle = new ObservableCollection<Rectangle>();
            privateStatusData = new List<PrivateStatus>();
        }

        public ICommand LoadDataCommand => new RelayCommand(async () =>
        {
            try
            {
                /* var filter = Builders<PrivateParking>.Filter.Eq(data => data.Id, "666763d4d2c61b754e32a094");
                 parkingLotData = await mongoDBService.GetDataFilter<PrivateParking>(CollectionName.PrivateParking, filter);*/
                parkingLotData?.Clear();
                FloorNames?.Clear();
                ListRectangle?.Clear();
                privateStatusData?.Clear();
                listFloorInfos?.Clear();
                parkingLotData = await mongoDBService.GetData<PrivateParking>(CollectionName.PrivateParking);

                if (parkingLotData == null || parkingLotData.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("No parking data found.");
                    return;
                }

                parkingLotData = parkingLotData.Where(p => p.Id == "666763d4d2c61b754e32a094").ToList();

                /*if (parkingLotData.Count > 0)
                {*/
                var selectedProperty = parkingLotData[0];
                address = selectedProperty.Address;
                city = selectedProperty.City;
                fee = selectedProperty.ParkingInfo.Fee;
                limitHour = selectedProperty.ParkingInfo.LimitedHour.ToString();
                listFloorInfos = selectedProperty.FloorInfo;
                foreach (var floor in listFloorInfos)
                {
                    FloorNames.Add(floor.Floor);
                }

                // Fetch PrivateStatus data from MongoDB
                privateStatusData = await mongoDBService.GetData<PrivateStatus>(CollectionName.PrivateStatus);
                if (privateStatusData == null || privateStatusData.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("No private status data found.");
                    return;
                }

                // Filter privateStatusData based on selectedPropertyId
                privateStatusData = privateStatusData.Where(item => item.AreaId == "666763d4d2c61b754e32a094").ToList();


                
                await dialogService.ShowPrivateMapBottomSheet($"{address} {city}", $"{fee} per hour", $"{limitHour}", "");

            }
            catch (Exception ex)
            {
                await dialogService.ShowAlertAsync("Error in Load data", ex.Message, "OK");
            }
        });

        partial void OnSelectedFloorNameChanged(string? value)
        {
            _ = ShowSelectedMap();
        }

        private async Task ShowSelectedMap()
        {
            // ShowSelectedMap Function
            FloorInfo selectedMap = listFloorInfos.FirstOrDefault(data => data.Floor == SelectedFloorName);
            if (selectedMap == null)
            {
                System.Diagnostics.Debug.WriteLine("No parking map found.");
                return;
            }

            // Filter by selectedFloorName and Create a dictionary for quick lookup of statuses by index
            var filterPrivateStatus = privateStatusData
                .Where(item => item.Floor == SelectedFloorName)
                .ToDictionary(item => item.Index, item => item.Status);

            // Variable to count availability status (false means available lot)
            int availabilityCount = 0;

            // Update rectangle colors based on status and add them to ListRectangle
            foreach (var rectangle in selectedMap.Rectangles)
            {
                if (filterPrivateStatus.TryGetValue(rectangle.Index, out bool isAvailable))
                {
                    if (!isAvailable)
                    {
                        rectangle.Color = "#009D00";
                        availabilityCount++;
                    }
                    else
                    {
                        rectangle.Color = "#E11919";
                    }
                }
                ListRectangle.Add(rectangle);
            }
            await dialogService.ShowPrivateMapBottomSheet($"{address} {city}", $"{fee} per hour", $"{limitHour}", $"{SelectedFloorName}: {availabilityCount} available lots");
        }
    }
}

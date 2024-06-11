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
        }

        [RelayCommand]
        public async void LoadData()
        {
            try
            {
                /* var filter = Builders<PrivateParking>.Filter.Eq(data => data.Id, "666763d4d2c61b754e32a094");
                 parkingLotData = await mongoDBService.GetDataFilter<PrivateParking>(CollectionName.PrivateParking, filter);*/

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
                string address = selectedProperty.Address;
                string city = selectedProperty.City;
                double fee = selectedProperty.ParkingInfo.Fee;
                string limitHour = selectedProperty.ParkingInfo.LimitedHour.ToString();
                List<FloorInfo> listFloorInfos = selectedProperty.FloorInfo;
                foreach (var floor in listFloorInfos)
                {
                    FloorNames.Add(floor.Floor);
                }

                FloorInfo selectedMap = listFloorInfos.FirstOrDefault(data => data.Floor == "Ground");
                if (selectedMap == null)
                {
                    System.Diagnostics.Debug.WriteLine("No parking map found.");
                    return;
                }

                ListRectangle.Clear();
                foreach (var rectangle in selectedMap.Rectangles)
                {
                    ListRectangle.Add(rectangle);
                }

                await dialogService.ShowPrivateMapBottomSheet($"{address} {city}", $"{fee} per hour", $"{limitHour}", "", false);
            }
            catch (Exception ex)
            {
                await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }
        }

        private void ShowSelectedMap()
        {
            /*var selectedMap = listFloorInfos.FirstOrDefault(data => data.Floor == SelectedFloorName);
            if (selectedMap == null)
            {
                System.Diagnostics.Debug.WriteLine("No parking map found.");
                return;
            }

            Rectangles.Clear();
            listRectangles = selectedMap.Rectangles;
            foreach (Rectangle rectangle in listRectangles)
            {
                float pointX = rectangle.Rect.X;
                float pointY = rectangle.Rect.Y;
                var rect = new RectF(pointX, pointY, rectangle.Rect.Width, rectangle.Rect.Height);
                Rectangles.Add(rect);
            }*/
        }
    }
}

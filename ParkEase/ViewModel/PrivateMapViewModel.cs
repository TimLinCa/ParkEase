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
using IImage = Microsoft.Maui.Graphics.IImage;
using Microsoft.Maui.Graphics.Platform;
using System.Reflection;
using ZXing.Net.Maui;
using CommunityToolkit.Mvvm.Messaging;
using ParkEase.Messages;
using System.ComponentModel;
using System.Runtime.CompilerServices;

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
        private ObservableCollection<Rectangle> listRectangleFill;

        [ObservableProperty]
        private IImage imgSourceData;

        private string selectedPropertyId;

        private string rectStrokeColor;

        //private List<Rectangle> listRectangles;

        private List<PrivateParking> parkingLotData;

        [ObservableProperty]
        private List<string> addressList;

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

        [ObservableProperty]
        private string barcodeResult;

        [ObservableProperty]
        private bool enableScanner;

        [ObservableProperty]
        private bool gridVisible;

        [ObservableProperty]
        private bool enableExpender;

        [ObservableProperty]
        private string scannerText;

        [ObservableProperty]
        private string scannerImage;

        private string privateParkingId;

        [ObservableProperty]
        private BarcodeDetectionEventArgs barcodeDetectionEventArgs;

        private double currentScale = 1;
        private double startScale = 1;
        private double xOffset = 0;
        private double yOffset = 0;


        public PrivateMapViewModel(IMongoDBService mongoDBService, IDialogService dialogService, ParkEaseModel model)
        {

            privateParkingId = DataService.GetId();

            this.mongoDBService = mongoDBService;
            this.dialogService = dialogService;
            this.parkEaseModel = model;
            selectedFloorName = string.Empty;
            FloorNames = new ObservableCollection<string>();
            ListRectangleFill = new ObservableCollection<Rectangle>();
            privateStatusData = new List<PrivateStatus>();
           
            BarcodeResult = string.Empty;
            EnableScanner = true;
            GridVisible = false;
            ScannerText = "";
            scannerImage = "scanner_image.png";

            //PinchCommand = new Command<PinchGestureUpdatedEventArgs>(OnPinchUpdated);
            //PanCommand = new Command<PanUpdatedEventArgs>(OnPanUpdated);

            TestLoadData();
            _ = LoadAddress();
        }

        private async Task LoadAddress()
        {
            var parkingLotData = await mongoDBService.GetData<PrivateParking>(CollectionName.PrivateParking);
            AddressList = parkingLotData.Select(data => data.Address).ToList();
        }

        private async Task LoadDataCommand()
        {
            try
            {
                parkingLotData?.Clear();
                FloorNames?.Clear();
                listFloorInfos?.Clear();
                privateStatusData?.Clear();
                ListRectangleFill?.Clear();

                // Fetch PrivateParking data from MongoDB
                parkingLotData = await mongoDBService.GetData<PrivateParking>(CollectionName.PrivateParking);

                if (parkingLotData == null || parkingLotData.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("No parking data found.");
                    return;
                }

                // Filter parkingLotData based on selected Address or BarcodeResult *****////*******////******************
                parkingLotData = parkingLotData.Where(p => p.Id == BarcodeResult).ToList();
                if (parkingLotData.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("No matching parking data found.");
                    return;
                }

                var selectedProperty = parkingLotData[0];
                address = selectedProperty.Address;
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
                privateStatusData = privateStatusData.Where(item => item.AreaId == BarcodeResult).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        partial void OnSelectedFloorNameChanged(string? value)
        {
            _ = ShowSelectedMap();
        }


/*        public ICommand BarcodesDetectedCommand => new RelayCommand<string>(async qrCode =>
        {
            //var result = qrCode;
            BarcodeResult = qrCode;
            await dialogService.ShowAlertAsync("Error", $"{BarcodeResult}", "OK");
            GridVisible = false;
            await LoadDataCommand();
        });*/

/*        [RelayCommand]
        public async Task ScannerButton()
        {
            try
            {
                GridVisible = !GridVisible;
            }
            catch (Exception ex)
            {
                await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }
        }*/


        private async Task ShowSelectedMap()
        {
            // ShowSelectedMap Function
            FloorInfo selectedMap = listFloorInfos.FirstOrDefault(data => data.Floor == SelectedFloorName);
            if (selectedMap == null)
            {
                System.Diagnostics.Debug.WriteLine("No selected floor map found.");
                return;
            }

            // Filter by selectedFloorName and Create a dictionary for quick lookup of statuses by index
            var matchingStatus = privateStatusData
                .Where(item => item.Floor == SelectedFloorName)
                .ToDictionary(item => item.Index, item => item.Status);

            // Fetch image data
            var imageData = selectedMap.ImageData;
            using (MemoryStream ms = new MemoryStream(imageData))
            {
                ImgSourceData = await Task.Run(() => PlatformImage.FromStream(ms));
            }

            // Variable to count availability status (false means available lot)
            int availabilityCount = 0;

            // Update rectangle colors based on status and add them to ListRectangle
            foreach (var rectangle in selectedMap.Rectangles)
            {
                if (matchingStatus.TryGetValue(rectangle.Index, out bool isAvailable))
                {
                    if (!isAvailable)
                    {
                        rectangle.Color = "green";
                        availabilityCount++;
                    }
                    else
                    {
                        rectangle.Color = "red";
                    }
                }
                ListRectangleFill.Add(rectangle);
            }
            await dialogService.ShowBottomSheet($"{address} {city}", $"{fee} per hour", $"{limitHour}", $"{SelectedFloorName}: {availabilityCount} available lots", false, "", "");
        }


        // Just a test function -> will be removed later
        private async Task TestLoadData()
        {
            try
            {
                parkingLotData?.Clear();
                FloorNames?.Clear();
                listFloorInfos?.Clear();
                privateStatusData?.Clear();
                ListRectangleFill?.Clear();

                // Fetch PrivateParking data from MongoDB
                var data = await mongoDBService.GetData<PrivateParking>(CollectionName.PrivateParking);

                if (data == null || data.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("No parking data found.");
                    return;
                }

                // Filter parkingLotData based on BarcodeResult
                parkingLotData = data.Where(p => p.Id == privateParkingId).ToList();
                if (parkingLotData.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("No matching parking data found.");
                    return;
                }

                var selectedProperty = parkingLotData[0];
                address = selectedProperty.Address;
                fee = selectedProperty.ParkingInfo.Fee;
                limitHour = selectedProperty.ParkingInfo.LimitedHour.ToString();
                listFloorInfos = selectedProperty.FloorInfo;

                foreach (var floor in listFloorInfos)
                {
                    FloorNames.Add(floor.Floor);
                }

                // Fetch PrivateStatus data from MongoDB
                var status = await mongoDBService.GetData<PrivateStatus>(CollectionName.PrivateStatus);
                if (status == null || status.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("No private status data found.");
                    return;
                }

                // Filter privateStatusData based on selectedPropertyId
                privateStatusData = status.Where(item => item.AreaId == privateParkingId).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        
    }
}

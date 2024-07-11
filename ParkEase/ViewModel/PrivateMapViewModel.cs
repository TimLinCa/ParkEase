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
using ParkEase.Page;
using ParkEase.Controls;
using CommunityToolkit.Maui.Storage;


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

        private List<PrivateParking> parkingLotData;

        [ObservableProperty]
        private List<string> addressList;

        private List<PrivateStatus> privateStatusData;

        private string address;
        private double fee;
        private string limitHour;
        private List<FloorInfo> listFloorInfos;

        private readonly IMongoDBService mongoDBService;

        private readonly IDialogService dialogService;

        private ParkEaseModel parkEaseModel;

        [ObservableProperty]
        private bool enableScanner;

        [ObservableProperty]
        private bool gridVisible;

        [ObservableProperty]
        private bool enableExpender;

        [ObservableProperty]
        private string scannerText;

        [ObservableProperty]
        private string scannerImage = "scanner_image.png";

        [ObservableProperty]
        private string arrowBack = "arrow_icon.png";

        private string privateParkingId;

        [ObservableProperty]
        private BarcodeDetectionEventArgs barcodeDetectionEventArgs;

        private CancellationTokenSource cts; 
        readonly bool stopping = false;
        CancellationTokenSource cls = new CancellationTokenSource();

        [ObservableProperty]
        private ImageSource mapImageData;

        IFileSaver fileSaver;

        public PrivateMapViewModel(IMongoDBService mongoDBService, IDialogService dialogService, ParkEaseModel model, IFileSaver _fileSaver)
        {

            this.mongoDBService = mongoDBService;
            this.dialogService = dialogService;
            this.parkEaseModel = model;
            this.fileSaver = _fileSaver;
            selectedFloorName = string.Empty;
            FloorNames = new ObservableCollection<string>();
            ListRectangleFill = new ObservableCollection<Rectangle>();
            privateStatusData = new List<PrivateStatus>();

            EnableScanner = true;
            GridVisible = false;
            ScannerText = "";
        }

        /*public async Task SaveImageToLocal(ImageSource imageSource)
        {
            try
            {
                //using var stream = new MemoryStream()
                if (imageSource is StreamImageSource streamImageSource)
                {
                    await dialogService.ShowAlertAsync("image data", "Private Map image: " + imageSource, "OK");
                    var stream = await streamImageSource.Stream(CancellationToken.None);
                    if (stream != null)
                    {
                        string fileName = $"CapturedImage_{DateTime.Now:yyyyMMddHHmmss}.jpg";

                        // Convert stream to byte array
                        using var memoryStream = new MemoryStream();
                        await stream.CopyToAsync(memoryStream);
                        byte[] imageData = memoryStream.ToArray();

                        //https://learn.microsoft.com/en-us/dotnet/maui/user-interface/controls/image?view=net-maui-8.0#load-an-image-from-a-stream

                        // Save file using FileSaver
                        var result = await fileSaver.SaveAsync(fileName, new MemoryStream(imageData), cts.Token);
                        cts.Cancel();

                        //https://www.youtube.com/watch?v=Q9T-dRYq3Ps&t=417s
                        // https://learn.microsoft.com/en-us/dotnet/communitytoolkit/maui/essentials/file-saver?tabs=android

                        if (result.IsSuccessful)
                        {
                            await Application.Current.MainPage.DisplayAlert("Success", $"Image saved successfully", "OK");
                        }
                        else
                        {
                            await Application.Current.MainPage.DisplayAlert("Error", "Failed to save image", "OK");
                        }
                    }
                    else
                    {
                        await Application.Current.MainPage.DisplayAlert("Error", "Failed to get image stream", "OK");
                    }
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Unsupported image source type", "OK");
                }
            }
            catch (Exception ex)
            {
                // Handle any errors
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to save image: {ex.Message}", "OK");
            }
        }
        

         */

        public ICommand LoadedCommand => new RelayCommand(async () =>
        {
            privateParkingId = DataService.GetId();
            //privateParkingId = parkEaseModel.PrivateMapId;
            await LoadParkingData();

            cts = new CancellationTokenSource();
            var token = cts.Token;
            _ = Run(token); // Start the real-time update loop
        });

        public ICommand UnLoadedCommand => new RelayCommand(() =>
        {
            cts?.Cancel(); // Cancel the real-time update loop
            parkingLotData?.Clear();
            FloorNames?.Clear();
            listFloorInfos?.Clear();
            privateStatusData?.Clear();
            ListRectangleFill?.Clear();
            ImgSourceData = null;
        });

        private async Task LoadParkingData()
        {
            try
            {
                // Fetch PrivateParking data from MongoDB
                var data = await mongoDBService.GetData<PrivateParking>(CollectionName.PrivateParking);

                if (data == null || data.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("No parking data found.");
                    return;
                }

                // Filter parkingLotData based on privateParkingId
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

                // Filter privateStatusData based on privateParkingId
                privateStatusData = status.Where(item => item.AreaId == privateParkingId).ToList();

                if (FloorNames.FirstOrDefault() != null)
                {
                    SelectedFloorName = FloorNames.First();
                }
                
                await dialogService.ShowBottomSheet($"{address}", $"{fee} per hour", $"{limitHour}", $"{SelectedFloorName}: ? available lots", false, "", "");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        /*partial void OnSelectedFloorNameChanged(string? value)
        {
            _ = ShowSelectedMap();
        }*/

        

        private async Task Run(CancellationToken token)
        {
            await Task.Run(async () =>
            {
                while (!stopping)
                {
                    token.ThrowIfCancellationRequested();
                    try
                    {
                        await ShowSelectedMap();
                        await Task.Delay(2000, token);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
            }, token);
        }

        private async Task ShowSelectedMap()
        {
            try
            {
                // Fetch PrivateStatus data from MongoDB
                var status = await mongoDBService.GetData<PrivateStatus>(CollectionName.PrivateStatus);
                if (status == null || status.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("No private status data found.");
                    return;
                }

                // Filter privateStatusData based on privateParkingId
                privateStatusData = status.Where(item => item.AreaId == privateParkingId).ToList();

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
                    .ToDictionary(item => item.LotId, item => item.Status);

                // Fetch image data
                if(ImgSourceData==null)
                {
                    var imageData = selectedMap.ImageData;
                    using (MemoryStream ms = new MemoryStream(imageData))
                    {
                        ImgSourceData = await Task.Run(() => PlatformImage.FromStream(ms));
                    }
                }

                // Variable to count availability status (false means available lot)
                int availabilityCount = 0;
                ObservableCollection<Rectangle> rectangles = new ObservableCollection<Rectangle>();
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
                    rectangles.Add(rectangle);
                }


                ListRectangleFill = rectangles;
                //await dialogService.ShowBottomSheet($"{address}", $"{fee} per hour", $"{limitHour}", $"{SelectedFloorName}: {availabilityCount} available lots", false, "", "");
            }
            catch (Exception)
            {

                throw;
            }
         
        }

        public ICommand NavigatePrivateSearchPage => new RelayCommand(async () =>
        {
            await dialogService.DismissBottomSheetAsync();
            await Shell.Current.GoToAsync($"///{nameof(PrivateSearchPage)}");
        });

    }
}

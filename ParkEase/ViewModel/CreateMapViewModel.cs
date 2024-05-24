using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using Microsoft.VisualBasic;
using ParkEase.Contracts.Services;
using ParkEase.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using IImage = Microsoft.Maui.Graphics.IImage;
using ParkEase.Page;
using ParkEase.Core.Contracts.Services;
using ParkEase.Core.Data;
using ParkEase.Core.Services;
using Microsoft.Maui.Media;


namespace ParkEase.ViewModel
{
    public partial class CreateMapViewModel : ObservableObject
    {
        [ObservableProperty]
        private string companyName;

        [ObservableProperty]
        private string address;

        [ObservableProperty]
        private string city;

        [ObservableProperty]
        private ObservableCollection<RectF> rectangles;

        [ObservableProperty]
        private int recCount;

        [ObservableProperty]
        private string imgPath;

        private byte[] imageData;

        private readonly IMongoDBService mongoDBService;

        private readonly IDialogService dialogService;



        public CreateMapViewModel(IMongoDBService mongoDBService, IDialogService dialogService)
        {
            this.mongoDBService = mongoDBService;
            this.dialogService = dialogService;
            companyName = string.Empty;
            address = string.Empty;
            city = string.Empty;
            rectangles = new ObservableCollection<RectF>();
        }

        public ICommand UploadImageClick => new RelayCommand(async () =>
        {
            try
            {
                if (MediaPicker.Default.IsCaptureSupported)
                {
                    // Load photo
                    FileResult myPhoto = await MediaPicker.PickPhotoAsync();
                    if (myPhoto != null)
                    {
                        ImgPath = myPhoto.FullPath;
                        using var stream = await myPhoto.OpenReadAsync();
                        using var memoryStream = new MemoryStream();
                        await stream.CopyToAsync(memoryStream);
                        var imageBytes = memoryStream.ToArray();

                        // Convert byte array to Base64 string and assign to ImageData
                        imageData = imageBytes;
                    }
                }
                else
                {
                    await dialogService.ShowAlertAsync("OOPS", "Your device isn't supported", "OK");
                }
            }
            catch (Exception ex)
            {
                await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }

        });

        public ICommand AddParkingInfoAsync => new RelayCommand(async () =>

        {
            try
            {
                var parkingInfo = new PrivateParking
                {
                    CompanyName = CompanyName,
                    Address = Address,
                    City = City,

                };
                await mongoDBService.InsertData(CollectionName.PrivateParking, parkingInfo);
                var TEST = await mongoDBService.GetData<PrivateParking>(CollectionName.PrivateParking);
                await dialogService.ShowAlertAsync("", "Your data is saved.", "OK");
            }
            catch (Exception ex)
            {
                await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }
        });




        public void AddRectangle(PointF point)
        {
            var rect = new RectF(point.X, point.Y, 100, 50);
            Rectangles.Add(rect);
            RecCount = RecCount + 1;
        }

        public ICommand RemoveRectangleClick => new RelayCommand(async () =>
        {
            try
            {
                if (RecCount > 0)
                {
                    Rectangles.RemoveAt(RecCount - 1);
                    RecCount--;
                }
                else
                {
                    await dialogService.ShowAlertAsync("Error", "There is nothing to deldete.\nPlease draw the map first!", "OK");
                }
            } catch (Exception ex)
            {
                await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }
            
        });

        public ICommand ClearAllRectangleClick => new RelayCommand(async () =>
        {
            try
            {
                if (RecCount > 0)
                {
                    Rectangles.Clear();
                    RecCount = 0;
                }
                else
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }

        });

    }

}

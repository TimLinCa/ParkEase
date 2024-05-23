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

namespace ParkEase.ViewModel
{
    public partial class CreateMapViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<RectF> rectangles;

        [ObservableProperty]
        private int recCount;

        [ObservableProperty]
        private string imgPath;

        private readonly IDialogService dialogService;

        public CreateMapViewModel(IDialogService dialogService)
        {
            this.dialogService = dialogService;
            rectangles = new ObservableCollection<RectF>();
        }

        public ICommand UploadImageClick => new RelayCommand(async () =>
        {
            if (MediaPicker.Default.IsCaptureSupported)
            {
                // Load photo
                FileResult myPhoto = await MediaPicker.Default.PickPhotoAsync();
                if (myPhoto != null)
                {
                    ImgPath = myPhoto.FullPath;
                }
            }
            else
            {
                await dialogService.ShowAlertAsync("OOPS", "Your device isn't supported", "OK");
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

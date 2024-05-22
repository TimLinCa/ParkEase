using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using Microsoft.VisualBasic;
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
        private ImageSource uploadedImage;

        [ObservableProperty]
        private int recCount;

        [ObservableProperty]
        private string imgPath;

        public CreateMapViewModel()
        {
            rectangles = new ObservableCollection<RectF>();
        }

        public ICommand UploadImageClicked => new RelayCommand(async () =>
        {
            if (MediaPicker.Default.IsCaptureSupported)
            {
                // Load photo
                FileResult myPhoto = await MediaPicker.Default.PickPhotoAsync();
                if (myPhoto != null)
                {
                    UploadedImage = ImageSource.FromFile(myPhoto.FullPath);
                }
            }
            else
            {
                await Shell.Current.DisplayAlert("OOPS", "Your device isn't supported", "OK");
            }
        });

        public ICommand OnGrapgicsViewClick => new RelayCommand<TappedEventArgs>(e =>
        {

        });


        public void AddRectangle(PointF point)
        {
            var rect = new RectF(point.X, point.Y, 100, 50);
            Rectangles.Add(rect);
            RecCount = RecCount + 1;
            //Drawable.UpdateRectangles(rectangles);

            //OnPropertyChanged(nameof(Rectangles));
            //graphicsView?.Invalidate();
        }

        public async Task SaveRectanglesAsync(string buildingName, string floorNo)
        {
            
        }
    }

}

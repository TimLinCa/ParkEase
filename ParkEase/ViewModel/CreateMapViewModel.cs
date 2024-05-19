using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ParkEase.ViewModel
{
    public partial class CreateMapViewModel : ObservableObject
    {

        /*private async void OnCounterClicked(object sender, EventArgs e)
        {
            if (MediaPicker.Default.IsCaptureSupported)
            {
                //Load photo
                FileResult myPhoto = await MediaPicker.Default.PickPhotoAsync();
                if (myPhoto != null)
                {
                    //save the image captured in the application.
                    string localFilePath = Path.Combine(FileSystem.CacheDirectory, myPhoto.FileName);
                    using Stream sourceStream = await myPhoto.OpenReadAsync();
                    using FileStream localFileStream = File.OpenWrite(localFilePath);
                    await sourceStream.CopyToAsync(localFileStream);
                }
            }
            else
            {
                await Shell.Current.DisplayAlert("OOPS", "Your device isn't supported", "OK");
            }

        }*/

        //Hazel
        /*private ObservableCollection<RectF> rectangles;

        [ObservableProperty]
        private GraphicsDrawable drawable;

        public ICommand StartInteractionCommand { get; }
        private GraphicsView graphicsView;

        public CreateMapViewModel()
        {
            rectangles = new ObservableCollection<RectF>();
            drawable = new GraphicsDrawable(rectangles);

            StartInteractionCommand = new Command<PointF>(OnStartInteraction);
        }

        public void SetGraphicsView(GraphicsView view)
        {
            graphicsView = view;
        }


        private void OnStartInteraction(PointF point)
        {
            var rect = new RectF(point.X - 50, point.Y - 50, 100, 100); // 100x100 rectangle centered at the touch point
            rectangles.Add(rect);
            drawable.UpdateRectangles(rectangles);
            graphicsView?.Invalidate();
        }
    }

    public class GraphicsDrawable : IDrawable
    {
        private ObservableCollection<RectF> rectangles;

        public GraphicsDrawable(ObservableCollection<RectF> rectangles)
        {
            this.rectangles = rectangles;

        }

        public void UpdateRectangles(ObservableCollection<RectF> newRectangles)
        {
            rectangles = newRectangles;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            foreach (var rect in rectangles)
            {
                canvas.StrokeColor = Colors.Red;
                canvas.StrokeSize = 2;
                canvas.DrawRectangle(rect);
            }
        }*/

        //Hazel

    }
    
}

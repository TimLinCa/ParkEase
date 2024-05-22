using Microsoft.Maui.Graphics.Platform;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using IImage = Microsoft.Maui.Graphics.IImage;

namespace ParkEase.Utilities
{
    public class RectDrawable : IDrawable
    {
        /* https://learn.microsoft.com/en-us/dotnet/maui/user-interface/graphics/draw?view=net-maui-8.0#draw-a-rectangle */

        private ObservableCollection<RectF> rectangles;
        private String imgPath;
        private IImage image;
        public RectDrawable(ObservableCollection<RectF> rectangles)
        {
            this.rectangles = rectangles;
        }

        public void UpdateRectangles(ObservableCollection<RectF> newRectangles)
        {
            rectangles = newRectangles;
        }

        public void UpdateImage(String imgPath)
        {
            if (!string.IsNullOrEmpty(imgPath) && File.Exists(imgPath))
            {
                using (var stream = File.OpenRead(imgPath))
                {
                    image = PlatformImage.FromStream(stream);
                }
            }
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.StrokeColor = Colors.Red;
            canvas.StrokeSize = 2;

            /*Assembly assembly = GetType().GetTypeInfo().Assembly;
            using (Stream stream = assembly.GetManifestResourceStream(imgPath))
            {
                image = PlatformImage.FromStream(stream);
            }*/

            if (image != null)
            {
                canvas.DrawImage(image, 10, 10, 700, 700);
            }

            foreach (var rect in rectangles)
            {
                if (rect.IntersectsWith(dirtyRect))
                {
                    canvas.DrawRectangle(rect);
                }
            }
        }
    }
}

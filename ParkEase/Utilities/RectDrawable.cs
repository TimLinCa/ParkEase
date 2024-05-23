using Microsoft.Maui.Graphics.Platform;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ParkEase.Utilities
{
    public class RectDrawable : IDrawable
    {
        /* https://learn.microsoft.com/en-us/dotnet/maui/user-interface/graphics/draw?view=net-maui-8.0#draw-a-rectangle */

        public string ImgPath { get; set; }

        public int RecCount { get; set; }

        public ObservableCollection<RectF> Rectangles { get; set; }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.StrokeColor = Colors.Green;
            canvas.StrokeSize = 2;

            if (ImgPath != null)
            {
                Microsoft.Maui.Graphics.IImage image;
                Assembly assembly = GetType().GetTypeInfo().Assembly;
                using (Stream stream = File.OpenRead(ImgPath))
                {
                    image = PlatformImage.FromStream(stream);
                }

                if (image != null)
                {
                    canvas.DrawImage(image, 10, 10, image.Width, image.Height);
                }
            }

            if (Rectangles != null)
            {
                foreach (var rect in Rectangles)
                {
                    canvas.DrawRectangle(rect);
                }
            }
         
        }
    }
}

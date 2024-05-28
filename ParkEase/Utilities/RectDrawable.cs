//using AVFoundation;
using Microsoft.Maui.Graphics.Platform;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Font = Microsoft.Maui.Graphics.Font;

namespace ParkEase.Utilities
{
    public class RectDrawable : IDrawable
    {
        /* https://learn.microsoft.com/en-us/dotnet/maui/user-interface/graphics/draw?view=net-maui-8.0#draw-a-rectangle */

        public string? ImgPath { get; set; }

        public int RecCount { get; set; }

        public ObservableCollection<RectF>? Rectangles { get; set; }

        public float RectWidth { get; set; } = 100;
        public float RectHeight { get; set; } = 50;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
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
                    // Calculate image width and height to show
                    // https://stackoverflow.com/questions/63541099/how-do-you-get-the-aspect-fit-size-of-a-uiimage-in-a-uimageview

                    float viewRatio = dirtyRect.Width / dirtyRect.Height;
                    float imageRatio = image.Width / image.Height;
                    float offsetX, offsetY, drawWidth, drawHeight;

                    if (imageRatio <= viewRatio)
                    {
                        drawHeight = dirtyRect.Height;
                        drawWidth = drawHeight / imageRatio;

                        offsetY = 0;
                        offsetX = (dirtyRect.Width - drawWidth) / 2;
                    }
                    else
                    {
                        drawWidth = dirtyRect.Width;
                        drawHeight = drawWidth / imageRatio;

                        offsetX = 0;
                        offsetY = (dirtyRect.Height - drawHeight) / 2;
                    }


                    canvas.DrawImage(image, offsetX, offsetY, drawWidth, drawHeight);

                    if (Rectangles != null)
                    {
                        canvas.StrokeColor = Colors.Green;
                        canvas.StrokeSize = 2;
                        canvas.FontColor = Colors.Black;
                        canvas.FontSize = 18;
                        canvas.Font = Font.DefaultBold;

                        for (int i = 0; i < Rectangles.Count; i++)
                        {
                            RectF rect = Rectangles[i];
                            canvas.DrawRectangle(rect);

                            var number = (i + 1).ToString();
                            var x = rect.X + 10;
                            var y = rect.Y + 20;
                            canvas.DrawString(number, x, y, HorizontalAlignment.Left);
                        }
                    }
                }
            }
            // https://github.com/dotnet/maui/issues/10624

            
         
        }
    }
}

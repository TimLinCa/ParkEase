using Microsoft.Maui.Graphics.Platform;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Font = Microsoft.Maui.Graphics.Font;
using ParkEase.Core.Data;

namespace ParkEase.Controls
{
    public class RectDrawableMobile : IDrawable
    {
        private object drawLock = new object();

        public int RectCount { get; set; }

        public ObservableCollection<Rectangle>? ListRectangle { get; set; }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            lock (drawLock)
            {
                try
                {
                    if (ListRectangle != null)
                    {
                        canvas.StrokeSize = 2;
                        canvas.FontColor = Colors.Black;
                        canvas.FontSize = 18;
                        canvas.Font = Font.DefaultBold;

                        foreach (Rectangle rectangle in ListRectangle)
                        {
                            float pointX = rectangle.Rect.X;
                            float pointY = rectangle.Rect.Y;
                            var rect = new RectF(pointX, pointY, rectangle.Rect.Width, rectangle.Rect.Height);
                            canvas.StrokeColor = Color.FromRgba(rectangle.Color);

                            canvas.DrawRectangle(rect);

                            var number = rectangle.Index.ToString();
                            var x = rect.X + 10;
                            var y = rect.Y + 20;
                            canvas.DrawString(number, x, y, HorizontalAlignment.Left);
                        }
                    }
                }
                catch (Exception)
                {

                }

            }
        }
    }
}

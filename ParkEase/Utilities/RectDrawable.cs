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

        private ObservableCollection<RectF> rectangles;

        public RectDrawable(ObservableCollection<RectF> rectangles)
        {
            this.rectangles = rectangles;
        }

        public void UpdateRectangles(ObservableCollection<RectF> newRectangles)
        {
            rectangles = newRectangles;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.StrokeColor = Colors.Green;
            canvas.StrokeSize = 2;

            foreach (var rect in rectangles)
            {
                //if (rect.IntersectsWith(dirtyRect))
                //{
                    canvas.DrawRectangle(rect);
                //}
            }
        }
    }
}

﻿//using AVFoundation;
using Microsoft.Maui.Graphics.Platform;
using ParkEase.Core.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Font = Microsoft.Maui.Graphics.Font;
using IImage = Microsoft.Maui.Graphics.IImage;

namespace ParkEase.Controls
{
    public class RectDrawable : IDrawable
    {
        /* https://learn.microsoft.com/en-us/dotnet/maui/user-interface/graphics/draw?view=net-maui-8.0#draw-a-rectangle */
        private object drawLock = new object();
        public IImage? ImageSource { get; set; }

        public int RectCount { get; set; }

        public ObservableCollection<Rectangle>? ListRectangle { get; set; }
        //public ObservableCollection<RectF>? Rectangles { get; set; }

        public float RectWidth { get; set; } = 100;
        public float RectHeight { get; set; } = 50;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            lock (drawLock)
            {
                if (ImageSource != null)
                {
                    IImage image = ImageSource;


                    if (image != null)
                    {
                        //Calculate image width and height to show
                        //https://stackoverflow.com/questions/63541099/how-do-you-get-the-aspect-fit-size-of-a-uiimage-in-a-uimageview

                        /*float viewRatio = dirtyRect.Width / dirtyRect.Height;
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
                        }*/

                        float viewRatio = 1134 / 830;
                        float imageRatio = image.Width / image.Height;
                        float offsetX, offsetY, drawWidth, drawHeight;

                        if (imageRatio <= viewRatio)
                        {
                            drawHeight = 830;
                            drawWidth = drawHeight / imageRatio;

                            offsetY = 0;
                            offsetX = (1134 - drawWidth) / 2;
                        }
                        else
                        {
                            drawWidth = 1134;
                            drawHeight = drawWidth / imageRatio;

                            offsetX = 0;
                            offsetY = (830 - drawHeight) / 2;
                        }

                        canvas.DrawImage(image, offsetX, offsetY, drawWidth, drawHeight);

                        if (ListRectangle?.Count > 0)
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
                }
                // https://github.com/dotnet/maui/issues/10624
            }
        }
    }
}

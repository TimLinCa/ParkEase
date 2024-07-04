using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IImage = Microsoft.Maui.Graphics.IImage;
using ParkEase.Core.Data;
using static System.Runtime.InteropServices.JavaScript.JSType;
using SharpCompress.Compressors.Xz;
using Microsoft.Maui;
using Syncfusion.Maui.Core.Internals;
using Microsoft.Maui.Handlers;
using Syncfusion.Maui.Core;

namespace ParkEase.Controls
{
    public class RecGraphicsView : GraphicsView
    {
        /// <summary>
        /// Due to Rectangles setter will not be triggered when to collection was added an item, so RecCount must be changed to perform the draw method.
        /// </summary>
        /// 
        double panX, panY;
        double currentScale = 1;
        double startScale = 1;
        double xOffset = 0;
        double yOffset = 0;
        private static object drawlock = new object();
        private static RecGraphicsView _currentInstance;
        private double imageWidth = 1134;
        private double imageHeight = 830;



        public IImage ImageSource
        {
            get => (IImage)GetValue(ImageSourceProperty); set { SetValue(ImageSourceProperty, value); }
        }


        public ObservableCollection<Rectangle> ListRectangle
        {
            get => (ObservableCollection<Rectangle>)GetValue(ListRectangleProperty);
            set
            {
                SetValue(ListRectangleProperty, value);
            }
        }

        public ObservableCollection<Rectangle> ListRectangleFill
        {
            get => (ObservableCollection<Rectangle>)GetValue(ListRectangleFillProperty);
            set
            {
                SetValue(ListRectangleFillProperty, value);
            }
        }


        public static readonly BindableProperty ImageSourceProperty = BindableProperty.Create(nameof(ImageSource), typeof(IImage), typeof(RecGraphicsView), propertyChanged: ImageSourcePropertyChanged);

        public static readonly BindableProperty ListRectangleProperty = BindableProperty.Create(nameof(ListRectangle), typeof(ObservableCollection<Rectangle>), typeof(RecGraphicsView), propertyChanged: ListRectanglePropertyChanged);

        public static readonly BindableProperty ListRectangleFillProperty = BindableProperty.Create(nameof(ListRectangleFill), typeof(ObservableCollection<Rectangle>), typeof(RecGraphicsView), propertyChanged: ListRectangleFillPropertyChanged);

        private bool moving = false;
        private bool zooming = false;

        public RecGraphicsView()
        {
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                //PanGestureRecognizer panGesture = new PanGestureRecognizer();
                //PinchGestureRecognizer pinchGesture = new PinchGestureRecognizer();
                //panGesture.PanUpdated += OnPanUpdated;
                //pinchGesture.PinchUpdated += OnPinchUpdated;
                //GestureRecognizers.Add(pinchGesture);
                //GestureRecognizers.Add(panGesture);
            }

        }
        private async void OnPanUpdated(object sender, PanUpdatedEventArgs e)
        {
            if (zooming) return;
            Console.WriteLine($"Pan:{e.StatusType.ToString()},{e.TotalX},{e.TotalY}");
            switch (e.StatusType)
            {
                case GestureStatus.Running:
                    // Translate and pan.
                    //var screenCoordinates = GetScreenCoordinates(this);
                    moving = true;
                    double boundsX = imageWidth - Width;
                    double boundsY = imageHeight - Height;
                    TranslationX = Math.Clamp(panX + e.TotalX, -boundsX, 0);
                    TranslationY = Math.Clamp(panY + e.TotalY, 0, boundsY);

                    break;

                case GestureStatus.Completed:
                    // Store the translation applied during the pan
                    moving = false;
                    panX = TranslationX;
                    panY = TranslationY;
                    break;
            }

            //switch (e.StatusType)
            //{
            //    case GestureStatus.Running:
            //        // Translate and ensure we don't pan beyond the wrapped user interface element bounds.
            //        TranslationX = Math.Max(Math.Min(0, panX + e.TotalX), -Math.Abs(Width - DeviceDisplay.MainDisplayInfo.Width));
            //        TranslationY = Math.Max(Math.Min(0, panY + e.TotalY), -Math.Abs(Height - DeviceDisplay.MainDisplayInfo.Height));
            //        break;

            //    case GestureStatus.Completed:
            //        // Store the translation applied during the pan
            //        panX = TranslationX;
            //        panY = TranslationY;
            //        break;
            //}
        }
        //https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/gestures/pan?view=net-maui-8.0

        void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
        {
            if (moving) return;
            //switch (e.Status)
            //{
            //    case GestureStatus.Started:
            //        // Store the current scale factor applied to the wrapped user interface element,
            //        // and zero the components for the center point of the translate transform.
            //        zooming = true;
            //        startScale = Scale;
            //        AnchorX = e.ScaleOrigin.X;
            //        AnchorY = e.ScaleOrigin.Y;
            //        break;
            //    case GestureStatus.Running:
            //        // Calculate the scale factor to be applied.
            //        currentScale += (e.Scale - 1) * startScale;
            //        currentScale = Math.Max(1, currentScale);
            //        Scale = currentScale;
            //        break;
            //    case GestureStatus.Completed:
            //        // Store the final scale factor applied to the wrapped user interface element.
            //        zooming = false;
            //        startScale = currentScale;
            //        break;
            //}
            Console.WriteLine($"Pan:{e.Status.ToString()},{e.Scale}");

            if (e.Status == GestureStatus.Started)
            {
                // Store the current scale factor applied to the wrapped user interface element,
                // and zero the components for the center point of the translate transform.
                startScale = Scale;
                AnchorX = 0;
                AnchorY = 0;
            }
            if (e.Status == GestureStatus.Running)
            {
                // Calculate the scale factor to be applied.
                currentScale += (e.Scale - 1) * startScale;
                currentScale = Math.Max(1, currentScale);

                // The ScaleOrigin is in relative coordinates to the wrapped user interface element,
                // so get the X pixel coordinate.
                double renderedX = X + xOffset;
                double deltaX = renderedX / Width;
                double deltaWidth = Width / (Width * startScale);
                double originX = (e.ScaleOrigin.X - deltaX) * deltaWidth;

                // The ScaleOrigin is in relative coordinates to the wrapped user interface element,
                // so get the Y pixel coordinate.
                double renderedY = Y + yOffset;
                double deltaY = renderedY / Height;
                double deltaHeight = Height / (Height * startScale);
                double originY = (e.ScaleOrigin.Y - deltaY) * deltaHeight;

                // Calculate the transformed element pixel coordinates.
                double targetX = xOffset - (originX * Width) * (currentScale - startScale);
                double targetY = yOffset - (originY * Height) * (currentScale - startScale);

                // Apply translation based on the change in origin.
                TranslationX = Math.Clamp(targetX, -Width * (currentScale - 1), 0);
                TranslationY = Math.Clamp(targetY, -Height * (currentScale - 1), 0);

                // Apply scale factor
                Scale = currentScale;
            }
            if (e.Status == GestureStatus.Completed)
            {
                // Store the translation delta's of the wrapped user interface element.
                xOffset = TranslationX;
                yOffset = TranslationY;
            }
        }
        //https://learn.microsoft.com/en-us/answers/questions/1163990/in-net-maui-how-can-i-implement-zooming-and-scroll

        private static void ImageSourcePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is not RecGraphicsView { Drawable: RectDrawable drawable } view)
            {
                return;
            }
           

            drawable.ImageSource = (IImage)newValue;
            reRender(view);
            view.TranslationX = 0;
            view.TranslationY = 0;
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                view.getDrawingInfo(drawable.ImageSource);
            }
        }

        private static void ListRectangle_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            //Triger reRender
            if (sender is ObservableCollection<Rectangle> listRectangle && listRectangle.Count >= 0)
            {
                if (DeviceInfo.Platform == DevicePlatform.WinUI)
                {
                    reRender(_currentInstance);
                }
            }
        }

        private static void ListRectanglePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is not RecGraphicsView { Drawable: RectDrawable drawable } view)
            {
                return;
            }
            ObservableCollection<Rectangle> listRectangle = (ObservableCollection<Rectangle>)newValue;
            listRectangle.CollectionChanged += ListRectangle_CollectionChanged;
            drawable.ListRectangle = listRectangle;
            _currentInstance = view;
            reRender(view);
        }

        // Mobile Rectangle list
        private static void ListRectangleFill_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            //Triger reRender
            if (sender is ObservableCollection<Rectangle> listRectangleFill && listRectangleFill.Count >= 0)
            {
                reRender(_currentInstance);
            }
        }

        private static void ListRectangleFillPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is not RecGraphicsView { Drawable: RectDrawable drawable } view)
            {
                return;
            }
            ObservableCollection<Rectangle> listRectangleFill = (ObservableCollection<Rectangle>)newValue;
            listRectangleFill.CollectionChanged += ListRectangle_CollectionChanged;
            drawable.ListRectangleFill = listRectangleFill;
            _currentInstance = view;
            reRender(view);
        }

        private static void reRender(RecGraphicsView view)
        {
            lock (drawlock)
            {
                try
                {
                    view.Invalidate();
                }
                catch (Exception)
                {

                }
            }
        }

        private void getDrawingInfo(IImage image)
        {
            if (image == null) return;
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

            imageWidth = drawWidth;
            imageHeight = drawHeight;

            double scaleX = Width / imageWidth;
            double scaleY = Height / imageHeight;
            if (scaleX < scaleY)
            {
                Scale = scaleX;
                TranslationX = -imageWidth * scaleX/4 - 30.5;
            }
            else
            {
                Scale = scaleY;
            }
        }

    }
}

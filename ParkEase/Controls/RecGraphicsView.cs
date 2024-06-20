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

        public IImage ImageSource
        {
            get => (IImage)GetValue(ImageSourceProperty); set { SetValue(ImageSourceProperty, value); }
        }

        /*public ObservableCollection<RectF> Rectangles
        {
            get => (ObservableCollection<RectF>)GetValue(RectanglesProperty);
            set
            {
                SetValue(RectanglesProperty, value);
            }
        }*/

        public ObservableCollection<Rectangle> ListRectangle
        {
            get => (ObservableCollection<Rectangle>)GetValue(ListRectangleProperty);
            set
            {
                SetValue(ListRectangleProperty, value);
            }
        }

        //public static readonly BindableProperty RectanglesProperty = BindableProperty.Create(nameof(Rectangles), typeof(ObservableCollection<RectF>), typeof(RecGraphicsView), propertyChanged: RectanglesPropertyChanged);

        public static readonly BindableProperty ImageSourceProperty = BindableProperty.Create(nameof(ImageSource), typeof(IImage), typeof(RecGraphicsView), propertyChanged: ImageSourcePropertyChanged);

        public static readonly BindableProperty ListRectangleProperty = BindableProperty.Create(nameof(ListRectangle), typeof(ObservableCollection<Rectangle>), typeof(RecGraphicsView), propertyChanged: ListRectanglePropertyChanged);


        public RecGraphicsView()
        {
            PanGestureRecognizer panGesture = new PanGestureRecognizer();
            panGesture.PanUpdated += OnPanUpdated;
            GestureRecognizers.Add(panGesture);
            PinchGestureRecognizer pinchGesture = new PinchGestureRecognizer();
            pinchGesture.PinchUpdated += OnPinchUpdated;
            GestureRecognizers.Add(pinchGesture);

        }

        private async void OnPanUpdated(object sender, PanUpdatedEventArgs e)
        {
            switch (e.StatusType)
            {
                case GestureStatus.Running:
                    // Translate and pan.
                    double boundsX = Width;
                    double boundsY = Height;
                    TranslationX = Math.Clamp(panX + e.TotalX, -boundsX, boundsX);
                    TranslationY = Math.Clamp(panY + e.TotalY, -boundsY, boundsY);
                    break;

                case GestureStatus.Completed:
                    // Store the translation applied during the pan
                    panX = TranslationX;
                    panY = TranslationY;
                    break;
            }
        }

        void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
        {
            switch (e.Status)
            {
                case GestureStatus.Started:
                    // Store the current scale factor applied to the wrapped user interface element,
                    // and zero the components for the center point of the translate transform.
                    startScale = Scale;
                    AnchorX = e.ScaleOrigin.X;
                    AnchorY = e.ScaleOrigin.Y;
                    break;
                case GestureStatus.Running:
                    // Calculate the scale factor to be applied.
                    currentScale += (e.Scale - 1) * startScale;
                    currentScale = Math.Max(1, currentScale);
                    Scale = currentScale;
                    break;
                case GestureStatus.Completed:
                    // Store the final scale factor applied to the wrapped user interface element.
                    startScale = currentScale;
                    break;
            }
        }
        //https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/gestures/pan?view=net-maui-8.0S

        /*private static void Rectangles_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            //Triger reRender
            if (sender is ObservableCollection<RectF> rectangles && rectangles.Count >= 0)
            {
                reRender(_currentInstance);
            }
        }

        private static void RectanglesPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is not RecGraphicsView { Drawable: RectDrawable drawable } view)
            {
                return;
            }
            ObservableCollection<RectF> rectFs = (ObservableCollection<RectF>)newValue;
            rectFs.CollectionChanged += Rectangles_CollectionChanged;
            drawable.Rectangles = rectFs;
            _currentInstance = view;
            reRender(view);
        }*/

        private static void ImageSourcePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is not RecGraphicsView { Drawable: RectDrawable drawable } view)
            {
                return;
            }

            drawable.ImageSource = (IImage)newValue;
            reRender(view);
        }

        private static void ListRectangle_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            //Triger reRender
            if (sender is ObservableCollection<Rectangle> listRectangle && listRectangle.Count >= 0)
            {
                reRender(_currentInstance);
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
    }
}

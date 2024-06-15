using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IImage = Microsoft.Maui.Graphics.IImage;
using ParkEase.Core.Data;

namespace ParkEase.Controls
{
    public class RecGraphicsView : GraphicsView
    {
        /// <summary>
        /// Due to Rectangles setter will not be triggered when to collection was added an item, so RecCount must be changed to perform the draw method.
        /// </summary>
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

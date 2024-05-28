using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IImage = Microsoft.Maui.Graphics.IImage;

namespace ParkEase.Utilities
{
    public class RecGraphicsView : GraphicsView
    {
        /// <summary>
        /// Due to Rectangles setter will not be triggered when to collection was added an item, so RecCount must be changed to perform the draw method.
        /// </summary>
        private static object drawlock = new object();


        public int RectCount
        {
            get => (int)GetValue(RectCountProperty); set { SetValue(RectCountProperty, value); }
        }

        public IImage ImageSource
        {
            get => (IImage)GetValue(ImageSourceProperty); set { SetValue(ImageSourceProperty, value); }
        }

        public ObservableCollection<RectF> Rectangles
        {
            get => (ObservableCollection<RectF>)GetValue(RectanglesProperty); set => SetValue(RectanglesProperty, value);
        }




        public static readonly BindableProperty RectCountProperty = BindableProperty.Create(nameof(RectCount), typeof(int), typeof(RecGraphicsView),propertyChanged: RectCountPropertyChanged);

        public static readonly BindableProperty RectanglesProperty = BindableProperty.Create(nameof(Rectangles), typeof(ObservableCollection<RectF>), typeof(RecGraphicsView), propertyChanged: RectanglesPropertyChanged);

        public static readonly BindableProperty ImageSourceProperty = BindableProperty.Create(nameof(ImageSource), typeof(IImage), typeof(RecGraphicsView), propertyChanged: ImageSourcePropertyChanged);



        private static void RectanglesPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is not RecGraphicsView { Drawable: RectDrawable drawable } view)
            {
                return;
            }

            drawable.Rectangles = (ObservableCollection<RectF>)newValue;
            reRender(view);
        }

        private static void RectCountPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is not RecGraphicsView { Drawable: RectDrawable drawable } view)
            {
                return;
            }

            drawable.RectCount = (int)newValue;
            reRender(view);


        }

        private static void ImageSourcePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is not RecGraphicsView { Drawable: RectDrawable drawable } view)
            {
                return;
            }

            drawable.ImageSource = (IImage)newValue;
            reRender(view);
        }

        private static void reRender(RecGraphicsView view)
        {
            lock(drawlock)
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

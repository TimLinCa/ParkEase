using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkEase.Utilities
{
    public class RecGraphicsView : GraphicsView
    {

        public int RecCount
        {
            get => (int)GetValue(RecCountProperty); set { SetValue(RecCountProperty, value); }

        }

        public string ImgPath
        {
            get => (string)GetValue(ImgPathProperty); set { SetValue(ImgPathProperty, value); }
        }

        public ObservableCollection<RectF> Rectangles
        {
            get => (ObservableCollection<RectF>)GetValue(RectanglesProperty); set => SetValue(RectanglesProperty, value);
        }

        public static readonly BindableProperty RecCountProperty = BindableProperty.Create(nameof(RecCount), typeof(int), typeof(RecGraphicsView),propertyChanged: RecCountPropertyChanged);

        public static readonly BindableProperty RectanglesProperty = BindableProperty.Create(nameof(Rectangles), typeof(ObservableCollection<RectF>), typeof(RecGraphicsView), propertyChanged: RectanglesPropertyChanged);

        public static readonly BindableProperty ImgPathProperty = BindableProperty.Create(nameof(ImgPath), typeof(ObservableCollection<string>), typeof(RecGraphicsView), propertyChanged: ImgPathPropertyChanged);


        private static void RectanglesPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is not RecGraphicsView { Drawable: RectDrawable drawable } view)
            {
                return;
            }

            drawable.Rectangles = (ObservableCollection<RectF>)newValue;
            view.Invalidate();
        }

        private static void RecCountPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is not RecGraphicsView { Drawable: RectDrawable drawable } view)
            {
                return;
            }

            drawable.RecCount = (int)newValue;
            view.Invalidate();
        }

        private static void ImgPathPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is not RecGraphicsView { Drawable: RectDrawable drawable } view)
            {
                return;
            }

            drawable.ImgPath = (string)newValue;
            view.Invalidate();
        }
    }
}

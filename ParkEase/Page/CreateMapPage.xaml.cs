using ParkEase.Utilities;
using ParkEase.ViewModel;
using System.Timers;
using ParkEase.Core.Services;
using Syncfusion.Maui.TabView;
using Microsoft.Maui.Controls;

namespace ParkEase.Page
{
    public partial class CreateMapPage : ContentPage
    {

        public CreateMapPage()
        {
            InitializeComponent();
        }

        public void OnTapGestureRecognizerTapped(object sender, TappedEventArgs args)
        {
            var viewModel = BindingContext as CreateMapViewModel;
            var touchPosition = args.GetPosition(RectangleDrawableView);

            if (viewModel != null && touchPosition.HasValue)
            {
                viewModel.AddRectangle(touchPosition.Value);
            }
        }

        /* https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/gestures/tap?view=net-maui-8.0#get-the-gesture-position
        https://github.com/Programming-With-Chris/MauiDemos/blob/main/MeterGraphicsExample/MainPage.xaml.cs */

        /*public void Additem_Clicked(object sender, EventArgs e)
        {
            var viewModel = BindingContext as CreateMapViewModel;

            SfTabItem tabItem = new();
            tabItem.Header = viewModel.Floor;
            RecGraphicsView graphicsView = new();
            graphicsView.SetBinding(RecGraphicsView.ImageSourceProperty,"ImgSourceData");
            graphicsView.SetBinding(RecGraphicsView.RectanglesProperty, "Rectangles");

            // add tap gesture recognizer
            TapGestureRecognizer tapGestureRecognizer = new TapGestureRecognizer();
            tapGestureRecognizer.Tapped += OnTapGestureRecognizerTapped;
            graphicsView.GestureRecognizers.Add(tapGestureRecognizer);

            // Add the RecGraphicsView to a StackLayout
            StackLayout stackLayout = new StackLayout
            {
                Children = { graphicsView }
            };

            tabItem.Content = stackLayout;
            tabView.Items.Add(tabItem);
        }*/

        /*https://github.com/SyncfusionExamples/getting-started-with-the-.net-maui-tab-view/blob/master/MauiProject/DataViewModel.cs
        https://github.com/SyncfusionExamples/How-to-add-or-remove-Tabs-from-Tab-View-in-.NET-MAUI/tree/master/SfTabviewSample*/

    }
}



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
        private bool isToolExpanded = false;
        private double ToolExpandedHeight;
        private bool isDrawingToolGrid = false;

        private bool isDeleteZoneExpanded = false;
        private double DeleteZoneExpandedHeight;
        private bool isDeleteZoneGrid = false;

        public CreateMapPage()
        {
            InitializeComponent();
            //ExpandedHeight = DeviceDisplay.Current.MainDisplayInfo.Height / DeviceDisplay.Current.MainDisplayInfo.Density / 5.5;
            ToolExpandedHeight = 230;
            DeleteZoneExpandedHeight = 235;
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


        private async void EditToolExpand(object sender, EventArgs e)
        {
            if (isToolExpanded)
            {
                await ToolCollapseAsync();
            }
            else
            {
                await ToolExpandAsync();
            }
            isToolExpanded = !isToolExpanded;
        }



        private async Task ToolExpandAsync()
        {
            DrawingToolGrid.IsVisible = true;
            var animation = new Animation(v => DrawingToolGrid.HeightRequest = v, 0, ToolExpandedHeight);
            animation.Commit(DrawingToolGrid, "ExpandAnimation", 16, 250, Easing.SpringOut);
        }

        private async Task ToolCollapseAsync()
        {
            DrawingToolGrid.IsVisible = false;
            var animation = new Animation(v => DrawingToolGrid.HeightRequest = v, ToolExpandedHeight, 0);
            animation.Commit(DrawingToolGrid, "CollapseAnimation", 16, 250, Easing.SpringIn);
        }

        // For Delete Zone Grid
        private async void DeleteZoneExpand(object sender, EventArgs e)
        {
            if (isDeleteZoneExpanded)
            {
                await DeleteCollapseAsync();
            }
            else
            {
                await DeleteExpandAsync();
            }
            isDeleteZoneExpanded = !isDeleteZoneExpanded;
        }

        private async Task DeleteExpandAsync()
        {
            DeleteZoneGrid.IsVisible = true;
            var animation = new Animation(v => DeleteZoneGrid.HeightRequest = v, 0, DeleteZoneExpandedHeight);
            animation.Commit(DeleteZoneGrid, "ExpandAnimation", 16, 250, Easing.SpringOut);
        }

        private async Task DeleteCollapseAsync()
        {
            DeleteZoneGrid.IsVisible = false;
            var animation = new Animation(v => DeleteZoneGrid.HeightRequest = v, DeleteZoneExpandedHeight, 0);
            animation.Commit(DeleteZoneGrid, "CollapseAnimation", 16, 250, Easing.SpringIn);
        }
    }
}



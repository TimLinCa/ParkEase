using Microsoft.Maui.Devices.Sensors;
using UraniumUI.Pages;
using UraniumUI;


namespace ParkEase.Page
{
    public partial class UserMapPage : UraniumContentPage
    {
        private bool isExpanded = false;
        private double ExpandedHeight;
        public UserMapPage()
        {
            InitializeComponent();
            ExpandedHeight = DeviceDisplay.Current.MainDisplayInfo.Height/ DeviceDisplay.Current.MainDisplayInfo.Density/ 6.2;
        }

        private async void FilterExpand(object sender, EventArgs e)
        {
            if (isExpanded)
            {
                await CollapseAsync();
            }
            else
            {
                await ExpandAsync();
            }
            isExpanded = !isExpanded;
        }

        private async Task ExpandAsync()
        {
            FilterGrid.IsVisible = true;
            var animation = new Animation(v => FilterGrid.HeightRequest = v, 0, ExpandedHeight);
            animation.Commit(FilterGrid, "ExpandAnimation", 16, 250, Easing.SpringOut);
        }

        private async Task CollapseAsync()
        {
            FilterGrid.IsVisible = false;
            var animation = new Animation(v => FilterGrid.HeightRequest = v, ExpandedHeight, 0);
            animation.Commit(FilterGrid, "CollapseAnimation", 16, 250, Easing.SpringIn);
        }


    }
}

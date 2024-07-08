using Microsoft.Maui.Devices.Sensors;
using UraniumUI.Pages;
using UraniumUI;


namespace ParkEase.Page
{
    public partial class UserMapPage : UraniumContentPage
    {
        public UserMapPage()
        {
            InitializeComponent();
        }

        private void OnButtonClicked(object sender, EventArgs e)
        {
            backdrop.IsPresented = true;
        }
    }
}

using Microsoft.Maui.Devices.Sensors;
using ParkEase.Utilities;

namespace ParkEase.Page;

public partial class UserMapPage : ContentPage
{
    public UserMapPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Show the bottom sheets
        MyBottomSheet sheet = new MyBottomSheet
        {
            HasHandle = true,
            HandleColor = Colors.Black
        };

        await sheet.ShowAsync();
    }

}
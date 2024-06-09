namespace ParkEase.Page;
using ParkEase.Utilities;

public partial class PrivateMapPage : ContentPage
{
	public PrivateMapPage()
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
using ParkEase.ViewModel;
using Microsoft.Maui.Controls.Maps;

namespace ParkEase.Page;

public partial class MapPage : ContentPage
{
    private MapViewModel _viewModel;

    public MapPage()
    {
        InitializeComponent();
        myMap.MapClicked += OnMapClicked;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel = (MapViewModel)BindingContext;
    }

    private void OnMapClicked(object sender, MapClickedEventArgs e)
    {
        // Debug to ensure the map is interactive
        Console.WriteLine($"Map clicked at: {e.Location.Latitude}, {e.Location.Longitude}");

        // Update the ViewModel with the clicked location information
        _viewModel.LocationInfo = $"Clicked location: Latitude {e.Location.Latitude}, Longitude {e.Location.Longitude}";
    }
}

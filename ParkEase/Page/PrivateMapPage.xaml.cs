namespace ParkEase.Page;
using ParkEase.Utilities;
using ZXing.Net.Maui;
using ParkEase.ViewModel;
using CommunityToolkit.Maui.Views;
using ParkEase.Controls;

public partial class PrivateMapPage : ContentPage
{
    public PrivateMapPage()
    {
        InitializeComponent();
    }

    public void OnTapGestureRecognizerTapped(object sender, TappedEventArgs args)
    {
        /*var popup = new PrivateMapImage();
        popup.SetImage(GraphicsViewMobile.MapImage);
        this.ShowPopup(popup);*/

        var viewModel = BindingContext as PrivateMapViewModel;
        viewModel.MapImageData = GraphicsViewMobile.MapImage;
    }
}
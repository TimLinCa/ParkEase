using ParkEase.Utilities;
using ParkEase.ViewModel;
using System.Timers;

namespace ParkEase.Page;

public partial class CreateMapPage : ContentPage
{
    public CreateMapPage()
    {
        InitializeComponent();

    }

    public async void OnTapGestureRecognizerTapped(object sender, TappedEventArgs args)
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

}



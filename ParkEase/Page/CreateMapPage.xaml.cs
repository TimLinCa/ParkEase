using ParkEase.Utilities;
using ParkEase.ViewModel;
using System.Timers;

namespace ParkEase.Page;

public partial class CreateMapPage : ContentPage
{

    private GraphicsView graphicsView;
    public CreateMapPage()
    {
        InitializeComponent();

        //viewModel?.SetGraphicsView(this.RectangleDrawableView);
    }

    /*private async void UploadImageClicked(object sender, EventArgs e)
    {
        if (MediaPicker.Default.IsCaptureSupported)
        {
            // Load photo
            FileResult myPhoto = await MediaPicker.Default.PickPhotoAsync();
            if (myPhoto != null)
            {
                // Display the image
                UploadedImage.Source = ImageSource.FromFile(myPhoto.FullPath);
            }
        }
        else
        {
            await Shell.Current.DisplayAlert("OOPS", "Your device isn't supported", "OK");
        }
    }*/

    public async void OnTapGestureRecognizerTapped(object sender, TappedEventArgs args)
    {
        var viewModel = BindingContext as CreateMapViewModel;
        //viewModel?.SetGraphicsView(this.RectangleDrawableView);
        var touchPosition = args.GetPosition(RectangleDrawableView);

        if (viewModel != null && touchPosition.HasValue)
        {
            viewModel.AddRectangle(touchPosition.Value);
        }

    }

    /* https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/gestures/tap?view=net-maui-8.0#get-the-gesture-position
    https://github.com/Programming-With-Chris/MauiDemos/blob/main/MeterGraphicsExample/MainPage.xaml.cs */

}



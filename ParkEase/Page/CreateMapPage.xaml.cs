namespace ParkEase.Page;

public partial class CreateMapPage : ContentPage
{
    private double initialWidth;
    private double initialHeight;
    private double initialX;
    private double initialY;

    public CreateMapPage()
    {
        InitializeComponent();

        //var viewModel = BindingContext as CreateMapViewModel;
        //viewModel?.SetGraphicsView(graphicsView);
    }
    //Sang
    private async void UploadImageClicked(object sender, EventArgs e)
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
    }
    //Sang

    /*private void OnTapGestureRecognizerTapped(object sender, TappedEventArgs e)
    {
        Console.WriteLine("OnTapGestureRecognizerTapped");
        if (sender is GraphicsView graphicsView)
        {
            var touchPoint = e.GetPosition(graphicsView);
            var viewModel = BindingContext as CreateMapViewModel;
            viewModel?.StartInteractionCommand.Execute(touchPoint);
        }
    }*/

}



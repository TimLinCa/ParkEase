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

}



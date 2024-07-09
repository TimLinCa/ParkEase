using CommunityToolkit.Maui.Views;

namespace ParkEase.Controls;

public partial class PrivateMapImage : Popup
{
    // https://www.youtube.com/watch?v=z2oHe9Njni0&t=3s
    public PrivateMapImage()
	{
		InitializeComponent();
    }

    public void SetImage(ImageSource imageSource)
    {
        privateMapImageData.Source = imageSource;
    }

    private async void OnCloseClicked(object sender, EventArgs e)
    {
        Close();
    }

}
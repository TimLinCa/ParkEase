using CommunityToolkit.Maui.Views;

namespace ParkEase.Controls;

public partial class PrivateMapImage : Popup
{
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
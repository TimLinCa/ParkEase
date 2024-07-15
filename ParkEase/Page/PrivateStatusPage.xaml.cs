using ParkEase.ViewModel;

namespace ParkEase.Page;


public partial class PrivateStatusPage : ContentPage
{
	public PrivateStatusPage()
	{
		InitializeComponent();
	}

    public void OnTapGestureRecognizerTapped(object sender, TappedEventArgs args)
    {
        var viewModel = BindingContext as PrivateStatusViewModel;
        var touchPosition = args.GetPosition(GraphicsViewStatusPage);

        if (viewModel != null && touchPosition.HasValue)
        {
            _ = viewModel.DisplaySingleLotInfo(touchPosition.Value);
        }
    }
}
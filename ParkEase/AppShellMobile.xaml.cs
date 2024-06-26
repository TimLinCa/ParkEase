using ParkEase.Page;
using ParkEase.ViewModel;

namespace ParkEase;

public partial class AppShellMobile : Shell
{
	public AppShellMobile(AppShellMobileViewModel viewModel)
	{
        InitializeComponent();
        BindingContext = viewModel;
        Routing.RegisterRoute(Routes.PrivateMapPage, typeof(PrivateMapPage));
    }
}
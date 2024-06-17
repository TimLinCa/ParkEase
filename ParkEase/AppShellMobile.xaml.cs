using ParkEase.Page;

namespace ParkEase;

public partial class AppShellMobile : Shell
{
	public AppShellMobile()
	{
        InitializeComponent();
        Routing.RegisterRoute(Routes.PrivateMapPage, typeof(PrivateMapPage));
    }
}
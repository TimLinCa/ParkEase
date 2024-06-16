using Microsoft.Maui.Controls;
using ParkEase.Page;
using ParkEase.ViewModel;

namespace ParkEase
{
    public partial class AppShell : Shell
    {
        public AppShell(AppShellViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}

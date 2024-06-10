using ParkEase.ViewModel;
using The49.Maui.BottomSheet;

namespace ParkEase.Controls
{
    public partial class MyBottomSheet : BottomSheet
    {
        public MyBottomSheet(BottomSheetViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
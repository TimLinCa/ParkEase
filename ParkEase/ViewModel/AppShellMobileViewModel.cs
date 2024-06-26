using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ParkEase.Contracts.Services;
using ParkEase.Core.Contracts.Services;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using ParkEase.Services;

namespace ParkEase.ViewModel
{
    public partial class AppShellMobileViewModel : ObservableObject
    {
        private IDialogService dialogService;
        public AppShellMobileViewModel()
        {
            this.dialogService =  AppServiceProvider.GetService<IDialogService>();
        }

        public ICommand NavigatingCommand => new RelayCommand(async() =>
        {
            await dialogService.DismissBottomSheetAsync();
        });
    }
}

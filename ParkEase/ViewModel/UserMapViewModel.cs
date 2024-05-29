using CommunityToolkit.Mvvm.ComponentModel;
using ParkEase.Core.Contracts.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using ParkEase.Contracts.Services;

namespace ParkEase.ViewModel
{
    public class UserMapViewModel : ObservableObject
    {
        private IMongoDBService mongoDBService;
        private IDialogService dialogService;
        public UserMapViewModel(IMongoDBService mongoDBService, IDialogService dialogService)
        {
            this.mongoDBService = mongoDBService;
            this.dialogService = dialogService;
        }
    }
}

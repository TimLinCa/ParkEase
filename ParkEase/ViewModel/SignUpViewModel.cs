using CommunityToolkit.Mvvm.ComponentModel;
using ParkEase.Core.Contracts.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkEase.ViewModel
{
    public partial class SignUpViewModel : ObservableObject
    {
        [ObservableProperty]
        private string email;

        [ObservableProperty]
        private string password;

        private IMongoDBService mongoDBService;

        public SignUpViewModel(IMongoDBService mongoDBService)
        {
            this.mongoDBService = mongoDBService;   
            Email = "";
            Password = "";
        }

      
    }
}

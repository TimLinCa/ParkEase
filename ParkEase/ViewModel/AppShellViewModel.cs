using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using ParkEase.Core.Data;
using ParkEase.Core.Model;
using ParkEase.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkEase.ViewModel
{
    public partial class AppShellViewModel : ObservableObject
    { 
        [ObservableProperty]
        private bool mapVisible;

        [ObservableProperty]
        private bool createMapVisible;

        public AppShellViewModel()
        {
            mapVisible = true;
            createMapVisible = true;
            WeakReferenceMessenger.Default.Register<UserChangedMessage>(this,(o, e) =>
            {
                UserChanged(e.Value);
            });
        }


        private void UserChanged(User user)
        {
            switch(user.Role)
            {
                case Roles.Engineer:
                    CreateMapVisible = false;
                    break;
                case Roles.Administrator:
                    MapVisible = false;
                    break;
                case Roles.User:
                    MapVisible = false;
                    CreateMapVisible = false;
                    break;
            }
        }


    }
}

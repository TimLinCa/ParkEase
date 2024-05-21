using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Controls.Maps;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ParkEase.ViewModel
{
    public class MapViewModel : INotifyPropertyChanged
    {
        private string _locationInfo;

        public string LocationInfo
        {
            get => _locationInfo;
            set
            {
                _locationInfo = value;
                OnPropertyChanged();
            }
        }

        private bool _draw; // Indicates whether the line is drawn or not

        public bool Draw
        {
            get => _draw;
            set
            {
                _draw = value;
                OnPropertyChanged();
            }
        }

        private Location? _startLocation; // The starting point of the line

        public Location? StartLocation
        {
            get => _startLocation;
            set
            {
                _startLocation = value;
                OnPropertyChanged();
            }
        }

        private Polyline? _selectedPolyline; // Selected line

        public Polyline? SelectedPolyline
        {
            get => _selectedPolyline;
            set
            {
                _selectedPolyline = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

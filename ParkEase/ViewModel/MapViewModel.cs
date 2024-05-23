using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ParkEase.Contracts.Services;
using ParkEase.Core.Contracts.Services;
using ParkEase.Core.Data;
using ParkEase.Core.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace ParkEase.ViewModel
{
    public partial class MapViewModel : ObservableObject
    {
        [ObservableProperty]
        private string parkingId;

        [ObservableProperty]
        private string parkingSpot;

        [ObservableProperty]
        private string parkingTime;

        [ObservableProperty]
        private string parkingFee;

        [ObservableProperty]
        private string parkingCapacity;

        [ObservableProperty]
        private string locationInfo;

        [ObservableProperty]
        private bool draw; // Indicates whether the line is drawn or not

        [ObservableProperty]
        private Location? startLocation; // The starting point of the line

        [ObservableProperty]
        private Polyline? selectedPolyline; // Selected line

        private readonly IMongoDBService mongoDBService;
        private readonly IDialogService dialogService;

        public ObservableCollection<string> ParkingTimes { get; }
        private string _selectedParkingTime;

        public string SelectedParkingTime
        {
            get => _selectedParkingTime;
            set
            {
                SetProperty(ref _selectedParkingTime, value);
                ParkingTime = value; // Update the ParkingTime property when a selection is made
            }
        }

        // New properties for Parking Fee Picker
        public ObservableCollection<string> ParkingFees { get; }
        private string _selectedParkingFee;

        public string SelectedParkingFee
        {
            get => _selectedParkingFee;
            set
            {
                SetProperty(ref _selectedParkingFee, value);
                ParkingFee = value; // Update the ParkingTime property when a selection is made
            }
        }
        public class Polyline
        {
            public List<Location> Points { get; set; }

            public Polyline()
            {
                Points = new List<Location>();
            }
        }

        public MapViewModel(IMongoDBService mongoDBService, IDialogService dialogService)
        {
            this.mongoDBService = mongoDBService;
            this.dialogService = dialogService;
            parkingId = "";
            parkingSpot = "";
            parkingTime = "";
            parkingFee = "";
            parkingCapacity = "";
            locationInfo = "";
            draw = false;
            startLocation = null;
            selectedPolyline = new Polyline();

            ParkingTimes = new ObservableCollection<string>   /*https://www.calgaryparking.com/find-parking/on-street.html*/
            {
                "Mon to Fri: 7am to 6pm",
                "Sat: 9am to 6pm",
                "Sun and holidays",
                "Evening after 6pm"
            };

            ParkingFees = new ObservableCollection<string> /*https://thecityofcalgary.maps.arcgis.com/apps/instant/sidebar/index.html?appid=10fd81aba2a548d49e7731f593c36282*/
            {
                "Free",
                "$1.50 per hour",
                "$2.00 per hour"
            };
        }

        public ICommand SubmitCommand => new RelayCommand(async () =>
        {
            try
            {
                if (!string.IsNullOrEmpty(ParkingTime) && !string.IsNullOrEmpty(ParkingFee) && !string.IsNullOrEmpty(ParkingCapacity))
                {
                    var parkingData = new ParkingData
                    {
                        ParkingSpot = ParkingSpot,
                        ParkingTime = ParkingTime,
                        ParkingFee = ParkingFee,
                        ParkingCapacity = ParkingCapacity
                    };

                    await mongoDBService.InsertData(CollectionName.ParkingData, parkingData);
     
                    await dialogService.ShowAlertAsync("", "Your information is submitted.", "OK");
                }
                else
                {
                    await dialogService.ShowAlertAsync("", "Please fill in all fields.", "OK");
                }
            }
            catch (Exception ex)
            {
                await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
            }
        });

        public ICommand DrawLineCommand => new RelayCommand(() =>
        {
            // Communicate with the WebView to start drawing a line
            DrawLineRequested?.Invoke(this, EventArgs.Empty);
        });

        public ICommand ClearLineCommand => new RelayCommand(() =>
        {
            // Communicate with the WebView to clear the selected line
            ClearLineRequested?.Invoke(this, EventArgs.Empty);
        });

        // Events to communicate with the WebView
        public event EventHandler DrawLineRequested;
        public event EventHandler ClearLineRequested;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}


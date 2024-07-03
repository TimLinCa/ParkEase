using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace ParkEase.Controls
{
    public partial class MultiSelectPopup : ContentPage
    {
        public ObservableCollection<SelectableItem> Options { get; set; }

        public MultiSelectPopup()
        {
            InitializeComponent();
            Options = new ObservableCollection<SelectableItem>
            {
                new SelectableItem { Name = "Public Parking" },
                new SelectableItem { Name = "Private Parking" },
                new SelectableItem { Name = "Available Parking" }
            };
            BindingContext = this;
        }

        public class SelectableItem : INotifyPropertyChanged
        {
            public string Name { get; set; }
            private bool isSelected;
            public bool IsSelected
            {
                get => isSelected;
                set
                {
                    if (isSelected != value)
                    {
                        isSelected = value;
                        OnPropertyChanged();
                    }
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public ICommand CloseCommand => new Command(async () => await Application.Current.MainPage.Navigation.PopModalAsync());
    }
}

using ParkEase.Utilities;
using ParkEase.ViewModel;
using System.Timers;
using ParkEase.Core.Services;
using Syncfusion.Maui.TabView;
using Microsoft.Maui.Controls;


namespace ParkEase.Page
{
    public partial class CreateMapPage : ContentPage
    {

        public CreateMapPage()
        {
            InitializeComponent();
            //var viewModel = new CreateMapViewModel();
           

        }




        public void OnTapGestureRecognizerTapped(object sender, TappedEventArgs args)
        {
            var viewModel = BindingContext as CreateMapViewModel;
            var touchPosition = args.GetPosition(RectangleDrawableView);

            if (viewModel != null && touchPosition.HasValue)
            {
                viewModel.AddRectangle(touchPosition.Value);
            }
        }

        /*public void Additem_Clicked(object sender, EventArgs e)
        {
            SfTabItem insertItem = new SfTabItem();
            insertItem.Header = "New Item Inserted";
            StackLayout stacklayout1 = new StackLayout();
            stacklayout1.BackgroundColor = Colors.PaleGreen;
            insertItem.Content = stacklayout1;
            if (tabView.Items.Count > 0)
                tabView.Items.Insert(1, insertItem);
            else
                tabView.Items.Insert(0, insertItem);
        }*/
    }
}



namespace ParkEase.Page;
using ParkEase.Utilities;
using ZXing.Net.Maui;
using ParkEase.ViewModel;
using CommunityToolkit.Maui.Views;
using ParkEase.Controls;
using System.IO;
using SkiaSharp;
using Microsoft.Maui.Controls.Compatibility;


public partial class PrivateMapPage : ContentPage
{
    public PrivateMapPage()
    {
        InitializeComponent();
    }

    /*public async void OnTapGestureRecognizerTapped(object sender, TappedEventArgs args)
    {
        *//*var popup = new PrivateMapImage();
        popup.SetImage(GraphicsViewMobile.MapImage);
        this.ShowPopup(popup);*//*

        var viewModel = BindingContext as PrivateMapViewModel;
        var mapImage = GraphicsViewMobile.MapImage;
        System.Diagnostics.Debug.WriteLine($"MapImage type: {mapImage.GetType().FullName}");
        await viewModel.SaveImageToLocal(mapImage);
    }*/

    /*private async void OnTapGestureRecognizerTapped(object sender, TappedEventArgs e)
    {
        try
        {
            // Capture the GraphicsView as an image
            var screenshot = await GraphicsViewMobile.CaptureAsync();
            if (screenshot != null)
            {
                // Get the image stream
                using var imageStream = await screenshot.OpenReadAsync();

                // Read the stream into a byte array
                byte[] imageData;
                using (var memoryStream = new MemoryStream())
                {
                    await imageStream.CopyToAsync(memoryStream);
                    imageData = memoryStream.ToArray();
                }

                // Create a StreamImageSource from the image data
                var imageSource = ImageSource.FromStream(() => new MemoryStream(imageData));

                // Create and show the popup
                var popup = new PrivateMapImage();
                popup.SetImage(imageSource);
                await this.ShowPopupAsync(popup);
            }
            else
            {
                await DisplayAlert("Error", "Failed to capture screenshot", "OK");
            }
        }
        catch (Exception ex)
        {
            // Handle any errors
            await DisplayAlert("Error", $"Failed to capture image: {ex.Message}", "OK");
        }
    }*/


}
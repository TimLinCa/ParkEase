namespace ParkEase.Page;
using ParkEase.Utilities;
using ZXing.Net.Maui;
using ParkEase.ViewModel;

public partial class PrivateMapPage : ContentPage
{
    private double currentScale = 1;
    private double startScale = 1;
    private double xOffset = 0;
    private double yOffset = 0;
    public PrivateMapPage()
    {
        InitializeComponent();
    }

    /*private void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
    {
        viewModel.OnPinchUpdated(e);
    }

    private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        viewModel.OnPanUpdated(e);
    }*/

    //private void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
    //{
    //    if (e.Status == GestureStatus.Started)
    //    {
    //        // Store the current scale factor applied to the wrapped user interface element,
    //        // and zero the components for the center point of the translate transform.
    //        startScale = RectangleDrawableViewMobile.Scale;
    //        RectangleDrawableViewMobile.AnchorX = 0;
    //        RectangleDrawableViewMobile.AnchorY = 0;
    //    }

    //    if (e.Status == GestureStatus.Running)
    //    {
    //        // Calculate the scale factor to be applied.
    //        currentScale += (e.Scale - 1) * startScale;
    //        currentScale = Math.Max(1, currentScale);

    //        // Apply the scale factor to the wrapped user interface element.
    //        RectangleDrawableViewMobile.Scale = currentScale;
    //    }

    //    if (e.Status == GestureStatus.Completed)
    //    {
    //        // Store the translation delta's applied during the pan gesture.
    //        xOffset = RectangleDrawableViewMobile.TranslationX;
    //        yOffset = RectangleDrawableViewMobile.TranslationY;
    //    }
    //}

    //private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
    //{
    //    if (e.StatusType == GestureStatus.Running)
    //    {
    //        // Translate the image based on the pan gesture.
    //        RectangleDrawableViewMobile.TranslationX = xOffset + e.TotalX;
    //        RectangleDrawableViewMobile.TranslationY = yOffset + e.TotalY;
    //    }

    //    if (e.StatusType == GestureStatus.Completed)
    //    {
    //        if (RectangleDrawableViewMobile.TranslationX < 0)
    //        {
    //            RectangleDrawableViewMobile.TranslationX = 0;
    //            xOffset = 0;
    //        }


    //        if (RectangleDrawableViewMobile.TranslationY < 0)
    //        {
    //            RectangleDrawableViewMobile.TranslationY = 0;
    //            yOffset = 0;
    //        }

    //        // Store the translation applied during the pan gesture.
    //        xOffset = RectangleDrawableViewMobile.TranslationX;
    //        yOffset = RectangleDrawableViewMobile.TranslationY;
    //    }
    //}
}
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
    private double panX, panY;
    public PrivateMapPage()
    {
        InitializeComponent();
    }

    void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
        {
            switch (e.Status)
            {
                case GestureStatus.Started:
                    // Store the current scale factor applied to the wrapped user interface element,
                    // and zero the components for the center point of the translate transform.
                    startScale = RectangleDrawableView.Scale;
                    RectangleDrawableView.AnchorX = e.ScaleOrigin.X;
                    RectangleDrawableView.AnchorY = e.ScaleOrigin.Y;
                    break;
                case GestureStatus.Running:
                    // Calculate the scale factor to be applied.
                    currentScale += (e.Scale - 1) * startScale;
                    currentScale = Math.Max(1, currentScale);
                    RectangleDrawableView.Scale = currentScale;
                    break;
                case GestureStatus.Completed:
                    // Store the final scale factor applied to the wrapped user interface element.
                    startScale = currentScale;
                    break;
            }
        }
    //https://learn.microsoft.com/en-us/answers/questions/1163990/in-net-maui-how-can-i-implement-zooming-and-scroll


    private async void OnPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Running:
                // Translate and pan.
                double boundsX = RectangleDrawableView.Width;
                double boundsY = RectangleDrawableView.Height;
                RectangleDrawableView.TranslationX = Math.Clamp(panX + e.TotalX, -boundsX, boundsX);
                RectangleDrawableView.TranslationY = Math.Clamp(panY + e.TotalY, -boundsY, boundsY);
                break;

            case GestureStatus.Completed:
                // Store the translation applied during the pan
                panX = RectangleDrawableView.TranslationX;
                panY = RectangleDrawableView.TranslationY;
                break;
        }
    }
    //https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/gestures/pan?view=net-maui-8.0
}
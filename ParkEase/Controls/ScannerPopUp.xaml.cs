using Camera.MAUI.ZXing;
using Camera.MAUI;
using CommunityToolkit.Maui.Views;
using Syncfusion.Maui.Core.Carousel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using ParkEase.Messages;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using ParkEase.Core.Model;
using ParkEase.Page;


namespace ParkEase.Controls;

public partial class ScannerPopUp : Popup
{
    private object cameraLock = new object();
    private bool isDetecting = false;
    public event StartCameraAsyncHandler StartCameraAsyncEvent;
    public event StopCameraAsyncHandler StopCameraAsyncEvent;
    private bool isPopupClosed = false;
    private CancellationTokenSource cancellationTokenSource;


    public ScannerPopUp()
	{
		InitializeComponent();
        cancellationTokenSource = new CancellationTokenSource();


    }

    private void OnPopupOpened(object sender, EventArgs e)
    {
        isPopupClosed = false;
        MainThread.BeginInvokeOnMainThread(ThisThreadCode);
        StartCameraAsyncEvent?.Invoke();
    }

    private void OnPopupClosed(object sender, EventArgs e)
    {
        PrivateSearchThreadCode();
        stopCameraAsyncEvent();
        StopCameraAsyncEvent?.Invoke();
    }

    private void CameraLoaded(object sender, EventArgs e)
    {
        /*cameraView.Camera = cameraView.Cameras.First();
        startCameraAsyncEvent();*/
        /*if(cameraView.Cameras.Count > 0)
        {
            cameraView.Camera = cameraView.Cameras.First();
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await cameraView.StopCameraAsync();
                await cameraView.StartCameraAsync();
            });
        }*/
        cameraView.BarCodeDecoder = new ZXingBarcodeDecoder();
        cameraView.BarCodeOptions = new BarcodeDecodeOptions
        {
            AutoRotate = true,
            PossibleFormats = { BarcodeFormat.QR_CODE },
            ReadMultipleCodes = false,
            TryHarder = true,
            TryInverted = true
        };

        cameraView.BarCodeDetectionFrameRate = 10;
        cameraView.BarCodeDetectionMaxThreads = 5;
        cameraView.BarCodeDetectionEnabled = true;
        if (cameraView.Cameras.Count > 0)
        {
            cameraView.Camera = cameraView.Cameras.First();
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await cameraView.StopCameraAsync();
                await cameraView.StartCameraAsync();
            });
        }

            /*cameraView.BarCodeDecoder = new ZXingBarcodeDecoder();
            cameraView.BarCodeOptions = new BarcodeDecodeOptions
            {
                AutoRotate = true,
                PossibleFormats = { BarcodeFormat.QR_CODE },
                ReadMultipleCodes = false,
                TryHarder = true,
                TryInverted = true
            };

            cameraView.BarCodeDetectionFrameRate = 10;
            cameraView.BarCodeDetectionMaxThreads = 5;
            cameraView.BarCodeDetectionEnabled = true;*/
        }

        private void startCameraAsyncEvent()
    {
        lock (cameraLock)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await cameraView.StartCameraAsync();
            });
        }

    }

    private void stopCameraAsyncEvent()
    {
        lock (cameraLock)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await cameraView.StopCameraAsync();
            });
        }

    }

    public void BarcodeDetectEventCommand(object sender, Camera.MAUI.ZXingHelper.BarcodeEventArgs args)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            DataService.SetId(args.Result[0].Text);
            //DataService.SetId(args.Result[0].Text);
        });
        stopCameraAsyncEvent();
        Close();
        MainThread.BeginInvokeOnMainThread(MyMainThreadCode);

    }
    void MyMainThreadCode()
    {
        Shell.Current.GoToAsync(nameof(PrivateMapPage));
    }

    void ThisThreadCode()
    {
        Shell.Current.GoToAsync(nameof(ScannerPopUp));

    }

    void PrivateSearchThreadCode()
    {
        Shell.Current.GoToAsync(nameof(PrivateSearchPage));
    }

    private void Popup_Closed(object sender, CommunityToolkit.Maui.Core.PopupClosedEventArgs e)
    {

    }
    /*public ICommand BarcodeDetectEventCommand => new RelayCommand<string>(async qrCode =>
{
   try
   {
       idResult = qrCode;
       StopCameraAsyncEvent?.Invoke();
       GridVisible = !GridVisible;
       parkEaseModel.PrivateMapId = idResult;
       MainThread.BeginInvokeOnMainThread(MyMainThreadCode);
   }
   catch (Exception ex)
   {
       await dialogService.ShowAlertAsync("Error", ex.Message, "OK");
   }
});*/
}
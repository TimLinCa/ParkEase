using Camera.MAUI;
using Camera.MAUI.ZXing;
using Camera.MAUI.ZXingHelper;
using ParkEase.ViewModel;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;
using BarcodeFormat = Camera.MAUI.BarcodeFormat;
using CommunityToolkit.Maui.Views;
using ParkEase.Core.Contracts.Services;
using ParkEase.Core.Model;
using MongoDB.Driver;
using ParkEase.Contracts.Services;


namespace ParkEase.Page;

public partial class PrivateSearchPage : ContentPage
{

/*    private object cameraLock = new object();
    private readonly IMongoDBService mongoDBService;

    private readonly IDialogService dialogService;

    private readonly ParkEaseModel parkEaseModel;*/

    public PrivateSearchPage()
	{
		InitializeComponent();
/*        BindingContext = viewmodel;
        viewmodel.StopCameraAsyncEvent += stopCameraAsyncEvent;
        viewmodel.StartCameraAsyncEvent += startCameraAsyncEvent;*/
    }

    


    //https://www.nuget.org/packages/Camera.MAUI.ZXing
    //https://learn.microsoft.com/en-us/answers/questions/468404/(xamarin-community-toolkit)-how-i-can-use-cameravi
   /* private void CameraLoaded(object sender, EventArgs e)
    {
        cameraView.Camera = cameraView.Cameras.First();
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
       
    }*/
}
using Camera.MAUI.ZXing;
using Camera.MAUI;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using ParkEase.Core.Model;
using ParkEase.Page;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ParkEase.Messages;

namespace ParkEase.Controls
{
    public partial class ScannerPopUp : Popup
    {
        private CancellationTokenSource cancellationTokenSource;

        public event StartCameraAsyncHandler StartCameraAsyncEvent;
        public event StopCameraAsyncHandler StopCameraAsyncEvent;

        public ScannerPopUp()
        {
            InitializeComponent();
            cancellationTokenSource = new CancellationTokenSource();
            this.Opened += OnPopupOpened;
            this.Closed += OnPopupClosed;
        }

        private void OnPopupOpened(object sender, EventArgs e)
        {
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = new CancellationTokenSource();
            StartCameraAsyncEvent?.Invoke();
            Console.WriteLine("Popup Opened");
        }
        // https://www.youtube.com/watch?v=FuvFrIS9wm0&t=433s

        private void OnPopupClosed(object sender, CommunityToolkit.Maui.Core.PopupClosedEventArgs e)
        {
            cancellationTokenSource.Cancel();
            StopCameraAsyncEvent?.Invoke();
            Console.WriteLine("Popup Closed");
        }

        private void CameraLoaded(object sender, EventArgs e)
        {
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
                StartCameraAsyncTask(cancellationTokenSource.Token);
            }
        }

        private async void StartCameraAsyncTask(CancellationToken cancellationToken)
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await cameraView.StartCameraAsync();
                    }
                });
            }
            catch (OperationCanceledException)
            {
                // Handle task cancellation
                Console.WriteLine("Camera start task was canceled.");
            }
            catch (Exception ex)
            {
                // Handle other exceptions
                Console.WriteLine($"Error starting camera: {ex.Message}");
            }
        }


        public void BarcodeDetectEventCommand(object sender, Camera.MAUI.ZXingHelper.BarcodeEventArgs args)
        {

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (args.Result != null && args.Result.Any())
                {
                    if (!string.IsNullOrEmpty(args.Result[0].Text))
                    {
                        DataService.SetId(args.Result[0].Text);
                    }
                    else
                    {
                        Console.WriteLine("Error: Barcode text is null or empty.");
                    }
                }
                else
                {
                    Console.WriteLine("Error: Barcode result is null or empty.");
                }

                StopCameraAsyncEvent?.Invoke();

                try
                {
                    Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error closing popup: {ex.Message}");
                }

                MyMainThreadCode();
            });
        }

        private void MyMainThreadCode()
        {
            try
            {
                Shell.Current.GoToAsync(nameof(PrivateMapPage));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error navigating to PrivateMapPage: {ex.Message}");
            }
        }

        private void PrivateSearchThreadCode()
        {
            Shell.Current.GoToAsync(nameof(PrivateSearchPage));
        }

    }
}

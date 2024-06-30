using ParkEase.Core.Contracts.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using ZXing.Aztec.Internal;

namespace ParkEase.Core.Services
{
    //https://vladislavantonyuk.github.io/articles/Real-time-live-tracking-using-.NET-MAUI/
    public class GeolocatorService : IGeolocatorService
    {
        private GeolocationContinuousListener? locator;

        public async Task StartListening(IProgress<Location> positionChangedProgress, CancellationToken cancellationToken)
        {
            var permission = await Permissions.CheckStatusAsync<Permissions.LocationAlways>();
            if (permission != PermissionStatus.Granted)
            {
                permission = await Permissions.RequestAsync<Permissions.LocationAlways>();
                if (permission != PermissionStatus.Granted)
                {
                    return;
                }
            }

            locator = new GeolocationContinuousListener();
           
            var taskCompletionSource = new TaskCompletionSource();
            locator.OnLocationChangedAction = location =>
                positionChangedProgress.Report(
                    new Location(location.Latitude, location.Longitude));
            locator.Run(cancellationToken);
            await taskCompletionSource.Task;
        }
    }

    internal class GeolocationContinuousListener : IDisposable
    {
        public Action<Location>? OnLocationChangedAction { get; set; }

        private CancellationToken token;

        readonly bool stopping = false;

        public GeolocationContinuousListener()
        {

        }

        public async Task Run(CancellationToken token)
        {
            this.token = token;
            await Task.Run(async () =>
            {
                while (!stopping)
                {
                    token.ThrowIfCancellationRequested();
                    try
                    {
                        Location? location = await Geolocation.GetLocationAsync();
                        if (location != null)
                        {
                            OnLocationChanged(location);
                        }
                        await Task.Delay(2000);
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
                return;
            },token);
        }

        public void OnLocationChanged(Location location)
        {
            OnLocationChangedAction?.Invoke(location);
        }

        public void OnProviderDisabled(string provider)
        {
        }
        public void OnProviderEnabled(string provider)
        {
        }

        public void Dispose()
        {
            
        }
    }
}

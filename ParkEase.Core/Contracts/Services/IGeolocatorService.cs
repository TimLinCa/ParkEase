using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkEase.Core.Contracts.Services
{
    public interface IGeolocatorService
    {
        Task StartListening(IProgress<Location> positionChangedProgress, CancellationToken cancellationToken);
    }
}

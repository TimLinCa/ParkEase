using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkEase.Core.Contracts.Services
{
    public interface IGeocodingService
    {
        Task<Location> GetLocationAsync(string address);
    }
}

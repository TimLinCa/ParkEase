using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ParkEase.Core.Data;

namespace ParkEase.Core.Contracts.Services
{
    public interface IGeocodingService
    {
        Task<Location> GetLocationAsync(string address);

        Task<List<SearchResultItem>> GetPredictedAddressAsync(string input, double? latitude, double? longitude);
    }
}

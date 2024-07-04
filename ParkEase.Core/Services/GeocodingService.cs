using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ParkEase.Core.Contracts.Services;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Maui.Devices.Sensors;

namespace ParkEase.Core.Services
{
    public class GeocodingService : IGeocodingService
    {
        private readonly string apiKey;

        public GeocodingService()
        {
            this.apiKey = Environment.GetEnvironmentVariable("GoogleAKYKey");
        }

        public async Task<Location> GetLocationAsync(string address)
        {
            using (var client = new HttpClient())
            {
                var url = $"https://maps.googleapis.com/maps/api/geocode/json?address={address}&key={apiKey}";
                var response = await client.GetStringAsync(url);
                var json = JObject.Parse(response);

                var status = json["status"].ToString();
                if (status == "OK")
                {
                    var location = json["results"][0]["geometry"]["location"];
                    var latitude = (double)location["lat"];
                    var longitude = (double)location["lng"];
                    Location newlocation = new Location(latitude, longitude);
                    return newlocation;
                }
                return null;
            }
        }
    }
}

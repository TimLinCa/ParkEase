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
using ParkEase.Core.Data;

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
        //https://developers.google.com/maps/documentation/places/web-service/autocomplete?_gl=1*1co6bdj*_up*MQ..*_ga*MTQyMjUxMTc5Mi4xNzIwNzI3NDUx*_ga_NRWSTWS78N*MTcyMDcyNzQ1MS4xLjAuMTcyMDcyNzQ1MS4wLjAuMA..
        public async Task<List<SearchResultItem>> GetPredictedAddressAsync(string input, double? latitude, double? longitude)
        {
            List<SearchResultItem> predictedAddress = new List<SearchResultItem>();
            try
            {
                string baseUrl = "https://maps.googleapis.com/maps/api/place/autocomplete/json";
                string url = $"{baseUrl}?input={input}&key={apiKey}";
                if (latitude != null && longitude != null && latitude != 0 && longitude != 0)
                {
                    url += $"=&origin={latitude},{longitude}";
                }
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    var responseStr = await response.Content.ReadAsStringAsync();
                    var jsonObject = JObject.Parse(responseStr);
                    var predictions = jsonObject["predictions"];

                    if (predictions != null)
                    {
                        predictedAddress = predictions.Select(pd => new SearchResultItem()
                        {
                            AddressName = pd["description"]?.ToString() ?? "",
                            SecondaryText = pd["structured_formatting"]?["secondary_text"]?.ToString() ?? "",
                            Distance = latitude != null && longitude != null && latitude != 0 && longitude != 0 ?
                                ((double?)pd["distance_meters"] ?? 0) / 1000 : 0
                        }).ToList();
                    }
                }

                return predictedAddress;
            }
            catch (Exception ex)
            {
                return predictedAddress;
            }
        }

    }
    
}

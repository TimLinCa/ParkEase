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
            List<SearchResultItem> predictedPlaces = new List<SearchResultItem>();
            try
            {
                //string baseUrl = "https://maps.googleapis.com/maps/api/place/autocomplete/json";
                //string url = $"{baseUrl}?input={input}&key={apiKey}";
                //if (latitude != null && longitude != null && latitude != 0 && longitude != 0)
                //{
                //    url += $"=&origin={latitude},{longitude}";
                //}
                //using (HttpClient client = new HttpClient())
                //{
                //    HttpResponseMessage response = await client.GetAsync(url);
                //    response.EnsureSuccessStatusCode();
                //    var responseStr = await response.Content.ReadAsStringAsync();
                //    var jsonObject = JObject.Parse(responseStr);
                //    var predictions = jsonObject["predictions"];

                //    if (predictions != null)
                //    {
                //        predictedAddress = predictions.Select(pd => new SearchResultItem()
                //        {
                //            AddressName = pd["description"]?.ToString() ?? "",
                //            SecondaryText = pd["structured_formatting"]?["secondary_text"]?.ToString() ?? "",
                //            Distance = latitude != null && longitude != null && latitude != 0 && longitude != 0 ?
                //                Math.Round((double?)pd["distance_meters"] ?? 0,1) / 1000 : 0
                //        }).Where(pre => pre.Distance < 30).OrderBy(pre=>pre.Distance).ToList();
                //    }
                //}

                //return predictedAddress;

                string baseUrl = "https://maps.googleapis.com/maps/api/place/autocomplete/json";
                string url = $"{baseUrl}?input={Uri.EscapeDataString(input)}&key={apiKey}&types=geocode|establishment&";

                if (latitude != null && longitude != null && latitude != 0 && longitude != 0)
                {
                    url += $"location={latitude},{longitude}&radius=30000&origin={latitude},{longitude}&rankby=distance";
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
                        predictedPlaces = predictions.Select(pd => new SearchResultItem()
                        {
                            PlaceId = pd["place_id"]?.ToString() ?? "",
                            AddressName = pd["description"]?.ToString() ?? "",
                            SecondaryText = pd["structured_formatting"]?["secondary_text"]?.ToString() ?? "",
                            Distance = pd["distance_meters"] != null ? Math.Round((double)pd["distance_meters"]/1000,1) : null
                        }).Where(pre=>pre.Distance!= null).OrderBy(pre => pre.Distance).ToList();
                    }
                }
                return predictedPlaces;
            }
            catch (Exception ex)
            {
                return predictedPlaces;
            }
        }

        public async Task<Location> GetCoordinatesByPlaceId(string placeId)
        {
            try
            {
                string baseUrl = "https://maps.googleapis.com/maps/api/place/details/json";
                string url = $"{baseUrl}?place_id={placeId}&fields=geometry&key={apiKey}";
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    var responseStr = await response.Content.ReadAsStringAsync();
                    var jsonObject = JObject.Parse(responseStr);

                    var result = jsonObject["result"];
                    if (result != null && result["geometry"] != null && result["geometry"]["location"] != null)
                    {
                        var location = result["geometry"]["location"];
                        double? latitude = location["lat"]?.Value<double>();
                        double? longitude = location["lng"]?.Value<double>();

                        if (latitude != null && longitude != null) { return new Location(latitude.Value, longitude.Value); }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while fetching coordinates: {ex.Message}");
            }

            return null;
        }

    }
    
}

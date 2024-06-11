using Microsoft.Maui.Controls;
using Newtonsoft.Json;
using ParkEase.Core.Data;
using ParkEase.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkEase.Controls
{
    public class GMapMobile : WebView
    {
        private bool mapInitialised = false;
        public delegate void EventArgsHandler(object? sender, EventArgs e);
        public event EventArgsHandler LoadedEvent;
        private static GMapMobile currentInstance;
        private static bool selfUpdatingLines = false;

        public ObservableCollection<MapLine> Lines
        {
            get => (ObservableCollection<MapLine>)GetValue(LinesProperty); set { SetValue(LinesProperty, value); }
        }

        public MapLine SelectedLine
        {
            get => (MapLine)GetValue(SelectedLineProperty); set { SetValue(SelectedLineProperty, value); }
        }


        public static readonly BindableProperty LinesProperty = BindableProperty.Create(nameof(Lines), typeof(ObservableCollection<MapLine>), typeof(GMapMobile), propertyChanged: LinesPropertyChanged, defaultBindingMode: BindingMode.TwoWay);

        public static readonly BindableProperty SelectedLineProperty = BindableProperty.Create(nameof(SelectedLine), typeof(MapLine), typeof(GMapMobile), defaultBindingMode: BindingMode.TwoWay);

        public GMapMobile()
        {
            HorizontalOptions = LayoutOptions.FillAndExpand;
            VerticalOptions = LayoutOptions.FillAndExpand;
            var htmlSource = new HtmlWebViewSource
            {
                Html = @"
        <!DOCTYPE html>
        <html lang=""en"" xmlns=""http://www.w3.org/1999/xhtml""> 
        

        <head>
            <meta charset=""utf-8"" />
            <title></title>
            <style>
                #map {
                    height: 100%;
                }

                html, body {
                    height: 100%;
                    margin: 0;
                    padding: 0;
                }
            </style>
        </head>
        <body>
            <div id=""map""></div>
            <script>
                let map;
                let start = false;
                let selectedPoints = [];
                let lines = [];
                let selectedLine = null;
                let hoverLine = null;
                let initial = true;
                let userMarker = null;
                // Initializes the Google Map 
                function initMap(lat, lng) {
                    map = new google.maps.Map(document.getElementById('map'), {
                        center: { lat: lat, lng: lng  },  // Specify the coordinates for the center of the map
                        zoom: 16// Specify the zoom level
                    });
                    // GPS marker for the user 
                    addUserMarker(lat, lng);
                }

                // GPS marker for the user
                function addUserMarker(lat, lng) {
                    if (userMarker) {
                        userMarker.setMap(null);
                    }

                    userMarker = new google.maps.Marker({
                        position: { lat: lat, lng: lng },
                        map: map,
                        title: 'Your Location',
                        icon: {
                            url: 'data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHdpZHRoPSIyNCIgaGVpZ2h0PSIyNCIgdmlld0JveD0iMCAwIDI0IDI0Ij48Y2lyY2xlIGN4PSIxMiIgY3k9IjEyIiByPSIxMiIgZmlsbD0iIzQyODVmNCIvPjwvc3ZnPg0K',
                            scaledSize: new google.maps.Size(20, 20) // Adjust the size as needed
                        }
                    });
                }

                function drawLine(latitude1, longitude1, latitude2, longitude2,color) {
                    // Draw a line on the map
                    let lineCoordinates = [
                        { lat: parseFloat(latitude1), lng: parseFloat(longitude1) },
                        { lat: parseFloat(latitude2), lng: parseFloat(longitude2) }
                    ];

                    let line = new google.maps.Polyline({
                        path: lineCoordinates,
                        geodesic: true,
                        strokeColor: color,
                        strokeOpacity: 1.0,
                        strokeWeight: 4
                    });

                    line.addListener('click', function() {
                         // If there is a previously selected line, reset its color.
                        if (selectedLine != null) {
                            selectedLine.setOptions({ strokeColor: ""#097969"" });
                        }
                        selectedLine = line; // Set the clicked line as the selected line
                        selectedLine.setOptions({ strokeColor: ""red"" });

                        let lineInfo = getLineInfo(line);
                        window.location.href = ""myapp://lineclicked?index="" + lines.indexOf(line) + ""&info="" + encodeURIComponent(lineInfo);
                    });

                    line.setMap(map);
                    
                    lines.push(line);
                }

                // Returns the path of the line as a string
                function getLineInfo(line) {
                    // Get the line's path
                    let path = line.getPath();
                
                    // Convert the path to a string representation
                    let pathStr = """";
                    for (let i = 0; i < path.length; i++) {
                        let point = path.getAt(i);
                        pathStr += point.lat() + "","" + point.lng() + "";"";
                    }
                
                    // Remove the trailing semicolon
                    pathStr = pathStr.slice(0, -1);
                
                    return pathStr;
                }
                // Deletes the selected line
                function deleteLine() {
                    if (selectedLine != null) {
                        selectedLine.setMap(null); // Removes the selected segment from the map
                        lines.splice(lines.indexOf(selectedLine), 1); // Removes the selected segment from the array
                        selectedLine = null; // Reset selectedLine
                    }
                }

                // Returns the list of drawn lines as a JSON string
                function getLines(){
                      let result = [];
                      for (let i = 0; i < lines.length; i++) {
                          let lineData = [];
                          let path = lines[i].getPath();
                          for (let j = 0; j < path.length; j++) {
                              let point = path.getAt(j);
                              lineData.push({ Lat: point.lat(), Lng: point.lng() });
                          }
                          result.push({ Index: i+1, Points: lineData });
                      }
                      return JSON.stringify(result);
                }

                let circle;

                function drawCircle(lat, lng, radius) {
                    // Remove the existing circle if it exists
                    if (circle) {
                        circle.setMap(null);
                    }

                    // Create a new circle
                    circle = new google.maps.Circle({
                        map: map,
                        radius: radius * 1000, // Radius in meters
                        center: { lat: lat, lng: lng },
                        fillColor: '#AA0000',
                        fillOpacity: 0.35,
                        strokeColor: '#AA0000',
                        strokeOpacity: 0.8,
                        strokeWeight: 2
                    });
                }

                // Call this function to initialize the map with a circle
                function initMapWithCircle(lat, lng) {
                     initMap(lat, lng); // Initialize the map
                     drawCircle(lat, lng, 0.2); // Draw a 200m radius circle
                }
         
            </script>
            <script src=""https://maps.googleapis.com/maps/api/js?key=AIzaSyCMPKV70vmSd-153eJsECz6gJD0AipZD-M&callback=initMap"" async defer></script>
        </body>
        </html>"
            };
            Source = htmlSource;
            Navigating += GMapMobile_Navigating;
            Loaded += GMapMobile_Loaded;
            Reload();
        }

        private async void GMapMobile_Navigating(object? sender, WebNavigatingEventArgs e)
        {
            if (e.Url.StartsWith("myapp://lineclicked"))
            {
                e.Cancel = true; // Cancel the navigation

                var info = e.Url.Split('=')[2]; // Extract the line index from the URL
                info = System.Net.WebUtility.UrlDecode(info);

                HandleLineClicked(info); // Call your C# function to handle the line click
            }
        }

        private static void LinesPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is not GMapMobile view)
            {
                return;
            }
            ObservableCollection<MapLine> lines = (ObservableCollection<MapLine>)newValue;
            lines.CollectionChanged += Lines_CollectionChanged;

            foreach (MapLine line in lines.Where(l => l.Points.Count > 1))
            {
                string jsCommand = $"drawLine({line.Points[0].Lat}, {line.Points[0].Lng}, {line.Points[1].Lat}, {line.Points[1].Lng},\"{line.Color}\");";
                view.EvaluateJavaScriptAsync(jsCommand);
            }
        }

        private static async void Lines_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                if (currentInstance.SelectedLine != null)
                {
                    //Remove the line from map
                    await currentInstance.EvaluateJavaScriptAsync("deleteLine()");
                    currentInstance.SelectedLine = null;
                }
            }
            else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                if (e.NewItems != null)
                {
                    foreach (MapLine mapLine in e.NewItems)
                    {
                        //Add the line to map
                        string jsCommand = $"drawLine({mapLine.Points[0].Lat}, {mapLine.Points[0].Lng}, {mapLine.Points[1].Lat}, {mapLine.Points[1].Lng},\"{mapLine.Color}\");";
                        await currentInstance.EvaluateJavaScriptAsync(jsCommand);
                    }
                }
            }
        }

        private async void GMapMobile_Loaded(object? sender, EventArgs e)
        {
            currentInstance = (GMapMobile)sender;
            var location = await Geolocation.GetLocationAsync();
            if (location != null)
            {   
                //For test only!!!!
                string jsCommand = $"initMapWithCircle({location.Latitude}, {location.Longitude});";
                await currentInstance.EvaluateJavaScriptAsync(jsCommand);
                LoadedEvent?.Invoke(sender, e);
            }
        }

        private async void HandleLineClicked(string info)
        {
            // Split the info string into individual points using ';' as the delimiter
            var points = info.Split(';');
            List<MapPoint> mapPoints = new List<MapPoint>();

            foreach (var point in points)
            {
                // Split the point into latitude and longitude
                var latLng = point.Split(',');

                // Create a new mapPoint object
                MapPoint mp = new MapPoint
                {
                    Lat = latLng[0],
                    Lng = latLng[1]
                };

                // Add the mapPoint object to the list
                mapPoints.Add(mp);
            }

            // Create a new Line object to represent the selected line
            MapLine line_temp = new MapLine(mapPoints);

            // Evaluate the JavaScript function "getLines()" to get the JSON string of all lines from the WebView
            var result = await this.EvaluateJavaScriptAsync("getLines()");
            if (result != null)
            {
                result = result.Replace("\\\"", "\"");
                // Deserialize the JSON string into a list of Line objects
                List<MapLine> lines = JsonConvert.DeserializeObject<List<MapLine>>(result);

                // Find the line that matches the selected line based on the points
                SelectedLine = lines.FirstOrDefault(line => line.Equals(line_temp));

                // Notify the view model about the line click
                var viewModel = BindingContext as UserMapViewModel;
                if (viewModel != null && SelectedLine != null)
                {
                    await viewModel.OnLineClicked(SelectedLine);
                }
            }
        }

    }
}

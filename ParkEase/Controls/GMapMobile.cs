using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Newtonsoft.Json;
using ParkEase.Contracts.Services;
using ParkEase.Core.Data;
using ParkEase.Messages;
using ParkEase.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
        private static Location location;
        private bool isLoaded = false;
        private List<string> markerIds = new List<string>(); // Track all marker IDs

        public ObservableCollection<MapLine> Lines
        {
            get => (ObservableCollection<MapLine>)GetValue(LinesProperty); set { SetValue(LinesProperty, value); }
        }

        public double Radius
        {
            get => (double)GetValue(RadiusProperty); set { SetValue(RadiusProperty, value); }
        }

        public MapLine SelectedLine
        {
            get => (MapLine)GetValue(SelectedLineProperty); set { SetValue(SelectedLineProperty, value); }
        }

        public double MarkerLatitude
        {
            get => (double)GetValue(MarkerLatitudeProperty); set => SetValue(MarkerLatitudeProperty, value);
        }

        public double MarkerLongitude
        {
            get => (double)GetValue(MarkerLongitudeProperty); set => SetValue(MarkerLongitudeProperty, value);
        }

        public static readonly BindableProperty LinesProperty = BindableProperty.Create(nameof(Lines), typeof(ObservableCollection<MapLine>), typeof(GMapMobile), propertyChanged: LinesPropertyChanged, defaultBindingMode: BindingMode.TwoWay);

        public static readonly BindableProperty SelectedLineProperty = BindableProperty.Create(nameof(SelectedLine), typeof(MapLine), typeof(GMapMobile), defaultBindingMode: BindingMode.TwoWay);

        public static readonly BindableProperty RadiusProperty = BindableProperty.Create(nameof(Radius), typeof(double), typeof(GMapMobile), propertyChanged: RadiusPropertyChanged, defaultBindingMode: BindingMode.TwoWay);

        public static readonly BindableProperty MarkerLatitudeProperty = BindableProperty.Create(nameof(MarkerLatitude), typeof(double), typeof(GMapMobile), propertyChanged: OnMarkerLatitudeChanged);

        public static readonly BindableProperty MarkerLongitudeProperty = BindableProperty.Create(nameof(MarkerLongitude), typeof(double), typeof(GMapMobile), propertyChanged: OnMarkerLongitudeChanged);
        public GMapMobile()
        {
            currentInstance = this;
            var apiKey = Environment.GetEnvironmentVariable("GoogleAKYKey");
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
                    let directionsService;
                    let directionsRenderer;
                    let start = false;
                    let selectedPoints = [];
                    let lines = [];
                    let selectedLine = null;
                    let hoverLine = null;
                    let initial = true;
                    let userMarker = null;
                    let circle;
                    let currentLat;
                    let currentLng;
                    let markers = []; // Track all markers

                // Initializes the Google Map 
                function initMap(lat, lng) {

                    currentLat = lat;
                    currentLng = lng;
                    map = new google.maps.Map(document.getElementById('map'), {
                        center: { lat: lat, lng: lng  },  // Specify the coordinates for the center of the map
                        zoom: 16// Specify the zoom level
                    });
                    // GPS marker for the user 
                    addUserMarker(lat, lng);
                    drawCircle(lat, lng, 0.2);

                    //https://developers.google.com/maps/documentation/javascript/reference/directions
                    directionsService = new google.maps.DirectionsService(); // communicate with the Google Maps Directions API
                    directionsRenderer = new google.maps.DirectionsRenderer({ suppressMarkers: true }); // taking the directions computed by DirectionsService and displaying them on the map
                    directionsRenderer.setMap(map);  // render the computed directions on the specified map                  
                }

                // Add a marker to the map  
                function addMarker(lat, lng, title, icon) {
                    const marker = new google.maps.Marker({
                        position: { lat: lat, lng: lng },
                        map: map,
                        title: title,
                        icon: {
                            url: icon,
                            scaledSize: new google.maps.Size(24, 24)
                        }
                    });
                    markers.push(marker); // Add marker to the list
                }  

                // Clear all markers
                function clearMarkers() {{
                    for (let i = 0; i < markers.length; i++) {{
                        markers[i].setMap(null);
                    }}
                    markers = [];
                }}

                // Clear all lines from the map
                function clearLines() {
                    lines.forEach(line => line.setMap(null));
                    lines = [];
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

                function drawLine(latitude1, longitude1, latitude2, longitude2, color) {
                
                    // Check if both points of the line are within the circle
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
                            strokeWeight: 10
                        });

                        line.originalColor = color; // Store the original color

                        line.addListener('click', function() {
                             // If there is a previously selected line, reset its color.
                            if (selectedLine != null) {
                                selectedLine.setOptions({ strokeColor: selectedLine.originalColor });
                            }
                            selectedLine = line; // Set the clicked line as the selected line
                            selectedLine.setOptions({ strokeColor: ""yellow"" });

                            let lineInfo = getLineInfo(line);
                            window.location.href = ""myapp://lineclicked?index="" + lines.indexOf(line) + ""&info="" + encodeURIComponent(lineInfo);
                            setSelectedLine(lineCoordinates);
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

                     // Calculate the circle's bounds
                        var bounds = new google.maps.LatLngBounds();
                        bounds.extend(new google.maps.LatLng(lat + (radius / 111), lng + (radius / 111))); // Top right
                        bounds.extend(new google.maps.LatLng(lat - (radius / 111), lng - (radius / 111))); // Bottom left

                        // Fit the map to the circle's bounds
                        map.fitBounds(bounds);
                }


                // Call this function to initialize the map with a circle
                function initMapWithCircle(lat, lng) {
                     initMap(lat, lng); // Initialize the map
                     updateRange(); // Draw the circle and lines within the range
                     
                }

                function updateRange() {
                    const rangeSelect = document.getElementById('rangeSelect');
                    const selectedRange = parseFloat(rangeSelect.value);
                    drawCircle(currentLat, currentLng, selectedRange);

                    // Clear existing lines
                    lines.forEach(line => line.setMap(null));
                    lines = [];

                    // Redraw lines within the new range
                    for (let line of allLines) { // allLines should be a separate array storing all original lines
                        const start = line.path[0];
                        const end = line.path[1];
                        drawLine(start.lat, start.lng, end.lat, end.lng, line.strokeColor, currentLat, currentLng, selectedRange);
                    }
                } 

                // Get user's current location
                function getUserLocation() {
                    if (navigator.geolocation) {
                        navigator.geolocation.getCurrentPosition((position) => {
                            currentLat = position.coords.latitude;
                            currentLng = position.coords.longitude;
                            initMapWithCircle(currentLat, currentLng);
                        }, (error) => {
                            console.error(""Error getting location: "", error);
                            // Handle error case, e.g., use a default location
                        });
                    } else {
                        console.error(""Geolocation is not supported by this browser."");
                        // Handle error case, e.g., use a default location
                    }
                }

                let selectedLineCoordinates = null;

                // Set the selected line coordinates when a line is clicked
                function setSelectedLine(lineCoordinates) {
                    selectedLineCoordinates = lineCoordinates;
                }

                // Display the route steps in the bottom sheet
                function navigateToLine() {
                    if (!selectedLineCoordinates) return;  // If no line is selected, it exits the function
                  
                    const endPoint = selectedLineCoordinates[selectedLineCoordinates.length - 1];

                    const request = {
                        origin: { lat: currentLat, lng: currentLng }, // Start point
                        destination: { lat: endPoint.lat, lng: endPoint.lng }, // End point
                        travelMode: google.maps.TravelMode.DRIVING
                    };
                    directionsService.route(request, function (result, status) {
                        if (status == 'OK') {
                            directionsRenderer.setDirections(result);
                            displayRouteSteps(result);
                        } else {
                            alert('Directions request failed: ' + status);
                        }
                    });
                }

               
                function receiveMessage(event) {
                    if (event.data === 'GetDirections') {
                        navigateToLine();
                    }
                }
                
                // listen for the message event
                window.addEventListener('message', receiveMessage, false);

                 // Initialize the map with the user's current location when the page loads
                 window.onload = function() {
                       getUserLocation();
                   };
         
            </script>" +
                    @$"<script src=""https://maps.googleapis.com/maps/api/js?key={apiKey}&callback=initMap"" async defer></script>" +
                @"</body>
                </html>"
            };
            Source = htmlSource;
            Navigating += GMapMobile_Navigating;
            Loaded += GMapMobile_Loaded;
            Reload();

            //https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/messagingcenter?view=net-maui-8.0
            // Listen for the GetDirections message from the BottomSheetViewModel
            MessagingCenter.Subscribe<MyBottomSheet> (this, "GetDirections", async (sender) =>
            {
                // Ensure the following code runs on the main thread - update the UI
                await Device.InvokeOnMainThreadAsync(async () =>
                {
                    // Evaluate the JavaScript function in the web view context
                    await EvaluateJavaScriptAsync("window.postMessage('GetDirections');"); // send a message to the JavaScript function
                });
            });

            // Subscribe to MessagingCenter messages
            MessagingCenter.Subscribe<UserMapViewModel, (double lat, double lng, string title)>(this, "AddMarker", async (sender, args) =>
            {
                await AddMarkerAsync(args.lat, args.lng, args.title);
            });

            // Subscribe to clear markers message
            MessagingCenter.Subscribe<UserMapViewModel>(this, "ClearMarkers", async (sender) =>
            {
                await ClearMarkersAsync();
            });
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
        private static void RadiusPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is not GMapMobile view)
            {
                return;
            }
            double newRadius = (double)newValue;
            string jsCommand = $"drawCircle({location.Latitude},{location.Longitude},{newRadius});";
            view.EvaluateJavaScriptAsync(jsCommand);
        }
        private static void LinesPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is not GMapMobile view)
            {
                return;
            }

            string jsClearCommand = $"clearLines();";
            view.EvaluateJavaScriptAsync(jsClearCommand);

            ObservableCollection<MapLine> lines = (ObservableCollection<MapLine>)newValue;
            lines.CollectionChanged += Lines_CollectionChanged;

            foreach (MapLine line in lines.Where(l => l.Points.Count > 1))
            {
                string jsCommand = $"drawLine({line.Points[0].Lat}, {line.Points[0].Lng}, {line.Points[1].Lat}, {line.Points[1].Lng},\"{line.Color}\");";
                view.EvaluateJavaScriptAsync(jsCommand);
            }
        }

        private static void OnMarkerLatitudeChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is GMapMobile view)
            {
                view.UpdateMarker();
            }
        }

        private static void OnMarkerLongitudeChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is GMapMobile view)
            {
                view.UpdateMarker();
            }
        }

        private async void UpdateMarker()
        {
            System.Diagnostics.Debug.WriteLine($"Updating marker: {MarkerLatitude}, {MarkerLongitude}");
            if (MarkerLatitude != 0 && MarkerLongitude != 0)
            {
                await AddMarkerAsync(MarkerLatitude, MarkerLongitude, "Private Parking");
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

        // async indicates that the method contains asynchronous operations.
        private async void GMapMobile_Loaded(object? sender, EventArgs e)
        {
            if(!isLoaded)
            {
                // assigned to the static variable
                currentInstance = (GMapMobile)sender; //sender the object that raised the event
                location = await Geolocation.GetLocationAsync(); // await is waiting for the operations completed.
                if (location != null)
                {
                    DataService.SetLocation(location);
                    // how to emulate GPS location in the Android emulator: https://stackoverflow.com/questions/2279647/how-to-emulate-gps-location-in-the-android-emulator
                    string jsCommand = $"initMapWithCircle({location.Latitude}, {location.Longitude});";
                    await currentInstance.EvaluateJavaScriptAsync(jsCommand);
                    LoadedEvent?.Invoke(sender, e); //The null-conditional operator ?. ensures that the event is only invoked if it is not null.

                    // Add marker after the map is initialized
                    await currentInstance.AddMarkerAsync(MarkerLatitude, MarkerLongitude, "Private Parking");

                }
                isLoaded = true;
            }
           
        }

        public async Task AddMarkerAsync(double lat, double lng, string title)
        {
            // SVG data URL for the circle "P" logo
            var markerIconPath = "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(@"
            <svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24'>
              <circle cx='12' cy='12' r='10' fill='#512BD4'/>
              <text x='12' y='16' font-size='12' font-family='Arial' font-weight='bold' text-anchor='middle' fill='white'>P</text>
             </svg>
        "));

            // JavaScript command to add the marker with the specified icon
            string jsCommand = $@"
            addMarker({lat}, {lng}, '{title}', '{markerIconPath}');
        ";
            System.Diagnostics.Debug.WriteLine($"Adding marker: {lat}, {lng}, {title}");

            await EvaluateJavaScriptAsync(jsCommand);
        }

        public async Task ClearMarkersAsync()
        {
            // JavaScript command to clear all markers
            string jsCommand = "clearMarkers();";
            System.Diagnostics.Debug.WriteLine("Clearing all markers");

            await EvaluateJavaScriptAsync(jsCommand);
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
                    await viewModel.OnLineClickedAsync(SelectedLine);
                }
            }
        }
    }
}

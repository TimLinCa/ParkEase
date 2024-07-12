﻿using Newtonsoft.Json;
using ParkEase.Core.Contracts.Services;
using ParkEase.Core.Data;
using ParkEase.Messages;
using ParkEase.Services;
using ParkEase.ViewModel;
using Syncfusion.Maui.Core.Carousel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection.PortableExecutable;
using System.Text;

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
        private IGeolocatorService geolocation;
        private CancellationTokenSource cancellationTokenSource;
        private List<string> markerIds = new List<string>(); // Track all marker IDs
        private (string Lat, string Lng)? savedMarker = null; // Track the currently saved marker
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

        public double LocationLat
        {
            get => (double)GetValue(LocationLatProperty); set => SetValue(LocationLatProperty, value);
        }

        public double LocationLng
        {
            get => (double)GetValue(LocationLngProperty); set => SetValue(LocationLngProperty, value);
        }

        public bool IsSearchInProgress
        {
            get => (bool)GetValue(IsSearchInProgressProperty); set => SetValue(IsSearchInProgressProperty, value);
        }

        public Location CenterLocation
        {
            get => (Location)GetValue(CenterLocationProperty); set => SetValue(CenterLocationProperty, value);
        }

        public static readonly BindableProperty LinesProperty = BindableProperty.Create(nameof(Lines), typeof(ObservableCollection<MapLine>), typeof(GMapMobile), propertyChanged: LinesPropertyChanged, defaultBindingMode: BindingMode.TwoWay);

        public static readonly BindableProperty SelectedLineProperty = BindableProperty.Create(nameof(SelectedLine), typeof(MapLine), typeof(GMapMobile), defaultBindingMode: BindingMode.TwoWay);

        public static readonly BindableProperty RadiusProperty = BindableProperty.Create(nameof(Radius), typeof(double), typeof(GMapMobile), propertyChanged: RadiusPropertyChanged, defaultBindingMode: BindingMode.TwoWay);

        public static readonly BindableProperty MarkerLatitudeProperty = BindableProperty.Create(nameof(MarkerLatitude), typeof(double), typeof(GMapMobile), propertyChanged: OnMarkerLatitudeChanged);

        public static readonly BindableProperty MarkerLongitudeProperty = BindableProperty.Create(nameof(MarkerLongitude), typeof(double), typeof(GMapMobile), propertyChanged: OnMarkerLongitudeChanged);

        public static readonly BindableProperty LocationLatProperty = BindableProperty.Create(nameof(LocationLat), typeof(double), typeof(GMapMobile), defaultBindingMode: BindingMode.TwoWay);

        public static readonly BindableProperty LocationLngProperty = BindableProperty.Create(nameof(LocationLng), typeof(double), typeof(GMapMobile), defaultBindingMode: BindingMode.TwoWay);

        public static readonly BindableProperty IsSearchInProgressProperty = BindableProperty.Create(nameof(IsSearchInProgress), typeof(bool), typeof(GMapMobile), defaultBindingMode: BindingMode.TwoWay);

        public static readonly BindableProperty CenterLocationProperty = BindableProperty.Create(nameof(CenterLocation), typeof(Location), typeof(GMapMobile), defaultBindingMode: BindingMode.TwoWay, propertyChanged: OnCenterLocationChanged);

        public GMapMobile()
        {
            this.geolocation = AppServiceProvider.GetService<IGeolocatorService>();
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
                    let previousSelectedMarker = null;
                    let savedLocationMarker = null; // Variable to keep track of the saved location marker

                // Initializes the Google Map 
                function initMap() {
                    map = new google.maps.Map(document.getElementById('map'), {
                        zoom: 16// Specify the zoom level
                    });

                    //https://developers.google.com/maps/documentation/javascript/reference/directions
                    directionsService = new google.maps.DirectionsService(); // communicate with the Google Maps Directions API
                    directionsRenderer = new google.maps.DirectionsRenderer({ suppressMarkers: true }); // taking the directions computed by DirectionsService and displaying them on the map
                    directionsRenderer.setMap(map);  // render the computed directions on the specified map                  
                
                    // Set up click event listeners for existing lines
                    lines.forEach(line => {
                        addLineClickListener(line);
                    });
                }
               
                // Add a marker to the map  
                function addMarker(lat, lng, title, icon, isSavedLocation = false) {

                    // Check if it's a saved location marker and update it separately
                    if (isSavedLocation) {

                        // Remove the existing saved location marker if it exists
                        if (savedLocationMarker) {
                            savedLocationMarker.setMap(null);
                        }

                        // Create a new marker for the saved location   
                        savedLocationMarker = new google.maps.Marker({
                            position: { lat: lat, lng: lng },
                            map: map,
                            title: title,
                            icon: {
                                url: icon,
                                scaledSize: new google.maps.Size(24, 24)
                            }
                        });

                        // Store the original icon of the saved location marker
                        savedLocationMarker.originalIcon = icon; 

                        // Exit the function after handling the saved location marker
                        return; 
                    }

                    // Create a new marker for private markers
                    const marker = new google.maps.Marker({
                        position: { lat: lat, lng: lng },
                        map: map,
                        title: title,
                        icon: {
                            url: icon,
                            scaledSize: new google.maps.Size(24, 24)
                        }
                    });

                    // Store the original color in the private marker object
                    marker.originalIcon = icon;  

                    // Add private marker to the list
                    markers.push(marker); 

                    // Add a click event listener to the private marker 
                    marker.addListener('click', function() {

                        // Reset the color of the previously selected marker, if any
                        if (previousSelectedMarker) {
                            previousSelectedMarker.setIcon({
                                url: previousSelectedMarker.originalIcon,
                                scaledSize: new google.maps.Size(24, 24)
                            });
                        }

                        // Reset the color of the previously selected line, if any
                            if (selectedLine) {
                                selectedLine.setOptions({ strokeColor: selectedLine.originalColor });
                                selectedLine = null;
                            }

                        // Change the marker color to yellow with black fill
                            marker.setIcon({
                                url: 'data:image/svg+xml;base64,' + btoa(`
                                    <svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24'>
                                        <circle cx='12' cy='12' r='12' fill='yellow'/>
                                        <text x='12' y='16' font-size='12' font-family='Arial' font-weight='bold' text-anchor='middle' fill='black'>P</text>
                                    </svg>
                                `),
                                scaledSize: new google.maps.Size(24, 24)
                            });

                        // Update the previous selected marker
                        previousSelectedMarker = marker;

                         // Store the selected marker's coordinates
                        selectedMarkerCoordinates = { lat: lat, lng: lng };

                        // Clear line coordinates 
                        selectedLineCoordinates = null;  

                        // Redirect to a custom URL scheme with the marker's information
                        //https://www.w3schools.com/js/js_window_location.asp
                        window.location.href = ""myapp://privateparkingclicked?lat="" + lat + ""&lng="" + lng + ""&title="" + title;
                    });

                }  

                // Clear all markers
                function clearMarkers() {

                    // Loop through all markers and remove them from the map
                    for (let i = 0; i < markers.length; i++) {
                        markers[i].setMap(null);
                    }

                    // Clear the markers array
                    markers = [];

                    // Remove the saved location marker if it exists
                    if (savedLocationMarker) {
                            savedLocationMarker.setMap(null);
                            savedLocationMarker = null;
                        }
                }


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
                    // Center the map on the new marker position
                    if(initial){
                        map.setCenter({ lat: lat, lng: lng });
                        initial = false;
                    }
                    
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
                        line.setMap(map);
                        lines.push(line);

                        // Add click event listener for the new line
                        line.addListener('click', function() {
                            if (selectedLine === line) {
                                clearSavedSpot();
                                selectedLine = null;
                                window.location.href = ""myapp://clearspot"";
                            } else {
                                if (selectedLine != null) {
                                    selectedLine.setOptions({ strokeColor: selectedLine.originalColor });
                                }

                                if (previousSelectedMarker) {
                                    previousSelectedMarker.setIcon({
                                        url: previousSelectedMarker.originalIcon,
                                        scaledSize: new google.maps.Size(24, 24)
                                    });
                                    previousSelectedMarker = null;
                                }

                                selectedLine = line;
                                selectedLine.setOptions({ strokeColor: ""yellow"" });

                                let lineInfo = getLineInfo(line);
                                window.location.href = ""myapp://lineclicked?index="" + lines.indexOf(line) + ""&info="" + encodeURIComponent(lineInfo);
                                setSelectedLine(line.getPath().getArray());
                                selectedMarkerCoordinates = null;
                            }
                        });
                    }
    
                // Clear the saved spot
                function clearSavedSpot() {
                    if (savedLocationMarker) {
                        savedLocationMarker.setMap(null);
                        savedLocationMarker = null;
                    }
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

                function SetMapCenter(lat,lng) {
                    map.setCenter({ lat: lat, lng: lng });
                }   

                // Deletes the selected line

                function getLineByCoordinates(lat1, lng1, lat2, lng2) {
                    for (let i = 0; i < lines.length; i++) {
                        let path = lines[i].getPath();
                        if (path.getLength() >= 2) {
                            let startPoint = path.getAt(0);
                            let endPoint = path.getAt(1);
                            
                            // Check if the line matches the given coordinates
                            if ((isCloseEnough(startPoint.lat(), lat1) && isCloseEnough(startPoint.lng(), lng1) &&
                                 isCloseEnough(endPoint.lat(), lat2) && isCloseEnough(endPoint.lng(), lng2)) ||
                                (isCloseEnough(startPoint.lat(), lat2) && isCloseEnough(startPoint.lng(), lng2) &&
                                 isCloseEnough(endPoint.lat(), lat1) && isCloseEnough(endPoint.lng(), lng1))) {
                                return lines[i];
                            }
                        }
                    }
                    return null; // Return null if no line is found
                }
                
                // Helper function to compare floating point numbers
                function isCloseEnough(a, b, epsilon = 0.000001) {
                    return Math.abs(a - b) < epsilon;
                }

                 // Deletes the selected line
                function deleteLine(latitude1, longitude1, latitude2, longitude2) {
                    let line = getLineByCoordinates(latitude1, longitude1, latitude2, longitude2);
                    if (line != null) {
                        line.setMap(null); // Removes the selected segment from the map
                        lines.splice(lines.indexOf(line), 1); // Removes the selected segment from the array
                        line = null;
                    }
                }

                // Helper function to compare floating point numbers
                function isCloseEnough(a, b, epsilon = 0.000001) {
                    return Math.abs(a - b) < epsilon;
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
                          result.push({ Index: i, Points: lineData });
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



                function updateLocation(lat,lng) {
                    currentLat = lat;
                    currentLng = lng;
                    // GPS marker for the user 
                    addUserMarker(lat, lng);
                };


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

                let selectedMarkerCoordinates = null;
                
                function navigateToMarker(lat, lng) {
                    const request = {
                        origin: { lat: currentLat, lng: currentLng },
                        destination: { lat: lat, lng: lng },
                        travelMode: google.maps.TravelMode.DRIVING
                    };
                    directionsService.route(request, function(result, status) {
                        if (status == 'OK') {
                            directionsRenderer.setDirections(result);
                            displayRouteSteps(result);
                        } else {
                            alert('Directions request failed: ' + status);
                        }
                    });
                }


                function receiveMessage(event) {
                    switch (event.data) {
                        case 'GetDirections':
                            if (selectedMarkerCoordinates) {
                                navigateToMarker(selectedMarkerCoordinates.lat, selectedMarkerCoordinates.lng);
                            } else if (selectedLineCoordinates) {
                                navigateToLine();
                            } 
                            break;
                        case event.data.startsWith('SaveParkingLocation') && event.data:
                            var latLng = event.data.split(',').slice(1);
                            addMarker(parseFloat(latLng[0]), parseFloat(latLng[1]), 'Saved Parking Location', 'https://maps.google.com/mapfiles/ms/icons/blue-dot.png', true);
                            break;
                        case event.data.startsWith('RemoveParkingLocation') && event.data:
                            var latLng = event.data.split(',').slice(1);
                            removeMarker(parseFloat(latLng[0]), parseFloat(latLng[1]), true);
                            break;
                        default:
                            console.warn('Unknown event data:', event.data);
                            break;
                    }
                }

                function removeMarker(lat, lng, isSavedLocation = false) {
                    if (isSavedLocation && savedLocationMarker) {
                        savedLocationMarker.setMap(null);
                        savedLocationMarker = null;
                        return;
                    }

                    for (let i = 0; i < markers.length; i++) {
                        if (markers[i].getPosition().lat() === lat && markers[i].getPosition().lng() === lng) {
                            markers[i].setMap(null);
                            markers.splice(i, 1);
                            break;
                        }
                    }
                }
                
                //https://pineco.de/cross-origin-communication-postmessage/
                // listen for the message event
                window.addEventListener('message', receiveMessage, false);
         
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
            MessagingCenter.Subscribe<MyBottomSheet>(this, "GetDirections", async (sender) =>
            {
                // Ensure the following code runs on the main thread - update the UI
                await Device.InvokeOnMainThreadAsync(async () =>
                {
                    // Evaluate the JavaScript function in the web view context
                    await EvaluateJavaScriptAsync("window.postMessage('GetDirections');"); // send a message to the JavaScript function
                });
            });

            // Subscribe to MessagingCenter messages(main thread)
            MessagingCenter.Subscribe<UserMapViewModel, (double lat, double lng, string title, string color)>(this, "AddMarker", async (sender, args) =>
            {
                await AddMarkerAsync(args.lat, args.lng, args.title, args.color);
            });

            // Subscribe to clear markers message
            MessagingCenter.Subscribe<UserMapViewModel>(this, "ClearMarkers", async (sender) =>
            {
                await ClearMarkersAsync();
            });

            // Subscribe to "SaveParkingLocation" message
            MessagingCenter.Subscribe<MyBottomSheet, (string lat, string lng)>(this, "SaveParkingLocation", (sender, args) =>
            {
                SaveParkingLocation(args.lat, args.lng);
            });

            // Subscribe to "RemoveParkingLocation" message
            MessagingCenter.Subscribe<MyBottomSheet, (string lat, string lng)>(this, "RemoveParkingLocation", (sender, args) =>
            {
                RemoveParkingLocation(args.lat, args.lng);
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
            else if (e.Url.StartsWith("myapp://privateparkingclicked"))
            {
                e.Cancel = true; // Cancel the navigation

                var query = new Uri(e.Url).Query;
                var queryParameters = System.Web.HttpUtility.ParseQueryString(query);

                var lat = double.Parse(queryParameters["lat"]);
                var lng = double.Parse(queryParameters["lng"]);
                var title = queryParameters["title"];

                HandlePrivateParkingClicked(lat, lng, title);
            }
            else if (e.Url.StartsWith("myapp://clearspot"))
            {
                e.Cancel = true; // Cancel the navigation
                await ClearSavedSpotOnMap(); // Call the ClearSavedSpotOnMap method
            }
        }

        private async Task ClearSavedSpotOnMap()
        {
            string jsCommand = "clearSavedSpot();";
            await EvaluateJavaScriptAsync(jsCommand);
        }

        private async void HandlePrivateParkingClicked(double lat, double lng, string title)
        {
            var viewModel = BindingContext as UserMapViewModel;
            if (viewModel != null)
            {
                var privateParking = await viewModel.GetPrivateParkingAsync(lat, lng);
                if (privateParking != null)
                {
                    await viewModel.ShowPrivateParkingBottomSheet(privateParking);
                }
                else
                {
                    await viewModel.ShowPrivateParkingBottomSheet(null); // Show default message or handle the error
                }
            }
        }

        private static void RadiusPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is not GMapMobile view)
            {
                return;
            }
            double newRadius = (double)newValue;

            double lat = view.IsSearchInProgress ? view.CenterLocation.Latitude : view.LocationLat;
            double lng = view.IsSearchInProgress ? view.CenterLocation.Longitude : view.LocationLng;

            string jsCommand = $"drawCircle({lat},{lng},{newRadius});";
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

            view.SelectedLine = null;
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

        private static void OnCenterLocationChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is GMapMobile view)
            {
                string jscommand = "SetMapCenter(" + view.CenterLocation.Latitude + "," + view.CenterLocation.Longitude + ")";
                view.EvaluateJavaScriptAsync(jscommand);
            }
        }


        private async void UpdateMarker()
        {
            Debug.WriteLine($"Updating marker: {MarkerLatitude}, {MarkerLongitude}");
            if (MarkerLatitude != 0 && MarkerLongitude != 0)
            {
                await AddMarkerAsync(MarkerLatitude, MarkerLongitude, "Private Parking", "");
            }
        }


        private static async void Lines_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                if (e.OldItems != null)
                {
                    foreach (MapLine mapLine in e.OldItems)
                    {
                        //string jsCommand = $"deleteLine({mapLine.Index})";
                        string jsCommand = $"deleteLine({mapLine.Points[0].Lat}, {mapLine.Points[0].Lng}, {mapLine.Points[1].Lat}, {mapLine.Points[1].Lng})";
                        await currentInstance.EvaluateJavaScriptAsync(jsCommand);

                        if (currentInstance.SelectedLine != null && currentInstance.SelectedLine.Equals(mapLine))
                        {
                            currentInstance.SelectedLine = null;
                        }
                    }
                    //Remove the line from map

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
            if (!isLoaded)
            {
                // assigned to the static variable
                currentInstance = (GMapMobile)sender; //sender the object that raised the event

                // how to emulate GPS location in the Android emulator: https://stackoverflow.com/questions/2279647/how-to-emulate-gps-location-in-the-android-emulator
                string jsCommand = $"initMap();";
                await currentInstance.EvaluateJavaScriptAsync(jsCommand);
                LoadedEvent?.Invoke(sender, e); //The null-conditional operator ?. ensures that the event is only invoked if it is not null.

                // Retrieve the saved location from SecureStorage
                string savedLat = await SecureStorage.Default.GetAsync("SavedParkingLat");
                string savedLng = await SecureStorage.Default.GetAsync("SavedParkingLng");

                if (!string.IsNullOrEmpty(savedLat) && !string.IsNullOrEmpty(savedLng))
                {
                    // Add the saved marker to the map
                    string saveJsCommand = $"window.postMessage('SaveParkingLocation,{savedLat},{savedLng}');";
                    await EvaluateJavaScriptAsync(saveJsCommand);

                    // Update the saved marker location
                    savedMarker = (savedLat, savedLng);

                    // Set the WalkNavigation button visibility
                    var viewModel = BindingContext as UserMapViewModel;
                    if (viewModel != null)
                    {
                        viewModel.IsWalkNavigationVisible = true;
                    }
                }

                // Add marker after the map is initialized
                if (cancellationTokenSource != null)
                {
                    cancellationTokenSource.Cancel();
                }
                cancellationTokenSource = new CancellationTokenSource();
                CancellationToken token = cancellationTokenSource.Token;
                var progress = new Progress<Location>(location =>
                {
                    LocationLat = location.Latitude;
                    LocationLng = location.Longitude;
                    DataService.SetLocation(location);
                    string jsCommand = $"updateLocation({location.Latitude},{location.Longitude});";
                    currentInstance.EvaluateJavaScriptAsync(jsCommand);
                });
                geolocation.StartListening(progress, token);
                isLoaded = true;
            }

        }

        public async Task AddMarkerAsync(double lat, double lng, string title, string color)
        {
            // SVG data URL for the circle "P" logo with color
            var markerIconPath = $"data:image/svg+xml;base64,{Convert.ToBase64String(Encoding.UTF8.GetBytes($@"
                <svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24'>
                  <circle cx='12' cy='12' r='10' fill='{color}'/>
                  <text x='12' y='16' font-size='12' font-family='Arial' font-weight='bold' text-anchor='middle' fill='white'>P</text>
                 </svg>
            "))}";

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
            }

            // Ask the user if they want to clear the saved spot if the line has a saved spot
            var viewModel = BindingContext as UserMapViewModel;
            if (viewModel != null && await viewModel.HasSavedSpotAsync(SelectedLine))
            {
                viewModel.ClearSavedSpotCommand.Execute(null);
            }
        }

        private async void SaveParkingLocation(string lat, string lng)
        {
            // Remove the existing saved marker if it exists
            if (savedMarker.HasValue)
            {
                //https://developer.mozilla.org/en-US/docs/Web/API/Window/postMessage
                string removeJsCommand = $"window.postMessage('RemoveParkingLocation,{savedMarker.Value.Lat},{savedMarker.Value.Lng}');";
                await EvaluateJavaScriptAsync(removeJsCommand);
            }

            // Add the new marker
            string saveJsCommand = $"window.postMessage('SaveParkingLocation,{lat},{lng}');";
            await EvaluateJavaScriptAsync(saveJsCommand);

            // Update the saved marker location
            savedMarker = (lat, lng);

            //https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/storage/secure-storage?view=net-maui-8.0&tabs=android
            // Save the location to SecureStorage
            await SecureStorage.Default.SetAsync("SavedParkingLat", lat);
            await SecureStorage.Default.SetAsync("SavedParkingLng", lng);

        }

        private async void RemoveParkingLocation(string lat, string lng)
        {
            string jsCommand = $"window.postMessage('RemoveParkingLocation,{lat},{lng}');";
            await EvaluateJavaScriptAsync(jsCommand);

            // Clear the saved marker location
            savedMarker = null;

            //https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/storage/secure-storage?view=net-maui-8.0&tabs=android
            // Remove the location from SecureStorage
            SecureStorage.Default.Remove("SavedParkingLat");
            SecureStorage.Default.Remove("SavedParkingLng");           
        }
    }
}

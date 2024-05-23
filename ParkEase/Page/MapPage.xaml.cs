
using Microsoft.Maui.Controls.Maps;
using MongoDB.Bson;
using Newtonsoft.Json;
using ParkEase.ViewModel;
using System;

namespace ParkEase.Page;

//// This class represents the MapPage, which contains the Google Map and buttons to interact with it.
public partial class MapPage : ContentPage
{
    // ViewModel for the MapPage to handle data binding.
    private MapViewModel _viewModel;

    public MapPage()
    {
        InitializeComponent();
        // Event handler for Draw button click.
        btn_Draw.Clicked += btn_Draw_Clicked;
        // Event handler for Clear button click.
        btn_Clear.Clicked += btn_Clear_Clicked;

        // HTML content to be loaded in the WebView for displaying Google Maps. https://www.google.com/search?sca_esv=00e485a4403845c8&sca_upv=1&rlz=1C1UEAD_enCA1040CA1040&sxsrf=ADLYWIIu_-3h0kGt3_IxavzDEMmyG-bAfg:1716486128385&q=HTML+content+to+be+loaded+in+the+WebView+for+displaying+Google+Maps.&tbm=vid&source=lnms&prmd=sivbnmtz&sa=X&ved=2ahUKEwi-zMaPqaSGAxWqJzQIHecgDQEQ0pQJegQIChAB&biw=1920&bih=911&dpr=1#fpstate=ive&vld=cid:7c1c270e,vid:s3g04pbAJBA,st:0
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

                        // Initializes the Google Map 
                        function initMap() {
                            map = new google.maps.Map(document.getElementById(""map""), {
                                center: { lat: 51.0499, lng: -114.0666 }, // Sets the initial center point of the map
                                zoom: 10, // Set the initial zoom level
                            });

                            // Add a click event listener to the map
                            map.addListener('click', function(event) {
                                if (start) {
                                    selectedPoints.push({ lat: event.latLng.lat(), lng: event.latLng.lng() });

                                    if (selectedPoints.length == 2) {
                                        drawLine(selectedPoints[0].lat, selectedPoints[0].lng, selectedPoints[1].lat, selectedPoints[1].lng);
                                        selectedPoints = [];
                                        start = false;
                                    }
                                } else {
                                    //findLine(event.latLng);
                                }
                            });
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

                        function drawLine(latitude1, longitude1, latitude2, longitude2) {
                            // Draw a line on the map
                            let lineCoordinates = [
                                { lat: parseFloat(latitude1), lng: parseFloat(longitude1) },
                                { lat: parseFloat(latitude2), lng: parseFloat(longitude2) }
                            ];

                            let line = new google.maps.Polyline({
                                path: lineCoordinates,
                                geodesic: true,
                                strokeColor: ""#097969"",
                                strokeOpacity: 1.0,
                                strokeWeight: 4
                            });

                            // Add mouseover and mouseout event listeners to the polyline
                            line.addListener('mouseover', function() {
                                line.setOptions({ strokeColor: ""yellow"" }); // Change color to red on mouseover
                            });

                            line.addListener('mouseout', function() {
                                if(line != selectedLine){
                                    line.setOptions({ strokeColor: ""#097969"" }); // Change color back to original on mouseout
                                }
                                if(selectedLine != null){
                                    selectedLine.setOptions({ strokeColor: ""red"" }); // Change color of selectedLine to red
                                }
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

                            console.log('draw');
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

                        // Finds the line that contains the given point
                        function findLine(latLng) {
                            for (let i = 0; i < lines.length; i++) {
                                let linePath = lines[i].getPath();
                                for (let j = 0; j < linePath.length - 1; j++) {
                                    let p1 = linePath.getAt(j);
                                    let p2 = linePath.getAt(j + 1);
                                    if (isPointOnLine(latLng, p1, p2)) {
                                        if (selectedLine != null) {
                                            selectedLine.setOptions({ strokeColor: ""#097969"" });
                                        }
                                        selectedLine = lines[i];
                                        selectedLine.setOptions({ strokeColor: ""red"" });
                                        console.log('find');
                                        break;
                                    }
                                }
                            }
                        }

                        // Checks if a point is on a line
                        function isPointOnLine(pt, p1, p2) {
                            let slope = (p2.lat() - p1.lat()) / (p2.lng() - p1.lng());
                            let yIntercept = p1.lat() - (slope * p1.lng());
                            let eps = 0.001;

                            let minY = Math.min(p1.lat(), p2.lat()) - eps;
                            let maxY = Math.max(p1.lat(), p2.lat()) + eps;

                            let minX = Math.min(p1.lng(), p2.lng()) - eps;
                            let maxX = Math.max(p1.lng(), p2.lng()) + eps;

                            if (pt.lat() < minY || pt.lat() > maxY || pt.lng() < minX || pt.lng() > maxX) {
                                return false;
                            }

                            let result = Math.abs(pt.lat() - (slope * pt.lng() + yIntercept)) < eps;
                            return result;
                        }

                        // Enables the point selection mode
                        function startSelectingPoints() {
                            start = true;
                        }

                        // Deletes the selected line
                        function deleteLine() {
                            if (selectedLine != null) {
                                selectedLine.setMap(null); // Removes the selected segment from the map
                                lines.splice(lines.indexOf(selectedLine), 1); // Removes the selected segment from the array
                                selectedLine = null; // Reset selectedLine
                            }
                        }
                    </script>
                    <script src=""https://maps.googleapis.com/maps/api/js?key=AIzaSyCMPKV70vmSd-153eJsECz6gJD0AipZD-M&callback=initMap"" async defer></script>
                </body>
                </html>"
        };

        
        mapWebView.Source = htmlSource; // Set the source of the WebView to the HTML content.
        mapWebView.Navigating += OnWebViewNavigating; // Event handler for WebView navigation.
    }

    private void OnWebViewNavigating(object sender, WebNavigatingEventArgs e)
    {
        if (e.Url.StartsWith("myapp://lineclicked"))
        {
            e.Cancel = true; // Cancel the navigation

            var info =  e.Url.Split('=')[2]; // Extract the line index from the URL
            info = System.Net.WebUtility.UrlDecode(info);

            HandleLineClicked(info); // Call your C# function to handle the line click
        }
    }

    // From ChatGPT: This method is called when the page appears on the screen
    protected override void OnAppearing()
    {
        // Call the base class's OnAppearing method to ensure any base class functionality is executed
        base.OnAppearing();
        // Set the _viewModel field to the current BindingContext of the page.
        _viewModel = (MapViewModel)BindingContext;
        // ensure that the ViewModel is available and up-to-date,keeping the UI and data in sync
    }

    // It handles the event by processing the line information and updating the UI accordingly
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
                Lat = double.Parse(latLng[0]),
                Lng = double.Parse(latLng[1])
            };

            // Add the mapPoint object to the list
            mapPoints.Add(mp);
        }

        // Create a new Line object to represent the selected line
        Line selectedLine = new Line();
        //Initialize the index to -1(indicating it is not yet matched)
        selectedLine.Index = -1;
        // Set the Points property to the list of MapPoints
        selectedLine.Points = mapPoints;

        // Evaluate the JavaScript function "getLines()" to get the JSON string of all lines from the WebView
        var result = await mapWebView.EvaluateJavaScriptAsync("getLines()");
        result = result.Replace("\\\"", "\"");
        // Deserialize the JSON string into a list of Line objects
        List<Line> lines = JsonConvert.DeserializeObject<List<Line>>(result);

        // Find the line that matches the selected line based on the points
        selectedLine = lines.FirstOrDefault(line => line.Equals(selectedLine));
    }

    // It logs the clicked location and updates the ViewModel with the location information
    private void OnMapClicked(object sender, MapClickedEventArgs e)
    {
        Console.WriteLine($"Map clicked at: {e.Location.Latitude}, {e.Location.Longitude}");// Debug to ensure the map is interactive
        _viewModel.LocationInfo = $"Clicked location: Latitude {e.Location.Latitude}, Longitude {e.Location.Longitude}";// Update the ViewModel with the clicked location information
    }

    // It sends a JavaScript command to the WebView to start selecting points for drawing a line on the map
    private void btn_Draw_Clicked(object sender, EventArgs e)
    {
        mapWebView.EvaluateJavaScriptAsync("startSelectingPoints()");// This function enables the mode where the user can click on the map to select points for drawing a line
    }

    private void btn_Clear_Clicked(object sender, EventArgs e)
    {
        mapWebView.EvaluateJavaScriptAsync("deleteLine()");
    }


    // It retrieves the lines drawn on the map from the WebView and processes the result
    private async void btn_Test_Test(object sender, EventArgs e)
    {
        var result = await mapWebView.EvaluateJavaScriptAsync("getLines()");
        result = result.Replace("\\\"", "\"");
        List<Line> lines = JsonConvert.DeserializeObject<List<Line>>(result);
    }

    // This class represents a point on the map with latitude and longitude
    private class MapPoint : IEquatable<MapPoint>
    {
        public double Lat { get; set; }
        public double Lng { get; set; }


        // Determines whether the specified MapPoint is equal to the current MapPoint
        public bool Equals(MapPoint? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if(other.Lat == this.Lat && other.Lng == this.Lng) return true;
            return false;
        }
    }

    // This class represents a line on the map consisting of multiple points
    private class Line : IEquatable<Line>
    {
        public int Index { get; set; }  // Index of the line, used for identification
        public List<MapPoint> Points { get; set; }

        // Determines whether the specified Line is equal to the current Line
        public bool Equals(Line? other)
        {
            if (ReferenceEquals(null, other)) return false;
            bool isEquals = true;
            for(var i = 0; i < Points.Count; i++)  // Compare each point in the line to check for equality
            {
                if (!this.Points[i].Equals(other.Points[i])) return false;
            }
            return isEquals;
        }
    }
}

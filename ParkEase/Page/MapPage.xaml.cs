using Microsoft.Maui.Controls.Maps;
using MongoDB.Bson;
using Newtonsoft.Json;
using ParkEase.ViewModel;

namespace ParkEase.Page;

public partial class MapPage : ContentPage
{
    private MapViewModel _viewModel;

    public MapPage()
    {
        InitializeComponent();
        btn_Draw.Clicked += btn_Draw_Clicked;
        btn_Clear.Clicked += btn_Clear_Clicked;

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

                        function startSelectingPoints() {
                            start = true;
                        }

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

        mapWebView.Source = htmlSource;
        mapWebView.Navigating += OnWebViewNavigating;
    }

    private void OnWebViewNavigating(object sender, WebNavigatingEventArgs e)
    {
        if (e.Url.StartsWith("myapp://lineclicked"))
        {
            e.Cancel = true; // Cancel the navigation

            // Extract the line index from the URL
            var info =  e.Url.Split('=')[2];
            info = System.Net.WebUtility.UrlDecode(info);
            // Call your C# function
            HandleLineClicked(info);
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel = (MapViewModel)BindingContext;
    }

    private async void HandleLineClicked(string info)
    {
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

        Line selectedLine = new Line();
        selectedLine.Index = -1;
        selectedLine.Points = mapPoints;

        var result = await mapWebView.EvaluateJavaScriptAsync("getLines()");
        result = result.Replace("\\\"", "\"");
        List<Line> lines = JsonConvert.DeserializeObject<List<Line>>(result);

        selectedLine = lines.FirstOrDefault(line => line.Equals(selectedLine));
    }

    private void OnMapClicked(object sender, MapClickedEventArgs e)
    {
        // Debug to ensure the map is interactive
        Console.WriteLine($"Map clicked at: {e.Location.Latitude}, {e.Location.Longitude}");

        // Update the ViewModel with the clicked location information
        _viewModel.LocationInfo = $"Clicked location: Latitude {e.Location.Latitude}, Longitude {e.Location.Longitude}";
    }

    private void btn_Draw_Clicked(object sender, EventArgs e)
    {
        mapWebView.EvaluateJavaScriptAsync("startSelectingPoints()");
    }

    private void btn_Clear_Clicked(object sender, EventArgs e)
    {
        mapWebView.EvaluateJavaScriptAsync("deleteLine()");
    }



    private async void btn_Test_Test(object sender, EventArgs e)
    {
        var result = await mapWebView.EvaluateJavaScriptAsync("getLines()");
        result = result.Replace("\\\"", "\"");
        List<Line> lines = JsonConvert.DeserializeObject<List<Line>>(result);
    }

    private class MapPoint : IEquatable<MapPoint>
    {
        public double Lat { get; set; }
        public double Lng { get; set; }

        public bool Equals(MapPoint? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if(other.Lat == this.Lat && other.Lng == this.Lng) return true;
            return false;
        }
    }

    private class Line : IEquatable<Line>
    {
        public int Index { get; set; }
        public List<MapPoint> Points { get; set; }

        public bool Equals(Line? other)
        {
            if (ReferenceEquals(null, other)) return false;
            bool isEquals = true;
            for(var i = 0; i < Points.Count; i++)
            {
                if (!this.Points[i].Equals(other.Points[i])) return false;
            }
            return isEquals;
        }
    }
}

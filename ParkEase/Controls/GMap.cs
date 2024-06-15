using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Newtonsoft.Json;
using ParkEase.Core.Data;
using Syncfusion.Maui.Core.Carousel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ParkEase.Controls
{
    public class GMap : WebView
    {
        private bool mapInitialised = false;
        public delegate void NavigatedEventHandler(object sender, WebNavigatedEventArgs e);
        public event NavigatedEventHandler NavigatedEvent;
        //public event Action<WebNavigatedEventArgs> NavigatedEvent;
        private static GMap currentInstance;
        private static bool selfUpdatingLines = false;
        public bool Drawing
        {
            get => (bool)GetValue(DrawingProperty); set { SetValue(DrawingProperty, value); }
        }

        public ObservableCollection<MapLine> Lines
        {
            get => (ObservableCollection<MapLine>)GetValue(LinesProperty); set { SetValue(LinesProperty, value); }
        }

        public MapLine SelectedLine
        {
            get => (MapLine)GetValue(SelectedLineProperty); set { SetValue(SelectedLineProperty, value); }
        }


        public static readonly BindableProperty LinesProperty = BindableProperty.Create(nameof(Lines), typeof(ObservableCollection<MapLine>), typeof(GMap), propertyChanged: LinesPropertyChanged,defaultBindingMode: BindingMode.TwoWay);

        public static readonly BindableProperty SelectedLineProperty = BindableProperty.Create(nameof(SelectedLine), typeof(MapLine), typeof(GMap), defaultBindingMode: BindingMode.TwoWay);

        public static readonly BindableProperty DrawingProperty = BindableProperty.Create(nameof(Drawing),typeof(bool), typeof(GMap), propertyChanged: DrawingPropertyChanged, defaultBindingMode: BindingMode.TwoWay);

        public GMap()
        {
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
                        let initial = true;
                        let userMarker = null;


                        // Initializes the Google Map 
                        function initMap(lat, lng) {
                            map = new google.maps.Map(document.getElementById(""map""), {
                                center: { lat: lat, lng: lng  }, 
                                zoom: 10, // Set the initial zoom level
                            });

                            // GPS marker for the user 
                            addUserMarker(lat, lng);

                            // Add a click event listener to the map
                            map.addListener('click', function(event) {
                                if (start) {
                                    selectedPoints.push({ lat: event.latLng.lat(), lng: event.latLng.lng() });

                                    if (selectedPoints.length == 2) {
                                        drawLine(selectedPoints[0].lat, selectedPoints[0].lng, selectedPoints[1].lat, selectedPoints[1].lng,""green"");
                                        selectedPoints = [];
                                        start = false; 
                                        window.location.href = 'myapp://lineDrawn';
                                    }
                                }
                            });
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
                                    url: 'https://maps.google.com/mapfiles/ms/icons/red-dot.png'
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

                            // Add mouseover and mouseout event listeners to the polyline
                            line.addListener('mouseover', function() {
                                line.setOptions({ strokeColor: ""yellow"" }); // Change color to yellow on mouseover
                            });

                            line.addListener('mouseout', function() {
                                if(line != selectedLine){
                                    line.setOptions({ strokeColor: ""#097969"" }); // Change color back to original on mouseout
                                }
                                if(selectedLine != null){
                                    selectedLine.setOptions({ strokeColor: ""yellow"" }); // Change color of selectedLine to yellow
                                }
                            });

                            line.addListener('click', function() {
                                 // If there is a previously selected line, reset its color.
                                if (selectedLine != null) {
                                    selectedLine.setOptions({ strokeColor: ""#097969"" });
                                }
                                selectedLine = line; // Set the clicked line as the selected line
                                selectedLine.setOptions({ strokeColor: ""yellow"" });

                                let lineInfo = getLineInfo(line);
                                window.location.href = ""myapp://lineclicked?index="" + lines.indexOf(line) + ""&info="" + encodeURIComponent(lineInfo);
                            });

                            line.setMap(map);
                            
                            lines.push(line);
                            
                            if(!initial)window.location.href = ""myapp://lineDrawn?index="";
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

                        // Enables the point selection mode
                        function startSelectingPoints() {
                            start = true;
                            //monitorLineDrawing();
                        }

                        // Disbles the point selection mode
                        function cancelSelectingPoints() {
                            start = false;
                            selectedPoints = [];
                        }

                        // Deletes the selected line
                        function deleteLine() {
                            if (selectedLine != null) {
                                selectedLine.setMap(null); // Removes the selected segment from the map
                                lines.splice(lines.indexOf(selectedLine), 1); // Removes the selected segment from the array
                                selectedLine = null; // Reset selectedLine
                                window.location.href = 'myapp://updateLines?index=""';
                            }
                        }

                        function setInitial(newValue) {
                            initial = newValue;
                        }
                    </script>
                    <script src=""https://maps.googleapis.com/maps/api/js?key=AIzaSyCMPKV70vmSd-153eJsECz6gJD0AipZD-M&callback=initMap"" async defer></script>
                </body>
                </html>"
            };

            Source = htmlSource; // Set the source of the WebView to the HTML content.
            Reload();
            Navigating += GMap_Navigating;
            Navigated += GMap_Navigated;
         
        }

        private static void LinesPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is not GMap view)
            {
                return;
            }
            ObservableCollection<MapLine> lines = (ObservableCollection<MapLine>)newValue;
            lines.CollectionChanged += Lines_CollectionChanged;
            if(!selfUpdatingLines) // update operation and make sure the lines on the map are displays are the same with lines conllection
            {
                foreach (MapLine line in lines.Where(l => l.Points.Count > 1))
                {
                    string jsCommand = $"drawLine({line.Points[0].Lat}, {line.Points[0].Lng}, {line.Points[1].Lat}, {line.Points[1].Lng},\"{line.Color}\");";
                    view.EvaluateJavaScriptAsync(jsCommand);
                }
            }
        }

        private static async void Lines_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (selfUpdatingLines) return;
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

        private static void DrawingPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is not GMap view)
            {
                return;
            }
            if((bool)oldValue == false && (bool)newValue == true)
            {
                view.EvaluateJavaScriptAsync("startSelectingPoints()");
            }
            if((bool)oldValue == true && (bool)newValue == false)
            {
                view.EvaluateJavaScriptAsync("cancelSelectingPoints()");
            }
        }

     

       

        private async void GMap_Navigated(object sender, WebNavigatedEventArgs e)
        {
            if(!mapInitialised)
            {
                currentInstance = (GMap)sender;
                var location = await Geolocation.GetLocationAsync();
                if (location != null)
                {
                    string jsCommand = $"initMap({location.Latitude}, {location.Longitude});";
                    await currentInstance.EvaluateJavaScriptAsync(jsCommand);

                    // Adding user marker on the map
                    string markerCommand = $"addUserMarker({location.Latitude}, {location.Longitude});";
                    await currentInstance.EvaluateJavaScriptAsync(markerCommand);
                }
                await currentInstance.EvaluateJavaScriptAsync("setInitial(false)");
                mapInitialised = true;
                NavigatedEvent?.Invoke(sender, e);
            }
        }

        private async void GMap_Navigating(object? sender, WebNavigatingEventArgs e)
        {
            if (e.Url.StartsWith("myapp://lineclicked"))
            {
                e.Cancel = true; // Cancel the navigation

                var info = e.Url.Split('=')[2]; // Extract the line index from the URL
                info = System.Net.WebUtility.UrlDecode(info);

                HandleLineClicked(info); // Call your C# function to handle the line click
            }

            else if (e.Url.StartsWith("myapp://linedrawn"))
            {
                if (Drawing)
                {
                    e.Cancel = true;
                    Drawing = false;
                    await UpdateLines();
                }
            }
            else if (e.Url.StartsWith("myapp://updateLines"))
            {
                e.Cancel = true;
                await UpdateLines();
            }
        }

        
        private async Task UpdateLines()
        {
            selfUpdatingLines = true;
            // Evaluate the JavaScript function "getLines()" to get the JSON string of all lines from the WebView
            var result = await this.EvaluateJavaScriptAsync("getLines()");
            if(result!= null)
            {
                result = result.Replace("\\\"", "\"");
                // Deserialize the JSON string into a list of Line objects
                List<MapLine> lines = JsonConvert.DeserializeObject<List<MapLine>>(result);
                Lines = new ObservableCollection<MapLine>(lines);
                selfUpdatingLines = false;
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
            if(result!= null)
            {
                result = result.Replace("\\\"", "\"");
                // Deserialize the JSON string into a list of Line objects
                List<MapLine> lines = JsonConvert.DeserializeObject<List<MapLine>>(result);

                // Find the line that matches the selected line based on the points
                SelectedLine = lines.FirstOrDefault(line => line.Equals(line_temp));
            }
        }
    }

    public class MapLine : IEquatable<MapLine>
    {
        public int Index { get; set; }
        public List<MapPoint> Points { get; set; }
        public string Color { get; set; }

        public MapLine(List<MapPoint> points, string color = "green")
        {
            if (points == null) throw new ArgumentNullException();
            if (points.Count != 2) throw new ArgumentException("Count of Points is not equal to 2");
            Points = points;
            Color = color;
        }
        public bool Equals(MapLine? other)
        {
            if (ReferenceEquals(null, other)) return false;
            bool isEquals = true;
            if (Points == null || Points.Count < 2) return false;
            for (var i = 0; i < Points.Count; i++)  // Compare each point in the line to check for equality
            {
                if (!this.Points[i].Equals(other.Points[i])) return false;
            }
            return isEquals;
        }
    }
}

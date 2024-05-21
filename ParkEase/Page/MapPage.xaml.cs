using Microsoft.Maui.Controls.Maps;
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
                                    findLine(event.latLng);
                                }
                            });
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

                            line.setMap(map);

                            lines.push(line);

                            console.log('draw');
                        }

                        function findLine(latLng) {
                            for (let i = 0; i < lines.length; i++) {
                                let linePath = lines[i].getPath();
                                for (let j = 0; j < linePath.length - 1; j++) {
                                    let p1 = linePath.getAt(j);
                                    let p2 = linePath.getAt(j + 1);
                                    if (isPointOnLine(latLng, p1, p2)) {
                                        selectedLine = lines[i];
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
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel = (MapViewModel)BindingContext;
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
}

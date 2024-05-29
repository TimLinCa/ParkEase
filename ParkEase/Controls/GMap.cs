using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkEase.Controls
{
    public class GMap : WebView
    {


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

                            addUserMarker(lat, lng);

                            // Add a click event listener to the map
                            map.addListener('click', function(event) {
                                if (start) {
                                    selectedPoints.push({ lat: event.latLng.lat(), lng: event.latLng.lng() });

                                    if (selectedPoints.length == 2) {
                                        drawLine(selectedPoints[0].lat, selectedPoints[0].lng, selectedPoints[1].lat, selectedPoints[1].lng);
                                        selectedPoints = [];
                                        start = false;
                                        window.location.href = 'myapp://lineDrawn';
                                    }
                                } else {
                                    //findLine(event.latLng);
                                }
                            });
                        }

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
                            monitorLineDrawing();
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
        }
    }
}

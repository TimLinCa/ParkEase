# ParkEase
ParkEase is a comprehensive cross-platform parking lot management software designed to streamline parking operations for both public and private areas.

# Overview
ParkEase offers a suite of tools for parking management, analysis, and user convenience:
- **Management Tools:** Create and monitor parking areas, including roadside and multi-level facilities.
- **Analysis Features:** Gain insights into parking usage patterns and occupancy rates.
- **Mobile App:** Help users find available spots and navigate to their parked vehicles.
- **Private Parking Management:** Detailed status information for private parking facilities.
- **Automated Monitoring:** Camera-based system for real-time parking spot detection.

# Technology Stack
- **Programming language:** Javascript, C# and Python>
- **Design Pattern:** MVVM
- **Platform:** .NET Core
- **Database** MongoDb
- **Framework:** .NET MAUI
- **Additional Tools** Qt designer

# Features
## Public Parking Area Management(Desktop)
![DeskTopPublic](https://github.com/TimLinCa/ParkEase/blob/master/ParkEase/Resources/Images/Readme/DeskTopPublic.png)

The management team can create roadside parking areas by drawing lines on the map and inputting essential details. This feature allows for precise delineation of parking zones along streets and roads, accompanied by the ability to specify relevant information for each area.

## Private Parking Lot Management(Desktop)
Create | Monitor 
:-------------------------:|:-------------------------:
![DeskTopPrivate](https://github.com/TimLinCa/ParkEase/blob/master/ParkEase/Resources/Images/Readme/DeskTopPrivate.png)|![DesktopPrivateStatus](https://github.com/TimLinCa/ParkEase/blob/master/ParkEase/Resources/Images/Readme/DesktopPrivateStatus.png)

Management teams can create and monitor comprehensive digital representations of parking facilities:
<ul>
<li>Importing parking lot images</li>
<li>Drawing rectangles to designate individual parking spots</li>
<li>Managing multiple floors within a single location</li>
<li>Setting the precise geographical location for map display</li>
<li>Monitor real-time parking status via desktop application</li>
</ul>
The system generates a unique QR code for each parking lot, enabling users to access instant parking availability information by scanning."

## Analysis Tool(Desktop)
![DeskTopPublic](https://github.com/TimLinCa/ParkEase/blob/master/ParkEase/Resources/Images/Readme/DeskTopAnalysisTool.png)
This analytical tool enables managers to make informed decisions about parking facility operations and optimization.

Management teams can perform data-driven analysis of parking facility usage:
<ul>
<li>Analyze log data for insights on occupancy and average parking duration</li>
<li>Filter analysis by custom date ranges and time periods</li>
<li>Focus on specific areas within parking facilities</li>
<li>For private areas, conduct floor-specific analysis</li>
</ul>

## Real-time Status Map(Mobile)
Map | Detail 
:-------------------------:|:-------------------------:
![MobilePublicMap](https://github.com/TimLinCa/ParkEase/blob/master/ParkEase/Resources/Images/Readme/MobilePublicMap.png)| ![MobilePublicDetail](https://github.com/TimLinCa/ParkEase/blob/master/ParkEase/Resources/Images/Readme/MobilePublicDetail.png)

Users can efficiently manage their parking experience through the app:
<ul>
<li>View available parking spots within their chosen area</li>
<li>Pin their parked car's location on the map</li>
<li>Use navigation features to easily return to their parked vehicle and selected parking area</li>
</ul>
These functionalities streamline the parking process, helping users find available spots or locate their vehicles with ease."

## Private Parking Info(Mobile)
Search | Detail
:-------------------------:|:-------------------------:
![MobilePrivateSearch](https://github.com/TimLinCa/ParkEase/blob/master/ParkEase/Resources/Images/Readme/MobilePrivateSearch.png)| ![MobilePrivateInfo](https://github.com/TimLinCa/ParkEase/blob/master/ParkEase/Resources/Images/Readme/MobilePrivateInfo.png)

For private parking areas, users can access detailed status information:
<ul>
<li>Retrieve parking lot status by searching the address or scanning a QR code</li>
<li>View a status page displaying occupancy information</li>
<li>Select different floors to check availability</li>
<li>Easily identify and locate available parking spots</li>
</ul>
This feature enhances the user experience in private parking facilities, providing quick access to real-time occupancy data across multiple levels."

## Camera Setup Program(Python)
Setting interface | Detection([Ref](https://www.youtube.com/watch?v=MyvylXVWYjY&t=3360s))
:-------------------------:|:-------------------------:
![PythonCameraSetUp](https://github.com/TimLinCa/ParkEase/blob/master/ParkEase/Resources/Images/Readme/PythonCameraSetUp.png)| ![PythonCameraDetection](https://github.com/TimLinCa/ParkEase/blob/master/ParkEase/Resources/Images/Readme/PythonCameraDetection.png)

Engineers and management teams can configure advanced automated parking spot monitoring:
<ul>
<li>Set up cameras for real-time parking spot detection</li>
<li>Use polygon tools to precisely define parking spots within the camera's field of view</li>
<li>Configure the system to automatically assess occupancy status of defined spots</li>
<li>Perform detection tests within the program to ensure accuracy</li>
<li>Synchronize detected status information with the database in real-time</li>
</ul>
This feature enables highly customizable, efficient, and accurate tracking of parking spot availability, allowing for flexible implementation in various parking layouts and reducing the need for manual oversight.

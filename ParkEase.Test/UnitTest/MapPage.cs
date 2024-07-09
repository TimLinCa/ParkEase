using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ParkEase.ViewModel;
using ParkEase.Contracts.Services;
using ParkEase.Core.Model;
using ParkEase.Core.Data;
using Xunit;
using Moq;
using ParkEase.Core.Contracts.Services;
using ParkEase.Utilities;
using System.Collections.ObjectModel;
using MongoDB.Driver;
using ParkEase.Controls;
using MongoDB.Bson.Serialization;


namespace ParkEase.Test.UnitTest
{
    public class MapPage
    {
        private MapViewModel _mapviewmodel;
        private readonly Mock<IMongoDBService> _mongoDBServiceMock;
        private readonly Mock<IDialogService> _dialogServiceMock;
        private readonly GMap _gmap;

        public MapPage()
        {
            _mongoDBServiceMock = new Mock<IMongoDBService>();
            _dialogServiceMock = new Mock<IDialogService>();
            _gmap = new GMap();
            _mapviewmodel = new MapViewModel(_mongoDBServiceMock.Object, _dialogServiceMock.Object);
        }


        [Fact]
        public void DrawLineOnMap()
        {
            // Arrange
            _mapviewmodel.DrawingLine = false;

            // Act
            _mapviewmodel.DrawCommand.Execute(null);

            // Assert
            Assert.True(_mapviewmodel.DrawingLine);
        }

        [Fact]
        public void ClearLineFromMap()
        {
            // Arrange
            var mapLine = new MapLine(new List<MapPoint> { new MapPoint { Lat = "0", Lng = "0" }, new MapPoint { Lat = "1", Lng = "1" } });
            _mapviewmodel.MapLines = new ObservableCollection<MapLine> { mapLine };
            _mapviewmodel.SelectedMapLine = mapLine;

            var mockDeleteResult = new DeleteDataResult { Success = true, DeleteCount = 1 };

            _mongoDBServiceMock
                .Setup(m => m.DeleteData(It.IsAny<string>(), It.IsAny<FilterDefinition<ParkingData>>()))
                .ReturnsAsync(mockDeleteResult);

            // Act
            _mapviewmodel.DeletedLineCommand.Execute(null);

            // Assert
            Assert.DoesNotContain(mapLine, _mapviewmodel.MapLines);
            _mongoDBServiceMock.Verify(m => m.DeleteData(It.IsAny<string>(), It.IsAny<FilterDefinition<ParkingData>>()), Times.Once);
        }

        [Fact]
        public void InputParkingData()
        {
            // Arrange
            var mapLine = new MapLine(new List<MapPoint> { new MapPoint { Lat = "0", Lng = "0" }, new MapPoint { Lat = "1", Lng = "1" } });
            _mapviewmodel.MapLines = new ObservableCollection<MapLine> { mapLine };
            _mapviewmodel.SelectedMapLine = mapLine;
            _mapviewmodel.ParkingSpot = "Test Spot";
            _mapviewmodel.SelectedParkingTime = "Mon to Fri: 7am to 6pm";
            _mapviewmodel.SelectedParkingFee = "$2.00 per hour";

            var parkingData = new ParkingData
            {
                ParkingSpot = "Test Spot",
                ParkingTime = "Mon to Fri: 7am to 6pm",
                ParkingFee = "$2.00 per hour",
                Points = mapLine.Points
            };

            // Mock the GetData method to return an empty list (indicating no existing data)
            _mongoDBServiceMock
                .Setup(m => m.GetData<ParkingData>(It.IsAny<string>()))
                .ReturnsAsync(new List<ParkingData>());

            // Mock the InsertData method to return the parking data
            _mongoDBServiceMock
                .Setup(m => m.InsertData(It.IsAny<string>(), It.IsAny<ParkingData>()))
                .ReturnsAsync(parkingData);

            // Act
            _mapviewmodel.SubmitCommand.Execute(null);

            // Assert
            _mongoDBServiceMock.Verify(m => m.InsertData(It.IsAny<string>(), It.Is<ParkingData>(pd =>
                pd.ParkingSpot == parkingData.ParkingSpot &&
                pd.ParkingTime == parkingData.ParkingTime &&
                pd.ParkingFee == parkingData.ParkingFee &&
                pd.Points.SequenceEqual(parkingData.Points))), Times.Once);
        }


        [Fact]
        public void EmptyFields()
        {
            // Arrange
            var mapLine = new MapLine(new List<MapPoint> { new MapPoint { Lat = "0", Lng = "0" }, new MapPoint { Lat = "1", Lng = "1" } });
            _mapviewmodel.MapLines = new ObservableCollection<MapLine> { mapLine };
            _mapviewmodel.SelectedMapLine = mapLine;

            // Act & Assert for empty ParkingSpot
            _mapviewmodel.ParkingSpot = "";
            _mapviewmodel.SelectedParkingTime = "Mon to Fri: 7am to 6pm";
            _mapviewmodel.SelectedParkingFee = "$2.00 per hour";
            _mapviewmodel.SubmitCommand.Execute(null);
            _dialogServiceMock.Verify(d => d.ShowAlertAsync("Warning", "Please fill in all fields.", "OK"), Times.Once);

            // Act & Assert for empty SelectedParkingTime
            _mapviewmodel.ParkingSpot = "Test Spot";
            _mapviewmodel.SelectedParkingTime = "";
            _mapviewmodel.SelectedParkingFee = "$2.00 per hour";
            _mapviewmodel.SubmitCommand.Execute(null);
            _dialogServiceMock.Verify(d => d.ShowAlertAsync("Warning", "Please fill in all fields.", "OK"), Times.Exactly(2));

            // Act & Assert for empty SelectedParkingFee
            _mapviewmodel.ParkingSpot = "Test Spot";
            _mapviewmodel.SelectedParkingTime = "Mon to Fri: 7am to 6pm";
            _mapviewmodel.SelectedParkingFee = "";
            _mapviewmodel.SubmitCommand.Execute(null);
            _dialogServiceMock.Verify(d => d.ShowAlertAsync("Warning", "Please fill in all fields.", "OK"), Times.Exactly(3));

            // Act & Assert for empty SelectedMapLine
            _mapviewmodel.ParkingSpot = "Test Spot";
            _mapviewmodel.SelectedParkingTime = "Mon to Fri: 7am to 6pm";
            _mapviewmodel.SelectedParkingFee = "$2.00 per hour";
            _mapviewmodel.SelectedMapLine = null;
            _mapviewmodel.SubmitCommand.Execute(null);
            _dialogServiceMock.Verify(d => d.ShowAlertAsync("Warning", "Please fill in all fields.", "OK"), Times.Exactly(4));
        }

        [Fact]
        public void SubmitParkingData()
        {
            // Arrange
            var mapLine = new MapLine(new List<MapPoint> { new MapPoint { Lat = "0", Lng = "0" }, new MapPoint { Lat = "1", Lng = "1" } });
            _mapviewmodel.MapLines = new ObservableCollection<MapLine> { mapLine };
            _mapviewmodel.SelectedMapLine = mapLine;
            _mapviewmodel.ParkingSpot = "Test Spot";
            _mapviewmodel.SelectedParkingTime = "Mon to Fri: 7am to 6pm";
            _mapviewmodel.SelectedParkingFee = "$2.00 per hour";

            var parkingData = new ParkingData
            {
                ParkingSpot = "Test Spot",
                ParkingTime = "Mon to Fri: 7am to 6pm",
                ParkingFee = "$2.00 per hour",
                Points = mapLine.Points
            };

            // Mock the GetData method to return an empty list (indicating no existing data)
            _mongoDBServiceMock
                .Setup(m => m.GetData<ParkingData>(It.IsAny<string>()))
                .ReturnsAsync(new List<ParkingData>());

            // Mock the InsertData method to return the parking data
            _mongoDBServiceMock
                .Setup(m => m.InsertData(It.IsAny<string>(), It.IsAny<ParkingData>()))
                .ReturnsAsync(parkingData);

            // Act
            _mapviewmodel.SubmitCommand.Execute(null);

            // Assert
            _mongoDBServiceMock.Verify(m => m.InsertData(It.IsAny<string>(), It.Is<ParkingData>(pd =>
                pd.ParkingSpot == parkingData.ParkingSpot &&
                pd.ParkingTime == parkingData.ParkingTime &&
                pd.ParkingFee == parkingData.ParkingFee &&
                pd.Points.SequenceEqual(parkingData.Points))), Times.Once);
        }



        [Fact]
        public void EditExistingParkingData()
        {
            // Arrange
            var mapLine = new MapLine(new List<MapPoint> { new MapPoint { Lat = "0", Lng = "0" }, new MapPoint { Lat = "1", Lng = "1" } });
            _mapviewmodel.MapLines = new ObservableCollection<MapLine> { mapLine };
            _mapviewmodel.SelectedMapLine = mapLine;
            _mapviewmodel.ParkingSpot = "Edited Spot";
            _mapviewmodel.SelectedParkingTime = "Mon to Fri: 7am to 6pm";
            _mapviewmodel.SelectedParkingFee = "$2.00 per hour";

            var existingParkingData = new ParkingData
            {
                ParkingSpot = "Original Spot",
                ParkingTime = "Mon to Fri: 7am to 6pm",
                ParkingFee = "$2.00 per hour",
                Points = mapLine.Points
            };

            _mongoDBServiceMock
                .Setup(m => m.GetData<ParkingData>(It.IsAny<string>()))
                .ReturnsAsync(new List<ParkingData> { existingParkingData });

            _mongoDBServiceMock
                .Setup(m => m.UpdateData(It.IsAny<string>(), It.IsAny<FilterDefinition<ParkingData>>(), It.IsAny<UpdateDefinition<ParkingData>>()))
                .Returns(Task.CompletedTask);

            // Act
            _mapviewmodel.SubmitCommand.Execute(null);

            // Assert
            _mongoDBServiceMock.Verify(m => m.UpdateData(
                It.IsAny<string>(),
                It.Is<FilterDefinition<ParkingData>>(f => FilterDefinitionMatches(f, existingParkingData.Points)),
                It.Is<UpdateDefinition<ParkingData>>(u => UpdateDefinitionMatches(u, "Edited Spot", "Mon to Fri: 7am to 6pm", "$2.00 per hour"))
            ), Times.Once);
        }

        private bool FilterDefinitionMatches(FilterDefinition<ParkingData> filter, List<MapPoint> points)
        {
            var bsonDocument = filter.Render(BsonSerializer.SerializerRegistry.GetSerializer<ParkingData>(), BsonSerializer.SerializerRegistry);
            var filterPoints = bsonDocument["Points"].AsBsonArray.Select(p => new MapPoint { Lat = p[0].AsString, Lng = p[1].AsString }).ToList();
            return points.SequenceEqual(filterPoints);
        }

        private bool UpdateDefinitionMatches(UpdateDefinition<ParkingData> update, string parkingSpot, string parkingTime, string parkingFee)
        {
            var bsonDocument = update.Render(BsonSerializer.SerializerRegistry.GetSerializer<ParkingData>(), BsonSerializer.SerializerRegistry);
            var updateDocument = bsonDocument["$set"].AsBsonDocument;
            return updateDocument["ParkingSpot"] == parkingSpot && updateDocument["ParkingTime"] == parkingTime && updateDocument["ParkingFee"] == parkingFee;
        }

        [Fact]
        public void UpdateExistingParkingData_OnSubmit_UpdatesDatabase()
        {
            // Arrange
            var mapLine = new MapLine(new List<MapPoint> { new MapPoint { Lat = "0", Lng = "0" }, new MapPoint { Lat = "1", Lng = "1" } });
            _mapviewmodel.MapLines = new ObservableCollection<MapLine> { mapLine };
            _mapviewmodel.SelectedMapLine = mapLine;
            _mapviewmodel.ParkingSpot = "Updated Spot";
            _mapviewmodel.SelectedParkingTime = "Mon to Fri: 8am to 5pm";
            _mapviewmodel.SelectedParkingFee = "$3.00 per hour";

            var existingParkingData = new ParkingData
            {
                ParkingSpot = "Original Spot",
                ParkingTime = "Mon to Fri: 7am to 6pm",
                ParkingFee = "$2.00 per hour",
                Points = mapLine.Points
            };

            var updatedParkingData = new ParkingData
            {
                ParkingSpot = "Updated Spot",
                ParkingTime = "Mon to Fri: 8am to 5pm",
                ParkingFee = "$3.00 per hour",
                Points = mapLine.Points
            };

            _mongoDBServiceMock
                .Setup(m => m.GetData<ParkingData>(It.IsAny<string>()))
                .ReturnsAsync(new List<ParkingData> { existingParkingData });

            _mongoDBServiceMock
                .Setup(m => m.UpdateData(It.IsAny<string>(), It.IsAny<FilterDefinition<ParkingData>>(), It.IsAny<UpdateDefinition<ParkingData>>()))
                .Returns(Task.CompletedTask);

            // Act
            _mapviewmodel.SubmitCommand.Execute(null);

            // Assert
            _mongoDBServiceMock.Verify(m => m.UpdateData(
                It.IsAny<string>(),
                It.Is<FilterDefinition<ParkingData>>(f => FilterDefinitionMatches(f, existingParkingData.Points)),
                It.Is<UpdateDefinition<ParkingData>>(u => UpdateDefinitionMatches(u, "Updated Spot", "Mon to Fri: 8am to 5pm", "$3.00 per hour"))
            ), Times.Once);
        }

        [Fact]
        public void DeleteCorrespondingParkingData_ClearsDataFromDatabase()
        {
            // Arrange
            var mapLine = new MapLine(new List<MapPoint> { new MapPoint { Lat = "0", Lng = "0" }, new MapPoint { Lat = "1", Lng = "1" } });
            _mapviewmodel.MapLines = new ObservableCollection<MapLine> { mapLine };
            _mapviewmodel.SelectedMapLine = mapLine;

            var parkingData = new ParkingData
            {
                ParkingSpot = "Test Spot",
                ParkingTime = "Mon to Fri: 7am to 6pm",
                ParkingFee = "$2.00 per hour",
                Points = mapLine.Points
            };

            var mockDeleteResult = new DeleteDataResult { Success = true, DeleteCount = 1 };

            _mongoDBServiceMock
                .Setup(m => m.GetData<ParkingData>(It.IsAny<string>()))
                .ReturnsAsync(new List<ParkingData> { parkingData });

            _mongoDBServiceMock
                .Setup(m => m.DeleteData(It.IsAny<string>(), It.IsAny<FilterDefinition<ParkingData>>()))
                .ReturnsAsync(mockDeleteResult);

            _dialogServiceMock
                .Setup(d => d.ShowAlertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            _mapviewmodel.DeletedLineCommand.Execute(null);

            // Assert
            Assert.DoesNotContain(mapLine, _mapviewmodel.MapLines);
            _mongoDBServiceMock.Verify(m => m.DeleteData(It.IsAny<string>(), It.Is<FilterDefinition<ParkingData>>(f => FilterDefinitionMatches(f, parkingData.Points))), Times.Once);
            _dialogServiceMock.Verify(d => d.ShowAlertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);  // Verify no error alert shown
        }

        [Fact]
        public void LoadMapData_OnPageRefresh_DisplaysCorrectData()
        {
            // Arrange
            var mapLine = new MapLine(new List<MapPoint>
            {
                new MapPoint { Lat = "0", Lng = "0" },
                new MapPoint { Lat = "1", Lng = "1" }
            });

            var parkingData = new ParkingData
            {
                ParkingSpot = "Test Spot",
                ParkingTime = "Mon to Fri: 7am to 6pm",
                ParkingFee = "$2.00 per hour",
                Points = mapLine.Points
            };

            _mongoDBServiceMock
                .Setup(m => m.GetData<ParkingData>(It.IsAny<string>()))
                .ReturnsAsync(new List<ParkingData> { parkingData });

            // Act
            _mapviewmodel.MapNavigatedCommand.Execute(null);

            // Assert
            Assert.Contains(_mapviewmodel.MapLines, line => line.Points.SequenceEqual(mapLine.Points));
        }

    }
}

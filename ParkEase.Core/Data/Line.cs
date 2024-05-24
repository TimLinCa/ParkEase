namespace ParkEase.Core.Data;

// This class represents a line on the map consisting of multiple points
public class Line : IEquatable<Line>
{
    public int Index { get; set; }  // Index of the line, used for identification
    public List<MapPoint> Points { get; set; }

    // Determines whether the specified Line is equal to the current Line
    public bool Equals(Line? other)
    {
        if (ReferenceEquals(null, other)) return false;
        bool isEquals = true;
        for (var i = 0; i < Points.Count; i++)  // Compare each point in the line to check for equality
        {
            if (!this.Points[i].Equals(other.Points[i])) return false;
        }
        return isEquals;
    }
}
using NetTopologySuite.Geometries;

namespace FarmScout.Models;

public class FarmLocation
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public Geometry Geometry { get; set; } = null!;
    public string FieldType { get; set; } = ""; // e.g., "Corn", "Soybeans", "Wheat"
    public double Area { get; set; } // in acres or hectares
    public string Owner { get; set; } = "";
    public DateTime LastUpdated { get; set; }
    
    public bool ContainsPoint(double latitude, double longitude)
    {
        try
        {
            var point = new NetTopologySuite.Geometries.Point(longitude, latitude);
            return Geometry.Contains(point);
        }
        catch
        {
            return false;
        }
    }
    
    public double DistanceToPoint(double latitude, double longitude)
    {
        try
        {
            var point = new NetTopologySuite.Geometries.Point(longitude, latitude);
            return Geometry.Distance(point);
        }
        catch
        {
            return double.MaxValue;
        }
    }
} 
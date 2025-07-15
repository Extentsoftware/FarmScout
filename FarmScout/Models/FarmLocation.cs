using NetTopologySuite.IO;
using SQLite;

namespace FarmScout.Models;

public class FarmLocation
{
    [PrimaryKey]
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Geometry { get; set; } = null!;
    public string FieldType { get; set; } = ""; // e.g., "Corn", "Soybeans", "Wheat"
    public double Area { get; set; } // in acres or hectares
    public string Owner { get; set; } = "";
    public DateTime LastUpdated { get; set; }
    
    public bool ContainsPoint(double latitude, double longitude)
    {
        try
        {
            var point = new NetTopologySuite.Geometries.Point(longitude, latitude);
            WKTReader wktr = new();
            var wkt = wktr.Read(Geometry);
            return wkt!.Contains(point);
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
            WKTReader wktr = new();
            var wkt = wktr.Read(Geometry);
            return wkt.Distance(point);
        }
        catch
        {
            return double.MaxValue;
        }
    }
} 
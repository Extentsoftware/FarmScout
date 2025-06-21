using NetTopologySuite.IO;
using NetTopologySuite.Geometries;
using FarmScout.Models;

namespace FarmScout.Services;

public class ShapefileService
{
    private List<FarmLocation> _farmLocations = new();
    
    public List<FarmLocation> FarmLocations => _farmLocations;
    
    public async Task LoadShapefileAsync(string shapefilePath)
    {
        try
        {
            // For now, just create sample farm locations
            // In a real implementation, you would load the shapefile here
            await CreateSampleFarmLocationsAsync();
        }
        catch (Exception)
        {
            // If shapefile loading fails, create sample data
            await CreateSampleFarmLocationsAsync();
        }
    }
    
    private Task CreateSampleFarmLocationsAsync()
    {
        _farmLocations.Clear();
        
        // Create sample farm locations with simple rectangular geometries
        var factory = new GeometryFactory();
        
        var sampleLocations = new[]
        {
            new { Name = "North Field", Type = "Corn", Lat = 40.7128, Lon = -74.0060, Width = 0.01, Height = 0.01 },
            new { Name = "South Field", Type = "Soybeans", Lat = 40.7028, Lon = -74.0060, Width = 0.01, Height = 0.01 },
            new { Name = "East Field", Type = "Wheat", Lat = 40.7128, Lon = -73.9960, Width = 0.01, Height = 0.01 },
            new { Name = "West Field", Type = "Alfalfa", Lat = 40.7128, Lon = -74.0160, Width = 0.01, Height = 0.01 }
        };
        
        int id = 1;
        foreach (var sample in sampleLocations)
        {
            var coordinates = new Coordinate[]
            {
                new(sample.Lon, sample.Lat),
                new(sample.Lon + sample.Width, sample.Lat),
                new(sample.Lon + sample.Width, sample.Lat + sample.Height),
                new(sample.Lon, sample.Lat + sample.Height),
                new(sample.Lon, sample.Lat)
            };
            
            var polygon = factory.CreatePolygon(factory.CreateLinearRing(coordinates));
            
            var farmLocation = new FarmLocation
            {
                Id = id++,
                Name = sample.Name,
                Description = $"Sample {sample.Type} field",
                Geometry = polygon,
                FieldType = sample.Type,
                Area = 100.0, // Sample area
                Owner = "Sample Farm",
                LastUpdated = DateTime.Now
            };
            
            _farmLocations.Add(farmLocation);
        }
        
        return Task.CompletedTask;
    }
    
    public FarmLocation? FindFarmLocationAtPoint(double latitude, double longitude)
    {
        return _farmLocations.FirstOrDefault(f => f.ContainsPoint(latitude, longitude));
    }
    
    public FarmLocation? FindNearestFarmLocation(double latitude, double longitude, double maxDistance = 0.1)
    {
        var nearest = _farmLocations
            .Where(f => f.DistanceToPoint(latitude, longitude) <= maxDistance)
            .OrderBy(f => f.DistanceToPoint(latitude, longitude))
            .FirstOrDefault();
            
        return nearest;
    }
    
    public List<FarmLocation> GetFarmLocations()
    {
        return _farmLocations.ToList();
    }
} 
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using FarmScout.Models;

namespace FarmScout.Services;

public class FarmLocationService
{
    private readonly List<FarmLocation> _farmLocations = [];

    public FarmLocationService()
    {
        MainThread.InvokeOnMainThreadAsync(LoadFarmShapefileAsync);
    }

    public List<FarmLocation> FarmLocations => _farmLocations;
    
    
    private async Task LoadFarmShapefileAsync()
    {
        _farmLocations.Clear();
        
        try
        {
            // Get the path to the farm.shp file in app data
            var appDataPath = Path.Combine(FileSystem.AppDataDirectory, "", "farm.shp");
            var appDataDirectory = Path.GetDirectoryName(appDataPath);
            
            // If the file doesn't exist in app data, copy it from the app bundle
            if (!File.Exists(appDataPath))
            {
                App.Log("Farm shapefile not found in app data, copying from bundle...");
                
                // Ensure the directory exists
                if (!Directory.Exists(appDataDirectory))
                {
                    Directory.CreateDirectory(appDataDirectory!);
                }
                
                // Copy the shapefile and its associated files with timeout
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)); // 30 second timeout
                await CopyShapefileFromBundle(appDataDirectory!).WaitAsync(cts.Token);
            }
            
            // Now read the shapefile from app data
            App.Log("Reading farm shapefile from app data directory");
            ReadShapefileFromAppData(appDataPath);
        }
        catch (Exception ex)
        {
            App.Log($"Error reading farm shapefile: {ex.Message}");
            // Fall back to sample data
            App.Log("Falling back to sample farm locations");
            await CreateSampleFarmLocationsAsync();
        }
    }
    
    private static async Task CopyShapefileFromBundle(string targetDirectory)
    {
        try
        {
            // List of shapefile files to copy
            var filesToCopy = new[]
            {
                "farm.shp",
                "farm.shx", 
                "farm.dbf",
                "farm.prj",
                "farm.qpj",
                "farm.cpg"
            };
            
            int copiedCount = 0;
            foreach (var fileName in filesToCopy)
            {
                try
                {
                    var bundlePath = $"{fileName}";
                    var targetPath = Path.Combine(targetDirectory, fileName);
                    
                    if (await CopyFileFromBundleIfExists(bundlePath, targetPath))
                    {
                        copiedCount++;
                        App.Log($"Successfully copied {fileName}");
                    }
                }
                catch (Exception ex)
                {
                    App.Log($"Warning: Could not copy {fileName}: {ex.Message}");
                }
            }
            
            if (copiedCount > 0)
            {
                App.Log($"Successfully copied {copiedCount} shapefile files from bundle to app data directory");
            }
            else
            {
                throw new InvalidDataException("No shapefile files could be copied from bundle");
            }
        }
        catch (Exception ex)
        {
            App.Log($"Error copying shapefile from bundle: {ex.Message}");
            throw;
        }
    }
    
        private static async Task<bool> CopyFileFromBundleIfExists(string bundlePath, string targetPath)
    {
        try
        {
            // Check if the file exists in the bundle first
            if (!await FileSystem.AppPackageFileExistsAsync(bundlePath))
            {
                App.Log($"File {bundlePath} does not exist in app bundle");
                return false;
            }
            
            using var sourceStream = await FileSystem.OpenAppPackageFileAsync(bundlePath);
            using var targetStream = File.Create(targetPath);
            
            // Use CopyToAsync for efficient binary copying
            // Shapefiles are binary files, so we don't need to worry about encoding
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)); // 10 second timeout per file
            await sourceStream.CopyToAsync(targetStream, cts.Token);
            
            return true;
        }
        catch (Exception ex)
        {
            App.Log($"Error copying {bundlePath}: {ex.Message}");
            return false;
        }
    }
    
    private void ReadShapefileFromAppData(string shapefilePath)
    {
        try
        {
            var gidIndex = 4;
            var descIndex = 2;
            var areaIndex = 3;
            
            // Read the shapefile
            using var shapefileReader = new ShapefileDataReader(shapefilePath, new GeometryFactory());

            // Read all features
            while (shapefileReader.Read())
            {
                var geometry = shapefileReader.Geometry;
                
                // Extract values from the shapefile attributes
                var gid = shapefileReader.GetGuid(gidIndex);
                var name = shapefileReader.GetString(descIndex);
                var area = areaIndex != -1 ? shapefileReader.GetDouble(areaIndex) : 0.0;
                
                var farmLocation = new FarmLocation
                {
                    Id = gid, // Generate new GUID for database compatibility
                    Name = name,
                    Description = $"Farm field: {name}",
                    Geometry = geometry.ToText(),
                    FieldType = "Unknown", // Could be added as another column if available
                    Area = area,
                    Owner = "Farm Owner", // Could be added as another column if available
                    LastUpdated = DateTime.Now
                };
                
                _farmLocations.Add(farmLocation);
            }
            
            App.Log($"Successfully loaded {_farmLocations.Count} farm locations from shapefile");
        }
        catch (Exception ex)
        {
            App.Log($"Error reading shapefile from app data: {ex.Message}");
            throw;
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
                Id = Guid.NewGuid(),
                Name = sample.Name,
                Description = $"Sample {sample.Type} field",
                Geometry = polygon.ToText(),
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
        return [.. _farmLocations];
    }
} 
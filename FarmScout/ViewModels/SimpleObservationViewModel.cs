using CommunityToolkit.Mvvm.ComponentModel;
using FarmScout.Models;
using FarmScout.Services;

namespace FarmScout.ViewModels;

public partial class SimpleObservationViewModel : ObservableObject
{
    private readonly IFarmScoutDatabase _database;
    private readonly FarmLocationService _shapefileService;

    public Observation Observation { get; }
    
    public SimpleObservationViewModel(
        Observation observation, 
        IFarmScoutDatabase database, 
        FarmLocationService shapefileService)
    {
        Observation = observation;
        _database = database;
        _shapefileService = shapefileService;

        _ = LoadPhotoAsync();

        _ = SetSummaryAsync(observation, database);
    }

    private const double CoordinateEpsilon = 1e-6;

    private static bool IsNonZeroCoordinate(double latitude, double longitude)
    {
        return Math.Abs(latitude) > CoordinateEpsilon && Math.Abs(longitude) > CoordinateEpsilon;
    }

    private async Task SetSummaryAsync(Observation observation, IFarmScoutDatabase database)
    {
        var metadata = await database.GetMetadataForObservationAsync(observation.Id);
        var observationTypes = await database.GetObservationTypesAsync();

        // Group metadata by observation type
        var metadataByType = metadata.GroupBy(m => m.ObservationTypeId).ToDictionary(g => g.Key, g => g.ToList());
        var selectedTypes = observationTypes.Where(t => metadataByType.ContainsKey(t.Id)).Select(x => x.Name).ToList();
        var selectedTypeNames = string.Join(",", selectedTypes);

        if (observation.FarmLocationId != null)
        {
            var loc = _shapefileService.FarmLocations.FirstOrDefault(x => x.Id == observation.FarmLocationId);
            if (loc != null)
                Summary = $"{selectedTypeNames} at {loc.Name}";
            else
                Summary = $"{selectedTypeNames} at unknown location";
        }
        else if (IsNonZeroCoordinate(observation.Latitude, observation.Longitude))
        {
            Summary = $"{selectedTypeNames} on {observation.Latitude:0.000},{observation.Longitude:0.000}";
        }
        else
            Summary = $"{selectedTypeNames}";
    }

    public string Notes => Observation.Notes;
    public string TimestampText => Observation.Timestamp.ToString("MMM dd, yyyy HH:mm");
    public string LocationText => $"📍 {Observation.Latitude:F4}, {Observation.Longitude:F4}";

    [ObservableProperty]
    public partial string Summary { get; set; } = "";

    [ObservableProperty] 
    public partial bool HasPhoto { get; set; } = false;

    [ObservableProperty]
    public partial bool NoPhoto { get; set; } = true;
    
    [ObservableProperty]
    public partial byte[]? PhotoData { get; set; }
    
    [ObservableProperty]
    public partial bool IsLoadingPhoto { get; set; } = true;

    private async Task LoadPhotoAsync()
    {
        try
        {
            IsLoadingPhoto = true;
            var photos = await _database.GetPhotosForObservationAsync(Observation.Id);
            if (photos.Count > 0)
            {
                PhotoData = photos[0].PhotoData;
                HasPhoto = true;
                NoPhoto = false;
            }
            else
            {
                HasPhoto = false;
                NoPhoto = true;
            }
        }
        catch (Exception ex)
        {
            App.Log($"Error loading photo for observation {Observation.Id}: {ex.Message}");
            HasPhoto = false;
            NoPhoto = true;
        }
        finally
        {
            IsLoadingPhoto = false;
        }
    }
} 
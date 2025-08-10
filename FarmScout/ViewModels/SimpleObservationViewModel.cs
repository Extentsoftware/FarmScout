using CommunityToolkit.Mvvm.ComponentModel;
using FarmScout.Models;
using FarmScout.Services;

namespace FarmScout.ViewModels;

public partial class SimpleObservationViewModel : ObservableObject
{
    private readonly IFarmScoutDatabase _database;
    
    public Observation Observation { get; }
    
    public SimpleObservationViewModel(Observation observation, IFarmScoutDatabase database)
    {
        Observation = observation;
        _database = database;
        _ = LoadPhotoAsync();
        //if (SelectedFarmLocation != null)
        //    observation.Summary = $"{SelectedTypesDisplay} on {SelectedFarmLocation.Name}";
        //else
        //    observation.Summary = $"{SelectedTypesDisplay}";
    }

    public string Notes => Observation.Notes;
    public string TimestampText => Observation.Timestamp.ToString("MMM dd, yyyy HH:mm");
    public string LocationText => $"📍 {Observation.Latitude:F4}, {Observation.Longitude:F4}";

    [ObservableProperty]
    public partial bool Summary { get; set; } = false;

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
                PhotoData = photos.First().PhotoData;
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
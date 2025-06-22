using System.Collections.ObjectModel;
using System.Windows.Input;
using FarmScout.Models;
using FarmScout.Services;

namespace FarmScout.ViewModels;

public class EditObservationViewModel : AddObservationViewModel
{
    private readonly FarmScoutDatabase _database;
    private readonly INavigationService _navigationService;
    private Observation? _originalObservation;

    public EditObservationViewModel(
        FarmScoutDatabase database, 
        PhotoService photoService, 
        LocationService locationService,
        ShapefileService shapefileService,
        INavigationService navigationService) 
        : base(database, photoService, locationService, shapefileService, navigationService)
    {
        _database = database;
        _navigationService = navigationService;
        Title = "Edit Observation";
        
        // Create new commands for editing
        UpdateObservationCommand = new Command(async () => await UpdateObservation());
    }

    public ICommand UpdateObservationCommand { get; }

    public async Task LoadObservationAsync(int observationId)
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            var observations = await _database.GetObservationsAsync();
            var observation = observations.FirstOrDefault(o => o.Id == observationId);
            _originalObservation = observation;

            // Load basic properties
            SoilMoisture = observation!.SoilMoisture;
            Notes = observation.Notes ?? string.Empty;
            SelectedSeverity = observation.Severity;

            // Load observation types
            var types = ObservationTypes.SplitTypes(observation.ObservationTypes);
            SelectedObservationTypes.Clear();
            foreach (var type in types)
            {
                SelectedObservationTypes.Add(type);
            }

            // Load farm location if available
            // TODO: Add GetFarmLocationsAsync method to FarmScoutDatabase
            /*
            if (observation.FarmLocationId.HasValue)
            {
                var farmLocations = await _database.GetFarmLocationsAsync();
                var farmLocation = farmLocations.FirstOrDefault(fl => fl.Id == observation.FarmLocationId.Value);
                if (farmLocation != null)
                {
                    SelectedFarmLocation = farmLocation;
                }
            }
            */

            // Load locations
            var locations = await _database.GetLocationsForObservationAsync(observation.Id);
            Locations.Clear();
            foreach (var location in locations)
            {
                Locations.Add(location);
            }

            // Load photos
            var photos = await _database.GetPhotosForObservationAsync(observation.Id);
            Photos.Clear();
            foreach (var photo in photos)
            {
                Photos.Add(photo);
            }

            // Load additional metrics from observation data
            LoadAdditionalMetrics(observation);

            App.Log($"Loaded observation {observation.Id} for editing");
        }
        catch (Exception ex)
        {
            App.Log($"Error loading observation for editing: {ex.Message}");
            await Shell.Current.DisplayAlert("Error", "Failed to load observation for editing", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void LoadAdditionalMetrics(Observation observation)
    {
        // Parse additional metrics from the observation data
        // This would need to be implemented based on how you store additional metrics
        // For now, we'll set basic properties that we know exist
        
        // You can extend this method to load disease, pest, harvest, weather, and soil data
        // based on your data structure
    }

    private async Task UpdateObservation()
    {
        if (IsBusy || _originalObservation == null) return;

        try
        {
            IsBusy = true;

            // Validate required fields
            if (SelectedObservationTypes.Count == 0)
            {
                await Shell.Current.DisplayAlert("Validation Error", "Please select at least one observation type", "OK");
                return;
            }

            // Update the original observation with new values
            _originalObservation.ObservationTypes = string.Join(",", SelectedObservationTypes);
            _originalObservation.Severity = SelectedSeverity;
            _originalObservation.SoilMoisture = SoilMoisture;
            _originalObservation.Notes = Notes;
            _originalObservation.FarmLocationId = SelectedFarmLocation?.Id;
            _originalObservation.Timestamp = DateTime.Now; // Update timestamp to reflect edit

            // Update additional metrics
            UpdateAdditionalMetrics(_originalObservation);

            // Save the updated observation
            await _database.UpdateObservationAsync(_originalObservation);

            // Update locations
            await UpdateLocations();

            // Update photos
            await UpdatePhotos();

            App.Log($"Updated observation {_originalObservation.Id}");

            await Shell.Current.DisplayAlert("Success", "Observation updated successfully", "OK");
            await _navigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            App.Log($"Error updating observation: {ex.Message}");
            await Shell.Current.DisplayAlert("Error", "Failed to update observation", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void UpdateAdditionalMetrics(Observation observation)
    {
        // Update additional metrics in the observation
        // This would need to be implemented based on how you store additional metrics
        // For now, we'll update basic properties that we know exist
        
        // You can extend this method to save disease, pest, harvest, weather, and soil data
        // based on your data structure
    }

    private async Task UpdateLocations()
    {
        if (_originalObservation == null) return;

        // Remove old locations
        var oldLocations = await _database.GetLocationsForObservationAsync(_originalObservation.Id);
        foreach (var location in oldLocations)
        {
            await _database.DeleteLocationAsync(location);
        }

        // Add new locations
        foreach (var location in Locations)
        {
            location.ObservationId = _originalObservation.Id;
            await _database.AddLocationAsync(location);
        }
    }

    private async Task UpdatePhotos()
    {
        if (_originalObservation == null) return;

        // Remove old photos
        var oldPhotos = await _database.GetPhotosForObservationAsync(_originalObservation.Id);
        foreach (var photo in oldPhotos)
        {
            await _database.DeletePhotoAsync(photo);
        }

        // Add new photos
        foreach (var photo in Photos)
        {
            photo.ObservationId = _originalObservation.Id;
            await _database.AddPhotoAsync(photo);
        }
    }
} 
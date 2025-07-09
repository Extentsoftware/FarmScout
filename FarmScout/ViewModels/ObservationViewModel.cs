using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmScout.Models;
using FarmScout.Services;

namespace FarmScout.ViewModels;

public partial class ObservationViewModel : ObservableObject
{
    private Observation? _originalObservation;
    private readonly IFarmScoutDatabase database;
    private readonly PhotoService photoService;
    private readonly LocationService locationService;
    private readonly ShapefileService shapefileService;
    private readonly INavigationService navigationService;

    public ObservationViewModel(
        IFarmScoutDatabase database,
        PhotoService photoService,
        LocationService locationService,
        ShapefileService shapefileService,
        INavigationService navigationService)
    {
        this.database = database;
        this.photoService = photoService;
        this.locationService = locationService;
        this.shapefileService = shapefileService;
        this.navigationService = navigationService;

        LoadFarmLocationsAsync().GetAwaiter().GetResult();
    }

    public enum ObservationMode
    {
        None,   
        Add,
        Edit,
        View
    }

    // Properties
    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string Title { get; set; } = "Loading..";

    // Mode properties
    [ObservableProperty]
    public partial bool IsAddMode { get; set; }

    [ObservableProperty]
    public partial bool IsEditMode { get; set; }

    [ObservableProperty]
    public partial bool IsViewMode { get; set; } = true;

    [ObservableProperty]
    public partial bool IsEditable { get; set; }

    public ObservableCollection<TaskItem> Tasks { get; set; } = [];

    public ObservableCollection<ObservationPhoto> Photos { get; set; } = [];

    public ObservableCollection<ObservationLocation> Locations { get; set; } = [];

    public ObservableCollection<ObservationType> AvailableObservationTypes { get; set; } = [];
    
    public ObservableCollection<ObservationType> SelectedObservationTypes { get; set; } = [];
    
    public ObservableCollection<FarmLocation> FarmLocations { get; set; } = [];

    [ObservableProperty]
    public partial string Notes { get; set; } = "";

    [ObservableProperty]
    public partial string NewTaskDescription { get; set; } = "";

    [ObservableProperty]
    public partial string SelectedSeverity { get; set; } = "";

    [ObservableProperty]
    public partial FarmLocation? SelectedFarmLocation { get; set; }

    [ObservableProperty]
    public partial string SelectedFarmLocationText { get; set; } = "";

    [ObservableProperty]
    public partial ObservationMode Mode { get; set; }

    // Metadata storage
    private readonly Dictionary<Guid, Dictionary<Guid, object>> _metadataByType = new();

    // Computed properties
    [ObservableProperty]
    public partial bool HasPhotos { get; set; }

    [ObservableProperty]
    public partial bool HasLocations { get; set; }

    [ObservableProperty]
    public partial bool HasObservationTypes { get; set; }

    [ObservableProperty]
    public partial string SelectedTypesDisplay { get; set; } = "";

    [ObservableProperty]
    public partial bool HasTasks { get; set; }

    partial void OnSelectedFarmLocationChanged(FarmLocation? value)
    {
        SelectedFarmLocationText = value?.Name ?? "";
    }

    partial void OnSelectedSeverityChanged(string value)
    {
        // Update severity display
    }

    [RelayCommand]
    private async Task ShowObservationTypes()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            // Load available observation types
            var types = await database.GetObservationTypesAsync();
            AvailableObservationTypes.Clear();
            foreach (var type in types)
            {
                AvailableObservationTypes.Add(type);
            }

            var action = await Shell.Current.DisplayActionSheet(
                "Select Observation Types",
                "Cancel",
                null,
                AvailableObservationTypes.Select(t => $"{t.Icon} {t.Name}").ToArray());

            if (action != null && action != "Cancel")
            {
                var selectedType = AvailableObservationTypes.FirstOrDefault(t => $"{t.Icon} {t.Name}" == action);
                if (selectedType != null)
                {
                    if (!SelectedObservationTypes.Any(t => t.Id == selectedType.Id))
                    {
                        SelectedObservationTypes.Add(selectedType);
                        _metadataByType[selectedType.Id] = new Dictionary<Guid, object>();
                    }
                    else
                    {
                        await Shell.Current.DisplayAlert("Info", "This observation type is already selected", "OK");
                    }
                }
            }

            UpdateSelectedTypesDisplay();
        }
        catch (Exception ex)
        {
            App.Log($"Error showing observation types: {ex.Message}");
            await Shell.Current.DisplayAlert("Error", "Failed to load observation types", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void RemoveObservationType(ObservationType? observationType)
    {
        if (observationType != null)
        {
            SelectedObservationTypes.Remove(observationType);
            _metadataByType.Remove(observationType.Id);
            UpdateSelectedTypesDisplay();
        }
    }

    private void UpdateSelectedTypesDisplay()
    {
        var displayText = string.Join(", ", SelectedObservationTypes.Select(t => t.Name));
        SelectedTypesDisplay = displayText.Length > 50 ? displayText[..47] + "..." : displayText;
        HasObservationTypes = SelectedObservationTypes.Count > 0;
    }

    [RelayCommand]
    private async Task TakePhoto()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            var photoPath = await photoService.CapturePhotoAsync();
            if (!string.IsNullOrEmpty(photoPath))
            {
                var photo = new ObservationPhoto
                {
                    PhotoPath = photoPath,
                    Description = "Observation photo",
                    Timestamp = DateTime.Now
                };

                Photos.Add(photo);
                HasPhotos = Photos.Count > 0;
                App.Log($"Photo taken and added: {photoPath}");
            }
        }
        catch (Exception ex)
        {
            App.Log($"Error taking photo: {ex.Message}");
            await Shell.Current.DisplayAlert("Error", "Failed to take photo", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void RemovePhoto(ObservationPhoto? photo)
    {
        if (photo != null)
        {
            Photos.Remove(photo);
            HasPhotos = Photos.Count > 0;
        }
    }

    [RelayCommand]
    private async Task GetLocation()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            var location = await locationService.GetCurrentLocationAsync();
            if (location != null)
            {
                var observationLocation = new ObservationLocation
                {
                    Latitude = location.Value.Latitude,
                    Longitude = location.Value.Longitude,
                    Description = "Current location",
                    Timestamp = DateTime.Now
                };

                Locations.Add(observationLocation);
                HasLocations = Locations.Count > 0;
                App.Log($"Location added: {location.Value.Latitude}, {location.Value.Longitude}");
            }
        }
        catch (Exception ex)
        {
            App.Log($"Error getting location: {ex.Message}");
            await Shell.Current.DisplayAlert("Error", "Failed to get location", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void RemoveLocation(ObservationLocation? location)
    {
        if (location != null)
        {
            Locations.Remove(location);
            HasLocations = Locations.Count > 0;
        }
    }

    [RelayCommand]
    private async Task ShowSeverityPopup()
    {
        if (IsBusy) return;

        var action = await Shell.Current.DisplayActionSheet(
            "Select Severity",
            "Cancel",
            null,
            SeverityLevels.AvailableSeverities);

        if (action != null && action != "Cancel")
        {
            SelectedSeverity = action;
        }
    }

    [RelayCommand]
    private async Task SelectFarmLocation(FarmLocation? farmLocation)
    {
        if (farmLocation != null)
        {
            SelectedFarmLocation = farmLocation;
            SelectedFarmLocationText = farmLocation.Name;
        }
    }

    [RelayCommand]
    private void AddTask()
    {
        if (!string.IsNullOrWhiteSpace(NewTaskDescription))
        {
            var task = new TaskItem
            {
                Description = NewTaskDescription,
                IsCompleted = false,
                CreatedAt = DateTime.Now
            };

            Tasks.Add(task);
            NewTaskDescription = "";
            HasTasks = Tasks.Count > 0;
        }
    }

    [RelayCommand]
    private void RemoveTask(TaskItem? task)
    {
        if (task != null)
        {
            Tasks.Remove(task);
            HasTasks = Tasks.Count > 0;
        }
    }

    [RelayCommand]
    private async Task SaveObservation()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            if (SelectedObservationTypes.Count == 0)
            {
                await Shell.Current.DisplayAlert("Validation Error", "Please select at least one observation type", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedSeverity))
            {
                await Shell.Current.DisplayAlert("Validation Error", "Please select a severity level", "OK");
                return;
            }

            if (Locations.Count == 0)
            {
                await Shell.Current.DisplayAlert("Validation Error", "Please add at least one location", "OK");
                return;
            }

            if (Mode == ObservationMode.Add)
            {
                await CreateNewObservation();
            }
            else if (Mode == ObservationMode.Edit)
            {
                await UpdateExistingObservation();
            }
        }
        catch (Exception ex)
        {
            App.Log($"Error saving observation: {ex.Message}");
            await Shell.Current.DisplayAlert("Error", "Failed to save observation", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GoBack()
    {
        await navigationService.GoBackAsync();
    }

    [RelayCommand]
    private void EditObservation()
    {
        if (Mode == ObservationMode.View)
        {
            Mode = ObservationMode.Edit;
            IsEditable = true;
            IsViewMode = false;
            IsEditMode = true;
            UpdateTitle();
        }
    }

    public async Task LoadObservationAsync(Guid observationId)
    {
        try
        {
            IsBusy = true;

            var observations = await database.GetObservationsAsync();
            var observation = observations.FirstOrDefault(o => o.Id == observationId);

            if (observation != null)
            {
                await LoadObservationForEditing(observation);
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", "Observation not found", "OK");
                await navigationService.GoBackAsync();
            }
        }
        catch (Exception ex)
        {
            App.Log($"Error loading observation: {ex.Message}");
            await Shell.Current.DisplayAlert("Error", "Failed to load observation", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void UpdateIndicators()
    {
        HasPhotos = Photos.Count > 0;
        HasLocations = Locations.Count > 0;
        HasObservationTypes = SelectedObservationTypes.Count > 0;
        HasTasks = Tasks.Count > 0;
    }

    private void UpdateTitle()
    {
        Title = Mode switch
        {
            ObservationMode.Add => "Add Observation",
            ObservationMode.Edit => "Edit Observation",
            ObservationMode.View => "View Observation",
            _ => "Observation"
        };
    }

    public async Task SetViewMode()
    {
        Mode = ObservationMode.View;
        IsViewMode = true;
        IsEditMode = false;
        IsAddMode = false;
        IsEditable = false;
        UpdateTitle();
    }

    public async Task SetAddMode()
    {
        Mode = ObservationMode.Add;
        IsAddMode = true;
        IsEditMode = false;
        IsViewMode = false;
        IsEditable = true;
        ClearForm();
        UpdateTitle();
    }

    public async Task SetEditMode()
    {
        Mode = ObservationMode.Edit;
        IsEditMode = true;
        IsAddMode = false;
        IsViewMode = false;
        IsEditable = true;
        UpdateTitle();
    }

    private void ClearForm()
    {
        Notes = "";
        SelectedSeverity = "";
        SelectedFarmLocation = null;
        SelectedFarmLocationText = "";
        NewTaskDescription = "";
        
        Tasks.Clear();
        Photos.Clear();
        Locations.Clear();
        SelectedObservationTypes.Clear();
        _metadataByType.Clear();
        
        UpdateIndicators();
        UpdateSelectedTypesDisplay();
    }

    private async Task CreateNewObservation()
    {
        var observation = new Observation
        {
            Notes = Notes,
            Severity = SelectedSeverity,
            FarmLocationId = SelectedFarmLocation?.Id,
            Timestamp = DateTime.Now,
            Latitude = Locations.FirstOrDefault()?.Latitude ?? 0,
            Longitude = Locations.FirstOrDefault()?.Longitude ?? 0
        };

        await database.AddObservationAsync(observation);

        // Add metadata for each observation type
        foreach (var observationType in SelectedObservationTypes)
        {
            if (_metadataByType.TryGetValue(observationType.Id, out var metadata))
            {
                foreach (var kvp in metadata)
                {
                    var observationMetadata = new ObservationMetadata
                    {
                        ObservationId = observation.Id,
                        ObservationTypeId = observationType.Id,
                        DataPointId = kvp.Key, // Now using the actual data point ID
                        Value = kvp.Value?.ToString() ?? ""
                    };
                    await database.AddObservationMetadataAsync(observationMetadata);
                }
            }
        }

        // Add locations
        foreach (var location in Locations)
        {
            location.ObservationId = observation.Id;
            await database.AddLocationAsync(location);
        }

        // Add photos
        foreach (var photo in Photos)
        {
            photo.ObservationId = observation.Id;
            await database.AddPhotoAsync(photo);
        }

        App.Log($"Created new observation with ID: {observation.Id}");
        await Shell.Current.DisplayAlert("Success", "Observation created successfully", "OK");
        await navigationService.GoBackAsync();
    }

    private async Task UpdateExistingObservation()
    {
        if (_originalObservation == null) return;

        // Update the original observation with new values
        _originalObservation.Notes = Notes;
        _originalObservation.Severity = SelectedSeverity;
        _originalObservation.FarmLocationId = SelectedFarmLocation?.Id;
        _originalObservation.Timestamp = DateTime.Now; // Update timestamp to reflect edit

        // Save the updated observation
        await database.UpdateObservationAsync(_originalObservation);

        // Update metadata
        await UpdateMetadata();

        // Update locations
        await UpdateLocations();

        // Update photos
        await UpdatePhotos();

        App.Log($"Updated observation {_originalObservation.Id}");
        await Shell.Current.DisplayAlert("Success", "Observation updated successfully", "OK");
        await navigationService.GoBackAsync();
    }

    private async Task UpdateMetadata()
    {
        if (_originalObservation == null) return;

        // Remove old metadata
        var oldMetadata = await database.GetMetadataForObservationAsync(_originalObservation.Id);
        foreach (var metadata in oldMetadata)
        {
            await database.DeleteObservationMetadataAsync(metadata);
        }

        // Add new metadata
        foreach (var observationType in SelectedObservationTypes)
        {
            if (_metadataByType.TryGetValue(observationType.Id, out var metadata))
            {
                foreach (var kvp in metadata)
                {
                    var observationMetadata = new ObservationMetadata
                    {
                        ObservationId = _originalObservation.Id,
                        ObservationTypeId = observationType.Id,
                        DataPointId = kvp.Key, // Now using the actual data point ID
                        Value = kvp.Value?.ToString() ?? ""
                    };
                    await database.AddObservationMetadataAsync(observationMetadata);
                }
            }
        }
    }

    private async Task UpdateLocations()
    {
        if (_originalObservation == null) return;

        // Remove old locations
        var oldLocations = await database.GetLocationsForObservationAsync(_originalObservation.Id);
        foreach (var location in oldLocations)
        {
            await database.DeleteLocationAsync(location);
        }

        // Add new locations
        foreach (var location in Locations)
        {
            location.ObservationId = _originalObservation.Id;
            await database.AddLocationAsync(location);
        }
    }

    private async Task UpdatePhotos()
    {
        if (_originalObservation == null) return;

        // Remove old photos
        var oldPhotos = await database.GetPhotosForObservationAsync(_originalObservation.Id);
        foreach (var photo in oldPhotos)
        {
            await database.DeletePhotoAsync(photo);
        }

        // Add new photos
        foreach (var photo in Photos)
        {
            photo.ObservationId = _originalObservation.Id;
            await database.AddPhotoAsync(photo);
        }
    }

    private async Task LoadObservationForEditing(Observation observation)
    {
        _originalObservation = observation;

        // Load basic properties
        Notes = observation.Notes ?? string.Empty;
        SelectedSeverity = observation.Severity;

        // Load observation types and metadata
        var metadata = await database.GetMetadataForObservationAsync(observation.Id);
        var observationTypes = await database.GetObservationTypesAsync();
        
        SelectedObservationTypes.Clear();
        _metadataByType.Clear();
        
        foreach (var meta in metadata)
        {
            var observationType = observationTypes.FirstOrDefault(t => t.Id == meta.ObservationTypeId);
            if (observationType != null && !SelectedObservationTypes.Any(t => t.Id == observationType.Id))
            {
                SelectedObservationTypes.Add(observationType);
                _metadataByType[observationType.Id] = new Dictionary<Guid, object>();
            }
            
            if (_metadataByType.ContainsKey(meta.ObservationTypeId))
            {
                _metadataByType[meta.ObservationTypeId][meta.DataPointId] = meta.Value;
            }
        }

        UpdateSelectedTypesDisplay();

        // Load locations
        var locations = await database.GetLocationsForObservationAsync(observation.Id);
        Locations.Clear();
        foreach (var location in locations)
        {
            Locations.Add(location);
        }

        SelectedFarmLocation = FarmLocations.FirstOrDefault(x => x.Id == observation.FarmLocationId);

        // Load photos
        var photos = await database.GetPhotosForObservationAsync(observation.Id);
        Photos.Clear();
        foreach (var photo in photos)
        {
            Photos.Add(photo);
        }

        // Update observable properties
        UpdateIndicators();
        
        App.Log($"Loaded observation {observation.Id} for editing");
    }

    private async Task LoadFarmLocationsAsync()
    {
        try
        {
            // Load shapefile or create sample data
            await shapefileService.LoadShapefileAsync("farm_locations.shp");

            FarmLocations.Clear();
            foreach (var location in shapefileService.GetFarmLocations())
            {
                FarmLocations.Add(location);
            }
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlert("Warning", "Could not load farm locations", "OK");
        }
    }

    public void UpdateMetadataForType(Guid observationTypeId, Dictionary<Guid, object> metadata)
    {
        _metadataByType[observationTypeId] = metadata;
    }
}
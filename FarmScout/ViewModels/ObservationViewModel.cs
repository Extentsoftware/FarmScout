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
    private readonly FarmLocationService shapefileService;
    private readonly INavigationService navigationService;

    public ObservationViewModel(
        IFarmScoutDatabase database,
        PhotoService photoService,
        LocationService locationService,
        FarmLocationService shapefileService,
        INavigationService navigationService)
    {
        this.database = database;
        this.photoService = photoService;
        this.locationService = locationService;
        this.shapefileService = shapefileService;
        this.navigationService = navigationService;

        // Initialize severity display
        SeverityDisplay = "Select Severity";

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
    
    public ObservableCollection<ObservationTypeViewModel> SelectedObservationTypes { get; set; } = [];
    
    public ObservableCollection<FarmLocation> FarmLocations { get; set; } = [];

    [ObservableProperty]
    public partial string Notes { get; set; } = "";

    [ObservableProperty]
    public partial string NewTaskDescription { get; set; } = "";

    [ObservableProperty]
    public partial string SelectedSeverity { get; set; } = "";

    [ObservableProperty]
    public partial string SeverityDisplay { get; set; }

    [ObservableProperty]
    public partial string SeverityColor { get; set; } = "#2196F3";

    [ObservableProperty]
    public partial FarmLocation? SelectedFarmLocation { get; set; }

    [ObservableProperty]
    public partial string SelectedFarmLocationText { get; set; } = "";

    [ObservableProperty]
    public partial ObservationMode Mode { get; set; }



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
        SeverityDisplay = string.IsNullOrWhiteSpace(value) ? "Select Severity" : $"{SeverityLevels.GetSeverityIcon(value)} {value}";
        SeverityColor = SeverityLevels.GetSeverityColor(value);
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
                [.. AvailableObservationTypes.Select(t => $"{t.Icon} {t.Name}")]);

            if (action != null && action != "Cancel")
            {
                var selectedType = AvailableObservationTypes.FirstOrDefault(t => $"{t.Icon} {t.Name}" == action);
                if (selectedType != null)
                {
                    if (!SelectedObservationTypes.Any(t => t.Id == selectedType.Id))
                    {
                        var observationTypeViewModel = new ObservationTypeViewModel(selectedType);
                        SelectedObservationTypes.Add(observationTypeViewModel);
                    }
                    else
                    {
                        await MauiProgram.DisplayAlertAsync("Info", "This observation type is already selected", "OK");
                    }
                }
            }

            UpdateSelectedTypesDisplay();
        }
        catch (Exception ex)
        {
            App.Log($"Error showing observation types: {ex.Message}");
            await MauiProgram.DisplayAlertAsync("Error", "Failed to load observation types", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void RemoveObservationType(ObservationTypeViewModel? observationTypeViewModel)
    {
        if (observationTypeViewModel != null)
        {
            SelectedObservationTypes.Remove(observationTypeViewModel);
            UpdateSelectedTypesDisplay();
        }
    }

    private void UpdateSelectedTypesDisplay()
    {
        var displayText = string.Join(", ", SelectedObservationTypes.Select(t => t.Name));
        SelectedTypesDisplay = displayText.Length > 50 ? displayText[..47] + "..." : displayText;
        HasObservationTypes = SelectedObservationTypes.Count > 0;
        OnPropertyChanged(nameof(SelectedObservationTypes));
    }

    [RelayCommand]
    private async Task TakePhoto()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            // Generate a temporary observation ID for the photo
            var tempObservationId = Guid.NewGuid();
            
            var photo = await PhotoService.CapturePhotoAsync(tempObservationId, "Observation photo");
            if (photo != null)
            {
                Photos.Add(photo);
                HasPhotos = Photos.Count > 0;
                App.Log($"Photo taken and added: {photo.OriginalFileName} ({photo.FileSizeDisplay})");
            }
        }
        catch (Exception ex)
        {
            App.Log($"Error taking photo: {ex.Message}");
            await MauiProgram.DisplayAlertAsync("Error", "Failed to take photo", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task PickPhoto()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            // Generate a temporary observation ID for the photo
            var tempObservationId = Guid.NewGuid();
            
            var photo = await PhotoService.PickPhotoAsync(tempObservationId, "Observation photo");
            if (photo != null)
            {
                Photos.Add(photo);
                HasPhotos = Photos.Count > 0;
                App.Log($"Photo picked and added: {photo.OriginalFileName} ({photo.FileSizeDisplay})");
            }
        }
        catch (Exception ex)
        {
            App.Log($"Error picking photo: {ex.Message}");
            await MauiProgram.DisplayAlertAsync("Error", "Failed to pick photo", "OK");
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
            await MauiProgram.DisplayAlertAsync("Error", "Failed to get location", "OK");
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
        if (IsBusy) return;

        var action = await Shell.Current.DisplayActionSheet(
            "Select Location",
            "Cancel",
            null,
            [.. FarmLocations.Select(x => x.Name)]);

        if (action != null && action != "Cancel")
        {
            SelectedFarmLocation = FarmLocations.First(x => x.Name == action);
            SelectedFarmLocationText = SelectedFarmLocation.Name;
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
                await MauiProgram.DisplayAlertAsync("Validation Error", "Please select at least one observation type", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedSeverity))
            {
                await MauiProgram.DisplayAlertAsync("Validation Error", "Please select a severity level", "OK");
                return;
            }

            if (Locations.Count == 0 && SelectedFarmLocation == null)
            {
                await MauiProgram.DisplayAlertAsync("Validation Error", "Please add at least one location", "OK");
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
            await MauiProgram.DisplayAlertAsync("Error", "Failed to save observation", "OK");
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
                await MauiProgram.DisplayAlertAsync("Error", "Observation not found", "OK");
                await navigationService.GoBackAsync();
            }
        }
        catch (Exception ex)
        {
            App.Log($"Error loading observation: {ex.Message}");
            await MauiProgram.DisplayAlertAsync("Error", "Failed to load observation", "OK");
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

    public Task SetViewModeAsync()
    {
        Mode = ObservationMode.View;
        IsViewMode = true;
        IsEditMode = false;
        IsAddMode = false;
        IsEditable = false;
        UpdateTitle();
        return Task.CompletedTask;
    }

    public Task SetAddModeAsync()
    {
        Mode = ObservationMode.Add;
        IsAddMode = true;
        IsEditMode = false;
        IsViewMode = false;
        IsEditable = true;
        ClearForm();
        UpdateTitle();
        return Task.CompletedTask;
    }

    public Task SetEditModeAsync()
    {
        Mode = ObservationMode.Edit;
        IsEditMode = true;
        IsAddMode = false;
        IsViewMode = false;
        IsEditable = true;
        UpdateTitle();
        return Task.CompletedTask;
    }

    private void ClearForm()
    {
        Notes = "";
        SelectedSeverity = "";
        SeverityDisplay = "Select Severity";
        SeverityColor = "#2196F3";
        SelectedFarmLocation = null;
        SelectedFarmLocationText = "";
        NewTaskDescription = "";
        
        Tasks.Clear();
        Photos.Clear();
        Locations.Clear();
        SelectedObservationTypes.Clear();
        
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

        if (SelectedFarmLocation != null)
            observation.Summary = $"{SelectedTypesDisplay} on {SelectedFarmLocation.Name}";
        else
            observation.Summary = $"{SelectedTypesDisplay}";

        await database.AddObservationAsync(observation);

        // Add metadata for each observation type
        foreach (var observationTypeViewModel in SelectedObservationTypes)
        {
            foreach (var kvp in observationTypeViewModel.Metadata)
            {
                var observationMetadata = new ObservationMetadata
                {
                    ObservationId = observation.Id,
                    ObservationTypeId = observationTypeViewModel.Id,
                    DataPointId = kvp.Key,
                    Value = kvp.Value?.ToString() ?? ""
                };
                await database.AddObservationMetadataAsync(observationMetadata);
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
        await MauiProgram.DisplayAlertAsync("Success", "Observation created successfully", "OK");
        await navigationService.GoBackAsync();
    }

    private async Task UpdateExistingObservation()
    {
        if (_originalObservation == null) return;

        // Update the original observation with new values

        if (SelectedFarmLocation != null)
            _originalObservation.Summary = $"{SelectedTypesDisplay} on {SelectedFarmLocation.Name}";
        else
            _originalObservation.Summary = $"{SelectedTypesDisplay}";

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
        await MauiProgram.DisplayAlertAsync("Success", "Observation updated successfully", "OK");
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
        foreach (var observationTypeViewModel in SelectedObservationTypes)
        {
            foreach (var kvp in observationTypeViewModel.Metadata)
            {
                var observationMetadata = new ObservationMetadata
                {
                    ObservationId = _originalObservation.Id,
                    ObservationTypeId = observationTypeViewModel.Id,
                    DataPointId = kvp.Key,
                    Value = kvp.Value?.ToString() ?? ""
                };
                await database.AddObservationMetadataAsync(observationMetadata);
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
        
        // Group metadata by observation type
        var metadataByType = metadata.GroupBy(m => m.ObservationTypeId).ToDictionary(g => g.Key, g => g.ToList());
        
        foreach (var kvp in metadataByType)
        {
            var observationType = observationTypes.FirstOrDefault(t => t.Id == kvp.Key);
            if (observationType != null)
            {
                var observationTypeViewModel = new ObservationTypeViewModel(observationType);
                
                // Add metadata to the view model
                foreach (var meta in kvp.Value)
                {
                    observationTypeViewModel.AddMetadata(meta.DataPointId, meta.Value);
                }
                
                SelectedObservationTypes.Add(observationTypeViewModel);
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
            FarmLocations.Clear();
            foreach (var location in shapefileService.GetFarmLocations())
            {
                FarmLocations.Add(location);
            }
        }
        catch (Exception)
        {
            await MauiProgram.DisplayAlertAsync("Warning", "Could not load farm locations", "OK");
        }
    }

    [RelayCommand]
    private void UpdateMetadataForType(Dictionary<Guid, object> metadata)
    {
        // This is a simplified approach - we'll need to track which observation type this belongs to
        // For now, we'll use the last selected type
        if (SelectedObservationTypes.Any())
        {
            var currentType = SelectedObservationTypes[^1];
            currentType.SetMetadata(metadata);
        }
    }

    public void UpdateMetadataForType(Guid observationTypeId, Dictionary<Guid, object> metadata)
    {
        var observationTypeViewModel = SelectedObservationTypes.FirstOrDefault(t => t.Id == observationTypeId);
        observationTypeViewModel?.SetMetadata(metadata);
    }
}
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmScout.Models;
using FarmScout.Services;

namespace FarmScout.ViewModels;

public partial class ObservationViewModel : BaseViewModel
{
    private readonly FarmScoutDatabase _database;
    private readonly PhotoService _photoService;
    private readonly LocationService _locationService;
    private readonly ShapefileService _shapefileService;
    private readonly INavigationService _navigationService;

    private Observation? _originalObservation;

    public enum ObservationMode
    {
        None,   
        Add,
        Edit,
        View
    }

    public ObservationViewModel(
        FarmScoutDatabase database, 
        PhotoService photoService, 
        LocationService locationService,
        ShapefileService shapefileService,
        INavigationService navigationService)
    {
        _database = database;
        _photoService = photoService;
        _locationService = locationService;
        _shapefileService = shapefileService;
        _navigationService = navigationService;
        
        _ = LoadFarmLocationsAsync();

        SoilMoisture = 0;
        Notes = "";
        NewTaskDescription = "";
        SelectedSeverity = "Information";
        SelectedTypesDisplay = "None";
    }

    public ObservableCollection<TaskItem> Tasks { get; set; } = [];

    public ObservableCollection<ObservationPhoto> Photos { get; set; } = [];

    public ObservableCollection<ObservationLocation> Locations { get; set; } = [];

    public ObservableCollection<string> SelectedObservationTypes { get; set; } = [];
    
    public ObservableCollection<FarmLocation> FarmLocations { get; set; } = [];

    // Properties
    [ObservableProperty]
    public partial double SoilMoisture { get; set; }

    [ObservableProperty]
    public partial string Notes { get; set; }

    [ObservableProperty]
    public partial string NewTaskDescription { get; set; }

    [ObservableProperty]
    public partial string SelectedSeverity { get; set; }

    [ObservableProperty]
    public partial FarmLocation? SelectedFarmLocation { get; set; }

    [ObservableProperty]
    public partial ObservationMode Mode { get; set; }

    // Additional metrics properties
    [ObservableProperty]
    public partial string DiseaseName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DiseaseType { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PestName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int? PlantCount { get; set; }

    [ObservableProperty]
    public partial int? PestCount { get; set; }

    [ObservableProperty]
    public partial double? AffectedAreaPercentage { get; set; }

    [ObservableProperty]
    public partial double? DamageLevel { get; set; }

    [ObservableProperty]
    public partial string DamageType { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string GrowthStage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double? HeightCm { get; set; }

    [ObservableProperty]
    public partial double? WeightKg { get; set; }

    [ObservableProperty]
    public partial string CropType { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double? HarvestWeight { get; set; }

    [ObservableProperty]
    public partial int? HarvestQuantity { get; set; }

    [ObservableProperty]
    public partial double? TemperatureCelsius { get; set; }

    [ObservableProperty]
    public partial double? Temperature { get; set; }

    [ObservableProperty]
    public partial double? HumidityPercentage { get; set; }

    [ObservableProperty]
    public partial double? Humidity { get; set; }

    [ObservableProperty]
    public partial double? WindSpeed { get; set; }

    [ObservableProperty]
    public partial double? Precipitation { get; set; }

    [ObservableProperty]
    public partial double? PhLevel { get; set; }

    [ObservableProperty]
    public partial double? SoilPH { get; set; }

    [ObservableProperty]
    public partial double? NutrientLevel { get; set; }

    [ObservableProperty]
    public partial double? SoilNitrogen { get; set; }

    [ObservableProperty]
    public partial double? SoilPhosphorus { get; set; }

    [ObservableProperty]
    public partial double? SoilPotassium { get; set; }

    [ObservableProperty]
    public partial string Symptoms { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Cause { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Quality { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double? HealthScore { get; set; }

    [ObservableProperty]
    public partial double? YieldPerArea { get; set; }

    [ObservableProperty]
    public partial string InfestationArea { get; set; } = string.Empty;

    // Computed properties
    public bool HasPhotos => Photos.Count > 0;
    public bool HasLocations => Locations.Count > 0;
    public bool HasObservationTypes => SelectedObservationTypes.Count > 0;

    [ObservableProperty]
    public partial string SelectedTypesDisplay { get; set; }    

    public string SeverityDisplay => $"{SeverityLevels.GetSeverityIcon(SelectedSeverity)} {SelectedSeverity}";
    public string SeverityColor => SeverityLevels.GetSeverityColor(SelectedSeverity);

    // Mode properties
    [ObservableProperty]
    public partial bool IsAddMode { get; set; }
    
    [ObservableProperty]
    public partial bool IsEditMode { get; set; }
    
    [ObservableProperty]
    public partial bool IsViewMode { get; set; }
    
    [ObservableProperty]
    public partial bool IsEditable { get; set; }

    partial void OnModeChanged(ObservationMode value)
    {
        UpdateTitle();
        IsAddMode = value == ObservationMode.Add;
        IsEditMode = value == ObservationMode.Edit;
        IsViewMode = value == ObservationMode.View;
        IsEditable = IsAddMode || IsEditMode;
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

    // Commands
    [RelayCommand]
    private async Task TakePhoto()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            App.Log("TakePhoto: Starting photo capture...");

            var photoPath = await _photoService.CapturePhotoAsync();
            App.Log($"TakePhoto: Photo service returned path: {photoPath}");

            if (!string.IsNullOrEmpty(photoPath))
            {
                var photo = new ObservationPhoto
                {
                    PhotoPath = photoPath,
                    Timestamp = DateTime.Now,
                    Description = $"Photo {Photos.Count + 1}"
                };
                Photos.Add(photo);
                App.Log($"TakePhoto: Added photo to collection. Total photos: {Photos.Count}");
                OnPropertyChanged(nameof(HasPhotos));
            }
            else
            {
                App.Log("TakePhoto: No photo path returned from service");
            }
        }
        catch (Exception ex)
        {
            App.Log($"TakePhoto: Exception occurred: {ex.Message}");
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
        if (photo != null && IsEditable)
        {
            Photos.Remove(photo);
            OnPropertyChanged(nameof(HasPhotos));
        }
    }

    [RelayCommand]
    private async Task GetLocation()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            App.Log("GetLocation: Starting location capture...");

            var location = await _locationService.GetCurrentLocationAsync();
            App.Log($"GetLocation: Location service returned: {location}");

            if (location.HasValue)
            {
                var obsLocation = new ObservationLocation
                {
                    Latitude = location.Value.Latitude,
                    Longitude = location.Value.Longitude,
                    Timestamp = DateTime.Now,
                    Description = $"Location {Locations.Count + 1}"
                };
                Locations.Add(obsLocation);
                App.Log($"GetLocation: Added location to collection. Total locations: {Locations.Count}");
                OnPropertyChanged(nameof(HasLocations));

                // Try to suggest farm location based on GPS
                var suggestedFarm = _shapefileService.FindNearestFarmLocation(location.Value.Latitude, location.Value.Longitude);
                if (suggestedFarm != null)
                {
                    var result = await Shell.Current.DisplayAlert(
                        "Farm Location Found",
                        $"GPS location is near '{suggestedFarm.Name}' ({suggestedFarm.FieldType}). Would you like to select this farm location?",
                        "Yes", "No");

                    if (result)
                    {
                        SelectedFarmLocation = suggestedFarm;
                    }
                }
            }
            else
            {
                App.Log("GetLocation: No location returned from service");
                await Shell.Current.DisplayAlert("Error", "Could not get location", "OK");
            }
        }
        catch (Exception ex)
        {
            App.Log($"GetLocation: Exception occurred: {ex.Message}");
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
        if (location != null && IsEditable)
        {
            Locations.Remove(location);
            OnPropertyChanged(nameof(HasLocations));
        }
    }

    [RelayCommand]
    private async void ShowObservationTypesPopup()
    {
        var availableTypes = ObservationTypes.AvailableTypes.ToList();
        var selectedTypes = SelectedObservationTypes.ToList();

        App.Log($"ShowObservationTypesPopup: Available types: {string.Join(", ", availableTypes)}");
        App.Log($"ShowObservationTypesPopup: Current selected types: {string.Join(", ", selectedTypes)}");

        // Create display strings with icons
        var displayOptions = availableTypes.Select(type =>
            $"{ObservationTypes.GetTypeIcon(type)} {type}").ToArray();

        var result = await Shell.Current.DisplayActionSheet(
            "Select Observation Types",
            "Cancel",
            null,
            displayOptions);

        if (result != null && result != "Cancel")
        {
            // Extract the type name from the display string (remove icon)
            var selectedType = availableTypes.FirstOrDefault(type =>
                result.Contains(type));

            if (selectedType != null)
            {
                App.Log($"ShowObservationTypesPopup: User selected: {selectedType}");

                if (selectedTypes.Contains(selectedType))
                {
                    SelectedObservationTypes.Remove(selectedType);
                    App.Log($"ShowObservationTypesPopup: Removed {selectedType}");
                }
                else
                {
                    SelectedObservationTypes.Add(selectedType);
                    App.Log($"ShowObservationTypesPopup: Added {selectedType}");
                }

                App.Log($"ShowObservationTypesPopup: Final selected types: {string.Join(", ", SelectedObservationTypes)}");

                OnPropertyChanged(nameof(HasObservationTypes));
                OnPropertyChanged(nameof(SelectedTypesDisplay));

                // Force UI refresh for conditional fields
                OnPropertyChanged(nameof(SelectedObservationTypes));
            }
        }
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SelectedTypesDisplay):
                SelectedTypesDisplay = string.Join(", ", SelectedObservationTypes.Select(type => type.Length > 20 ? type[..17] + "..." : type));
                break;
        }
    }


    [RelayCommand]
    private async void ShowSeverityPopup()
    {
        var availableSeverities = SeverityLevels.AvailableSeverities.ToList();
        var selectedSeverity = SelectedSeverity;

        var result = await Shell.Current.DisplayActionSheet(
            "Select Severity",
            "Cancel",
            null,
            [.. availableSeverities]);

        if (result != null && result != "Cancel")
        {
            SelectedSeverity = result;
            OnPropertyChanged(nameof(SeverityDisplay));
            OnPropertyChanged(nameof(SeverityColor));
        }
    }

    [RelayCommand]
    private void SelectFarmLocation(FarmLocation? farmLocation)
    {
        if (IsEditable)
        {
            SelectedFarmLocation = farmLocation;
        }
    }

    [RelayCommand]
    private void AddTask()
    {
        if (!IsEditable || string.IsNullOrWhiteSpace(NewTaskDescription)) return;

        var task = new TaskItem
        {
            Description = NewTaskDescription,
            IsCompleted = false,
            CreatedAt = DateTime.Now
        };

        Tasks.Add(task);
        NewTaskDescription = string.Empty;
    }

    [RelayCommand]
    private void RemoveTask(TaskItem? task)
    {
        if (task != null && IsEditable)
        {
            Tasks.Remove(task);
        }
    }

    [RelayCommand]
    private async Task SaveObservation()
    {
        if (!IsEditable) return;

        try
        {
            IsBusy = true;

            if (Mode == ObservationMode.Add)
            {
                await CreateNewObservation();
            }
            else if (Mode == ObservationMode.Edit)
            {
                await UpdateExistingObservation();
            }

            await GoBack();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to save observation: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GoBack()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task EditObservation()
    {
        if (Mode == ObservationMode.View)
        {
            SetEditMode();
        }
    }

    // Additional commands for the UI
    public ICommand ShowObservationTypesCommand => ShowObservationTypesPopupCommand;
    public ICommand ShowSeverityCommand => ShowSeverityPopupCommand;
    public ICommand ShowFarmLocationCommand => SelectFarmLocationCommand;
    public ICommand AddPhotoCommand => TakePhotoCommand;

    // Public methods
    public async Task LoadObservationAsync(int observationId)
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            var observations = await _database.GetObservationsAsync();
            var observation = observations.FirstOrDefault(o => o.Id == observationId);
            
            if (observation != null)
            {
                await LoadObservationForEditing(observation);
                Mode = ObservationMode.Edit;
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

    public void SetViewMode()
    {
        Mode = ObservationMode.View;
    }

    public void SetAddMode()
    {
        Mode = ObservationMode.Add;
        ClearForm();
    }

    public void SetEditMode()
    {
        Mode = ObservationMode.Edit;
    }

    private void ClearForm()
    {
        SoilMoisture = 50;
        Notes = string.Empty;
        SelectedSeverity = "Information";
        SelectedFarmLocation = null;
        SelectedObservationTypes.Clear();
        Photos.Clear();
        Locations.Clear();
        Tasks.Clear();
        
        // Clear additional metrics
        DiseaseName = string.Empty;
        DiseaseType = string.Empty;
        PestName = string.Empty;
        PlantCount = null;
        PestCount = null;
        AffectedAreaPercentage = null;
        DamageLevel = null;
        DamageType = string.Empty;
        GrowthStage = string.Empty;
        HeightCm = null;
        WeightKg = null;
        CropType = string.Empty;
        HarvestWeight = null;
        HarvestQuantity = null;
        Temperature = null;
        Humidity = null;
        WindSpeed = null;
        Precipitation = null;
        SoilPH = null;
        SoilNitrogen = null;
        SoilPhosphorus = null;
        SoilPotassium = null;
        Symptoms = string.Empty;
        Cause = string.Empty;
        Quality = string.Empty;
        HealthScore = null;
        YieldPerArea = null;
        InfestationArea = string.Empty;
    }

    private async Task LoadObservationForEditing(Observation observation)
    {
        _originalObservation = observation;

        // Load basic properties
        SoilMoisture = observation.SoilMoisture;
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
        //OnPropertyChanged(nameof(SelectedTypesDisplay));
        App.Log($"Loaded observation {observation.Id} for editing");
    }

    private void LoadAdditionalMetrics(Observation observation)
    {
        // Parse additional metrics from the observation data
        // This would need to be implemented based on how you store additional metrics
        // For now, we'll set basic properties that we know exist
        
        // You can extend this method to load disease, pest, harvest, weather, and soil data
        // based on your data structure
    }

    private async Task CreateNewObservation()
    {
        var observation = new Observation
        {
            ObservationTypes = string.Join(",", SelectedObservationTypes),
            Severity = SelectedSeverity,
            SoilMoisture = SoilMoisture,
            Notes = Notes,
            FarmLocationId = SelectedFarmLocation?.Id,
            Timestamp = DateTime.Now,
            Latitude = Locations.FirstOrDefault()?.Latitude ?? 0,
            Longitude = Locations.FirstOrDefault()?.Longitude ?? 0
        };

        // Add additional metrics
        AddAdditionalMetrics(observation);

        var observationId = await _database.AddObservationAsync(observation);

        // Add locations
        foreach (var location in Locations)
        {
            location.ObservationId = observationId;
            await _database.AddLocationAsync(location);
        }

        // Add photos
        foreach (var photo in Photos)
        {
            photo.ObservationId = observationId;
            await _database.AddPhotoAsync(photo);
        }

        App.Log($"Created new observation with ID: {observationId}");
        await Shell.Current.DisplayAlert("Success", "Observation created successfully", "OK");
        await _navigationService.GoBackAsync();
    }

    private async Task UpdateExistingObservation()
    {
        if (_originalObservation == null) return;

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

    private void AddAdditionalMetrics(Observation observation)
    {
        // Add additional metrics to the observation
        // This would need to be implemented based on how you store additional metrics
        // For now, we'll update basic properties that we know exist
        
        // You can extend this method to save disease, pest, harvest, weather, and soil data
        // based on your data structure
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

    private async Task LoadFarmLocationsAsync()
    {
        try
        {
            // Load shapefile or create sample data
            await _shapefileService.LoadShapefileAsync("farm_locations.shp");

            FarmLocations.Clear();
            foreach (var location in _shapefileService.GetFarmLocations())
            {
                FarmLocations.Add(location);
            }
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlert("Warning", "Could not load farm locations", "OK");
        }
    }

    public List<string> GetRelevantMetrics()
    {
        var metrics = new List<string>();
        foreach (var type in SelectedObservationTypes)
        {
            metrics.AddRange(GetMetricsForType(type));
        }
        return [.. metrics.Distinct()];
    }

    private List<string> GetMetricsForType(string type)
    {
        return type.ToLower() switch
        {
            "disease" => ["Disease Name", "Disease Type", "Symptoms", "Cause"],
            "pest" => ["Pest Name", "Pest Count", "Infestation Area"],
            "harvest" => ["Crop Type", "Weight", "Quantity", "Quality"],
            "weather" => ["Temperature", "Humidity", "Wind Speed", "Precipitation"],
            "soil" => ["pH Level", "Nitrogen", "Phosphorus", "Potassium"],
            "growth" => ["Growth Stage", "Height", "Health Score"],
            _ => []
        };
    }
} 
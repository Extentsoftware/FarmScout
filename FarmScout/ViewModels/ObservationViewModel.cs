using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmScout.Models;
using FarmScout.Services;

namespace FarmScout.ViewModels;

public partial class ObservationViewModel : ObservableObject
{
    private Observation? _originalObservation;
    private readonly FarmScoutDatabase database;
    private readonly PhotoService photoService;
    private readonly LocationService locationService;
    private readonly ShapefileService shapefileService;
    private readonly INavigationService navigationService;

    public ObservationViewModel(
        FarmScoutDatabase database,
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

    public ObservableCollection<string> SelectedObservationTypes { get; set; } = [];
    
    public ObservableCollection<FarmLocation> FarmLocations { get; set; } = [];

    [ObservableProperty]
    public partial double SoilMoisture { get; set; }

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

    [ObservableProperty]
    public partial string DiseaseName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DiseaseType { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PestName { get; set; } = string.Empty;

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
    [ObservableProperty]
    public partial bool HasPhotos { get; set; }

    [ObservableProperty]
    public partial bool HasLocations { get; set; }

    [ObservableProperty]
    public partial bool HasObservationTypes { get; set; }

    [ObservableProperty]
    public partial string SelectedTypesDisplay { get; set; } = "";

    [ObservableProperty]
    public partial string SeverityDisplay { get; set; } = "";
    
    [ObservableProperty]
    public partial string SeverityColor { get; set; } = "";

    partial void OnSelectedFarmLocationChanged(FarmLocation? value)
    {
        SelectedFarmLocationText = value!.Name;
    }

    partial void OnSelectedSeverityChanged(string value)
    {
        SeverityDisplay = $"{SeverityLevels.GetSeverityIcon(SelectedSeverity)} {SelectedSeverity}";
        SeverityColor = SeverityLevels.GetSeverityColor(SelectedSeverity);
    }

    [RelayCommand]
    private async Task ShowObservationTypes()
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

                HasObservationTypes = SelectedObservationTypes.Count > 0;

                // Force UI refresh for conditional fields
                SelectedTypesDisplay = string.Join(", ", SelectedObservationTypes.Select(type => type.Length > 20 ? type[..17] + "..." : type));
                OnPropertyChanged(nameof(SelectedObservationTypes));
            }
        }
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

            var photoPath = await photoService.CapturePhotoAsync();
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
                HasPhotos = Photos.Count > 0;
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
            App.Log("GetLocation: Starting location capture...");

            var location = await locationService.GetCurrentLocationAsync();
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
                HasLocations = Locations.Count > 0;

                // Try to suggest farm location based on GPS
                var suggestedFarm = shapefileService.FindNearestFarmLocation(location.Value.Latitude, location.Value.Longitude);
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
            HasLocations = Locations.Count > 0;
        }
    }

    [RelayCommand]
    private async Task ShowSeverityPopup()
    {
        var availableSeverities = SeverityLevels.AvailableSeverities.ToList();

        var result = await Shell.Current.DisplayActionSheet(
            "Select Severity",
            "Cancel",
            null,
            [.. availableSeverities]);

        if (result != null && result != "Cancel")
        {
            SelectedSeverity = result;
        }
    }

    [RelayCommand]
    private async Task SelectFarmLocation(FarmLocation? farmLocation)
    {
        var locations = FarmLocations.Select(x=>x.Name).ToArray();

        var result = await Shell.Current.DisplayActionSheet(
            "Select Location",
            "Cancel",
            null,
            [..locations]);

        if (result != null && result != "Cancel")
        {
            SelectedFarmLocation = FarmLocations.FirstOrDefault(x => x.Name == result);
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
        await navigationService.GoBackAsync();
    }

    [RelayCommand]
    private void EditObservation()
    {
        if (Mode == ObservationMode.View)
        {
            SetEditMode();
        }
    }

    // Public methods
    public async Task LoadObservationAsync(int observationId)
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            var observations = await database.GetObservationsAsync();
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

    private void UpdateIndicators()
    {
        IsAddMode = Mode == ObservationMode.Add;
        IsEditMode = Mode == ObservationMode.Edit;
        IsViewMode = Mode == ObservationMode.View;
        IsEditable = IsAddMode || IsEditMode;
    }

    private void UpdateTitle()
    {
        Title = Mode switch
        {
            ObservationMode.Add => "Add New Observation",
            ObservationMode.Edit => "Edit Observation",
            ObservationMode.View => "View Observation",
            _ => "Observation"
        };
    }

    public async Task SetViewMode()
    {
        Mode = ObservationMode.View;
        UpdateIndicators();
        UpdateTitle();
    }

    public async Task SetAddMode()
    {
        Mode = ObservationMode.Add;
        ClearForm();
        UpdateIndicators();
        UpdateTitle();
    }

    public async Task SetEditMode()
    {
        Mode = ObservationMode.Edit;
        UpdateIndicators();
        UpdateTitle();
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
        
        // Update observable properties
        HasPhotos = false;
        HasLocations = false;
        HasObservationTypes = false;
        
        // Clear additional metrics
        DiseaseName = string.Empty;
        DiseaseType = string.Empty;
        PestName = string.Empty;
        PestCount = null;
        AffectedAreaPercentage = null;
        DamageLevel = null;
        DamageType = string.Empty;
        GrowthStage = string.Empty;
        HeightCm = null;
        WeightKg = null;
        CropType = string.Empty;
        HarvestWeight = null;
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
            Longitude = Locations.FirstOrDefault()?.Longitude ?? 0,
            
            // Disease Information
            DiseaseName = DiseaseName,
            Disease = DiseaseType, // Using Disease property for DiseaseType
            
            // Pest Information
            PestCount = PestCount,
            PestName = PestName,
            
            // Harvest Information
            CropType = CropType,
            WeightKg = HarvestWeight,
            
            // Weather Information
            TemperatureCelsius = Temperature,
            HumidityPercentage = Humidity,
            WindSpeed = WindSpeed,
            
            // Soil Information
            PhLevel = SoilPH,
            NutrientLevel = SoilNitrogen, // Using NutrientLevel for Nitrogen
            SoilPhosphorus = SoilPhosphorus,
            SoilPotassium = SoilPotassium
        };

        var observationId = await database.AddObservationAsync(observation);

        // Add locations
        foreach (var location in Locations)
        {
            location.ObservationId = observationId;
            await database.AddLocationAsync(location);
        }

        // Add photos
        foreach (var photo in Photos)
        {
            photo.ObservationId = observationId;
            await database.AddPhotoAsync(photo);
        }

        App.Log($"Created new observation with ID: {observationId}");
        await Shell.Current.DisplayAlert("Success", "Observation created successfully", "OK");
        await navigationService.GoBackAsync();
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

        // Disease Information
        _originalObservation.DiseaseName = DiseaseName;
        _originalObservation.Disease = DiseaseType; // Using Disease property for DiseaseType

        // Pest Information
        _originalObservation.PestCount = PestCount;
        _originalObservation.PestName = PestName;

        // Harvest Information
        _originalObservation.CropType = CropType;
        _originalObservation.WeightKg = HarvestWeight;

        // Weather Information
        _originalObservation.TemperatureCelsius = Temperature;
        _originalObservation.HumidityPercentage = Humidity;
        _originalObservation.WindSpeed = WindSpeed;

        // Soil Information
        _originalObservation.PhLevel = SoilPH;
        _originalObservation.NutrientLevel = SoilNitrogen; // Using NutrientLevel for Nitrogen
        _originalObservation.SoilPhosphorus = SoilPhosphorus;
        _originalObservation.SoilPotassium = SoilPotassium;

        // Save the updated observation
        await database.UpdateObservationAsync(_originalObservation);

        // Update locations
        await UpdateLocations();

        // Update photos
        await UpdatePhotos();

        App.Log($"Updated observation {_originalObservation.Id}");
        await Shell.Current.DisplayAlert("Success", "Observation updated successfully", "OK");
        await navigationService.GoBackAsync();
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
        SoilMoisture = observation.SoilMoisture;
        Notes = observation.Notes ?? string.Empty;
        SelectedSeverity = observation.Severity;

        // Load Disease Information
        DiseaseName = observation.DiseaseName ?? string.Empty;
        DiseaseType = observation.Disease ?? string.Empty; // Using Disease property for DiseaseType

        // Load Pest Information
        PestCount = observation.PestCount;
        PestName = observation.PestName ?? string.Empty;

        // Load Harvest Information
        CropType = observation.CropType ?? string.Empty;
        HarvestWeight = observation.WeightKg;

        // Load Weather Information
        Temperature = observation.TemperatureCelsius;
        Humidity = observation.HumidityPercentage;
        WindSpeed = observation.WindSpeed;

        // Load Soil Information
        SoilPH = observation.PhLevel;
        SoilNitrogen = observation.NutrientLevel; // Using NutrientLevel for Nitrogen
        SoilPhosphorus = observation.SoilPhosphorus;
        SoilPotassium = observation.SoilPotassium;

        // Load observation types
        var types = ObservationTypes.SplitTypes(observation.ObservationTypes);
        SelectedObservationTypes.Clear();
        foreach (var type in types)
        {
            SelectedObservationTypes.Add(type);
        }
        SelectedTypesDisplay = string.Join(", ", SelectedObservationTypes.Select(type => type.Length > 20 ? type[..17] + "..." : type));
        OnPropertyChanged(nameof(SelectedObservationTypes));

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
        HasPhotos = Photos.Count > 0;
        HasLocations = Locations.Count > 0;
        HasObservationTypes = SelectedObservationTypes.Count > 0;
        
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

    public List<string> GetRelevantMetrics()
    {
        var metrics = new List<string>();
        foreach (var type in SelectedObservationTypes)
        {
            metrics.AddRange(GetMetricsForType(type));
        }
        return [.. metrics.Distinct()];
    }

    private static List<string> GetMetricsForType(string type)
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
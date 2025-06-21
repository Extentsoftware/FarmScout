using System.Collections.ObjectModel;
using System.Windows.Input;
using FarmScout.Models;
using FarmScout.Services;

namespace FarmScout.ViewModels;

public class AddObservationViewModel : BaseViewModel
{
    private readonly FarmScoutDatabase _database;
    private readonly PhotoService _photoService;
    private readonly LocationService _locationService;
    private readonly ShapefileService _shapefileService;
    private readonly INavigationService _navigationService;

    public AddObservationViewModel(
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
        Title = "Add Observation";

        Tasks = new ObservableCollection<TaskItem>();
        Photos = new ObservableCollection<ObservationPhoto>();
        Locations = new ObservableCollection<ObservationLocation>();
        SelectedObservationTypes = new ObservableCollection<string>();
        FarmLocations = new ObservableCollection<FarmLocation>();
        
        TakePhotoCommand = new Command(async () => await TakePhoto());
        RemovePhotoCommand = new Command<ObservationPhoto>(RemovePhoto);
        GetLocationCommand = new Command(async () => await GetLocation());
        RemoveLocationCommand = new Command<ObservationLocation>(RemoveLocation);
        ShowObservationTypesPopupCommand = new Command(ShowObservationTypesPopup);
        ShowSeverityPopupCommand = new Command(ShowSeverityPopup);
        SelectFarmLocationCommand = new Command<FarmLocation>(SelectFarmLocation);
        AddTaskCommand = new Command(AddTask);
        RemoveTaskCommand = new Command<TaskItem>(RemoveTask);
        SaveCommand = new Command(async () => await SaveObservation());
        
        _ = LoadFarmLocationsAsync();
    }

    public ObservableCollection<TaskItem> Tasks { get; }
    public ObservableCollection<ObservationPhoto> Photos { get; }
    public ObservableCollection<ObservationLocation> Locations { get; }
    public ObservableCollection<string> SelectedObservationTypes { get; }
    public ObservableCollection<FarmLocation> FarmLocations { get; }

    public ICommand TakePhotoCommand { get; }
    public ICommand RemovePhotoCommand { get; }
    public ICommand GetLocationCommand { get; }
    public ICommand RemoveLocationCommand { get; }
    public ICommand ShowObservationTypesPopupCommand { get; }
    public ICommand ShowSeverityPopupCommand { get; }
    public ICommand SelectFarmLocationCommand { get; }
    public ICommand AddTaskCommand { get; }
    public ICommand RemoveTaskCommand { get; }
    public ICommand SaveCommand { get; }

    // Additional commands for the new UI
    public ICommand ShowObservationTypesCommand => ShowObservationTypesPopupCommand;
    public ICommand ShowSeverityCommand => ShowSeverityPopupCommand;
    public ICommand ShowFarmLocationCommand => SelectFarmLocationCommand;
    public ICommand AddPhotoCommand => TakePhotoCommand;
    public ICommand SaveObservationCommand => SaveCommand;

    private double _soilMoisture = 50;
    public double SoilMoisture
    {
        get => _soilMoisture;
        set => SetProperty(ref _soilMoisture, value);
    }

    private string _notes = string.Empty;
    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    private string _newTaskDescription = string.Empty;
    public string NewTaskDescription
    {
        get => _newTaskDescription;
        set => SetProperty(ref _newTaskDescription, value);
    }

    private string _selectedSeverity = "Information";
    public string SelectedSeverity
    {
        get => _selectedSeverity;
        set => SetProperty(ref _selectedSeverity, value);
    }

    private FarmLocation? _selectedFarmLocation;
    public FarmLocation? SelectedFarmLocation
    {
        get => _selectedFarmLocation;
        set => SetProperty(ref _selectedFarmLocation, value);
    }

    // Additional metrics properties
    private string _diseaseName = string.Empty;
    public string DiseaseName
    {
        get => _diseaseName;
        set => SetProperty(ref _diseaseName, value);
    }

    private string _diseaseType = string.Empty;
    public string DiseaseType
    {
        get => _diseaseType;
        set => SetProperty(ref _diseaseType, value);
    }

    private string _pestName = string.Empty;
    public string PestName
    {
        get => _pestName;
        set => SetProperty(ref _pestName, value);
    }

    private int? _plantCount;
    public int? PlantCount
    {
        get => _plantCount;
        set => SetProperty(ref _plantCount, value);
    }

    private int? _pestCount;
    public int? PestCount
    {
        get => _pestCount;
        set => SetProperty(ref _pestCount, value);
    }

    private double? _affectedAreaPercentage;
    public double? AffectedAreaPercentage
    {
        get => _affectedAreaPercentage;
        set => SetProperty(ref _affectedAreaPercentage, value);
    }

    private double? _damageLevel;
    public double? DamageLevel
    {
        get => _damageLevel;
        set => SetProperty(ref _damageLevel, value);
    }

    private string _damageType = string.Empty;
    public string DamageType
    {
        get => _damageType;
        set => SetProperty(ref _damageType, value);
    }

    private string _growthStage = string.Empty;
    public string GrowthStage
    {
        get => _growthStage;
        set => SetProperty(ref _growthStage, value);
    }

    private double? _heightCm;
    public double? HeightCm
    {
        get => _heightCm;
        set => SetProperty(ref _heightCm, value);
    }

    private double? _weightKg;
    public double? WeightKg
    {
        get => _weightKg;
        set => SetProperty(ref _weightKg, value);
    }

    private string _cropType = string.Empty;
    public string CropType
    {
        get => _cropType;
        set => SetProperty(ref _cropType, value);
    }

    private double? _harvestWeight;
    public double? HarvestWeight
    {
        get => _harvestWeight;
        set => SetProperty(ref _harvestWeight, value);
    }

    private int? _harvestQuantity;
    public int? HarvestQuantity
    {
        get => _harvestQuantity;
        set => SetProperty(ref _harvestQuantity, value);
    }

    private double? _temperatureCelsius;
    public double? TemperatureCelsius
    {
        get => _temperatureCelsius;
        set => SetProperty(ref _temperatureCelsius, value);
    }

    private double? _temperature;
    public double? Temperature
    {
        get => _temperature;
        set => SetProperty(ref _temperature, value);
    }

    private double? _humidityPercentage;
    public double? HumidityPercentage
    {
        get => _humidityPercentage;
        set => SetProperty(ref _humidityPercentage, value);
    }

    private double? _humidity;
    public double? Humidity
    {
        get => _humidity;
        set => SetProperty(ref _humidity, value);
    }

    private double? _windSpeed;
    public double? WindSpeed
    {
        get => _windSpeed;
        set => SetProperty(ref _windSpeed, value);
    }

    private double? _precipitation;
    public double? Precipitation
    {
        get => _precipitation;
        set => SetProperty(ref _precipitation, value);
    }

    private double? _phLevel;
    public double? PhLevel
    {
        get => _phLevel;
        set => SetProperty(ref _phLevel, value);
    }

    private double? _soilPH;
    public double? SoilPH
    {
        get => _soilPH;
        set => SetProperty(ref _soilPH, value);
    }

    private double? _nutrientLevel;
    public double? NutrientLevel
    {
        get => _nutrientLevel;
        set => SetProperty(ref _nutrientLevel, value);
    }

    private double? _soilNitrogen;
    public double? SoilNitrogen
    {
        get => _soilNitrogen;
        set => SetProperty(ref _soilNitrogen, value);
    }

    private double? _soilPhosphorus;
    public double? SoilPhosphorus
    {
        get => _soilPhosphorus;
        set => SetProperty(ref _soilPhosphorus, value);
    }

    private double? _soilPotassium;
    public double? SoilPotassium
    {
        get => _soilPotassium;
        set => SetProperty(ref _soilPotassium, value);
    }

    private string _symptoms = string.Empty;
    public string Symptoms
    {
        get => _symptoms;
        set => SetProperty(ref _symptoms, value);
    }

    private string _cause = string.Empty;
    public string Cause
    {
        get => _cause;
        set => SetProperty(ref _cause, value);
    }

    private string _quality = string.Empty;
    public string Quality
    {
        get => _quality;
        set => SetProperty(ref _quality, value);
    }

    private double? _healthScore;
    public double? HealthScore
    {
        get => _healthScore;
        set => SetProperty(ref _healthScore, value);
    }

    private double? _yieldPerArea;
    public double? YieldPerArea
    {
        get => _yieldPerArea;
        set => SetProperty(ref _yieldPerArea, value);
    }

    private string _infestationArea = string.Empty;
    public string InfestationArea
    {
        get => _infestationArea;
        set => SetProperty(ref _infestationArea, value);
    }

    public bool HasPhotos => Photos.Count > 0;
    public bool HasLocations => Locations.Count > 0;
    public bool HasObservationTypes => SelectedObservationTypes.Count > 0;
    public string SelectedTypesDisplay => string.Join(", ", SelectedObservationTypes.Select(type => 
        $"{ObservationTypes.GetTypeIcon(type)} {type}"));
    public string SeverityDisplay => $"{SeverityLevels.GetSeverityIcon(SelectedSeverity)} {SelectedSeverity}";
    public string SeverityColor => SeverityLevels.GetSeverityColor(SelectedSeverity);

    // Helper method to get relevant metrics for selected observation types
    public List<string> GetRelevantMetrics()
    {
        var metrics = new List<string>();
        foreach (var type in SelectedObservationTypes)
        {
            metrics.AddRange(ObservationTypes.GetMetricsForType(type));
        }
        return metrics.Distinct().ToList();
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

    private void RemovePhoto(ObservationPhoto? photo)
    {
        if (photo != null)
        {
            Photos.Remove(photo);
            OnPropertyChanged(nameof(HasPhotos));
        }
    }

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

    private void RemoveLocation(ObservationLocation? location)
    {
        if (location != null)
        {
            Locations.Remove(location);
            OnPropertyChanged(nameof(HasLocations));
        }
    }

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

    private async void ShowSeverityPopup()
    {
        var availableSeverities = SeverityLevels.AvailableSeverities.ToList();
        var selectedSeverity = SelectedSeverity;
        
        var result = await Shell.Current.DisplayActionSheet(
            "Select Severity",
            "Cancel",
            null,
            availableSeverities.ToArray());
        
        if (result != null && result != "Cancel")
        {
            SelectedSeverity = result;
            OnPropertyChanged(nameof(SeverityDisplay));
            OnPropertyChanged(nameof(SeverityColor));
        }
    }

    private void SelectFarmLocation(FarmLocation? farmLocation)
    {
        if (farmLocation != null)
        {
            // Only allow one farm location to be selected at a time
            SelectedFarmLocation = farmLocation;
        }
    }

    private void AddTask()
    {
        if (!string.IsNullOrWhiteSpace(NewTaskDescription))
        {
            Tasks.Add(new TaskItem { Description = NewTaskDescription, IsCompleted = false });
            NewTaskDescription = string.Empty;
        }
    }

    private void RemoveTask(TaskItem? task)
    {
        if (task != null)
        {
            Tasks.Remove(task);
        }
    }

    private async Task SaveObservation()
    {
        if (IsBusy) return;

        if (SelectedObservationTypes.Count == 0)
        {
            await Shell.Current.DisplayAlert("Error", "Please select at least one observation type", "OK");
            return;
        }

        try
        {
            IsBusy = true;
            App.Log("Starting to save observation...");

            // Create observation
            var observation = new Observation
            {
                ObservationTypes = ObservationTypes.JoinTypes(SelectedObservationTypes),
                SoilMoisture = SoilMoisture,
                Notes = Notes,
                Timestamp = DateTime.Now,
                Severity = SelectedSeverity,
                FarmLocationId = SelectedFarmLocation?.Id,
                
                // Additional metrics
                DiseaseName = DiseaseName,
                PestName = PestName,
                PlantCount = PlantCount,
                PestCount = PestCount,
                AffectedAreaPercentage = AffectedAreaPercentage,
                DamageLevel = DamageLevel,
                DamageType = DamageType,
                GrowthStage = GrowthStage,
                HeightCm = HeightCm,
                WeightKg = WeightKg,
                CropType = CropType,
                TemperatureCelsius = TemperatureCelsius,
                HumidityPercentage = HumidityPercentage,
                WindSpeed = WindSpeed,
                Precipitation = Precipitation,
                PhLevel = PhLevel,
                NutrientLevel = NutrientLevel,
                Symptoms = Symptoms,
                Cause = Cause,
                Quality = Quality,
                HealthScore = HealthScore,
                YieldPerArea = YieldPerArea,
                InfestationArea = InfestationArea
            };

            App.Log($"Created observation with types: {observation.ObservationTypes}");

            // Save observation
            var result = await _database.AddObservationAsync(observation);
            App.Log($"Observation save result: {result}");

            // Get the saved observation to get its ID
            var savedObservations = await _database.GetObservationsAsync();
            var savedObservation = savedObservations.OrderByDescending(o => o.Timestamp).FirstOrDefault();
            
            if (savedObservation != null)
            {
                App.Log($"Found saved observation with ID: {savedObservation.Id}");
                var observationId = savedObservation.Id;

                // Save photos
                foreach (var photo in Photos)
                {
                    photo.ObservationId = observationId;
                    await _database.AddPhotoAsync(photo);
                    App.Log($"Saved photo: {photo.PhotoPath}");
                }

                // Save locations
                foreach (var location in Locations)
                {
                    location.ObservationId = observationId;
                    await _database.AddLocationAsync(location);
                    App.Log($"Saved location: {location.Description}");
                }

                // Save tasks
                foreach (var task in Tasks)
                {
                    task.ObservationId = observationId;
                    await _database.AddTaskAsync(task);
                    App.Log($"Saved task: {task.Description}");
                }

                App.Log("Observation saved successfully!");
                await Shell.Current.DisplayAlert("Success", "Observation saved successfully!", "OK");
                await _navigationService.GoBackAsync();
            }
            else
            {
                App.Log("ERROR: Could not find saved observation!");
                await Shell.Current.DisplayAlert("Error", "Failed to retrieve saved observation", "OK");
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
} 
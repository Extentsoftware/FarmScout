using System.Collections.ObjectModel;
using System.Windows.Input;
using FarmScout.Models;
using FarmScout.Services;

namespace FarmScout.ViewModels;

public class ObservationsViewModel : BaseViewModel
{
    private readonly FarmScoutDatabase _database;
    private readonly INavigationService _navigationService;

    public ObservationsViewModel(FarmScoutDatabase database, INavigationService navigationService)
    {
        _database = database;
        _navigationService = navigationService;
        Title = "Observations";

        Observations = new ObservableCollection<ObservationViewModel>();
        
        LoadObservationsCommand = new Command(async () => await LoadObservations());
        ViewDetailsCommand = new Command<ObservationViewModel>(async (obs) => await ViewDetails(obs));
        DeleteCommand = new Command<ObservationViewModel>(async (obs) => await DeleteObservation(obs));
        RefreshCommand = new Command(async () => await LoadObservations());
        AddObservationCommand = new Command(async () => await AddObservation());
        GoBackCommand = new Command(async () => await GoBack());
    }

    public ObservableCollection<ObservationViewModel> Observations { get; }

    public ICommand LoadObservationsCommand { get; }
    public ICommand ViewDetailsCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand AddObservationCommand { get; }
    public ICommand GoBackCommand { get; }

    public async Task LoadObservations()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            App.Log("Loading observations...");

            var observations = await _database.GetObservationsAsync();
            App.Log($"Retrieved {observations.Count} observations from database");
            
            var observationViewModels = new List<ObservationViewModel>();

            foreach (var obs in observations.OrderByDescending(o => o.Timestamp))
            {
                App.Log($"Processing observation: ID={obs.Id}, Types={obs.ObservationTypes}, Timestamp={obs.Timestamp}");
                observationViewModels.Add(new ObservationViewModel(obs));
            }

            Observations.Clear();
            foreach (var obs in observationViewModels)
            {
                Observations.Add(obs);
            }
            
            App.Log($"Added {Observations.Count} observations to the UI");
        }
        catch (Exception ex)
        {
            App.Log($"Error loading observations: {ex.Message}");
            await Shell.Current.DisplayAlert("Error", "Failed to load observations", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ViewDetails(ObservationViewModel? obs)
    {
        if (obs != null)
        {
            var parameters = new Dictionary<string, object>
            {
                { "ObservationId", obs.Observation.Id }
            };
            await _navigationService.NavigateToAsync("ObservationDetail", parameters);
        }
    }

    private async Task DeleteObservation(ObservationViewModel? obs)
    {
        if (obs == null) return;

        var result = await Shell.Current.DisplayAlert("Confirm Delete", 
            "Are you sure you want to delete this observation?", "Yes", "No");
        
        if (result)
        {
            try
            {
                IsBusy = true;

                // Delete associated tasks first
                var tasks = await _database.GetTasksForObservationAsync(obs.Observation.Id);
                foreach (var task in tasks)
                {
                    await _database.DeleteTaskAsync(task);
                }
                
                // Delete observation
                await _database.DeleteObservationAsync(obs.Observation);
                await LoadObservations();
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlert("Error", "Failed to delete observation", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    private async Task AddObservation()
    {
        await _navigationService.NavigateToAsync("AddObservation");
    }

    private async Task GoBack()
    {
        await Shell.Current.GoToAsync("..");
    }
}

public class ObservationViewModel
{
    public Observation Observation { get; }
    public string SoilMoistureText => $"Soil: {Observation.SoilMoisture:F0}%";
    public string TimestampText => Observation.Timestamp.ToString("MMM dd, yyyy HH:mm");
    public string LocationText => $"📍 {Observation.Latitude:F4}, {Observation.Longitude:F4}";
    public string ObservationTypesText 
    {
        get
        {
            var types = ObservationTypes.SplitTypes(Observation.ObservationTypes);
            return types.Count > 0 ? string.Join(", ", types) : "No type specified";
        }
    }
    public string SeverityText => $"{SeverityLevels.GetSeverityIcon(Observation.Severity)} {Observation.Severity}";
    public string SeverityColor => SeverityLevels.GetSeverityColor(Observation.Severity);
    public bool HasPhoto => false; // Will be updated when we load photos
    public bool NoPhoto => true; // Will be updated when we load photos

    public ObservationViewModel(Observation observation)
    {
        Observation = observation;
    }
} 
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmScout.Models;
using FarmScout.Services;

namespace FarmScout.ViewModels;

public partial class ObservationsViewModel(FarmScoutDatabase database, INavigationService navigationService) : ObservableObject
{
    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string Title { get; set; } = "Observations";

    [ObservableProperty]
    public partial List<SimpleObservationViewModel> Observations { get; set; } = [];

    [RelayCommand]
    public async Task LoadObservations()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            App.Log("Loading observations...");

            var observations = await database.GetObservationsAsync();
            App.Log($"Retrieved {observations.Count} observations from database");
            
            var observationViewModels = new List<SimpleObservationViewModel>();

            foreach (var obs in observations.OrderByDescending(o => o.Timestamp))
            {
                App.Log($"Processing observation: ID={obs.Id}, Types={obs.ObservationTypes}, Timestamp={obs.Timestamp}");
                observationViewModels.Add(new SimpleObservationViewModel(obs));
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

    [RelayCommand]
    private async Task ViewDetails(SimpleObservationViewModel? obs)
    {
        if (obs != null)
        {
            var parameters = new Dictionary<string, object>
            {
                { "ObservationId", obs.Observation.Id },
                { "Mode", "view" }
            };
            await navigationService.NavigateToAsync("Observation", parameters);
        }
    }

    [RelayCommand]
    private async Task DeleteObservation(SimpleObservationViewModel? obs)
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
                var tasks = await database.GetTasksForObservationAsync(obs.Observation.Id);
                foreach (var task in tasks)
                {
                    await database.DeleteTaskAsync(task);
                }
                
                // Delete observation
                await database.DeleteObservationAsync(obs.Observation);
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

    [RelayCommand]
    private async Task AddObservation()
    {
        var parameters = new Dictionary<string, object>
        {
            { "Mode", "add" }
        };
        await navigationService.NavigateToAsync("Observation", parameters);
    }

    [RelayCommand]
    private static async Task GoBack()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task Refresh()
    {
        await LoadObservations();
    }
}

public partial class SimpleObservationViewModel(Observation observation) : ObservableObject
{
    public Observation Observation { get; } = observation;
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

    [ObservableProperty] 
    public partial bool HasPhoto { get; set; } = false; // Will be updated when we load photos

    [ObservableProperty]
    public partial bool NoPhoto { get; set; } = true; // Will be updated when we load photos
} 
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmScout.Models;
using FarmScout.Services;
using System.Collections.ObjectModel;

namespace FarmScout.ViewModels;

public partial class ObservationsViewModel(IFarmScoutDatabase database, INavigationService navigationService) : ObservableObject
{
    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string Title { get; set; } = "Observations";

    [ObservableProperty]
    public partial ObservableCollection<SimpleObservationViewModel> Observations { get; set; } = [];

    [ObservableProperty]
    public partial bool IsLoadingMore { get; set; }

    [ObservableProperty]
    public partial bool HasMoreItems { get; set; } = true;

    [ObservableProperty]
    public partial int TotalObservationsCount { get; set; }

    private const int PageSize = 10;
    private int _currentPage = 0;

    [RelayCommand]
    public async Task LoadObservations()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            App.Log("Loading initial observations...");

            // Reset pagination state
            _currentPage = 0;
            HasMoreItems = true;
            Observations.Clear();

            // Get total count
            TotalObservationsCount = await database.GetObservationsCountAsync();

            // Load first page
            await LoadMoreObservations();
        }
        catch (Exception ex)
        {
            App.Log($"Error loading observations: {ex.Message}");
            await MauiProgram.DisplayAlertAsync("Error", "Failed to load observations", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task LoadMoreObservations()
    {
        if (IsLoadingMore || !HasMoreItems) return;

        try
        {
            IsLoadingMore = true;
            App.Log($"Loading more observations (page {_currentPage + 1})...");

            var observations = await database.GetObservationsAsync(_currentPage * PageSize, PageSize);
            App.Log($"Retrieved {observations.Count} observations from database");
            
            if (observations.Count < PageSize)
            {
                HasMoreItems = false;
                App.Log("No more observations to load");
            }

            foreach (var obs in observations)
            {
                App.Log($"Processing observation: ID={obs.Id}, Timestamp={obs.Timestamp}");
                Observations.Add(new SimpleObservationViewModel(obs, database));
            }
            
            _currentPage++;
            App.Log($"Added {observations.Count} observations to the UI. Total: {Observations.Count}");
        }
        catch (Exception ex)
        {
            App.Log($"Error loading more observations: {ex.Message}");
            await MauiProgram.DisplayAlertAsync("Error", "Failed to load more observations", "OK");
            // Reset loading state on error
            HasMoreItems = false;
        }
        finally
        {
            IsLoadingMore = false;
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

        var result = await MauiProgram.DisplayAlertAsync("Confirm Delete", 
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
                IsBusy = false;
                await LoadObservations();
            }
            catch (Exception)
            {
                await MauiProgram.DisplayAlertAsync("Error", "Failed to delete observation", "OK");
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
    private async Task GoBack()
    {
        await navigationService.GoBackAsync();
    }

    [RelayCommand]
    public async Task Refresh()
    {
        // Reset pagination state and reload from the beginning
        await LoadObservations();
    }

    [RelayCommand]
    public async Task OnAppearing()
    {
        // Always refresh when the page appears to show any new observations
        // This ensures new observations appear when returning from the add page
        await LoadObservations();
    }
}

public partial class SimpleObservationViewModel : ObservableObject
{
    private readonly IFarmScoutDatabase _database;
    
    public Observation Observation { get; }
    
    public SimpleObservationViewModel(Observation observation, IFarmScoutDatabase database)
    {
        Observation = observation;
        _database = database;
        _ = LoadPhotoAsync();
    }
    
    public string Notes => Observation.Notes;
    public string TimestampText => Observation.Timestamp.ToString("MMM dd, yyyy HH:mm");
    public string LocationText => $"📍 {Observation.Latitude:F4}, {Observation.Longitude:F4}";

    public string Summary => $"{Observation.Summary}";

    public string SeverityText => $"{SeverityLevels.GetSeverityIcon(Observation.Severity)} {Observation.Severity}";
    public string SeverityColor => SeverityLevels.GetSeverityColor(Observation.Severity);

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
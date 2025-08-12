using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmScout.Models;
using FarmScout.Services;
using System.Collections.ObjectModel;

namespace FarmScout.ViewModels;

public partial class ObservationsViewModel(
    IFarmScoutDatabase database, 
    INavigationService navigationService,
    FarmLocationService shapefileService) : ObservableObject
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

    // Filter properties
    [ObservableProperty]
    public partial string SelectedDateRange { get; set; } = "Any Time";

    public DateTime? StartDate => GetDateRangeStart(SelectedDateRange);
    public DateTime? EndDate => GetDateRangeEnd(SelectedDateRange);

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial FarmLocation? SelectedField { get; set; }

    [ObservableProperty]
    public partial ObservationType? SelectedObservationType { get; set; }

    [ObservableProperty]
    public partial string SortBy { get; set; } = "Timestamp";

    [ObservableProperty]
    public partial bool SortAscending { get; set; } = false;

    [ObservableProperty]
    public partial bool IsFilterExpanded { get; set; } = false;

    // Available options for filters
    [ObservableProperty]
    public partial ObservableCollection<FarmLocation> AvailableFields { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<ObservationType> AvailableObservationTypes { get; set; } = [];

    // Sort options
    [ObservableProperty]
    public partial ObservableCollection<string> SortOptions { get; set; } = ["Timestamp", "Severity", "Summary"];

    // Date range options
    [ObservableProperty]
    public partial ObservableCollection<string> DateRangeOptions { get; set;  } = ["Any Time", "This Week", "Last Week", "Last Month", "This Year", "Last Year"];

    private const int PageSize = 10;
    private int _currentPage = 0;

    private DateTime? GetDateRangeStart(string dateRange)
    {
        var today = DateTime.Today;
        return dateRange switch
        {
            "This Week" => today.AddDays(-(int)today.DayOfWeek),
            "Last Week" => today.AddDays(-(int)today.DayOfWeek - 7),
            "Last Month" => today.AddMonths(-1),
            "This Year" => new DateTime(today.Year, 1, 1),
            "Last Year" => new DateTime(today.Year - 1, 1, 1),
            _ => null // "Any Time"
        };
    }

    private DateTime? GetDateRangeEnd(string dateRange)
    {
        var today = DateTime.Today;
        return dateRange switch
        {
            "This Week" => today.AddDays(-(int)today.DayOfWeek + 6),
            "Last Week" => today.AddDays(-(int)today.DayOfWeek - 1),
            "Last Month" => today.AddDays(-1),
            "This Year" => new DateTime(today.Year, 12, 31),
            "Last Year" => new DateTime(today.Year - 1, 12, 31),
            _ => null // "Any Time"
        };
    }

    [RelayCommand]
    public async Task LoadObservations()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            App.Log("Loading initial observations...");

            // Load filter options
            await LoadFilterOptions();

            // Reset pagination state
            _currentPage = 0;
            HasMoreItems = true;
            Observations.Clear();

            // Get total count with filters
            TotalObservationsCount = await database.GetObservationsCountAsync(GetFilterParameters());

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

    private async Task LoadFilterOptions()
    {
        try
        {
            // Load available fields            
            var fields = shapefileService.GetFarmLocations();
            App.Log($"Loaded {fields.Count} farm locations from database");
            AvailableFields.Clear();
            AvailableFields.Add(new FarmLocation { Id = Guid.Empty, Name = "All Fields" });
            foreach (var field in fields)
            {
                AvailableFields.Add(field);
                App.Log($"Added field: {field.Name} (ID: {field.Id})");
            }

            // Load available observation types
            var observationTypes = await database.GetObservationTypesAsync();
            App.Log($"Loaded {observationTypes.Count} observation types from database");
            AvailableObservationTypes.Clear();
            AvailableObservationTypes.Add(new ObservationType { Id = Guid.Empty, Name = "All Types" });
            foreach (var obsType in observationTypes)
            {
                AvailableObservationTypes.Add(obsType);
                App.Log($"Added observation type: {obsType.Name} (ID: {obsType.Id})");
            }
        }
        catch (Exception ex)
        {
            App.Log($"Error loading filter options: {ex.Message}");
        }
    }

    private FilterParameters GetFilterParameters()
    {
        return new FilterParameters
        {
            StartDate = StartDate,
            EndDate = EndDate,
            SearchText = SearchText?.Trim(),
            FieldId = SelectedField?.Id == Guid.Empty ? null : SelectedField?.Id,
            ObservationTypeId = SelectedObservationType?.Id == Guid.Empty ? null : SelectedObservationType?.Id,
            SortBy = SortBy,
            SortAscending = SortAscending
        };
    }

    [RelayCommand]
    public async Task LoadMoreObservations()
    {
        if (IsLoadingMore || !HasMoreItems) return;

        try
        {
            IsLoadingMore = true;
            App.Log($"Loading more observations (page {_currentPage + 1})...");

            var filterParams = GetFilterParameters();
            var observations = await database.GetObservationsAsync(_currentPage * PageSize, PageSize, filterParams);
            App.Log($"Retrieved {observations.Count} observations from database");
            
            if (observations.Count < PageSize)
            {
                HasMoreItems = false;
                App.Log("No more observations to load");
            }

            foreach (var obs in observations)
            {
                App.Log($"Processing observation: ID={obs.Id}, Timestamp={obs.Timestamp}");
                Observations.Add(new SimpleObservationViewModel(obs, database, shapefileService));
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
    public async Task ApplyFilters()
    {
        // Reset pagination and reload with new filters
        _currentPage = 0;
        HasMoreItems = true;
        Observations.Clear();
        
        //await LoadObservations();
        // Get total count with filters
        TotalObservationsCount = await database.GetObservationsCountAsync(GetFilterParameters());

        // Load first page
        await LoadMoreObservations();
    }

    [RelayCommand]
    public async Task ClearFilters()
    {
        SelectedDateRange = "Any Time";
        SearchText = string.Empty;
        SelectedField = AvailableFields.Count > 0 ? AvailableFields.FirstOrDefault() : null;
        SelectedObservationType = AvailableObservationTypes.Count > 0 ? AvailableObservationTypes.FirstOrDefault() : null;
        SortBy = "Timestamp";
        SortAscending = false;
        
        await ApplyFilters();
    }

    [RelayCommand]
    public async Task ToggleFilterPanel()
    {
        IsFilterExpanded = !IsFilterExpanded;
    }

    [RelayCommand]
    public async Task SortByField(string fieldName)
    {
        if (SortBy == fieldName)
        {
            SortAscending = !SortAscending;
        }
        else
        {
            SortBy = fieldName;
            SortAscending = true;
        }
        
        await ApplyFilters();
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

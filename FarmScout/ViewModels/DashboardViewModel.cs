using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmScout.Models;
using FarmScout.Services;
using System.Linq;
using System.Collections.Generic;

namespace FarmScout.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    private readonly FarmScoutDatabase _database;
    private readonly INavigationService _navigationService;
    private static readonly string LogFilePath = Path.Combine(FileSystem.AppDataDirectory, "startup.log");

    public DashboardViewModel(FarmScoutDatabase database, INavigationService navigationService)
    {
        _database = database;
        _navigationService = navigationService;
        Title = "Farm Scout Dashboard";
        
        RecentActivity = [];
        RecentObservations = [];
    }

    public ObservableCollection<ActivityItem> RecentActivity { get; }

    [ObservableProperty]
    private int _observationCount;

    [ObservableProperty]
    private int _taskCount;

    public ObservableCollection<SimpleObservationViewModel> RecentObservations { get; }

    // Additional properties for the new UI
    public int TotalObservations => ObservationCount;
    public int TotalTasks => TaskCount;
    public double AverageSoilMoisture 
    { 
        get 
        {
            if (RecentObservations.Count == 0) return 0.0;
            var total = RecentObservations.Sum(obs => obs.Observation.SoilMoisture);
            return total / RecentObservations.Count;
        }
    }

    [RelayCommand]
    private async Task AddObservation()
    {
        await _navigationService.NavigateToAsync("Observation", new Dictionary<string, object> { { "Mode", "add" } });
    }

    [RelayCommand]
    private async Task ViewObservations()
    {
        await _navigationService.NavigateToAsync("Observations");
    }

    [RelayCommand]
    private async Task ViewTasks()
    {
        await _navigationService.NavigateToAsync("Tasks");
    }

    [RelayCommand]
    private async Task Refresh()
    {
        await LoadDashboardData();
    }

    [RelayCommand]
    private async Task ViewObservation(SimpleObservationViewModel obs)
    {
        try
        {
            // Temporarily show alert instead of navigating to prevent crash
            await Shell.Current.DisplayAlert("Observation Details", 
                $"Observation: {obs.ObservationTypesText}\nSeverity: {obs.SeverityText}\nTimestamp: {obs.TimestampText}", 
                "OK");
        }
        catch (Exception ex)
        {
            App.Log($"Error showing observation details: {ex.Message}");
        }
    }

    // Additional commands for the new UI
    public ICommand NavigateToAddObservationCommand => AddObservationCommand;
    public ICommand NavigateToObservationsCommand => ViewObservationsCommand;
    public ICommand NavigateToTasksCommand => ViewTasksCommand;

    public async Task LoadDashboardData()
    {
        App.Log("DashboardViewModel: LoadDashboardData start");
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            App.Log("DashboardViewModel: Getting observations");
            var observations = await _database.GetObservationsAsync();
            App.Log($"DashboardViewModel: Retrieved {observations.Count} observations from database");
            
            ObservationCount = observations.Count;
            App.Log($"DashboardViewModel: Set ObservationCount to {ObservationCount}");
            App.Log($"DashboardViewModel: TotalObservations property returns {TotalObservations}");
            
            // Force UI refresh
            OnPropertyChanged(nameof(ObservationCount));
            OnPropertyChanged(nameof(TotalObservations));

            // Count all tasks
            int totalTasks = 0;
            foreach (var obs in observations)
            {
                var tasks = await _database.GetTasksForObservationAsync(obs.Id);
                totalTasks += tasks.Count;
            }
            TaskCount = totalTasks;
            App.Log($"DashboardViewModel: Set TaskCount to {TaskCount}");
            
            // Force UI refresh for task count
            OnPropertyChanged(nameof(TaskCount));
            OnPropertyChanged(nameof(TotalTasks));

            // Load recent observations for the UI
            var recentObservations = observations.OrderByDescending(o => o.Timestamp).Take(5).ToList();
            RecentObservations.Clear();
            foreach (var obs in recentObservations)
            {
                RecentObservations.Add(new SimpleObservationViewModel(obs));
            }
            App.Log($"DashboardViewModel: Added {RecentObservations.Count} recent observations to UI");
            
            // Force UI refresh for collections and computed properties
            OnPropertyChanged(nameof(RecentObservations));
            OnPropertyChanged(nameof(AverageSoilMoisture));

            // Load recent activity (last 5 observations) for the old UI
            RecentActivity.Clear();
            foreach (var obs in recentObservations)
            {
                var types = ObservationTypes.SplitTypes(obs.ObservationTypes);
                var typeText = types.Count > 0 ? string.Join(", ", types) : "No type specified";
                RecentActivity.Add(new ActivityItem
                {
                    Icon = "🌱",
                    Title = $"Observation: {typeText}",
                    Description = $"Soil: {obs.SoilMoisture:F0}%",
                    Timestamp = obs.Timestamp.ToString("MMM dd, HH:mm")
                });
            }
            App.Log("DashboardViewModel: LoadDashboardData success");
        }
        catch (Exception ex)
        {
            App.Log($"DashboardViewModel: Exception: {ex}");
            try { await Shell.Current.DisplayAlert("Error", $"Failed to load dashboard data: {ex.Message}", "OK"); } catch { }
        }
        finally
        {
            IsBusy = false;
            App.Log("DashboardViewModel: LoadDashboardData end");
        }
    }
}

public class ActivityItem
{
    public string Icon { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Timestamp { get; set; } = "";
} 
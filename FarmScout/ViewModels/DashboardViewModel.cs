using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmScout.Services;

namespace FarmScout.ViewModels;

public partial class DashboardViewModel(IFarmScoutDatabase database, INavigationService navigationService) : ObservableObject
{
    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string Title { get; set; } = "Farm Scout Dashboard";

    public ObservableCollection<ActivityItem> RecentActivity { get; } = [];

    [ObservableProperty]
    public partial int ObservationCount { get; set; }

    [ObservableProperty]
    public partial int TaskCount { get; set; }

    public ObservableCollection<SimpleObservationViewModel> RecentObservations { get; } = [];

    // Additional properties for the new UI
    public int TotalObservations => ObservationCount;
    public int TotalTasks => TaskCount;

    [ObservableProperty]
    public partial double AverageSoilMoisture { get; set; }

    [RelayCommand]
    private async Task ViewTasks()
    {
        await navigationService.NavigateToAsync("Tasks");
    }

    [RelayCommand]
    private async Task Refresh()
    {
        await LoadDashboardData();
    }

    public async Task LoadDashboardData()
    {
        for (int i = 0; i < 100 && !database.IsReady; i++)
            await Task.Delay(10);

        App.Log("DashboardViewModel: LoadDashboardData start");
        if (IsBusy || !database.IsReady) 
            return;

        try
        {
            IsBusy = true;
            App.Log("DashboardViewModel: Getting observations");
            var observations = await database.GetObservationsAsync();
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
                var tasks = await database.GetTasksForObservationAsync(obs.Id);
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
                RecentObservations.Add(new SimpleObservationViewModel(obs, database));
            }
            App.Log($"DashboardViewModel: Added {RecentObservations.Count} recent observations to UI");
            
            // Load recent activity (last 5 observations) for the old UI
            RecentActivity.Clear();
            foreach (var obs in recentObservations)
            {
                // For now, use placeholder data since we need to load from metadata
                RecentActivity.Add(new ActivityItem
                {
                    Icon = "🌱",
                    Title = "Observation",
                    Description = "Data loading...",
                    Timestamp = obs.Timestamp.ToString("MMM dd, HH:mm")
                });
            }
            App.Log("DashboardViewModel: LoadDashboardData success");
        }
        catch (Exception ex)
        {
            App.Log($"DashboardViewModel: Exception: {ex}");
            try 
            {
                await MauiProgram.DisplayAlertAsync("Error", $"Failed to load dashboard data: {ex.Message}", "OK"); 
            }
            catch
            {
                // do nothing
            }
        }
        finally
        {
            IsBusy = false;
            App.Log("DashboardViewModel: LoadDashboardData end");
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
}

public class ActivityItem
{
    public string Icon { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Timestamp { get; set; } = "";
} 
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmScout.Models;
using FarmScout.Services;

namespace FarmScout.ViewModels;

public partial class TasksViewModel(IFarmScoutDatabase database) : ObservableObject
{
    public ObservableCollection<TaskViewModel> Tasks { get; } = [];

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string Title { get; set; } = "Tasks";

    [RelayCommand]
    public async Task LoadTasks()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            var observations = await database.GetObservationsAsync();
            var allTasks = new List<TaskViewModel>();

            foreach (var obs in observations)
            {
                var tasks = await database.GetTasksForObservationAsync(obs.Id);
                foreach (var task in tasks)
                {
                    allTasks.Add(new TaskViewModel(task, obs));
                }
            }

            Tasks.Clear();
            foreach (var task in allTasks.OrderByDescending(t => t.TaskItem.Id))
            {
                Tasks.Add(task);
            }
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlert("Error", "Failed to load tasks", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task UpdateTaskStatus(TaskViewModel? taskVM)
    {
        if (taskVM == null) return;

        try
        {
            await database.UpdateTaskAsync(taskVM.TaskItem);
            await LoadTasks(); // Refresh to update visual state
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlert("Error", "Failed to update task status", "OK");
        }
    }

    [RelayCommand]
    private async Task DeleteTask(TaskViewModel? taskVM)
    {
        if (taskVM == null) return;

        var result = await Shell.Current.DisplayAlert("Confirm Delete", 
            "Are you sure you want to delete this task?", "Yes", "No");
        
        if (result)
        {
            try
            {
                IsBusy = true;
                await database.DeleteTaskAsync(taskVM.TaskItem);
                await LoadTasks();
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlert("Error", "Failed to delete task", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    [RelayCommand]
    private async Task Refresh()
    {
        await LoadTasks();
    }

    // Additional commands for the new UI
    public ICommand CompleteTaskCommand => UpdateTaskStatusCommand;
}

public class TaskViewModel(TaskItem taskItem, Observation observation)
{
    public TaskItem TaskItem { get; } = taskItem;
    public Observation Observation { get; } = observation;
    public string ObservationInfo 
    {
        get
        {
            var types = ObservationTypes.SplitTypes(Observation.ObservationTypes);
            var typeText = types.Count > 0 ? string.Join(", ", types) : "No type specified";
            return $"From: {typeText}";
        }
    }
    public string TimestampText => Observation.Timestamp.ToString("MMM dd, yyyy");
    public TextDecorations TextDecoration => TaskItem.IsCompleted ? TextDecorations.Strikethrough : TextDecorations.None;
} 
using System.Collections.ObjectModel;
using System.Windows.Input;
using FarmScout.Models;
using FarmScout.Services;

namespace FarmScout.ViewModels;

public class TasksViewModel : BaseViewModel
{
    private readonly FarmScoutDatabase _database;

    public TasksViewModel(FarmScoutDatabase database)
    {
        _database = database;
        Title = "Tasks";

        Tasks = new ObservableCollection<TaskViewModel>();
        
        LoadTasksCommand = new Command(async () => await LoadTasks());
        UpdateTaskStatusCommand = new Command<TaskViewModel>(async (task) => await UpdateTaskStatus(task));
        DeleteTaskCommand = new Command<TaskViewModel>(async (task) => await DeleteTask(task));
        RefreshCommand = new Command(async () => await LoadTasks());
        
        // Additional commands for the new UI
        CompleteTaskCommand = UpdateTaskStatusCommand;
    }

    public ObservableCollection<TaskViewModel> Tasks { get; }

    public ICommand LoadTasksCommand { get; }
    public ICommand UpdateTaskStatusCommand { get; }
    public ICommand DeleteTaskCommand { get; }
    public ICommand RefreshCommand { get; }
    
    // Additional commands for the new UI
    public ICommand CompleteTaskCommand { get; }

    public async Task LoadTasks()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            var observations = await _database.GetObservationsAsync();
            var allTasks = new List<TaskViewModel>();

            foreach (var obs in observations)
            {
                var tasks = await _database.GetTasksForObservationAsync(obs.Id);
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

    public async Task UpdateTaskStatus(TaskViewModel? taskVM)
    {
        if (taskVM == null) return;

        try
        {
            await _database.UpdateTaskAsync(taskVM.TaskItem);
            await LoadTasks(); // Refresh to update visual state
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlert("Error", "Failed to update task status", "OK");
        }
    }

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
                await _database.DeleteTaskAsync(taskVM.TaskItem);
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
}

public class TaskViewModel
{
    public TaskItem TaskItem { get; }
    public Observation Observation { get; }
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

    public TaskViewModel(TaskItem taskItem, Observation observation)
    {
        TaskItem = taskItem;
        Observation = observation;
    }
} 
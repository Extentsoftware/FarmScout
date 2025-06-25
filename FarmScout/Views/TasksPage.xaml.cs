using FarmScout.ViewModels;

namespace FarmScout.Views;

public partial class TasksPage : ContentPage
{
    public TasksPage(TasksViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        App.Log("TasksPage ViewModel set from constructor injection");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is TasksViewModel viewModel)
        {
            await viewModel.LoadTasks();
        }
    }

    private async void OnTaskStatusChanged(object sender, CheckedChangedEventArgs e)
    {
        if (sender is CheckBox checkBox && checkBox.BindingContext is TaskViewModel taskVM)
        {
            if (BindingContext is TasksViewModel viewModel)
            {
                await viewModel.UpdateTaskStatus(taskVM);
            }
        }
    }
} 
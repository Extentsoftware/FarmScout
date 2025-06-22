using Microsoft.Extensions.DependencyInjection;
using System.Windows.Input;

namespace FarmScout;

public partial class AppShell : Shell
{
	private static readonly string LogFilePath = Path.Combine(FileSystem.AppDataDirectory, "startup.log");

	public AppShell()
	{
		App.Log("AppShell constructor start");
		try
		{
			InitializeComponent();
			App.Log("AppShell after InitializeComponent");
			
			// Register routes for navigation
			App.Log("Registering routes");

			Routing.RegisterRoute("AddObservation", typeof(Views.AddObservationPage));
			Routing.RegisterRoute("Observations", typeof(Views.ObservationsPage));
			Routing.RegisterRoute("Tasks", typeof(Views.TasksPage));
			Routing.RegisterRoute("ObservationDetail", typeof(Views.ObservationDetailPage));
			Routing.RegisterRoute("EditObservation", typeof(Views.EditObservationPage));
			
			// Set the binding context for menu commands
			BindingContext = this;
			
			App.Log("AppShell constructor complete");
		}
		catch (Exception ex)
		{
			App.Log($"AppShell constructor exception: {ex}");
			throw;
		}
	}

	// Navigation commands for menu items
	public ICommand AddObservationCommand => new Command(async () => await GoToAsync("AddObservation"));
	public ICommand ViewObservationsCommand => new Command(async () => await GoToAsync("Observations"));
	public ICommand ViewTasksCommand => new Command(async () => await GoToAsync("Tasks"));

	private static void Log(string message)
	{
		try
		{
			File.AppendAllText(LogFilePath, $"[{DateTime.Now:O}] {message}\n");
		}
		catch { }
	}
}

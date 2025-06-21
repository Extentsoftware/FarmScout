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
			Routing.RegisterRoute("Dashboard", typeof(Views.DashboardPage));
			Routing.RegisterRoute("AddObservation", typeof(Views.AddObservationPage));
			Routing.RegisterRoute("Observations", typeof(Views.ObservationsPage));
			Routing.RegisterRoute("Tasks", typeof(Views.TasksPage));
			Routing.RegisterRoute("ObservationDetail", typeof(Views.ObservationDetailPage));
			
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

	// Factory method for creating DashboardPage with DI
	public Views.DashboardPage CreateDashboardPage()
	{
		return Handler?.MauiContext?.Services?.GetRequiredService<Views.DashboardPage>() 
			?? throw new InvalidOperationException("Services not available");
	}

	// Factory method for creating other pages with DI
	public Views.AddObservationPage CreateAddObservationPage()
	{
		return Handler?.MauiContext?.Services?.GetRequiredService<Views.AddObservationPage>() 
			?? throw new InvalidOperationException("Services not available");
	}

	public Views.ObservationsPage CreateObservationsPage()
	{
		return Handler?.MauiContext?.Services?.GetRequiredService<Views.ObservationsPage>() 
			?? throw new InvalidOperationException("Services not available");
	}

	public Views.TasksPage CreateTasksPage()
	{
		return Handler?.MauiContext?.Services?.GetRequiredService<Views.TasksPage>() 
			?? throw new InvalidOperationException("Services not available");
	}

	private static void Log(string message)
	{
		try
		{
			File.AppendAllText(LogFilePath, $"[{DateTime.Now:O}] {message}\n");
		}
		catch { }
	}
}

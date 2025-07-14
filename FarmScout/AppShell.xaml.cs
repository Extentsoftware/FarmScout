using System.Windows.Input;

namespace FarmScout;

public partial class AppShell : Shell
{
	public AppShell()
	{
		App.Log("AppShell constructor start");
		try
		{
			InitializeComponent();
			App.Log("AppShell after InitializeComponent");
			
			// Register routes for navigation
			App.Log("Registering routes");

			Routing.RegisterRoute("Observations", typeof(Views.ObservationsPage));
			Routing.RegisterRoute("Tasks", typeof(Views.TasksPage));
			Routing.RegisterRoute("Observation", typeof(Views.ObservationPage));
			Routing.RegisterRoute("LookupPage", typeof(Views.LookupPage));
            Routing.RegisterRoute("LookupItemPage", typeof(Views.LookupItemPage));
            Routing.RegisterRoute("ObservationTypesPage", typeof(Views.ObservationTypesPage));
            Routing.RegisterRoute("ObservationTypeEditPage", typeof(Views.ObservationTypeEditPage));
            

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
	public ICommand AddObservationCommand => new Command(async () => await GoToAsync("Observation", new Dictionary<string, object> { { "Mode", "add" } }));
    public ICommand ViewObservationsCommand => new Command(async () => await GoToAsync("Observations"));
	public ICommand ViewTasksCommand => new Command(async () => await GoToAsync("Tasks"));
	public ICommand ViewLookupTablesCommand => new Command(async () => await GoToAsync("LookupPage"));

}

using FarmScout.Services;
using FarmScout.ViewModels;
using FarmScout.Views;

namespace FarmScout;

public partial class App : Application
{
	private static readonly string LogFilePath = Path.Combine(FileSystem.AppDataDirectory, "startup.log");

	public App()
	{
		Log("App constructor start");
		AppDomain.CurrentDomain.UnhandledException += (s, e) =>
		{
			Log($"UnhandledException: {e.ExceptionObject}");
		};
		TaskScheduler.UnobservedTaskException += (s, e) =>
		{
			Log($"UnobservedTaskException: {e.Exception}");
		};
#if WINDOWS
		try
		{
			Microsoft.UI.Xaml.Application.Current.UnhandledException += (s, e) =>
			{
				Log($"DispatcherUnhandledException: {e.Exception}");
			};
		}
        catch 
        {
            // do nothing
        }
#endif
        InitializeComponent();
		Log("App InitializeComponent complete");
		Log("App constructor complete");
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		Log("CreateWindow called");
		
		try
		{
			Log("Creating AppShell and Window");
			var appShell = new AppShell();
			
			// Register the AppShell as a resource for XAML access
			Resources["AppShell"] = appShell;
			
			var window = new Window(appShell)
			{
				Title = "FarmScout"
			};
			
			Log("Window created successfully");
			return window;
		}
		catch (Exception ex)
		{
			Log($"Exception creating window: {ex}");
			// Fallback to simple ContentPage
			Log("Creating fallback ContentPage");
			var fallbackPage = new ContentPage
			{
				Content = new Label
				{
					Text = "FarmScout - Fallback Page\nApp loaded successfully!",
					HorizontalOptions = LayoutOptions.Center,
					VerticalOptions = LayoutOptions.Center
				}
			};
			var fallbackWindow = new Window(fallbackPage)
			{
				Title = "FarmScout - Fallback"
			};
			Log("Fallback window created");
			return fallbackWindow;
		}
	}

    private static object MyLock { get; } = new object();

    public static void Log(string message)
	{
		
		lock (MyLock)
		{
			try
			{
				File.AppendAllText(LogFilePath, $"[{DateTime.Now:O}] {message}\n");
			}
			catch
			{
				// do nothing
			}
		}
	}
}

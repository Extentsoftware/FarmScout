using Microsoft.Extensions.Logging;
using FarmScout.Services;
using FarmScout.ViewModels;
using FarmScout.Views;
using System.IO;

namespace FarmScout;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// Register services
		string dbPath = Path.Combine(FileSystem.AppDataDirectory, "farmscout.db3");
		builder.Services.AddSingleton(s => new FarmScoutDatabase(dbPath));
		builder.Services.AddSingleton<PhotoService>();
		builder.Services.AddSingleton<LocationService>();
		builder.Services.AddSingleton<ShapefileService>();
		builder.Services.AddSingleton<INavigationService, NavigationService>();

		// Register ViewModels
		builder.Services.AddTransient<DashboardViewModel>();
		builder.Services.AddTransient<AddObservationViewModel>();
		builder.Services.AddTransient<ObservationsViewModel>();
		builder.Services.AddTransient<TasksViewModel>();
		builder.Services.AddTransient<ObservationDetailViewModel>();

		// Register pages
		builder.Services.AddTransient<DashboardPage>();
		builder.Services.AddTransient<AddObservationPage>();
		builder.Services.AddTransient<ObservationsPage>();
		builder.Services.AddTransient<TasksPage>();
		builder.Services.AddTransient<ObservationDetailPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}

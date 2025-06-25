using Microsoft.Extensions.Logging;
using FarmScout.Services;
using FarmScout.ViewModels;
using FarmScout.Views;
using System.IO;
using Microsoft.Extensions.Logging.Abstractions;

namespace FarmScout;

public static class MauiProgram
{
	public static ServiceProvider Services { get; private set; } = default!;

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

		builder.Services.AddSingleton<FarmScoutDatabase>();
		builder.Services.AddSingleton<PhotoService>();
		builder.Services.AddSingleton<LocationService>();
		builder.Services.AddSingleton<ShapefileService>();
		builder.Services.AddSingleton<INavigationService, NavigationService>();

		// Register ViewModels
		builder.Services.AddTransient<DashboardViewModel>();
		builder.Services.AddTransient<ObservationsViewModel>();
		builder.Services.AddTransient<TasksViewModel>();
		builder.Services.AddTransient<ObservationViewModel>();

		// Register pages
		builder.Services.AddTransient<DashboardPage>();
		builder.Services.AddTransient<ObservationsPage>();
		builder.Services.AddTransient<TasksPage>();
		builder.Services.AddTransient<ObservationPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif


        Services = builder.Services.BuildServiceProvider();

        return builder.Build();
	}
}

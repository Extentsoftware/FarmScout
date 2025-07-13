using Microsoft.Extensions.Logging;
using FarmScout.Services;
using FarmScout.ViewModels;
using FarmScout.Views;

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

		builder.Services.AddSingleton<IFarmScoutDatabase, FarmScoutDatabase>();
		builder.Services.AddSingleton<PhotoService>();
		builder.Services.AddSingleton<LocationService>();
		builder.Services.AddSingleton<ShapefileService>();
        builder.Services.AddSingleton<INavigationService, NavigationService>();

        // Register ViewModels
        builder.Services.AddTransient<DashboardViewModel>();
		builder.Services.AddTransient<ObservationsViewModel>();
		builder.Services.AddTransient<TasksViewModel>();
		builder.Services.AddTransient<ObservationViewModel>();
		builder.Services.AddTransient<LookupViewModel>();
		builder.Services.AddTransient<LookupItemViewModel>();
        builder.Services.AddSingleton<ILookupPageFactory, LookupPageFactory>();

        // Register pages
        builder.Services.AddTransient<DashboardPage>();
		builder.Services.AddTransient<ObservationsPage>();
		builder.Services.AddTransient<TasksPage>();
		builder.Services.AddTransient<ObservationPage>();
		builder.Services.AddTransient<LookupPage>();
		builder.Services.AddTransient<LookupItemPage>();

		// Register converters
		builder.Services.AddSingleton<Converters.BoolToColorConverter>();
		builder.Services.AddSingleton<Converters.BoolToStringConverter>();
		builder.Services.AddSingleton<Converters.NotNullConverter>();
		builder.Services.AddSingleton<Converters.StringContainsConverter>();
		builder.Services.AddSingleton<Converters.StringToBoolConverter>();
		builder.Services.AddSingleton<Converters.GroupIconConverter>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

        Services = builder.Services.BuildServiceProvider();

        var app = builder.Build();

		return app;

    }
    public static async Task DisplayAlertAsync(string title, string message, string cancel)
    {
        await Shell.Current.DisplayAlert(title, message, cancel);
    }

    public static async Task<bool> DisplayAlertAsync(string title, string message, string accept, string cancel)
    {
        return await Shell.Current.DisplayAlert(title, message, accept, cancel);
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmScout.Services;

namespace FarmScout.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IFarmScoutDatabase _database;
    private readonly INavigationService _navigationService;

    public SettingsViewModel(IFarmScoutDatabase database, INavigationService navigationService)
    {
        _database = database;
        _navigationService = navigationService;
    }

    [ObservableProperty]
    private bool _isLoading = false;

    [RelayCommand]
    private async Task NavigateToObservationTypes()
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;
            await _navigationService.NavigateToAsync("//ObservationTypesPage");
        }
        catch (Exception ex)
        {
            await MauiProgram.DisplayAlertAsync("Error", $"Failed to navigate to observation types: {ex.Message}", "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task NavigateToLookups()
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;
            await _navigationService.NavigateToAsync("//LookupPage");
        }
        catch (Exception ex)
        {
            await MauiProgram.DisplayAlertAsync("Error", $"Failed to navigate to lookups: {ex.Message}", "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ExportData()
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;
            
            // TODO: Implement data export functionality
            await MauiProgram.DisplayAlertAsync("Info", "Data export functionality will be implemented in a future update.", "OK");
        }
        catch (Exception ex)
        {
            await MauiProgram.DisplayAlertAsync("Error", $"Failed to export data: {ex.Message}", "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ResetDatabase()
    {
        if (IsLoading) return;

        var confirmed = await MauiProgram.DisplayAlertAsync(
            "Reset Database", 
            "This will delete all data and reset the database to its initial state. This action cannot be undone. Are you sure?",
            "Yes, Reset",
            "Cancel");

        if (!confirmed) return;

        try
        {
            IsLoading = true;
            
            // TODO: Implement database reset functionality
            await MauiProgram.DisplayAlertAsync("Info", "Database reset functionality will be implemented in a future update.", "OK");
        }
        catch (Exception ex)
        {
            await MauiProgram.DisplayAlertAsync("Error", $"Failed to reset database: {ex.Message}", "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }
} 
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmScout.Services;

namespace FarmScout.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IFarmScoutDatabase _database;
    private readonly INavigationService _navigationService;

    public SettingsViewModel(IFarmScoutDatabase database, INavigationService navigationService, IDatabaseResetService resetService)
    {
        _database = database;
        _navigationService = navigationService;
        _resetService = resetService;
    }

    private readonly IDatabaseResetService _resetService;

    [ObservableProperty]
    private bool _isLoading = false;

    [RelayCommand]
    private async Task NavigateToObservationTypes()
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;
            await _navigationService.NavigateToAsync("ObservationTypesPage");
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
            await _navigationService.NavigateToAsync("LookupPage");
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

        try
        {
            IsLoading = true;
            await _navigationService.NavigateToAsync("DatabaseResetPage");
        }
        catch (Exception ex)
        {
            await MauiProgram.DisplayAlertAsync("Error", $"Failed to navigate to database reset page: {ex.Message}", "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }
} 
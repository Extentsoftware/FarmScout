using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmScout.Models;
using FarmScout.Services;
using System.Collections.ObjectModel;

namespace FarmScout.ViewModels;

public partial class ObservationTypesViewModel : ObservableObject
{
    private readonly IFarmScoutDatabase _database;
    private readonly INavigationService _navigationService;

    public ObservationTypesViewModel(IFarmScoutDatabase database, INavigationService navigationService)
    {
        _database = database;
        _navigationService = navigationService;
        ObservationTypes = new ObservableCollection<ObservationTypeViewModel>();
    }

    [ObservableProperty]
    private bool _isLoading = false;

    public ObservableCollection<ObservationTypeViewModel> ObservationTypes { get; }

    [RelayCommand]
    private async Task LoadObservationTypes()
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;
            ObservationTypes.Clear();

            var types = await _database.GetObservationTypesAsync();
            foreach (var type in types.OrderBy(t => t.SortOrder).ThenBy(t => t.Name))
            {
                var dataPoints = await _database.GetDataPointsForObservationTypeAsync(type.Id);
                var viewModel = new ObservationTypeViewModel(type)
                {
                    DataPointsCount = dataPoints.Count
                };
                ObservationTypes.Add(viewModel);
            }
        }
        catch (Exception ex)
        {
            await MauiProgram.DisplayAlertAsync("Error", $"Failed to load observation types: {ex.Message}", "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AddObservationType()
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;
            await _navigationService.NavigateToAsync("//ObservationTypeEditPage");
        }
        catch (Exception ex)
        {
            await MauiProgram.DisplayAlertAsync("Error", $"Failed to navigate to add observation type: {ex.Message}", "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task EditObservationType(ObservationTypeViewModel observationType)
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;
            var parameters = new Dictionary<string, object>
            {
                { "ObservationTypeId", observationType.Id }
            };
            await _navigationService.NavigateToAsync("//ObservationTypeEditPage", parameters);
        }
        catch (Exception ex)
        {
            await MauiProgram.DisplayAlertAsync("Error", $"Failed to navigate to edit observation type: {ex.Message}", "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeleteObservationType(ObservationTypeViewModel observationType)
    {
        if (IsLoading) return;

        var confirmed = await MauiProgram.DisplayAlertAsync(
            "Delete Observation Type", 
            $"Are you sure you want to delete '{observationType.Name}'? This will also delete all associated data points and cannot be undone.",
            "Yes, Delete",
            "Cancel");

        if (!confirmed) return;

        try
        {
            IsLoading = true;

            // Check if this observation type is used in any observations
            // TODO: Add method to check if observation type is in use
            // var isInUse = await _database.IsObservationTypeInUseAsync(observationType.Id);
            // if (isInUse)
            // {
            //     await MauiProgram.DisplayAlertAsync("Cannot Delete", "This observation type is currently in use by existing observations and cannot be deleted.", "OK");
            //     return;
            // }

            // Delete the observation type (this should cascade delete data points)
            await _database.DeleteObservationTypeAsync(observationType.ObservationType);
            
            // Remove from the collection
            ObservationTypes.Remove(observationType);

            await MauiProgram.DisplayAlertAsync("Success", "Observation type deleted successfully.", "OK");
        }
        catch (Exception ex)
        {
            await MauiProgram.DisplayAlertAsync("Error", $"Failed to delete observation type: {ex.Message}", "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task OnAppearing()
    {
        await LoadObservationTypes();
    }
} 
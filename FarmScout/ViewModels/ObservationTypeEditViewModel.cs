using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmScout.Models;
using FarmScout.Services;
using System.Collections.ObjectModel;

namespace FarmScout.ViewModels;

public partial class ObservationTypeEditViewModel : ObservableObject
{
    private readonly IFarmScoutDatabase _database;
    private readonly INavigationService _navigationService;
    private Guid? _observationTypeId;

    public ObservationTypeEditViewModel(IFarmScoutDatabase database, INavigationService navigationService)
    {
        _database = database;
        _navigationService = navigationService;
        DataPoints = new ObservableCollection<ObservationTypeDataPoint>();
    }

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private bool _isNew = true;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _icon = string.Empty;

    [ObservableProperty]
    private string _color = "#607D8B";

    [ObservableProperty]
    private int _sortOrder = 0;

    [ObservableProperty]
    private bool _isActive = true;

    public ObservableCollection<ObservationTypeDataPoint> DataPoints { get; }

    [RelayCommand]
    private async Task Save()
    {
        if (IsLoading) return;

        if (string.IsNullOrWhiteSpace(Name))
        {
            await MauiProgram.DisplayAlertAsync("Validation Error", "Name is required.", "OK");
            return;
        }

        try
        {
            IsLoading = true;

            var observationType = new ObservationType
            {
                Id = _observationTypeId ?? Guid.NewGuid(),
                Name = Name.Trim(),
                Description = Description?.Trim() ?? string.Empty,
                Icon = Icon?.Trim() ?? string.Empty,
                Color = Color?.Trim() ?? "#607D8B",
                SortOrder = SortOrder,
                IsActive = IsActive,
                UpdatedAt = DateTime.Now
            };

            if (IsNew)
            {
                observationType.CreatedAt = DateTime.Now;
                await _database.AddObservationTypeAsync(observationType);
            }
            else
            {
                await _database.UpdateObservationTypeAsync(observationType);
            }

            // Save data points
            foreach (var dataPoint in DataPoints)
            {
                dataPoint.ObservationTypeId = observationType.Id;
                dataPoint.UpdatedAt = DateTime.Now;

                if (dataPoint.Id == Guid.Empty)
                {
                    dataPoint.Id = Guid.NewGuid();
                    dataPoint.CreatedAt = DateTime.Now;
                    await _database.AddObservationTypeDataPointAsync(dataPoint);
                }
                else
                {
                    await _database.UpdateObservationTypeDataPointAsync(dataPoint);
                }
            }

            await MauiProgram.DisplayAlertAsync("Success", "Observation type saved successfully.", "OK");
            await _navigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            await MauiProgram.DisplayAlertAsync("Error", $"Failed to save observation type: {ex.Message}", "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Cancel()
    {
        await _navigationService.GoBackAsync();
    }

    [RelayCommand]
    private async Task AddDataPoint()
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;
            await _navigationService.NavigateToAsync("//DataPointEditPage", new Dictionary<string, object>
            {
                { "ObservationTypeId", _observationTypeId ?? Guid.Empty }
            });
        }
        catch (Exception ex)
        {
            await MauiProgram.DisplayAlertAsync("Error", $"Failed to navigate to add data point: {ex.Message}", "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task EditDataPoint(ObservationTypeDataPoint dataPoint)
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;
            await _navigationService.NavigateToAsync("//DataPointEditPage", new Dictionary<string, object>
            {
                { "DataPointId", dataPoint.Id }
            });
        }
        catch (Exception ex)
        {
            await MauiProgram.DisplayAlertAsync("Error", $"Failed to navigate to edit data point: {ex.Message}", "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeleteDataPoint(ObservationTypeDataPoint dataPoint)
    {
        if (IsLoading) return;

        var confirmed = await MauiProgram.DisplayAlertAsync(
            "Delete Data Point", 
            $"Are you sure you want to delete '{dataPoint.Label}'?",
            "Yes, Delete",
            "Cancel");

        if (!confirmed) return;

        try
        {
            IsLoading = true;

            if (dataPoint.Id != Guid.Empty)
            {
                await _database.DeleteObservationTypeDataPointAsync(dataPoint);
            }

            DataPoints.Remove(dataPoint);
            await MauiProgram.DisplayAlertAsync("Success", "Data point deleted successfully.", "OK");
        }
        catch (Exception ex)
        {
            await MauiProgram.DisplayAlertAsync("Error", $"Failed to delete data point: {ex.Message}", "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task OnAppearing()
    {
        await LoadObservationType();
    }

    private async Task LoadObservationType()
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;

            // Check if we have an observation type ID from navigation
            if (Shell.Current.CurrentState.Location.ToString().Contains("ObservationTypeId="))
            {
                var parameters = Shell.Current.CurrentState.Location.ToString()
                    .Split('?')[1]
                    .Split('&')
                    .ToDictionary(p => p.Split('=')[0], p => p.Split('=')[1]);

                if (parameters.ContainsKey("ObservationTypeId") && Guid.TryParse(parameters["ObservationTypeId"], out var id))
                {
                    _observationTypeId = id;
                    IsNew = false;

                    var observationType = await _database.GetObservationTypeByIdAsync(id);
                    if (observationType != null)
                    {
                        Name = observationType.Name;
                        Description = observationType.Description;
                        Icon = observationType.Icon;
                        Color = observationType.Color;
                        SortOrder = observationType.SortOrder;
                        IsActive = observationType.IsActive;

                        // Load data points
                        var dataPoints = await _database.GetDataPointsForObservationTypeAsync(id);
                        DataPoints.Clear();
                        foreach (var dataPoint in dataPoints.OrderBy(dp => dp.SortOrder))
                        {
                            DataPoints.Add(dataPoint);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await MauiProgram.DisplayAlertAsync("Error", $"Failed to load observation type: {ex.Message}", "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }
} 
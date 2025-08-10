using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmScout.Models;
using FarmScout.Services;
using System.Collections.ObjectModel;

namespace FarmScout.ViewModels;

[QueryProperty(nameof(ObservationTypeId), "ObservationTypeId")] 
public partial class ObservationTypeEditViewModel(IFarmScoutDatabase database, INavigationService navigationService) : ObservableObject
{
    public Guid ObservationTypeId { get; set; } = Guid.Empty;

    [ObservableProperty]
    public partial bool IsLoading { get; set; } = false;

    [ObservableProperty]
    public partial bool IsNew { get; set; } = true;

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Icon { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Color { get; set; } = "#000000";

    [ObservableProperty]
    public partial int SortOrder { get; set; } = 0;

    [ObservableProperty]
    public partial bool IsActive { get; set; } = true;

    public ObservableCollection<ObservationTypeDataPoint> DataPoints { get; } = [];

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
                Id = ObservationTypeId,
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
                await database.AddObservationTypeAsync(observationType);
            }
            else
            {
                await database.UpdateObservationTypeAsync(observationType);
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
                    await database.AddObservationTypeDataPointAsync(dataPoint);
                }
                else
                {
                    await database.UpdateObservationTypeDataPointAsync(dataPoint);
                }
            }

            await MauiProgram.DisplayAlertAsync("Success", "Observation type saved successfully.", "OK");
            await navigationService.GoBackAsync();
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
        await navigationService.GoBackAsync();
    }

    [RelayCommand]
    private async Task AddDataPoint()
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;
            await navigationService.NavigateToAsync("DataPointEditPage", new Dictionary<string, object>
            {
                { "ObservationTypeId", ObservationTypeId }
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
            await navigationService.NavigateToAsync("DataPointEditPage", new Dictionary<string, object>
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
                await database.DeleteObservationTypeDataPointAsync(dataPoint);
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
            if (ObservationTypeId != Guid.Empty)
            {
                IsNew = false;

                var observationType = await database.GetObservationTypeByIdAsync(ObservationTypeId);
                if (observationType != null)
                {
                    Name = observationType.Name;
                    Description = observationType.Description;
                    Icon = observationType.Icon;
                    Color = observationType.Color;
                    SortOrder = observationType.SortOrder;
                    IsActive = observationType.IsActive;

                    // Load data points
                    var dataPoints = await database.GetDataPointsForObservationTypeAsync(ObservationTypeId);
                    DataPoints.Clear();
                    foreach (var dataPoint in dataPoints.OrderBy(dp => dp.SortOrder))
                    {
                        DataPoints.Add(dataPoint);
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
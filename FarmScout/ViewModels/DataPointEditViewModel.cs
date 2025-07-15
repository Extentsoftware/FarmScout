using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmScout.Models;
using FarmScout.Services;
using System.Collections.ObjectModel;

namespace FarmScout.ViewModels;

public partial class DataPointEditViewModel : ObservableObject
{
    private readonly IFarmScoutDatabase _database;
    private readonly INavigationService _navigationService;
    private Guid? _dataPointId;
    private Guid? _observationTypeId;

    public DataPointEditViewModel(IFarmScoutDatabase database, INavigationService navigationService)
    {
        _database = database;
        _navigationService = navigationService;
        AvailableDataTypes = new ObservableCollection<string>(DataTypes.AvailableTypes);
        AvailableLookupGroups = new ObservableCollection<string>();
    }

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private bool _isNew = true;

    [ObservableProperty]
    private string _code = string.Empty;

    [ObservableProperty]
    private string _label = string.Empty;

    [ObservableProperty]
    private string _selectedDataType = DataTypes.String;

    [ObservableProperty]
    private string _selectedLookupGroup = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private int _sortOrder = 0;

    [ObservableProperty]
    private bool _isRequired = false;

    [ObservableProperty]
    private bool _isActive = true;

    public ObservableCollection<string> AvailableDataTypes { get; }
    public ObservableCollection<string> AvailableLookupGroups { get; }

    public bool IsLookupType => SelectedDataType == DataTypes.Lookup;

    partial void OnSelectedDataTypeChanged(string value)
    {
        OnPropertyChanged(nameof(IsLookupType));
    }

    [RelayCommand]
    private async Task Save()
    {
        if (IsLoading) return;

        if (string.IsNullOrWhiteSpace(Code))
        {
            await MauiProgram.DisplayAlertAsync("Validation Error", "Code is required.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(Label))
        {
            await MauiProgram.DisplayAlertAsync("Validation Error", "Label is required.", "OK");
            return;
        }

        if (SelectedDataType == DataTypes.Lookup && string.IsNullOrWhiteSpace(SelectedLookupGroup))
        {
            await MauiProgram.DisplayAlertAsync("Validation Error", "Lookup group is required for lookup data type.", "OK");
            return;
        }

        try
        {
            IsLoading = true;

            var dataPoint = new ObservationTypeDataPoint
            {
                Id = _dataPointId ?? Guid.NewGuid(),
                ObservationTypeId = _observationTypeId ?? Guid.Empty,
                Code = Code.Trim(),
                Label = Label.Trim(),
                DataType = SelectedDataType,
                LookupGroupName = SelectedDataType == DataTypes.Lookup ? SelectedLookupGroup : string.Empty,
                Description = Description?.Trim() ?? string.Empty,
                SortOrder = SortOrder,
                IsRequired = IsRequired,
                IsActive = IsActive,
                UpdatedAt = DateTime.Now
            };

            if (IsNew)
            {
                dataPoint.CreatedAt = DateTime.Now;
                await _database.AddObservationTypeDataPointAsync(dataPoint);
            }
            else
            {
                await _database.UpdateObservationTypeDataPointAsync(dataPoint);
            }

            await MauiProgram.DisplayAlertAsync("Success", "Data point saved successfully.", "OK");
            await _navigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            await MauiProgram.DisplayAlertAsync("Error", $"Failed to save data point: {ex.Message}", "OK");
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

    public async Task OnAppearing()
    {
        await LoadDataPoint();
        await LoadLookupGroups();
    }

    private async Task LoadDataPoint()
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;

            // Check if we have a data point ID from navigation
            if (Shell.Current.CurrentState.Location.ToString().Contains("DataPointId="))
            {
                var parameters = Shell.Current.CurrentState.Location.ToString()
                    .Split('?')[1]
                    .Split('&')
                    .ToDictionary(p => p.Split('=')[0], p => p.Split('=')[1]);

                if (parameters.ContainsKey("DataPointId") && Guid.TryParse(parameters["DataPointId"], out var id))
                {
                    _dataPointId = id;
                    IsNew = false;

                    var dataPoint = await _database.GetDataPointByIdAsync(id);
                    if (dataPoint != null)
                    {
                        Code = dataPoint.Code;
                        Label = dataPoint.Label;
                        SelectedDataType = dataPoint.DataType;
                        SelectedLookupGroup = dataPoint.LookupGroupName;
                        Description = dataPoint.Description;
                        SortOrder = dataPoint.SortOrder;
                        IsRequired = dataPoint.IsRequired;
                        IsActive = dataPoint.IsActive;
                        _observationTypeId = dataPoint.ObservationTypeId;
                    }
                }
            }
            else if (Shell.Current.CurrentState.Location.ToString().Contains("ObservationTypeId="))
            {
                var parameters = Shell.Current.CurrentState.Location.ToString()
                    .Split('?')[1]
                    .Split('&')
                    .ToDictionary(p => p.Split('=')[0], p => p.Split('=')[1]);

                if (parameters.ContainsKey("ObservationTypeId") && Guid.TryParse(parameters["ObservationTypeId"], out var id))
                {
                    _observationTypeId = id;
                    IsNew = true;
                }
            }
        }
        catch (Exception ex)
        {
            await MauiProgram.DisplayAlertAsync("Error", $"Failed to load data point: {ex.Message}", "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadLookupGroups()
    {
        try
        {
            var groups = await _database.GetLookupGroupsAsync();
            AvailableLookupGroups.Clear();
            foreach (var group in groups.OrderBy(g => g.Name))
            {
                AvailableLookupGroups.Add(group.Name);
            }
        }
        catch (Exception ex)
        {
            App.Log($"Error loading lookup groups: {ex.Message}");
        }
    }
} 
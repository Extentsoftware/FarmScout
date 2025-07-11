using FarmScout.Models;
using FarmScout.Services;
using System.Windows.Input;

namespace FarmScout.Controls;

public partial class ObservationTypeControl : ContentView
{
    private readonly IFarmScoutDatabase _database;
    private readonly Dictionary<Guid, object> _values = new(); // Changed to use DataPointId as key
    private readonly Dictionary<string, Guid> _codeToIdMap = new(); // Map data point codes to IDs
    
    public static readonly BindableProperty ObservationTypeIdProperty =
        BindableProperty.Create(nameof(ObservationTypeId), typeof(Guid), typeof(ObservationTypeControl), Guid.Empty, propertyChanged: OnObservationTypeIdChanged);
    
    public static readonly BindableProperty ValuesChangedCommandProperty =
        BindableProperty.Create(nameof(ValuesChangedCommand), typeof(ICommand), typeof(ObservationTypeControl), null);
    
    public Guid ObservationTypeId
    {
        get => (Guid)GetValue(ObservationTypeIdProperty);
        set => SetValue(ObservationTypeIdProperty, value);
    }
    
    public ICommand ValuesChangedCommand
    {
        get => (ICommand)GetValue(ValuesChangedCommandProperty);
        set => SetValue(ValuesChangedCommandProperty, value);
    }
    
    public event EventHandler<Dictionary<Guid, object>>? ValuesChanged;

    private static async void OnObservationTypeIdChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is ObservationTypeControl control && newValue is Guid observationTypeId && observationTypeId != Guid.Empty)
        {
            await control.LoadObservationTypeAsync(observationTypeId);
        }
    }

    public ObservationTypeControl()
    {
        InitializeComponent();
        _database = MauiProgram.Services.GetRequiredService<IFarmScoutDatabase>();
    }

    public async Task LoadObservationTypeAsync(Guid observationTypeId)
    {
        try
        {
            MainLayout.Children.Clear();
            _values.Clear();

            var dataPoints = await _database.GetDataPointsForObservationTypeAsync(observationTypeId);
            
            foreach (var dataPoint in dataPoints.OrderBy(dp => dp.SortOrder))
            {
                _codeToIdMap[dataPoint.Code] = dataPoint.Id; // Store the mapping

                Application.Current?.Dispatcher.Dispatch(() =>
                {
                    var control = CreateControlForDataPoint(dataPoint);
                    if (control != null)
                    {
                        MainLayout.Children.Add(control);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            App.Log($"Error loading observation type control: {ex.Message}");
            await Shell.Current.DisplayAlert("Error", "Failed to load observation type configuration", "OK");
        }
    }

    private View? CreateControlForDataPoint(ObservationTypeDataPoint dataPoint)
    {
        return dataPoint.DataType switch
        {
            DataTypes.String => CreateStringControl(dataPoint),
            DataTypes.Long => CreateLongControl(dataPoint),
            DataTypes.Lookup => CreateLookupControl(dataPoint),
            _ => null
        };
    }

    private View CreateStringControl(ObservationTypeDataPoint dataPoint)
    {
        var label = new Label
        {
            TextColor= Colors.Black,
            Text = dataPoint.Label,
            FontAttributes = FontAttributes.Bold,
            Margin = new Thickness(0, 5, 0, 5)
        };

        var entry = new Entry
        {
            TextColor = Colors.Black,
            Placeholder = $"Enter {dataPoint.Label.ToLower()}",
            Margin = new Thickness(0, 0, 0, 10)
        };

        entry.TextChanged += (sender, e) =>
        {
            _values[dataPoint.Id] = e.NewTextValue;
            ValuesChanged?.Invoke(this, _values);
            ValuesChangedCommand?.Execute(_values);
        };

        return new VerticalStackLayout
        {
            Children = { label, entry }
        };
    }

    private View CreateLongControl(ObservationTypeDataPoint dataPoint)
    {
        var label = new Label
        {
            TextColor = Colors.Black,
            Text = dataPoint.Label,
            FontAttributes = FontAttributes.Bold,
            Margin = new Thickness(0, 5, 0, 5)
        };

        var entry = new Entry
        {
            TextColor = Colors.Black,
            Placeholder = $"Enter {dataPoint.Label.ToLower()}",
            Keyboard = Keyboard.Numeric,
            Margin = new Thickness(0, 0, 0, 10)
        };

        entry.TextChanged += (sender, e) =>
        {
            if (long.TryParse(e.NewTextValue, out var value))
            {
                _values[dataPoint.Id] = value;
            }
            else if (string.IsNullOrEmpty(e.NewTextValue))
            {
                _values.Remove(dataPoint.Id);
            }
            ValuesChanged?.Invoke(this, _values);
            ValuesChangedCommand?.Execute(_values);
        };

        return new VerticalStackLayout
        {
            Children = { label, entry }
        };
    }

    private View CreateLookupControl(ObservationTypeDataPoint dataPoint)
    {
        var label = new Label
        {
            TextColor = Colors.Black,
            Text = dataPoint.Label,
            FontAttributes = FontAttributes.Bold,
            Margin = new Thickness(0, 5, 0, 5)
        };

        var picker = new Picker
        {
            TextColor = Colors.Black,
            Title = $"Select {dataPoint.Label.ToLower()}",
            Margin = new Thickness(0, 0, 0, 10)
        };

        // Load lookup items asynchronously
        _ = LoadLookupItemsAsync(picker, dataPoint.LookupGroupName);

        picker.SelectedIndexChanged += (sender, e) =>
        {
            if (picker.SelectedItem is LookupItem selectedItem)
            {
                _values[dataPoint.Id] = selectedItem.Name;
            }
            else
            {
                _values.Remove(dataPoint.Id);
            }
            ValuesChanged?.Invoke(this, _values);
            ValuesChangedCommand?.Execute(_values);
        };

        return new VerticalStackLayout
        {
            Children = { label, picker }
        };
    }

    private async Task LoadLookupItemsAsync(Picker picker, string groupName)
    {
        try
        {
            var items = await _database.GetLookupItemsByGroupAsync(groupName);
            picker.ItemsSource = items;
            picker.ItemDisplayBinding = new Binding("Name");
        }
        catch (Exception ex)
        {
            App.Log($"Error loading lookup items for group {groupName}: {ex.Message}");
        }
    }

    public Dictionary<Guid, object> GetValues()
    {
        return new Dictionary<Guid, object>(_values);
    }

    public void SetValues(Dictionary<Guid, object> values)
    {
        _values.Clear();
        foreach (var kvp in values)
        {
            _values[kvp.Key] = kvp.Value;
        }
        
        // Update UI controls with the values
        UpdateControlsWithValues();
    }

    private void UpdateControlsWithValues()
    {
        // This would need to be implemented to update the UI controls
        // with the current values when loading existing data
        // For now, we'll leave this as a placeholder
    }
} 
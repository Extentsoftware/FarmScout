using FarmScout.Models;
using FarmScout.Services;
using System.Windows.Input;

namespace FarmScout.Controls;

public partial class ObservationTypeControl : ContentView
{
    private readonly IFarmScoutDatabase _database;
    private readonly Dictionary<Guid, object> _values = []; // Changed to use DataPointId as key
    private readonly Dictionary<string, Guid> _codeToIdMap = []; // Map data point codes to IDs
    private readonly Dictionary<Guid, ControlReference> _controlReferences = [];
    
    private sealed class ControlReference
    {
        public Entry? Entry { get; set; }
        public Label? Label { get; set; }
        public Picker? Picker { get; set; }
    }
    
    public static readonly BindableProperty ObservationTypeIdProperty =
        BindableProperty.Create(nameof(ObservationTypeId), typeof(Guid), typeof(ObservationTypeControl), Guid.Empty, propertyChanged: OnObservationTypeIdChanged);
    
    public static readonly BindableProperty ValuesChangedCommandProperty =
        BindableProperty.Create(nameof(ValuesChangedCommand), typeof(ICommand), typeof(ObservationTypeControl), null);
    
    public static readonly BindableProperty IsEditableProperty =
        BindableProperty.Create(nameof(IsEditable), typeof(bool), typeof(ObservationTypeControl), true, propertyChanged: OnIsEditableChanged);
    
    public static readonly BindableProperty InitialValuesProperty =
        BindableProperty.Create(nameof(InitialValues), typeof(Dictionary<Guid, object>), typeof(ObservationTypeControl), null, propertyChanged: OnInitialValuesChanged);
    
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
    
    public bool IsEditable
    {
        get => (bool)GetValue(IsEditableProperty);
        set => SetValue(IsEditableProperty, value);
    }
    
    public Dictionary<Guid, object> InitialValues
    {
        get => (Dictionary<Guid, object>)GetValue(InitialValuesProperty);
        set => SetValue(InitialValuesProperty, value);
    }
    
    public event EventHandler<Dictionary<Guid, object>>? ValuesChanged;

    private static async void OnObservationTypeIdChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is ObservationTypeControl control && newValue is Guid observationTypeId && observationTypeId != Guid.Empty)
        {
            await control.LoadObservationTypeAsync(observationTypeId);
        }
    }
    
    private static void OnInitialValuesChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is ObservationTypeControl control && newValue is Dictionary<Guid, object> values)
        {
            control.SetValues(values);
        }
    }
    
    private static void OnIsEditableChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is ObservationTypeControl control && newValue is bool isEditable)
        {
            control.UpdateControlVisibility(isEditable);
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
            _controlReferences.Clear();

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

                    UpdateControlWithValues(dataPoint.Id);
                });
            }

            UpdateControlsWithValues();

        }
        catch (Exception ex)
        {
            App.Log($"Error loading observation type control: {ex.Message}");
            await MauiProgram.DisplayAlertAsync("Error", "Failed to load observation type configuration", "OK");
        }
    }

    private VerticalStackLayout? CreateControlForDataPoint(ObservationTypeDataPoint dataPoint)
    {
        return dataPoint.DataType switch
        {
            DataTypes.String => CreateStringControl(dataPoint),
            DataTypes.Long => CreateLongControl(dataPoint),
            DataTypes.Lookup => CreateLookupControl(dataPoint),
            _ => null
        };
    }

    private VerticalStackLayout CreateStringControl(ObservationTypeDataPoint dataPoint)
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
            Margin = new Thickness(0, 0, 0, 10),
            IsEnabled = IsEditable,
            IsVisible = IsEditable
        };

        var valueLabel = new Label
        {
            TextColor = Colors.Black,
            FontSize = 16,
            Margin = new Thickness(0, 0, 0, 10),
            IsVisible = !IsEditable
        };

        entry.TextChanged += (sender, e) =>
        {
            _values[dataPoint.Id] = e.NewTextValue;
            ValuesChanged?.Invoke(this, _values);
            ValuesChangedCommand?.Execute(_values);
        };

        // Store references for value updates
        _controlReferences[dataPoint.Id] = new ControlReference { Entry = entry, Label = valueLabel };

        return new VerticalStackLayout
        {
            Children = { label, entry, valueLabel }
        };
    }

    private VerticalStackLayout CreateLongControl(ObservationTypeDataPoint dataPoint)
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
            Margin = new Thickness(0, 0, 0, 10),
            IsEnabled = IsEditable,
            IsVisible = IsEditable
        };

        var valueLabel = new Label
        {
            TextColor = Colors.Black,
            FontSize = 16,
            Margin = new Thickness(0, 0, 0, 10),
            IsVisible = !IsEditable
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

        // Store references for value updates
        _controlReferences[dataPoint.Id] = new ControlReference { Entry = entry, Label = valueLabel };

        return new VerticalStackLayout
        {
            Children = { label, entry, valueLabel }
        };
    }

    private VerticalStackLayout CreateLookupControl(ObservationTypeDataPoint dataPoint)
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
            Margin = new Thickness(0, 0, 0, 10),
            IsEnabled = IsEditable,
            IsVisible = IsEditable
        };

        var valueLabel = new Label
        {
            TextColor = Colors.Black,
            FontSize = 16,
            Margin = new Thickness(0, 0, 0, 10),
            IsVisible = !IsEditable
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

        // Store references for value updates
        _controlReferences[dataPoint.Id] = new ControlReference { Picker = picker, Label = valueLabel };

        return new VerticalStackLayout
        {
            Children = { label, picker, valueLabel }
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
        foreach (var kvp in _values)
        {
            if (_controlReferences.TryGetValue(kvp.Key, out var controlRef))
            {
                UpdateControlValues(kvp, controlRef);
            }
        }
    }

    private void UpdateControlWithValues(Guid datapointId)
    {
        if (_values.TryGetValue(datapointId, out var value) && _controlReferences.TryGetValue(datapointId, out ControlReference? controlRef))
        {
            UpdateControlValues(value, controlRef);
        }
    }

    private static void UpdateControlValues(object obj, ControlReference controlRef)
    {
        var value = obj?.ToString() ?? "";

        // Update entry controls
        if (controlRef.Entry != null)
            controlRef.Entry.Text = value;

        // Update picker controls
        if (controlRef.Picker != null)
        {
            // Find the matching item in the picker
            for (int i = 0; i < controlRef.Picker.Items.Count; i++)
            {
                if (controlRef.Picker.Items[i] == value)
                {
                    controlRef.Picker.SelectedIndex = i;
                    break;
                }
            }
        }

        // Update label controls (for view mode)
        if (controlRef.Label != null)
            controlRef.Label.Text = value;
    }

    private void UpdateControlVisibility(bool isEditable)
    {
        foreach (var controlRef in _controlReferences.Values)
        {
            if (controlRef.Entry != null)
            {
                controlRef.Entry.IsEnabled = isEditable;
                controlRef.Entry.IsVisible = isEditable;
            }
            
            if (controlRef.Picker != null)
            {
                controlRef.Picker.IsEnabled = isEditable;
                controlRef.Picker.IsVisible = isEditable;
            }
            
            if (controlRef.Label != null)
            {
                controlRef.Label.IsVisible = !isEditable;
            }
        }
    }
} 
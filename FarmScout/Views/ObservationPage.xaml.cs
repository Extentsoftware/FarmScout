using FarmScout.ViewModels;
using FarmScout.Models;
using FarmScout.Controls;
using FarmScout.Services;

namespace FarmScout.Views;

[QueryProperty(nameof(ObservationId), "ObservationId")]
[QueryProperty(nameof(Mode), "Mode")]
public partial class ObservationPage : ContentPage
{
    public Guid ObservationId { get; set; }
    public string? Mode { get; set; }
    
    private readonly Dictionary<Guid, ObservationTypeControl> _observationTypeControls = new();

    public ObservationPage(ObservationViewModel viewModel, IFarmScoutDatabase database)
    {
        InitializeComponent();
        
        BindingContext = viewModel;
        App.Log("ObservationPage ViewModel set from constructor injection");
        
        // Subscribe to observation type changes
        if (viewModel is ObservationViewModel obsViewModel)
        {
            obsViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }
    
    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ObservationViewModel.SelectedObservationTypes))
        {
            _ = LoadObservationTypeControlsAsync();
        }
    }

    private async Task LoadObservationTypeControlsAsync()
    {
        if (BindingContext is not ObservationViewModel viewModel) return;
        
        try
        {
            // Clear existing controls
            foreach (var control in _observationTypeControls.Values)
            {
                if (control.Parent is Layout parent)
                {
                    parent.Children.Remove(control);
                }
            }
            _observationTypeControls.Clear();
            
            // Load controls for each selected observation type
            foreach (var observationType in viewModel.SelectedObservationTypes)
            {
                var control = new ObservationTypeControl();
                control.ValuesChanged += (sender, values) =>
                {
                    viewModel.UpdateMetadataForType(observationType.Id, values);
                };
                
                await control.LoadObservationTypeAsync(observationType.Id);
                _observationTypeControls[observationType.Id] = control;
                
                // Find the container for this observation type and add the control
                // This will be handled by the XAML binding, but we need to ensure the control is properly initialized
            }
        }
        catch (Exception ex)
        {
            App.Log($"Error loading observation type controls: {ex.Message}");
            await Shell.Current.DisplayAlert("Error", "Failed to load observation type controls", "OK");
        }
    }
    
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext != null)
        {
            if (BindingContext is ObservationViewModel viewModel)
            {
                // Set the mode based on navigation parameters
                if (!string.IsNullOrEmpty(Mode))
                {
                    switch (Mode.ToLower())
                    {
                        case "add":
                            await viewModel.SetAddMode();
                            break;
                        case "edit":
                            await viewModel.LoadObservationAsync(ObservationId);
                            await viewModel.SetEditMode();
                            break;
                        case "view":
                            await viewModel.LoadObservationAsync(ObservationId);
                            await viewModel.SetViewMode();
                            break;
                    }
                }
                else if (ObservationId != Guid.Empty)
                {
                    // Default to edit mode if observation ID is provided
                    await viewModel.LoadObservationAsync(ObservationId);
                    await viewModel.SetEditMode();
                }
                else
                {
                    // Default to add mode
                    await viewModel.SetAddMode();
                }
                
                // Load observation type controls after the view model is initialized
                await LoadObservationTypeControlsAsync();
            }
        }
    }
    
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        
        // Clean up event subscriptions
        if (BindingContext is ObservationViewModel viewModel)
        {
            viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }
    }
} 
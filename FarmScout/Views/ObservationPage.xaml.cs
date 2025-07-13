using FarmScout.ViewModels;
using FarmScout.Controls;
using FarmScout.Services;

namespace FarmScout.Views;

[QueryProperty(nameof(ObservationId), "ObservationId")]
[QueryProperty(nameof(Mode), "Mode")]
public partial class ObservationPage : ContentPage
{
    public Guid ObservationId { get; set; }
    public string? Mode { get; set; }
    
    private readonly Dictionary<Guid, ObservationTypeControl> _observationTypeControls = [];

    public ObservationPage(ObservationViewModel viewModel, IFarmScoutDatabase database)
    {
        InitializeComponent();
        
        BindingContext = viewModel;
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
                            await viewModel.SetAddModeAsync();
                            break;
                        case "edit":
                            await viewModel.LoadObservationAsync(ObservationId);
                            await viewModel.SetEditModeAsync();
                            break;
                        case "view":
                            await viewModel.LoadObservationAsync(ObservationId);
                            await viewModel.SetViewModeAsync();
                            break;
                    }
                }
                else if (ObservationId != Guid.Empty)
                {
                    // Default to edit mode if observation ID is provided
                    await viewModel.LoadObservationAsync(ObservationId);
                    await viewModel.SetEditModeAsync();
                }
                else
                {
                    // Default to add mode
                    await viewModel.SetAddModeAsync();
                }
                Dispatcher.Dispatch(() =>(this as IView).InvalidateArrange());
            }
        }
    }
} 
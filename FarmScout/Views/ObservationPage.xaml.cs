using FarmScout.ViewModels;
using FarmScout.Models;

namespace FarmScout.Views;

[QueryProperty(nameof(ObservationId), "ObservationId")]
[QueryProperty(nameof(Mode), "Mode")]
public partial class ObservationPage : ContentPage
{
    public int ObservationId { get; set; }
    public string? Mode { get; set; }

    public ObservationPage(ObservationViewModel viewModel)
    {
        InitializeComponent();
        
        BindingContext = viewModel;
        App.Log("ObservationPage ViewModel set from constructor injection");
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is ObservationViewModel viewModel)
        {
            // Set the mode based on navigation parameters
            if (!string.IsNullOrEmpty(Mode))
            {
                switch (Mode.ToLower())
                {
                    case "add":
                        viewModel.SetAddMode();
                        break;
                    case "edit":
                        await viewModel.LoadObservationAsync(ObservationId);
                        viewModel.SetEditMode();
                        break;
                    case "view":
                        await viewModel.LoadObservationAsync(ObservationId);
                        viewModel.SetViewMode();
                        break;
                }
            }
            else if (ObservationId>0)
            {
                // Default to edit mode if observation ID is provided
                await viewModel.LoadObservationAsync(ObservationId);
                viewModel.SetEditMode();
            }
            else
            {
                // Default to add mode
                viewModel.SetAddMode();
            }

            // Update DiseaseControl after ViewModel is loaded
            UpdateDiseaseControl();
        }
    }

    private void UpdateDiseaseControl()
    {
        // Find the DiseaseControl in the visual tree and update it
        if (this.FindByName<Controls.DiseaseControl>("DiseaseControl") is Controls.DiseaseControl diseaseControl)
        {
            diseaseControl.UpdateFromParent();
        }
    }
} 
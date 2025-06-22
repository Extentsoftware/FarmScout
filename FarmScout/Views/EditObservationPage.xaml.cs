using FarmScout.ViewModels;
using FarmScout.Models;

namespace FarmScout.Views;

[QueryProperty(nameof(ObservationId), "ObservationId")]
public partial class EditObservationPage : ContentPage
{
    public int ObservationId { get; set; }

    public EditObservationPage(EditObservationViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is EditObservationViewModel viewModel)
        {
            await viewModel.LoadObservationAsync(ObservationId);
        }
    }
} 
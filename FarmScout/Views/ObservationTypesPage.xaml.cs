using FarmScout.ViewModels;

namespace FarmScout.Views;

public partial class ObservationTypesPage : ContentPage
{
    public ObservationTypesPage(ObservationTypesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }


    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ObservationTypesViewModel viewModel)
        {
            await viewModel.LoadObservationTypesCommand.ExecuteAsync(null);
        }
    }
} 
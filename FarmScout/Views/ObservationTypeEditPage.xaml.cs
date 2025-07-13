using FarmScout.ViewModels;

namespace FarmScout.Views;

public partial class ObservationTypeEditPage : ContentPage
{
    public ObservationTypeEditPage(ObservationTypeEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        if (BindingContext is ObservationTypeEditViewModel viewModel)
        {
            await viewModel.OnAppearing();
        }
    }
} 
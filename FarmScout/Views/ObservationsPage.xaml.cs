using FarmScout.ViewModels;

namespace FarmScout.Views;

public partial class ObservationsPage : ContentPage
{
    public ObservationsPage(ObservationsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        App.Log("ObservationsPage ViewModel set from constructor injection");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        if (BindingContext is ObservationsViewModel viewModel)
        {
            await viewModel.Refresh();
        }
    }
} 
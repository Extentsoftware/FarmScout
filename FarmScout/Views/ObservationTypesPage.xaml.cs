using FarmScout.ViewModels;

namespace FarmScout.Views;

public partial class ObservationTypesPage : ContentPage
{
    public ObservationTypesPage(ObservationTypesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
} 
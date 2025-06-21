using FarmScout.ViewModels;
using FarmScout.Models;

namespace FarmScout.Views;

public partial class ObservationDetailPage : ContentPage
{
    public ObservationDetailPage(ObservationDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        App.Log("ObservationDetailPage ViewModel set from constructor injection");
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Binding context is already set in constructor
    }
} 
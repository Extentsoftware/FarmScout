using FarmScout.ViewModels;

namespace FarmScout.Views;

public partial class AddObservationPage : ContentPage
{
    public AddObservationPage(AddObservationViewModel viewModel)
    {
        App.Log("AddObservationPage constructor start");
        try
        {
            InitializeComponent();
            BindingContext = viewModel;
            App.Log("AddObservationPage ViewModel set from constructor injection");
            App.Log("AddObservationPage constructor complete");
        }
        catch (Exception ex)
        {
            App.Log($"AddObservationPage constructor exception: {ex}");
            throw;
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Binding context is already set in constructor
    }
}
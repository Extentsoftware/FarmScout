using FarmScout.ViewModels;

namespace FarmScout.Views;

public partial class DatabaseResetPage : ContentPage
{
    public DatabaseResetPage(DatabaseResetViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is DatabaseResetViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}

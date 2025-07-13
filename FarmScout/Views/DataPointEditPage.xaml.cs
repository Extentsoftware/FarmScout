using FarmScout.ViewModels;

namespace FarmScout.Views;

public partial class DataPointEditPage : ContentPage
{
    public DataPointEditPage(DataPointEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        if (BindingContext is DataPointEditViewModel viewModel)
        {
            await viewModel.OnAppearing();
        }
    }
} 
using CommunityToolkit.Mvvm.Input;
using FarmScout.ViewModels;

namespace FarmScout.Views;

public partial class LookupItemPage : ContentPage
{
    public LookupItemPage(LookupItemViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is LookupItemViewModel viewModel)
        {
            await viewModel.LoadLookupItems();
        }
    }
}
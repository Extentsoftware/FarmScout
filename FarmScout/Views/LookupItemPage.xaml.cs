using FarmScout.ViewModels;

namespace FarmScout.Views;

public partial class LookupItemPage : ContentPage
{
    public LookupItemPage(LookupItemViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
} 
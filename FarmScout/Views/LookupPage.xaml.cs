using FarmScout.ViewModels;

namespace FarmScout.Views;

public partial class LookupPage : ContentPage
{
    public LookupPage(LookupViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is LookupViewModel viewModel)
        {
            viewModel.LoadLookupItemsCommand.Execute(null);
        }
    }

    private void OnSearchButtonPressed(object sender, EventArgs e)
    {
        if (BindingContext is LookupViewModel viewModel)
        {
            viewModel.SearchCommand.Execute(null);
        }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (BindingContext is LookupViewModel viewModel)
        {
            viewModel.SearchCommand.Execute(e.NewTextValue);
        }
    }

    private void OnGroupSelectedIndexChanged(object sender, EventArgs e)
    {
        if (BindingContext is LookupViewModel viewModel && sender is Picker picker)
        {
            viewModel.FilterByGroupCommand.Execute(picker.SelectedItem?.ToString());
        }
    }
} 
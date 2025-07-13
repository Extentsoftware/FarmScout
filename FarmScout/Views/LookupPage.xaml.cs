using FarmScout.ViewModels;

namespace FarmScout.Views;

public interface ILookupPageFactory
{
    LookupPage Create(string title, int count);
}

public class LookupPageFactory(IServiceProvider serviceProvider) : ILookupPageFactory
{
    public LookupPage Create(string title, int count)
    {
        var parameters = new LookupParameters
        {
            LookupMode = true,
            SearchText = "",
            SelectedGroup = "Diseases"
        };
        return ActivatorUtilities.CreateInstance<LookupPage>(serviceProvider, parameters);
    }
}

public class LookupParameters
{
    public bool? LookupMode { get; set; }
    public string SelectedGroup { get; set; } = "";
    public string SearchText { get; set; } = "";
}


public partial class LookupPage : ContentPage
{
    public LookupPage(LookupViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    public LookupPage(LookupViewModel viewModel, LookupParameters parameters)
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
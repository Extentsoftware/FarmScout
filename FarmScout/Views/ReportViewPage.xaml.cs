using FarmScout.ViewModels;
using FarmScout.Models;

namespace FarmScout.Views;

[QueryProperty(nameof(Report), "Report")]
public partial class ReportViewPage : ContentPage
{
    public MarkdownReport? Report { get; set; }

    public ReportViewPage(ReportViewViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is ReportViewViewModel viewModel && Report != null)
        {
            viewModel.LoadReport(Report);
        }
    }
} 
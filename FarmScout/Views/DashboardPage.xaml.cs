using FarmScout.ViewModels;

namespace FarmScout.Views;

public partial class DashboardPage : ContentPage
{
    public DashboardPage()
    {
        App.Log("DashboardPage constructor start");
        try
        {
            InitializeComponent();
            App.Log("DashboardPage constructor complete");
        }
        catch (Exception ex)
        {
            App.Log($"DashboardPage constructor exception: {ex}");
            throw;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        App.Log("DashboardPage OnAppearing");
        
       var dashboardViewModel = MauiProgram.Services.GetRequiredService<DashboardViewModel>();
       if (dashboardViewModel != null)
       {
            BindingContext = dashboardViewModel;
            await dashboardViewModel.LoadDashboardData();
            App.Log("DashboardPage ViewModel set from DI in OnAppearing");
       }
       else
       {
            App.Log("DashboardPage ViewModel NOT resolved from DI in OnAppearing");
       }
    }
} 
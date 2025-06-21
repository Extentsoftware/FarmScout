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
            
            // Set the ViewModel from DI in constructor
            try
            {
                var dashboardViewModel = Handler?.MauiContext?.Services?.GetRequiredService<DashboardViewModel>();
                if (dashboardViewModel != null)
                {
                    BindingContext = dashboardViewModel;
                    App.Log("DashboardPage ViewModel set from DI in constructor");
                }
                else
                {
                    App.Log("DashboardPage ViewModel NOT resolved from DI in constructor");
                }
            }
            catch (Exception ex)
            {
                App.Log($"DashboardPage constructor DI exception: {ex}");
            }
            
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
        
        // Set the ViewModel from DI if not already set
        if (BindingContext == null)
        {
            try
            {
                var dashboardViewModel = Handler?.MauiContext?.Services?.GetRequiredService<DashboardViewModel>();
                if (dashboardViewModel != null)
                {
                    BindingContext = dashboardViewModel;
                    App.Log("DashboardPage ViewModel set from DI in OnAppearing");
                }
                else
                {
                    App.Log("DashboardPage ViewModel NOT resolved from DI");
                }
            }
            catch (Exception ex)
            {
                App.Log($"DashboardPage OnAppearing exception: {ex}");
            }
        }
        
        if (BindingContext is DashboardViewModel viewModel)
        {
            App.Log("DashboardPage calling LoadDashboardData");
            await viewModel.LoadDashboardData();
            App.Log("DashboardPage LoadDashboardData complete");
        }
    }
} 
using FarmScout.ViewModels;

namespace FarmScout.Views
{
    public partial class DatabaseResetPage : ContentPage
    {
        private readonly DatabaseResetViewModel _viewModel;

        public DatabaseResetPage(DatabaseResetViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.InitializeAsync();
        }
    }
} 
using FarmScout.Services;
using FarmScout.Views;

namespace FarmScout.Controls
{
    public partial class DiseaseControl : ContentView
    {
        public DiseaseControl()
        {            
            InitializeComponent();
        }

        private async void OnDiseaseInfoButtonClicked(object sender, EventArgs e)
        {
            var view = MauiProgram.Services.GetRequiredService<LookupPage>();
            await Shell.Current.Navigation.PushModalAsync(view);
        }
    }
} 
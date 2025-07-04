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
            await Shell.Current.GoToAsync("LookupPage", new Dictionary<string, object> { { "LookupMode", true }, { "SelectedGroup", "Diseases" }, { "SelectedText", "" } } );

            //var factory = MauiProgram.Services.GetRequiredService<ILookupPageFactory>();
            //var view = factory.Create("kl", 0);
            //await Shell.Current.Navigation.PushModalAsync(view);
        }
    }
} 
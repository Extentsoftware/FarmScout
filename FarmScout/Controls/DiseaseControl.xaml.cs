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

            //var farmScoutDatabase = MauiProgram.Services.GetRequiredService<FarmScoutDatabase>();

            //var diseases = await farmScoutDatabase.GetLookupItemsByGroupAsync("Diseases");
            //var displayOptions = diseases.Select(x=>$"{x.Name} ({x.SubGroup}) - {x.Description}").ToArray();

            //var result = await Shell.Current.DisplayActionSheet(
            //    "Select Disease",
            //    "Cancel",
            //    null,
            //    displayOptions);

            //if (result != null && result != "Cancel")
            //{
            //}
        }
    }
} 
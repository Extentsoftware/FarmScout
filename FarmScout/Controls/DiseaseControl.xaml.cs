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
            var parameters = new Dictionary<string, object>
            {
                { "SelectMode", true },
                { "EditMode", false },
                { "SelectedGroup", "Diseases" },
                { "SelectedText", "" }
            };

            await Shell.Current.GoToAsync("LookupPage", parameters );
        }
    }
} 
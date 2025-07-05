using FarmScout.Services;
using FarmScout.Models;
using System.Collections.ObjectModel;

namespace FarmScout.Controls
{
    public partial class DiseaseControl : ContentView
    {
        private readonly FarmScoutDatabase _database;

        public static readonly BindableProperty DiseaseNameProperty =
            BindableProperty.Create(nameof(DiseaseName), typeof(string), typeof(DiseaseControl), string.Empty);

        public static readonly BindableProperty DiseaseTypeProperty =
            BindableProperty.Create(nameof(DiseaseType), typeof(string), typeof(DiseaseControl), string.Empty);

        public static readonly BindableProperty DiseaseDescriptionProperty =
            BindableProperty.Create(nameof(DiseaseDescription), typeof(string), typeof(DiseaseControl), string.Empty);

        public static readonly BindableProperty SelectedDiseaseIdProperty =
            BindableProperty.Create(nameof(SelectedDiseaseId), typeof(Guid?), typeof(DiseaseControl), null);

        public string DiseaseName
        {
            get => (string)GetValue(DiseaseNameProperty);
            set => SetValue(DiseaseNameProperty, value);
        }

        public string DiseaseType
        {
            get => (string)GetValue(DiseaseTypeProperty);
            set => SetValue(DiseaseTypeProperty, value);
        }

        public string DiseaseDescription
        {
            get => (string)GetValue(DiseaseDescriptionProperty);
            set => SetValue(DiseaseDescriptionProperty, value);
        }

        public Guid? SelectedDiseaseId
        {
            get => (Guid?)GetValue(SelectedDiseaseIdProperty);
            set => SetValue(SelectedDiseaseIdProperty, value);
        }

        public DiseaseControl()
        {            
            InitializeComponent();
            _database = new FarmScoutDatabase();
            BindingContext = this;
        }

        private async void OnSelectDiseaseClicked(object sender, EventArgs e)
        {
            try
            {
                // Load all diseases from database
                var diseases = await _database.GetLookupItemsByGroupAsync("Diseases");
                
                if (!diseases.Any())
                {
                    return;
                }

                // Create selection dialog
                var options = diseases.Select(d => $"{d.Name} ({d.SubGroup})").ToArray();
                var selectedItem = await Shell.Current.DisplayActionSheet(
                    "Select a Disease",
                    "Cancel",
                    null,
                    options);

                if (selectedItem != null && selectedItem != "Cancel" && selectedItem != null)
                {
                    var selectedIndex = options.IndexOf(selectedItem);

                    var selectedDisease = diseases[selectedIndex];
                    
                    // Update the bindable properties
                    DiseaseName = selectedDisease.Name;
                    DiseaseType = selectedDisease.SubGroup;
                    DiseaseDescription = selectedDisease.Description;
                    SelectedDiseaseId = selectedDisease.Id;

                    // Show detailed information
                    await ShowDiseaseDetails(selectedDisease);
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Failed to load diseases: {ex.Message}", "OK");
            }
        }

        private async Task ShowDiseaseDetails(LookupItem disease)
        {
            var details = $"**Disease Details**\n\n" +
                         $"**Name:** {disease.Name}\n" +
                         $"**Type:** {disease.SubGroup}\n" +
                         $"**Description:** {disease.Description}\n" +
                         $"**Created:** {disease.CreatedAt:MMM dd, yyyy}\n" +
                         $"**Last Updated:** {disease.UpdatedAt:MMM dd, yyyy}";

            await Shell.Current.DisplayAlert("Disease Information", details, "OK");
        }

        public void ClearSelection()
        {
            DiseaseName = string.Empty;
            DiseaseType = string.Empty;
            DiseaseDescription = string.Empty;
            SelectedDiseaseId = null;
        }

        public void SetDisease(LookupItem disease)
        {
            if (disease != null)
            {
                DiseaseName = disease.Name;
                DiseaseType = disease.SubGroup;
                DiseaseDescription = disease.Description;
                SelectedDiseaseId = disease.Id;
            }
        }
    }
} 
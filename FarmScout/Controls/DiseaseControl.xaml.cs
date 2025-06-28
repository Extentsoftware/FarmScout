using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FarmScout.Models;
using FarmScout.Services;

namespace FarmScout.Controls
{
    public partial class DiseaseControl : ContentView
    {
        private readonly DiseaseControlViewModel _viewModel;

        public DiseaseControl()
        {
            InitializeComponent();
            _viewModel = MauiProgram.Services.GetService<DiseaseControlViewModel>() ?? 
                        new DiseaseControlViewModel(
                            MauiProgram.Services.GetService<FarmScoutDatabase>() ?? new FarmScoutDatabase(),
                            MauiProgram.Services.GetService<INavigationService>() ?? new NavigationService());
            
            // Set the BindingContext to the parent ViewModel instead of the DiseaseControlViewModel
            // The DiseaseControlViewModel will be used only for the lookup data
            BindingContext = this;
        }

        // Dependency properties to bind to parent ViewModel
        public static readonly BindableProperty DiseaseNameProperty =
            BindableProperty.Create(nameof(DiseaseName), typeof(string), typeof(DiseaseControl), string.Empty, propertyChanged: OnDiseaseNameChanged);

        public static readonly BindableProperty DiseaseTypeProperty =
            BindableProperty.Create(nameof(DiseaseType), typeof(string), typeof(DiseaseControl), string.Empty, propertyChanged: OnDiseaseTypeChanged);

        public static readonly BindableProperty IsViewModeProperty =
            BindableProperty.Create(nameof(IsViewMode), typeof(bool), typeof(DiseaseControl), false, propertyChanged: OnIsViewModeChanged);

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

        public bool IsViewMode
        {
            get => (bool)GetValue(IsViewModeProperty);
            set => SetValue(IsViewModeProperty, value);
        }

        // Properties for the pickers
        public ObservableCollection<LookupItem> AvailableDiseases => _viewModel.AvailableDiseases;
        public ObservableCollection<LookupItem> AvailableDiseaseTypes => _viewModel.AvailableDiseaseTypes;

        public LookupItem? SelectedDisease
        {
            get => AvailableDiseases?.FirstOrDefault(d => d.Name == DiseaseName);
            set
            {
                if (value != null)
                {
                    DiseaseName = value.Name;
                }
            }
        }

        public LookupItem? SelectedDiseaseType
        {
            get => AvailableDiseaseTypes?.FirstOrDefault(d => d.Name == DiseaseType);
            set
            {
                if (value != null)
                {
                    DiseaseType = value.Name;
                }
            }
        }

        // Commands for adding new items
        public ICommand AddNewDiseaseCommand => _viewModel.AddNewDiseaseCommand;
        public ICommand AddNewDiseaseTypeCommand => _viewModel.AddNewDiseaseTypeCommand;

        private static void OnDiseaseNameChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is DiseaseControl control && control._viewModel != null)
            {
                control._viewModel.DiseaseName = (string)newValue;
            }
        }

        private static void OnDiseaseTypeChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is DiseaseControl control && control._viewModel != null)
            {
                control._viewModel.DiseaseType = (string)newValue;
            }
        }

        private static void OnIsViewModeChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is DiseaseControl control && control._viewModel != null)
            {
                control._viewModel.IsViewMode = (bool)newValue;
            }
        }

        // Method to update parent ViewModel when selections change
        public void UpdateParentViewModel()
        {
            if (_viewModel != null)
            {
                DiseaseName = _viewModel.DiseaseName;
                DiseaseType = _viewModel.DiseaseType;
            }
        }

        // Method to update ViewModel from parent
        public void UpdateFromParent()
        {
            if (_viewModel != null)
            {
                _viewModel.DiseaseName = DiseaseName;
                _viewModel.DiseaseType = DiseaseType;
                _viewModel.IsViewMode = IsViewMode;
            }
        }
    }

    public class DiseaseControlViewModel : INotifyPropertyChanged
    {
        private readonly FarmScoutDatabase _database;
        private readonly INavigationService _navigationService;
        
        private string _diseaseName = "";
        private string _diseaseType = "";
        private bool _isViewMode;
        private bool _isLoading;
        private ObservableCollection<LookupItem> _availableDiseases;
        private ObservableCollection<LookupItem> _availableDiseaseTypes;
        private LookupItem? _selectedDisease;
        private LookupItem? _selectedDiseaseType;

        public DiseaseControlViewModel(FarmScoutDatabase database, INavigationService navigationService)
        {
            _database = database;
            _navigationService = navigationService;
            
            _availableDiseases = new ObservableCollection<LookupItem>();
            _availableDiseaseTypes = new ObservableCollection<LookupItem>();
            
            LoadDiseasesCommand = new Command(async () => await LoadDiseasesAsync());
            LoadDiseaseTypesCommand = new Command(async () => await LoadDiseaseTypesAsync());
            AddNewDiseaseCommand = new Command(async () => await AddNewDiseaseAsync());
            AddNewDiseaseTypeCommand = new Command(async () => await AddNewDiseaseTypeAsync());
            
            LoadDataAsync();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public string DiseaseName
        {
            get => _diseaseName;
            set
            {
                if (SetProperty(ref _diseaseName, value))
                {
                    // Update the selected item if the string doesn't match
                    if (_selectedDisease?.Name != value)
                    {
                        _selectedDisease = AvailableDiseases?.FirstOrDefault(d => d.Name == value);
                        OnPropertyChanged(nameof(SelectedDisease));
                    }
                }
            }
        }

        public string DiseaseType
        {
            get => _diseaseType;
            set
            {
                if (SetProperty(ref _diseaseType, value))
                {
                    // Update the selected item if the string doesn't match
                    if (_selectedDiseaseType?.Name != value)
                    {
                        _selectedDiseaseType = AvailableDiseaseTypes?.FirstOrDefault(d => d.Name == value);
                        OnPropertyChanged(nameof(SelectedDiseaseType));
                    }
                }
            }
        }

        public bool IsViewMode
        {
            get => _isViewMode;
            set
            {
                _isViewMode = value;
                OnPropertyChanged();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<LookupItem> AvailableDiseases
        {
            get => _availableDiseases;
            set => SetProperty(ref _availableDiseases, value);
        }

        public ObservableCollection<LookupItem> AvailableDiseaseTypes
        {
            get => _availableDiseaseTypes;
            set => SetProperty(ref _availableDiseaseTypes, value);
        }

        public LookupItem? SelectedDisease
        {
            get => _selectedDisease;
            set
            {
                if (SetProperty(ref _selectedDisease, value))
                {
                    // Update the string property for parent binding
                    DiseaseName = value?.Name ?? "";
                }
            }
        }

        public LookupItem? SelectedDiseaseType
        {
            get => _selectedDiseaseType;
            set
            {
                if (SetProperty(ref _selectedDiseaseType, value))
                {
                    // Update the string property for parent binding
                    DiseaseType = value?.Name ?? "";
                }
            }
        }

        public ICommand LoadDiseasesCommand { get; }
        public ICommand LoadDiseaseTypesCommand { get; }
        public ICommand AddNewDiseaseCommand { get; }
        public ICommand AddNewDiseaseTypeCommand { get; }

        private async Task LoadDataAsync()
        {
            try
            {
                await LoadDiseasesAsync();
                await LoadDiseaseTypesAsync();
            }
            catch (Exception ex)
            {
                App.Log($"Error loading disease data: {ex.Message}");
            }
        }

        private async Task LoadDiseasesAsync()
        {
            try
            {
                var diseases = await _database.GetLookupItemsByGroupAsync("Diseases");
                AvailableDiseases.Clear();
                foreach (var disease in diseases)
                {
                    AvailableDiseases.Add(disease);
                }
            }
            catch (Exception ex)
            {
                App.Log($"Error loading diseases: {ex.Message}");
            }
        }

        private async Task LoadDiseaseTypesAsync()
        {
            try
            {
                var diseaseTypes = await _database.GetLookupItemsByGroupAsync("Disease Types");
                AvailableDiseaseTypes.Clear();
                foreach (var diseaseType in diseaseTypes)
                {
                    AvailableDiseaseTypes.Add(diseaseType);
                }
            }
            catch (Exception ex)
            {
                App.Log($"Error loading disease types: {ex.Message}");
            }
        }

        private async Task AddNewDiseaseAsync()
        {
            try
            {
                var newItem = new LookupItem
                {
                    Name = "",
                    Group = "Diseases",
                    SubGroup = "",
                    Description = ""
                };

                await _navigationService.NavigateToAsync("LookupItemPage", new Dictionary<string, object>
                {
                    { "Item", newItem },
                    { "IsNew", true }
                });

                await LoadDiseasesAsync();
            }
            catch (Exception ex)
            {
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", $"Failed to add new disease: {ex.Message}", "OK");
                }
            }
        }

        private async Task AddNewDiseaseTypeAsync()
        {
            try
            {
                var newItem = new LookupItem
                {
                    Name = "",
                    Group = "Disease Types",
                    SubGroup = "",
                    Description = ""
                };

                await _navigationService.NavigateToAsync("LookupItemPage", new Dictionary<string, object>
                {
                    { "Item", newItem },
                    { "IsNew", true }
                });

                await LoadDiseaseTypesAsync();
            }
            catch (Exception ex)
            {
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", $"Failed to add new disease type: {ex.Message}", "OK");
                }
            }
        }
    }
} 
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FarmScout.Models;
using FarmScout.Services;

namespace FarmScout.ViewModels
{
    public class LookupViewModel : INotifyPropertyChanged
    {
        private readonly FarmScoutDatabase _database;
        private readonly INavigationService _navigationService;
        
        private ObservableCollection<LookupItem> _lookupItems;
        private ObservableCollection<LookupItem> _filteredItems;
        private string _selectedGroup = "All";
        private string _searchText = "";
        private LookupItem? _selectedItem;
        private bool _isLoading;

        public LookupViewModel(FarmScoutDatabase database, INavigationService navigationService)
        {
            _database = database;
            _navigationService = navigationService;
            _lookupItems = [];
            _filteredItems = [];
            
            LoadLookupItemsCommand = new Command(async () => await LoadLookupItemsAsync());
            AddLookupItemCommand = new Command(async () => await AddLookupItemAsync());
            EditLookupItemCommand = new Command<LookupItem>(async (item) => await EditLookupItemAsync(item));
            DeleteLookupItemCommand = new Command<LookupItem>(async (item) => await DeleteLookupItemAsync(item));
            FilterByGroupCommand = new Command<string>(async (group) => await FilterByGroupAsync(group));
            SearchCommand = new Command<string>(async (text) => await SearchAsync(text));
            
            // Load initial data
            _ = LoadLookupItemsAsync();
        }

        public ObservableCollection<LookupItem> LookupItems
        {
            get => _lookupItems;
            set
            {
                _lookupItems = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<LookupItem> FilteredItems
        {
            get => _filteredItems;
            set
            {
                _filteredItems = value;
                OnPropertyChanged();
            }
        }

        public string SelectedGroup
        {
            get => _selectedGroup;
            set
            {
                _selectedGroup = value;
                OnPropertyChanged();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
            }
        }

        public LookupItem? SelectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = value;
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

        public ICommand LoadLookupItemsCommand { get; }
        public ICommand AddLookupItemCommand { get; }
        public ICommand EditLookupItemCommand { get; }
        public ICommand DeleteLookupItemCommand { get; }
        public ICommand FilterByGroupCommand { get; }
        public ICommand SearchCommand { get; }

        public string[] AvailableGroups => LookupGroups.AvailableGroups;

        private async Task LoadLookupItemsAsync()
        {
            try
            {
                IsLoading = true;
                var items = await _database.GetLookupItemsAsync();
                
                LookupItems.Clear();
                foreach (var item in items)
                {
                    LookupItems.Add(item);
                }
                
                await ApplyFiltersAsync();
            }
            catch (Exception ex)
            {
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", $"Failed to load lookup items: {ex.Message}", "OK");
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task AddLookupItemAsync()
        {
            try
            {
                var newItem = new LookupItem
                {
                    Name = "",
                    Group = AvailableGroups.FirstOrDefault() ?? "Crop Types",
                    SubGroup = "",
                    Description = ""
                };

                await _navigationService.NavigateToAsync("LookupItemPage", new Dictionary<string, object>
                {
                    { "Item", newItem },
                    { "IsNew", true }
                });

                await LoadLookupItemsAsync();
            }
            catch (Exception ex)
            {
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", $"Failed to add lookup item: {ex.Message}", "OK");
                }
            }
        }

        private async Task EditLookupItemAsync(LookupItem item)
        {
            if (item == null) return;

            try
            {
                await _navigationService.NavigateToAsync("LookupItemPage", new Dictionary<string, object>
                {
                    { "Item", item },
                    { "IsNew", false }
                });

                await LoadLookupItemsAsync();
            }
            catch (Exception ex)
            {
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", $"Failed to edit lookup item: {ex.Message}", "OK");
                }
            }
        }

        private async Task DeleteLookupItemAsync(LookupItem item)
        {
            if (item == null) return;

            if (Application.Current?.MainPage == null) return;

            var confirm = await Application.Current.MainPage.DisplayAlert(
                "Confirm Delete",
                $"Are you sure you want to delete '{item.Name}'?",
                "Delete",
                "Cancel");

            if (confirm)
            {
                try
                {
                    await _database.DeleteLookupItemAsync(item);
                    await LoadLookupItemsAsync();
                }
                catch (Exception ex)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", $"Failed to delete lookup item: {ex.Message}", "OK");
                }
            }
        }

        private async Task FilterByGroupAsync(string group)
        {
            await ApplyFiltersAsync();
        }

        private async Task SearchAsync(string searchText)
        {
            await ApplyFiltersAsync();
        }

        private async Task ApplyFiltersAsync()
        {
            try
            {
                var filtered = LookupItems.AsEnumerable();

                // Filter by group
                if (SelectedGroup != "All")
                {
                    filtered = filtered.Where(item => item.Group == SelectedGroup);
                }

                // Filter by search text
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    filtered = filtered.Where(item => 
                        item.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                        item.SubGroup.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                        item.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
                }

                FilteredItems.Clear();
                foreach (var item in filtered)
                {
                    FilteredItems.Add(item);
                }
            }
            catch (Exception ex)
            {
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", $"Failed to apply filters: {ex.Message}", "OK");
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 
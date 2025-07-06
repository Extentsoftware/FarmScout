using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmScout.Models;
using FarmScout.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FarmScout.ViewModels
{
    public partial class LookupViewModel : ObservableObject
    {
        private readonly FarmScoutDatabase _database;
        private readonly INavigationService _navigationService;
        
        public LookupViewModel(FarmScoutDatabase database, INavigationService navigationService)
        {
            _database = database;
            _navigationService = navigationService;
                        
            // Load initial data
            _ = LoadLookupItems();
        }

        /// <summary>
        /// when true, the page allows selection ofan item
        /// </summary>
        [ObservableProperty]
        public partial bool SelectMode { get; set; }

        [ObservableProperty]
        public partial bool EditMode { get; set; } = true;

        [ObservableProperty]
        public partial ObservableCollection<LookupItem> LookupItems { get; set; } = [];

        [ObservableProperty]
        public partial ObservableCollection<LookupItem> FilteredItems { get; set; } = [];

        [ObservableProperty]
        public partial string SelectedGroup { get; set; } = "";

        [ObservableProperty]
        public partial string SearchText { get; set; } = "";

        [ObservableProperty]
        public partial LookupItem? SelectedItem { get; set; }

        [ObservableProperty]
        public partial bool IsLoading { get; set; }

        public string[] AvailableGroups => LookupGroups.AvailableGroups;


        [RelayCommand]
        private async Task LoadLookupItems()
        {
            try
            {
                IsLoading = true;
                var items = await _database.GetLookupItemsAsync();

                items = items.OrderBy(x => x.SubGroup).ThenBy(x=>x.Name).ToList();
                
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

        [RelayCommand]
        private async Task AddLookupItem()
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

                await LoadLookupItems();
            }
            catch (Exception ex)
            {
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", $"Failed to add lookup item: {ex.Message}", "OK");
                }
            }
        }

        [RelayCommand]
        private async Task LookupItemAction(LookupItem item)
        {
            if (item == null) return;

            // Create selection dialog
            string[] options = ["Edit", "Delete"];
            var action = await Shell.Current.DisplayActionSheet(
                "Select action",
                "Cancel",
                null,
                options);
            switch (action)
            {
                case "Edit":
                    await EditLookupItem(item);
                    break;
                case "Delete":
                    await DeleteLookupItem(item);
                    break;
            }
        }


        [RelayCommand]
        private async Task EditLookupItem(LookupItem item)
        {
            if (item == null) return;

            try
            {
                await _navigationService.NavigateToAsync("LookupItemPage", new Dictionary<string, object>
                {
                    { "Item", item },
                    { "IsNew", false }
                });

                await LoadLookupItems();
            }
            catch (Exception ex)
            {
                if (Application.Current?.MainPage != null)
                {
                    await Shell.Current.DisplayAlert("Error", $"Failed to edit lookup item: {ex.Message}", "OK");
                }
            }
        }

        [RelayCommand]
        private async Task DeleteLookupItem(LookupItem item)
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
                    await LoadLookupItems();
                }
                catch (Exception ex)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", $"Failed to delete lookup item: {ex.Message}", "OK");
                }
            }
        }

        [RelayCommand]
        private async Task FilterByGroup(string group)
        {
            await ApplyFiltersAsync();
        }

        [RelayCommand]
        private async Task Search(string searchText)
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
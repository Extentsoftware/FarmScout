using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmScout.Models;
using FarmScout.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace FarmScout.ViewModels
{   
    [QueryProperty(nameof(IsNew), "IsNew")]
    [QueryProperty(nameof(Item), "Item")]
    public partial class LookupItemViewModel : ObservableObject
    {
        private readonly IFarmScoutDatabase _database;
        private readonly INavigationService _navigationService;
        
        private LookupItem? _item;

        public LookupItemViewModel(IFarmScoutDatabase database, INavigationService navigationService)
        {
            _database = database;
            _navigationService = navigationService;
        }

        public LookupItem? Item
        {
            get => _item;
            set
            {
                _item = value;
                if (value != null)
                {
                    Name = value.Name;
                    Group = value.Group;
                    SubGroup = value.SubGroup;
                    Description = value.Description;
                }
                OnPropertyChanged();
            }
        }

        [ObservableProperty]
        private partial bool IsNew { get; set; }

        [ObservableProperty]
        private partial string Name { get; set; } = "";

        [ObservableProperty]
        private partial string Group { get; set; } = "";
        //{
        //    get => _group;
        //    set
        //    {
        //        _group = value;
        //        // Reset SubGroup when Group changes
        //        SubGroup = "";
        //        OnPropertyChanged();
        //        OnPropertyChanged(nameof(AvailableSubGroups));
        //    }
        //}

        [ObservableProperty]
        private partial string SubGroup {  get; set; } = "";

        [ObservableProperty]
        private partial string Description { get; set; } = "";

        [ObservableProperty]
        private partial bool IsLoading { get; set; }

        public string[] AvailableGroups => LookupGroups.AvailableGroups;
        public string[] AvailableSubGroups => LookupGroups.GetSubGroupsForGroup(Group);

        [RelayCommand]
        private async Task Save()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert("Validation Error", "Name is required.", "OK");
                }
                return;
            }

            if (string.IsNullOrWhiteSpace(Group))
            {
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert("Validation Error", "Group is required.", "OK");
                }
                return;
            }

            if (Item == null)
            {
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "No item to save.", "OK");
                }
                return;
            }

            try
            {
                IsLoading = true;

                // Check if item already exists (case-insensitive)
                var exists = await _database.LookupItemExistsAsync(Name, Group, IsNew ? null : Item.Id);
                if (exists)
                {
                    if (Application.Current?.MainPage != null)
                    {
                        await Application.Current.MainPage.DisplayAlert("Validation Error", 
                            $"A {Group} with the name '{Name}' already exists.", "OK");
                    }
                    return;
                }

                // Update the item
                Item.Name = Name.Trim();
                Item.Group = Group;
                Item.SubGroup = SubGroup?.Trim() ?? "";
                Item.Description = Description?.Trim() ?? "";

                if (IsNew)
                {
                    await _database.AddLookupItemAsync(Item);
                }
                else
                {
                    await _database.UpdateLookupItemAsync(Item);
                }

                await _navigationService.GoBackAsync();
            }
            catch (Exception ex)
            {
                if (Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", $"Failed to save lookup item: {ex.Message}", "OK");
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task Cancel()
        {
            await _navigationService.GoBackAsync();
        }

        //public event PropertyChangedEventHandler? PropertyChanged;

        //protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        //{
        //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        //}
    }
} 
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmScout.Extensions;
using FarmScout.Models;
using FarmScout.Services;
using System.Collections.ObjectModel;

namespace FarmScout.ViewModels
{   
    [QueryProperty(nameof(IsNew), "IsNew")]
    [QueryProperty(nameof(Item), "Item")]
    public partial class LookupItemViewModel(IFarmScoutDatabase database, INavigationService navigationService) : ObservableObject
    {
        private LookupItem? _item;

        [ObservableProperty]
        public partial bool IsNew { get; set; }

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
        public partial string Name { get; set; } = "";

        [ObservableProperty]
        public partial string Group { get; set; } = "";

        [RelayCommand]
        public async Task LoadLookupItems()
        {
            try
            {
                IsLoading = true;
                var group = Group;
                await AvailableGroups.PopulateFromAsync(async () => await database.GetLookupGroupsAsync());
                Group = group; // Reset to previous value if it was set

            }
            catch (Exception ex)
            {
                await MauiProgram.DisplayAlertAsync("Error", $"Failed to load lookup items: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        partial void OnGroupChanged(string value)
        {
            // Reset SubGroup when Group changes
            SubGroup = "";
            OnPropertyChanged(nameof(AvailableSubGroups));
        }

        partial void OnSubGroupChanged(string value)
        {
            database.GetLookupSubGroupsAsync(Group)
                .ContinueWith(t => 
                {
                    if (t.IsCompletedSuccessfully)
                    {
                        AvailableSubGroups.PopulateFrom(t.Result);
                    }
                    else
                    {
                        AvailableSubGroups.Clear();
                    }
                });
        }

        [ObservableProperty]
        public partial string SubGroup { get; set; } = "";

        [ObservableProperty]
        public partial string Description { get; set; } = "";

        [ObservableProperty]
        public partial bool IsLoading { get; set; }

        [ObservableProperty]
        public partial ObservableCollection<string> AvailableGroups { get; set; } = [];

        [ObservableProperty]
        public partial ObservableCollection<string> AvailableSubGroups { get; set; } = [];

        [RelayCommand]
        private async Task Save()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                await MauiProgram.DisplayAlertAsync("Validation Error", "Name is required.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(Group))
            {
                await MauiProgram.DisplayAlertAsync("Validation Error", "Group is required.", "OK");
                return;
            }

            if (Item == null)
            {
                await MauiProgram.DisplayAlertAsync("Error", "No item to save.", "OK");
                return;
            }

            try
            {
                IsLoading = true;

                // Check if item already exists (case-insensitive)
                var exists = await database.LookupItemExistsAsync(Name, Group, IsNew ? null : Item.Id);
                if (exists)
                {
                    await MauiProgram.DisplayAlertAsync("Validation Error", $"A {Group} with the name '{Name}' already exists.", "OK");
                    return;
                }

                // Update the item
                Item.Name = Name.Trim();
                Item.Group = Group;
                Item.SubGroup = SubGroup?.Trim() ?? "";
                Item.Description = Description?.Trim() ?? "";

                if (IsNew)
                {
                    await database.AddLookupItemAsync(Item);
                }
                else
                {
                    await database.UpdateLookupItemAsync(Item);
                }

                await navigationService.GoBackAsync();
            }
            catch (Exception ex)
            {
                await MauiProgram.DisplayAlertAsync("Error", $"Failed to save lookup item: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task Cancel()
        {
            await navigationService.GoBackAsync();
        }
    }
} 
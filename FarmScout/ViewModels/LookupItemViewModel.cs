using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmScout.Models;
using FarmScout.Services;

namespace FarmScout.ViewModels
{   
    [QueryProperty(nameof(IsNew), "IsNew")]
    [QueryProperty(nameof(Item), "Item")]
    public partial class LookupItemViewModel(IFarmScoutDatabase database, INavigationService navigationService) : ObservableObject
    {
        private LookupItem? _item;

        public static bool IsNew { get; set; }

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

        partial void OnGroupChanged(string value)
        {
            // Reset SubGroup when Group changes
            SubGroup = "";
            OnPropertyChanged(nameof(AvailableSubGroups));
        }

        [ObservableProperty]
        public partial string SubGroup { get; set; } = "";

        [ObservableProperty]
        public partial string Description { get; set; } = "";

        [ObservableProperty]
        public partial bool IsLoading { get; set; }

        public static string[] AvailableGroups => LookupGroups.AvailableGroups;
        
        public string[] AvailableSubGroups => LookupGroups.GetSubGroupsForGroup(Group);

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
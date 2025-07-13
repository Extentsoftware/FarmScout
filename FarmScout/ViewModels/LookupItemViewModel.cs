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
                    GroupId = value.GroupId;
                    // We'll need to load the group name and subgroup name separately
                    _ = LoadGroupAndSubGroupNamesAsync();
                    Description = value.Description;
                }
                OnPropertyChanged();
            }
        }

        private async Task LoadGroupAndSubGroupNamesAsync()
        {
            try
            {
                if (Item != null)
                {
                    var group = await database.GetLookupGroupByIdAsync(Item.GroupId);
                    if (group != null)
                    {
                        GroupName = group.Name;
                    }

                    if (Item.SubGroupId.HasValue)
                    {
                        var subgroups = await database.GetLookupSubGroupsAsync(Item.GroupId);
                        var subgroup = subgroups.FirstOrDefault(sg => sg.Id == Item.SubGroupId.Value);
                        if (subgroup != null)
                        {
                            SubGroupName = subgroup.Name;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                App.Log($"Error loading group and subgroup names: {ex.Message}");
            }
        }

        [ObservableProperty]
        public partial string Name { get; set; } = "";

        [ObservableProperty]
        public partial Guid GroupId { get; set; } = Guid.Empty;

        [ObservableProperty]
        public partial string GroupName { get; set; } = "";

        [RelayCommand]
        public async Task LoadLookupItems()
        {
            try
            {
                IsLoading = true;

                var groupId = GroupId;
                var groups = await database.GetLookupGroupsAsync();
                AvailableGroups.Clear();
                foreach (var group in groups)
                {
                    AvailableGroups.Add(group.Name);
                }
                
                if (groupId != Guid.Empty)
                {
                    GroupId = groupId; // Reset to previous value if it was set
                }

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

        partial void OnGroupNameChanged(string value)
        {
            // Reset SubGroup when Group changes
            SubGroupName = "";
            OnPropertyChanged(nameof(AvailableSubGroups));
            
            // Load subgroups for the selected group
            _ = LoadSubGroupsAsync();
        }

        private async Task LoadSubGroupsAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(GroupName))
                {
                    AvailableSubGroups.Clear();
                    return;
                }

                var group = await database.GetLookupGroupByNameAsync(GroupName);
                if (group != null)
                {
                    GroupId = group.Id;
                    var subgroups = await database.GetLookupSubGroupNamesAsync(group.Id);
                    AvailableSubGroups.Clear();
                    foreach (var subgroup in subgroups)
                    {
                        AvailableSubGroups.Add(subgroup);
                    }
                }
                else
                {
                    AvailableSubGroups.Clear();
                }
            }
            catch (Exception ex)
            {
                App.Log($"Error loading subgroups: {ex.Message}");
                AvailableSubGroups.Clear();
            }
        }

        [ObservableProperty]
        public partial string SubGroupName { get; set; } = "";

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

            if (string.IsNullOrWhiteSpace(GroupName))
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

                // Get the group ID
                var group = await database.GetLookupGroupByNameAsync(GroupName);
                if (group == null)
                {
                    await MauiProgram.DisplayAlertAsync("Validation Error", "Selected group not found.", "OK");
                    return;
                }

                // Get the subgroup ID if a subgroup is selected
                Guid? subGroupId = null;
                if (!string.IsNullOrWhiteSpace(SubGroupName))
                {
                    var subgroups = await database.GetLookupSubGroupsAsync(group.Id);
                    var subgroup = subgroups.FirstOrDefault(sg => sg.Name == SubGroupName);
                    if (subgroup != null)
                    {
                        subGroupId = subgroup.Id;
                    }
                }

                // Check if item already exists (case-insensitive)
                var exists = await database.LookupItemExistsAsync(Name, GroupName, IsNew ? null : Item.Id);
                if (exists)
                {
                    await MauiProgram.DisplayAlertAsync("Validation Error", $"A {GroupName} with the name '{Name}' already exists.", "OK");
                    return;
                }

                // Update the item
                Item.Name = Name.Trim();
                Item.GroupId = group.Id;
                Item.SubGroupId = subGroupId;
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
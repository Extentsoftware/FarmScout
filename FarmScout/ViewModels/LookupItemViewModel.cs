using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FarmScout.Models;
using FarmScout.Services;

namespace FarmScout.ViewModels
{
    public class LookupItemViewModel : INotifyPropertyChanged
    {
        private readonly FarmScoutDatabase _database;
        private readonly INavigationService _navigationService;
        
        private LookupItem? _item;
        private bool _isNew;
        private string _name = "";
        private string _group = "";
        private string _description = "";
        private bool _isLoading;

        public LookupItemViewModel(FarmScoutDatabase database, INavigationService navigationService)
        {
            _database = database;
            _navigationService = navigationService;
            
            SaveCommand = new Command(async () => await SaveAsync());
            CancelCommand = new Command(async () => await CancelAsync());
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
                    Description = value.Description;
                }
                OnPropertyChanged();
            }
        }

        public bool IsNew
        {
            get => _isNew;
            set
            {
                _isNew = value;
                OnPropertyChanged();
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        public string Group
        {
            get => _group;
            set
            {
                _group = value;
                OnPropertyChanged();
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                _description = value;
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

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public string[] AvailableGroups => LookupGroups.AvailableGroups;

        private async Task SaveAsync()
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

        private async Task CancelAsync()
        {
            await _navigationService.GoBackAsync();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 
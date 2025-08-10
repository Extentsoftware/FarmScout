using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmScout.Models;
using FarmScout.Services;
using System.Collections.ObjectModel;

namespace FarmScout.ViewModels
{
    public partial class DatabaseResetViewModel : ObservableObject
    {
        private readonly IDatabaseResetService _resetService;

        [ObservableProperty]
        public partial bool IsLoading { get; set; }

        [ObservableProperty]
        public partial string StatusMessage { get; set; } = string.Empty;

        [ObservableProperty]
        public partial DatabaseResetInfo? DatabaseInfo { get; set; }

        [ObservableProperty]
        public partial bool ShowConfirmationDialog { get; set; }

        [ObservableProperty]
        public partial string ConfirmationMessage { get; set; } = string.Empty;

        [ObservableProperty]
        public partial bool IsResetInProgress { get; set; }

        [ObservableProperty]
        public partial bool IsBackupInProgress { get; set; }

        [ObservableProperty]
        public partial bool IsRestoreInProgress { get; set; }

        public ObservableCollection<string> BackupFiles { get; } = new();

        public DatabaseResetViewModel(IDatabaseResetService resetService)
        {
            _resetService = resetService;
        }

        [RelayCommand]
        private async Task LoadDatabaseInfoAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Loading database information...";

                DatabaseInfo = await _resetService.GetDatabaseInfoAsync();
                
                if (DatabaseInfo != null)
                {
                    StatusMessage = $"Database loaded: {DatabaseInfo.TotalRecordCount} records, {DatabaseInfo.DatabaseSizeFormatted}";
                }
                else
                {
                    StatusMessage = "Failed to load database information";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading database info: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task ResetDatabaseAsync()
        {
            try
            {
                IsResetInProgress = true;
                StatusMessage = "Resetting database...";

                var result = await _resetService.ResetDatabaseAsync();
                
                if (result)
                {
                    StatusMessage = "Database reset completed successfully";
                    await LoadDatabaseInfoAsync(); // Refresh the info
                }
                else
                {
                    StatusMessage = "Database reset failed";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error during database reset: {ex.Message}";
            }
            finally
            {
                IsResetInProgress = false;
            }
        }

        [RelayCommand]
        private async Task ResetDatabaseWithSeedingAsync()
        {
            try
            {
                IsResetInProgress = true;
                StatusMessage = "Resetting database with seeding...";

                var result = await _resetService.ResetDatabaseWithSeedingAsync();
                
                if (result)
                {
                    StatusMessage = "Database reset with seeding completed successfully";
                    await LoadDatabaseInfoAsync(); // Refresh the info
                }
                else
                {
                    StatusMessage = "Database reset with seeding failed";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error during database reset with seeding: {ex.Message}";
            }
            finally
            {
                IsResetInProgress = false;
            }
        }

        [RelayCommand]
        private async Task BackupDatabaseAsync()
        {
            try
            {
                IsBackupInProgress = true;
                StatusMessage = "Creating database backup...";

                // Create backup filename with timestamp
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupPath = Path.Combine(FileSystem.AppDataDirectory, "Backups", $"farmscout_backup_{timestamp}.db3");

                var result = await _resetService.BackupDatabaseAsync(backupPath);
                
                if (result)
                {
                    StatusMessage = $"Database backup completed successfully: {Path.GetFileName(backupPath)}";
                    await RefreshBackupFilesAsync();
                }
                else
                {
                    StatusMessage = "Database backup failed";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error during database backup: {ex.Message}";
            }
            finally
            {
                IsBackupInProgress = false;
            }
        }

        [RelayCommand]
        private async Task RestoreDatabaseAsync(string backupPath)
        {
            if (string.IsNullOrEmpty(backupPath))
            {
                StatusMessage = "No backup file selected";
                return;
            }

            try
            {
                IsRestoreInProgress = true;
                StatusMessage = "Restoring database from backup...";

                var result = await _resetService.RestoreDatabaseAsync(backupPath);
                
                if (result)
                {
                    StatusMessage = "Database restore completed successfully. Please restart the application.";
                    await LoadDatabaseInfoAsync(); // Refresh the info
                }
                else
                {
                    StatusMessage = "Database restore failed";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error during database restore: {ex.Message}";
            }
            finally
            {
                IsRestoreInProgress = false;
            }
        }

        [RelayCommand]
        private async Task RefreshBackupFilesAsync()
        {
            try
            {
                BackupFiles.Clear();
                var backupDir = Path.Combine(FileSystem.AppDataDirectory, "Backups");
                
                if (Directory.Exists(backupDir))
                {
                    var backupFiles = Directory.GetFiles(backupDir, "farmscout_backup_*.db3")
                        .OrderByDescending(f => File.GetLastWriteTime(f))
                        .Select(f => Path.GetFileName(f))
                        .ToList();

                    foreach (var file in backupFiles)
                    {
                        BackupFiles.Add(file);
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error refreshing backup files: {ex.Message}";
            }
        }

        [RelayCommand]
        private void ShowResetConfirmation()
        {
            ConfirmationMessage = "Are you sure you want to reset the database? This will delete ALL data and cannot be undone.";
            ShowConfirmationDialog = true;
        }

        [RelayCommand]
        private void HideConfirmationDialog()
        {
            ShowConfirmationDialog = false;
            ConfirmationMessage = string.Empty;
        }

        [RelayCommand]
        private async Task ConfirmResetAsync()
        {
            HideConfirmationDialog();
            await ResetDatabaseWithSeedingAsync();
        }

        [RelayCommand]
        private async Task DeleteBackupAsync(string backupFileName)
        {
            if (string.IsNullOrEmpty(backupFileName))
                return;

            try
            {
                var backupPath = Path.Combine(FileSystem.AppDataDirectory, "Backups", backupFileName);
                
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                    StatusMessage = $"Backup file deleted: {backupFileName}";
                    await RefreshBackupFilesAsync();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error deleting backup file: {ex.Message}";
            }
        }

        public async Task InitializeAsync()
        {
            await LoadDatabaseInfoAsync();
            await RefreshBackupFilesAsync();
        }
    }
} 
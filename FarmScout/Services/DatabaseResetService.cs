using FarmScout.Models;

namespace FarmScout.Services
{
    public interface IDatabaseResetService
    {
        Task<bool> ResetDatabaseAsync();
        Task<bool> ResetDatabaseWithSeedingAsync();
        Task<DatabaseResetInfo> GetDatabaseInfoAsync();
        Task<bool> BackupDatabaseAsync(string backupPath);
        Task<bool> RestoreDatabaseAsync(string backupPath);
    }

    public class DatabaseResetService : IDatabaseResetService
    {
        private readonly IFarmScoutDatabase _database;

        public DatabaseResetService(IFarmScoutDatabase database)
        {
            _database = database;
        }

        public async Task<bool> ResetDatabaseAsync()
        {
            try
            {
                App.Log("DatabaseResetService: Starting database reset...");
                
                var result = await _database.ResetDatabaseAsync();
                
                if (result)
                {
                    App.Log("DatabaseResetService: Database reset completed successfully");
                }
                else
                {
                    App.Log("DatabaseResetService: Database reset failed");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                App.Log($"DatabaseResetService: Error during database reset: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ResetDatabaseWithSeedingAsync()
        {
            try
            {
                App.Log("DatabaseResetService: Starting database reset with seeding...");
                
                var result = await _database.ResetDatabaseWithSeedingAsync();
                
                if (result)
                {
                    App.Log("DatabaseResetService: Database reset with seeding completed successfully");
                }
                else
                {
                    App.Log("DatabaseResetService: Database reset with seeding failed");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                App.Log($"DatabaseResetService: Error during database reset with seeding: {ex.Message}");
                return false;
            }
        }

        public async Task<DatabaseResetInfo> GetDatabaseInfoAsync()
        {
            try
            {
                App.Log("DatabaseResetService: Getting database info...");
                
                var info = await _database.GetDatabaseInfoAsync();
                
                App.Log($"DatabaseResetService: Retrieved database info - {info.TotalRecordCount} total records, {info.DatabaseSizeFormatted}");
                
                return info;
            }
            catch (Exception ex)
            {
                App.Log($"DatabaseResetService: Error getting database info: {ex.Message}");
                return new DatabaseResetInfo
                {
                    IsReady = false,
                    DatabasePath = "Error retrieving path",
                    DatabaseSizeBytes = 0,
                    LastModified = DateTime.MinValue,
                    TotalRecordCount = 0
                };
            }
        }

        public async Task<bool> BackupDatabaseAsync(string backupPath)
        {
            try
            {
                App.Log($"DatabaseResetService: Starting database backup to {backupPath}...");
                
                // Get the current database path
                var info = await _database.GetDatabaseInfoAsync();
                var sourcePath = info.DatabasePath;
                
                if (!File.Exists(sourcePath))
                {
                    App.Log("DatabaseResetService: Source database file not found");
                    return false;
                }

                // Ensure backup directory exists
                var backupDir = Path.GetDirectoryName(backupPath);
                if (!string.IsNullOrEmpty(backupDir) && !Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }

                // Copy the database file
                File.Copy(sourcePath, backupPath, true);
                
                App.Log($"DatabaseResetService: Database backup completed successfully to {backupPath}");
                return true;
            }
            catch (Exception ex)
            {
                App.Log($"DatabaseResetService: Error during database backup: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RestoreDatabaseAsync(string backupPath)
        {
            try
            {
                App.Log($"DatabaseResetService: Starting database restore from {backupPath}...");
                
                if (!File.Exists(backupPath))
                {
                    App.Log("DatabaseResetService: Backup file not found");
                    return false;
                }

                // Get the current database path
                var info = await _database.GetDatabaseInfoAsync();
                var targetPath = info.DatabasePath;
                
                // Close the database connection first
                // Note: This is a limitation - we can't easily close the connection from here
                // In a real implementation, you might need to restart the app or handle this differently
                
                // Copy the backup file to the database location
                File.Copy(backupPath, targetPath, true);
                
                App.Log($"DatabaseResetService: Database restore completed successfully from {backupPath}");
                
                // Note: The application may need to be restarted for the restored database to be properly loaded
                return true;
            }
            catch (Exception ex)
            {
                App.Log($"DatabaseResetService: Error during database restore: {ex.Message}");
                return false;
            }
        }
    }
} 
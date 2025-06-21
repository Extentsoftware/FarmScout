using SQLite;
using System.Collections.Generic;
using System.Threading.Tasks;
using FarmScout.Models;
using System.IO;
using System.Text.Json;

namespace FarmScout.Services
{
    public class FarmScoutDatabase
    {
        private readonly SQLiteAsyncConnection _database;

        public FarmScoutDatabase(string dbPath)
        {
            App.Log($"Initializing database at path: {dbPath}");
            
            // Initialize SQLite
            SQLitePCL.Batteries_V2.Init();
            
            _database = new SQLiteAsyncConnection(dbPath);
            
            // Initialize database synchronously to avoid hanging
            try
            {
                InitializeDatabaseAsync().Wait(TimeSpan.FromSeconds(10));
            }
            catch (Exception ex)
            {
                App.Log($"Database initialization failed: {ex.Message}");
                // Continue anyway - the database might still work
            }
        }

        private async Task InitializeDatabaseAsync()
        {
            try
            {
                App.Log("Creating database tables...");
                
                App.Log("Creating Observation table...");
                var syncConnection = new SQLiteConnection(_database.DatabasePath);
                syncConnection.CreateTable<Observation>();
                App.Log("Observation table created successfully using sync method");
                
                App.Log("Creating TaskItem table...");
                syncConnection.CreateTable<TaskItem>();
                App.Log("TaskItem table created successfully");
                
                App.Log("Creating ObservationPhoto table...");
                syncConnection.CreateTable<ObservationPhoto>();
                App.Log("ObservationPhoto table created successfully");
                
                App.Log("Creating ObservationLocation table...");
                syncConnection.CreateTable<ObservationLocation>();
                App.Log("ObservationLocation table created successfully");
                
                App.Log("Creating FarmLocation table...");
                syncConnection.CreateTable<FarmLocation>();
                App.Log("FarmLocation table created successfully");
                
                App.Log("Database initialization completed successfully");
                
                // Log the number of observations in the database
                var count = await _database.Table<Observation>().CountAsync();
                App.Log($"Database initialized. Current observation count: {count}");
                
                // List all observations for debugging
                var allObservations = await _database.Table<Observation>().ToListAsync();
                foreach (var obs in allObservations)
                {
                    App.Log($"Existing observation: ID={obs.Id}, Types={obs.ObservationTypes}, Timestamp={obs.Timestamp}");
                }
            }
            catch (Exception ex)
            {
                App.Log($"Error initializing database: {ex.Message}");
                App.Log($"Exception details: {ex}");
                throw;
            }
        }

        // Observation CRUD
        public async Task<int> AddObservationAsync(Observation obs)
        {
            try
            {
                App.Log($"AddObservationAsync called: {JsonSerializer.Serialize(obs)}");
                var result = await _database.InsertAsync(obs);
                App.Log($"Observation added with ID: {obs.Id}");
                return result;
            }
            catch (Exception ex)
            {
                App.Log($"Error adding observation: {ex.Message}");
                throw;
            }
        }
        
        public async Task<List<Observation>> GetObservationsAsync()
        {
            try
            {
                App.Log("GetObservationsAsync called");
                var observations = await _database.Table<Observation>().ToListAsync();
                App.Log($"Retrieved {observations.Count} observations from database");
                
                // Log details of each observation
                foreach (var obs in observations)
                {
                    App.Log($"Observation: ID={obs.Id}, Types={obs.ObservationTypes}, Timestamp={obs.Timestamp}, Severity={obs.Severity}");
                }
                
                return observations;
            }
            catch (Exception ex)
            {
                App.Log($"Error retrieving observations: {ex.Message}");
                throw;
            }
        }
        
        public Task<int> UpdateObservationAsync(Observation obs) => _database.UpdateAsync(obs);
        public Task<int> DeleteObservationAsync(Observation obs) => _database.DeleteAsync(obs);

        // TaskItem CRUD
        public Task<int> AddTaskAsync(TaskItem task) => _database.InsertAsync(task);
        public Task<List<TaskItem>> GetTasksForObservationAsync(int observationId) =>
            _database.Table<TaskItem>().Where(t => t.ObservationId == observationId).ToListAsync();
        public Task<int> UpdateTaskAsync(TaskItem task) => _database.UpdateAsync(task);
        public Task<int> DeleteTaskAsync(TaskItem task) => _database.DeleteAsync(task);

        // ObservationPhoto CRUD
        public Task<int> AddPhotoAsync(ObservationPhoto photo) => _database.InsertAsync(photo);
        public Task<List<ObservationPhoto>> GetPhotosForObservationAsync(int observationId) =>
            _database.Table<ObservationPhoto>().Where(p => p.ObservationId == observationId).ToListAsync();
        public Task<int> UpdatePhotoAsync(ObservationPhoto photo) => _database.UpdateAsync(photo);
        public Task<int> DeletePhotoAsync(ObservationPhoto photo) => _database.DeleteAsync(photo);

        // ObservationLocation CRUD
        public Task<int> AddLocationAsync(ObservationLocation location) => _database.InsertAsync(location);
        public Task<List<ObservationLocation>> GetLocationsForObservationAsync(int observationId) =>
            _database.Table<ObservationLocation>().Where(l => l.ObservationId == observationId).ToListAsync();
        public Task<int> UpdateLocationAsync(ObservationLocation location) => _database.UpdateAsync(location);
        public Task<int> DeleteLocationAsync(ObservationLocation location) => _database.DeleteAsync(location);
    }
} 
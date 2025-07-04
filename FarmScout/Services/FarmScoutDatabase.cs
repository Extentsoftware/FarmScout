using SQLite;
using System.Collections.Generic;
using System.Threading.Tasks;
using FarmScout.Models;
using System.IO;
using System.Text.Json;
using FarmScout.Views;

namespace FarmScout.Services
{
    public class FarmScoutDatabase
    {
        private readonly SQLiteAsyncConnection _database;
        public const SQLite.SQLiteOpenFlags Flags =
                   // open the database in read/write mode
                   SQLite.SQLiteOpenFlags.ReadWrite |
                   // create the database if it doesn't exist
                   SQLite.SQLiteOpenFlags.Create |
                   // enable multi-threaded database access
                   SQLite.SQLiteOpenFlags.SharedCache;
        public FarmScoutDatabase()
        {
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "farmscout.db3");
            App.Log($"Initializing database at path: {dbPath}");

            // Initialize SQLite
            SQLitePCL.Batteries_V2.Init();
            

            _database = new SQLiteAsyncConnection(dbPath, Flags);

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
                
                await _database.EnableWriteAheadLoggingAsync();

                App.Log("Creating Observation table...");

                await _database.CreateTableAsync<Observation>();
                App.Log("Observation table created successfully using sync method");
                
                App.Log("Creating TaskItem table...");
                await _database.CreateTableAsync<TaskItem>();
                App.Log("TaskItem table created successfully");
                
                App.Log("Creating ObservationPhoto table...");
                await _database.CreateTableAsync<ObservationPhoto>();
                App.Log("ObservationPhoto table created successfully");
                
                App.Log("Creating ObservationLocation table...");
                await _database.CreateTableAsync<ObservationLocation>();
                App.Log("ObservationLocation table created successfully");
                
                App.Log("Creating FarmLocation table...");
                await _database.CreateTableAsync<FarmLocation>();
                App.Log("FarmLocation table created successfully");

                App.Log("Creating LookupItem table...");
                await _database.CreateTableAsync<LookupItem>();
                App.Log("LookupItem table created successfully");

                // Seed initial lookup data
                await SeedLookupDataAsync();

                App.Log("Database initialization completed successfully");
                
                // Log the number of observations in the database
                var count = await _database.Table<Observation>().CountAsync();
                App.Log($"Database initialized. Current observation count: {count}");

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

        // LookupItem CRUD
        public async Task<int> AddLookupItemAsync(LookupItem item)
        {
            try
            {
                item.CreatedAt = DateTime.Now;
                item.UpdatedAt = DateTime.Now;
                var result = await _database.InsertAsync(item);
                App.Log($"LookupItem added with ID: {item.Id}");
                return result;
            }
            catch (Exception ex)
            {
                App.Log($"Error adding lookup item: {ex.Message}");
                throw;
            }
        }

        public async Task<List<LookupItem>> GetLookupItemsAsync()
        {
            try
            {
                var items = await _database.Table<LookupItem>()
                    .Where(l => l.IsActive)
                    .OrderBy(l => l.Group)
                    .ThenBy(l => l.Name)
                    .ToListAsync();
                App.Log($"Retrieved {items.Count} lookup items from database");
                return items;
            }
            catch (Exception ex)
            {
                App.Log($"Error retrieving lookup items: {ex.Message}");
                throw;
            }
        }

        public async Task<List<LookupItem>> GetLookupItemsByGroupAsync(string group)
        {
            try
            {
                var items = await _database.Table<LookupItem>()
                    .Where(l => l.Group == group && l.IsActive)
                    .OrderBy(l => l.Name)
                    .ToListAsync();
                return items;
            }
            catch (Exception ex)
            {
                App.Log($"Error retrieving lookup items by group: {ex.Message}");
                throw;
            }
        }

        public async Task<List<string>> GetLookupGroupsAsync()
        {
            try
            {
                var items = await _database.Table<LookupItem>()
                    .Where(l => l.IsActive)
                    .ToListAsync();
                
                var groups = items.Select(l => l.Group)
                    .Distinct()
                    .OrderBy(g => g)
                    .ToList();
                
                return groups;
            }
            catch (Exception ex)
            {
                App.Log($"Error retrieving lookup groups: {ex.Message}");
                throw;
            }
        }

        public async Task<int> UpdateLookupItemAsync(LookupItem item)
        {
            try
            {
                item.UpdatedAt = DateTime.Now;
                var result = await _database.UpdateAsync(item);
                App.Log($"LookupItem updated with ID: {item.Id}");
                return result;
            }
            catch (Exception ex)
            {
                App.Log($"Error updating lookup item: {ex.Message}");
                throw;
            }
        }

        public async Task<int> DeleteLookupItemAsync(LookupItem item)
        {
            try
            {
                // Soft delete by setting IsActive to false
                item.IsActive = false;
                item.UpdatedAt = DateTime.Now;
                var result = await _database.UpdateAsync(item);
                App.Log($"LookupItem soft deleted with ID: {item.Id}");
                return result;
            }
            catch (Exception ex)
            {
                App.Log($"Error deleting lookup item: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> LookupItemExistsAsync(string name, string group, int? excludeId = null)
        {
            try
            {
                var query = _database.Table<LookupItem>()
                    .Where(l => l.Name.ToLower() == name.ToLower() && 
                               l.Group == group && 
                               l.IsActive);
                
                if (excludeId.HasValue)
                {
                    query = query.Where(l => l.Id != excludeId.Value);
                }
                
                var count = await query.CountAsync();
                return count > 0;
            }
            catch (Exception ex)
            {
                App.Log($"Error checking if lookup item exists: {ex.Message}");
                throw;
            }
        }

        private async Task SeedLookupDataAsync()
        {
            try
            {
                // Check if data already exists
                var existingCount = await _database.Table<LookupItem>().CountAsync();
                if (existingCount > 0)
                {
                    App.Log($"Lookup table already has {existingCount} items, skipping seed");
                    return;
                }

                App.Log("Seeding lookup table with initial data...");

                var seedData = new List<LookupItem>
                {
                    // Crop Types
                    new() { Name = "Corn", Group = "Crop Types", Description = "Maize crop for grain or silage" },
                    new() { Name = "Soybeans", Group = "Crop Types", Description = "Legume crop for oil and protein" },
                    new() { Name = "Wheat", Group = "Crop Types", Description = "Cereal grain crop" },
                    new() { Name = "Cotton", Group = "Crop Types", Description = "Fiber crop" },
                    new() { Name = "Rice", Group = "Crop Types", Description = "Staple grain crop" },

                    // Diseases
                    new() { Name = "Rust", Group = "Diseases", SubGroup = "Fungal", Description = "Fungal disease affecting leaves and stems" },
                    new() { Name = "Blight", Group = "Diseases", SubGroup = "Bacterial", Description = "Rapid plant disease causing wilting" },
                    new() { Name = "Mildew", Group = "Diseases", SubGroup = "Fungal", Description = "Fungal growth on plant surfaces" },
                    new() { Name = "Root Rot", Group = "Diseases", SubGroup = "Fungal", Description = "Fungal disease affecting plant roots" },
                    new() { Name = "Leaf Spot", Group = "Diseases", SubGroup = "Fungal", Description = "Fungal disease causing spots on leaves" },

                    // Pests
                    new() { Name = "Aphids", Group = "Pests", SubGroup = "Insects", Description = "Small sap-sucking insects" },
                    new() { Name = "Corn Borer", Group = "Pests", SubGroup = "Insects", Description = "Larva that bores into corn stalks" },
                    new() { Name = "Spider Mites", Group = "Pests", SubGroup = "Mites", Description = "Tiny arachnids that feed on plant sap" },
                    new() { Name = "Cutworms", Group = "Pests", SubGroup = "Insects", Description = "Caterpillars that cut plant stems" },
                    new() { Name = "Wireworms", Group = "Pests", SubGroup = "Insects", Description = "Click beetle larvae that damage roots" },

                    // Chemicals
                    new() { Name = "Glyphosate", Group = "Chemicals", SubGroup = "Herbicide", Description = "Broad-spectrum herbicide" },
                    new() { Name = "Atrazine", Group = "Chemicals", SubGroup = "Herbicide", Description = "Selective herbicide for corn" },
                    new() { Name = "2,4-D", Group = "Chemicals", SubGroup = "Herbicide", Description = "Selective herbicide for broadleaf weeds" },
                    new() { Name = "Paraquat", Group = "Chemicals", SubGroup = "Herbicide", Description = "Contact herbicide" },
                    new() { Name = "Dicamba", Group = "Chemicals", SubGroup = "Herbicide", Description = "Selective herbicide for broadleaf weeds" },
                    new() { Name = "Chlorothalonil", Group = "Chemicals", SubGroup = "Fungicide", Description = "Protectant fungicide for foliar diseases" },
                    new() { Name = "Azoxystrobin", Group = "Chemicals", SubGroup = "Fungicide", Description = "Systemic fungicide for disease control" },
                    new() { Name = "Malathion", Group = "Chemicals", SubGroup = "Insecticide", Description = "Organophosphate insecticide" },
                    new() { Name = "Carbaryl", Group = "Chemicals", SubGroup = "Insecticide", Description = "Carbamate insecticide for pest control" },
                    new() { Name = "Gibberellic Acid", Group = "Chemicals", SubGroup = "Growth Regulator", Description = "Plant growth regulator" },

                    // Fertilizers
                    new() { Name = "Urea", Group = "Fertilizers", SubGroup = "Nitrogen", Description = "Nitrogen fertilizer (46-0-0)" },
                    new() { Name = "Ammonium Nitrate", Group = "Fertilizers", SubGroup = "Nitrogen", Description = "Nitrogen fertilizer (34-0-0)" },
                    new() { Name = "Triple Superphosphate", Group = "Fertilizers", SubGroup = "Phosphorus", Description = "Phosphorus fertilizer (0-46-0)" },
                    new() { Name = "Potassium Chloride", Group = "Fertilizers", SubGroup = "Potassium", Description = "Potassium fertilizer (0-0-60)" },
                    new() { Name = "NPK 10-10-10", Group = "Fertilizers", SubGroup = "Nitrogen", Description = "Balanced fertilizer" },
                    new() { Name = "Compost", Group = "Fertilizers", SubGroup = "Organic", Description = "Organic soil amendment" },

                    // Soil Types
                    new() { Name = "Clay", Group = "Soil Types", SubGroup = "Mineral", Description = "Fine-grained soil with high water retention" },
                    new() { Name = "Silt", Group = "Soil Types", SubGroup = "Mineral", Description = "Medium-grained soil" },
                    new() { Name = "Sandy", Group = "Soil Types", SubGroup = "Mineral", Description = "Coarse-grained soil with good drainage" },
                    new() { Name = "Loam", Group = "Soil Types", SubGroup = "Mixed", Description = "Well-balanced soil mixture" },
                    new() { Name = "Peat", Group = "Soil Types", SubGroup = "Organic", Description = "Organic-rich soil" },

                    // Weather Conditions
                    new() { Name = "Sunny", Group = "Weather Conditions", SubGroup = "Temperature", Description = "Clear skies with full sun" },
                    new() { Name = "Cloudy", Group = "Weather Conditions", SubGroup = "Pressure", Description = "Overcast conditions" },
                    new() { Name = "Rainy", Group = "Weather Conditions", SubGroup = "Precipitation", Description = "Precipitation occurring" },
                    new() { Name = "Windy", Group = "Weather Conditions", SubGroup = "Wind", Description = "High wind conditions" },
                    new() { Name = "Foggy", Group = "Weather Conditions", SubGroup = "Humidity", Description = "Low visibility due to fog" },

                    // Growth Stages
                    new() { Name = "Germination", Group = "Growth Stages", SubGroup = "Vegetative", Description = "Seed sprouting and root development" },
                    new() { Name = "Vegetative", Group = "Growth Stages", SubGroup = "Vegetative", Description = "Leaf and stem growth" },
                    new() { Name = "Flowering", Group = "Growth Stages", SubGroup = "Reproductive", Description = "Flower development and pollination" },
                    new() { Name = "Fruiting", Group = "Growth Stages", SubGroup = "Reproductive", Description = "Fruit or grain development" },
                    new() { Name = "Maturity", Group = "Growth Stages", SubGroup = "Maturity", Description = "Full development and harvest ready" },

                    // Damage Types
                    new() { Name = "Hail Damage", Group = "Damage Types", SubGroup = "Environmental", Description = "Physical damage from hail stones" },
                    new() { Name = "Wind Damage", Group = "Damage Types", SubGroup = "Environmental", Description = "Damage from high winds" },
                    new() { Name = "Drought Stress", Group = "Damage Types", SubGroup = "Environmental", Description = "Damage from lack of water" },
                    new() { Name = "Flood Damage", Group = "Damage Types", SubGroup = "Environmental", Description = "Damage from excess water" },
                    new() { Name = "Frost Damage", Group = "Damage Types", SubGroup = "Environmental", Description = "Damage from freezing temperatures" },

                    // Treatment Methods
                    new() { Name = "Chemical Treatment", Group = "Treatment Methods", SubGroup = "Chemical", Description = "Application of pesticides or herbicides" },
                    new() { Name = "Biological Control", Group = "Treatment Methods", SubGroup = "Biological", Description = "Use of natural predators or beneficial organisms" },
                    new() { Name = "Cultural Control", Group = "Treatment Methods", SubGroup = "Cultural", Description = "Management practices to prevent problems" },
                    new() { Name = "Mechanical Control", Group = "Treatment Methods", SubGroup = "Mechanical", Description = "Physical removal or barriers" },
                    new() { Name = "Integrated Pest Management", Group = "Treatment Methods", SubGroup = "Integrated", Description = "Combined approach using multiple methods" }
                };

                foreach (var item in seedData)
                {
                    await AddLookupItemAsync(item);
                }

                App.Log($"Successfully seeded {seedData.Count} lookup items");
            }
            catch (Exception ex)
            {
                App.Log($"Error seeding lookup data: {ex.Message}");
                // Don't throw - seeding failure shouldn't prevent app startup
            }
        }
    }
} 
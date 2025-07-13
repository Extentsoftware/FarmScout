using SQLite;
using FarmScout.Models;
using System.Text.Json;

namespace FarmScout.Services
{
    public class FarmScoutDatabase : IFarmScoutDatabase
    {
        public bool IsReady { get; set; }

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
            IsReady = false;

            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "farmscout.db3");
            App.Log($"Initializing database at path: {dbPath}");

            // Initialize SQLite
            SQLitePCL.Batteries_V2.Init();

            _database = new SQLiteAsyncConnection(dbPath, Flags);

            MainThread.InvokeOnMainThreadAsync(InitializeDatabaseAsync);
        }

        public async Task InitializeDatabaseAsync()
        {
            try
            {
                App.Log("Creating database tables...");

                _database.Trace = true;
                _database.Tracer = App.Log;

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

                App.Log("Creating ObservationType table...");
                await _database.CreateTableAsync<ObservationType>();
                App.Log("ObservationType table created successfully");

                App.Log("Creating ObservationTypeDataPoint table...");
                await _database.CreateTableAsync<ObservationTypeDataPoint>();
                App.Log("ObservationTypeDataPoint table created successfully");

                App.Log("Creating ObservationMetadata table...");
                await _database.CreateTableAsync<ObservationMetadata>();
                App.Log("ObservationMetadata table created successfully");

                // Seed initial lookup data
                await SeedLookupDataAsync();
                
                // Seed initial observation types and data points
                await SeedObservationTypesAsync();

                App.Log("Database initialization completed successfully");

                // Log the number of observations in the database
                var count = await _database.Table<Observation>().CountAsync();
                App.Log($"Database initialized. Current observation count: {count}");
                
                IsReady = true;
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
                    App.Log($"Observation: ID={obs.Id}, Timestamp={obs.Timestamp}, Severity={obs.Severity}");
                }

                return observations;
            }
            catch (Exception ex)
            {
                App.Log($"Error retrieving observations: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Observation>> GetObservationsAsync(int skip, int take)
        {
            try
            {
                App.Log($"GetObservationsAsync called with skip={skip}, take={take}");
                var observations = await _database.Table<Observation>()
                    .OrderByDescending(o => o.Timestamp)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync();
                App.Log($"Retrieved {observations.Count} observations from database (skip={skip}, take={take})");

                return observations;
            }
            catch (Exception ex)
            {
                App.Log($"Error retrieving observations: {ex.Message}");
                throw;
            }
        }

        public async Task<int> GetObservationsCountAsync()
        {
            try
            {
                var count = await _database.Table<Observation>().CountAsync();
                App.Log($"Total observations count: {count}");
                return count;
            }
            catch (Exception ex)
            {
                App.Log($"Error getting observations count: {ex.Message}");
                throw;
            }
        }

        public Task<int> UpdateObservationAsync(Observation obs) => _database.UpdateAsync(obs);
        public Task<int> DeleteObservationAsync(Observation obs) => _database.DeleteAsync(obs);

        // TaskItem CRUD
        public Task<int> AddTaskAsync(TaskItem task) => _database.InsertAsync(task);
        public Task<List<TaskItem>> GetTasksForObservationAsync(Guid observationId) =>
            _database.Table<TaskItem>().Where(t => t.ObservationId == observationId).ToListAsync();
        public Task<int> UpdateTaskAsync(TaskItem task) => _database.UpdateAsync(task);
        public Task<int> DeleteTaskAsync(TaskItem task) => _database.DeleteAsync(task);

        // ObservationPhoto CRUD
        public Task<int> AddPhotoAsync(ObservationPhoto photo) => _database.InsertAsync(photo);
        public Task<List<ObservationPhoto>> GetPhotosForObservationAsync(Guid observationId) =>
            _database.Table<ObservationPhoto>().Where(p => p.ObservationId == observationId).ToListAsync();
        public Task<int> UpdatePhotoAsync(ObservationPhoto photo) => _database.UpdateAsync(photo);
        public Task<int> DeletePhotoAsync(ObservationPhoto photo) => _database.DeleteAsync(photo);

        // ObservationLocation CRUD
        public Task<int> AddLocationAsync(ObservationLocation location) => _database.InsertAsync(location);
        public Task<List<ObservationLocation>> GetLocationsForObservationAsync(Guid observationId) =>
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

        public async Task<bool> LookupItemExistsAsync(string name, string group, Guid? excludeId = null)
        {
            try
            {
                var query = _database.Table<LookupItem>()
                    .Where(l => l.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase) &&
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

        // ObservationType CRUD
        public Task<int> AddObservationTypeAsync(ObservationType observationType) => _database.InsertAsync(observationType);
        public Task<int> UpdateObservationTypeAsync(ObservationType observationType) => _database.UpdateAsync(observationType);
        public Task<int> DeleteObservationTypeAsync(ObservationType observationType) => _database.DeleteAsync(observationType);

        public async Task<List<ObservationType>> GetObservationTypesAsync()
        {
            try
            {
                var types = await _database.Table<ObservationType>()
                    .Where(t => t.IsActive)
                    .OrderBy(t => t.SortOrder)
                    .ThenBy(t => t.Name)
                    .ToListAsync();
                App.Log($"Retrieved {types.Count} observation types from database");
                return types;
            }
            catch (Exception ex)
            {
                App.Log($"Error retrieving observation types: {ex.Message}");
                throw;
            }
        }

        public async Task<ObservationType?> GetObservationTypeByIdAsync(Guid id)
        {
            try
            {
                var type = await _database.Table<ObservationType>()
                    .Where(t => t.Id == id && t.IsActive)
                    .FirstOrDefaultAsync();
                return type;
            }
            catch (Exception ex)
            {
                App.Log($"Error retrieving observation type by ID: {ex.Message}");
                throw;
            }
        }

        public async Task<ObservationType?> GetObservationTypeByNameAsync(string name)
        {
            try
            {
                var type = await _database.Table<ObservationType>()
                    .Where(t => t.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase) && t.IsActive)
                    .FirstOrDefaultAsync();
                return type;
            }
            catch (Exception ex)
            {
                App.Log($"Error retrieving observation type by name: {ex.Message}");
                throw;
            }
        }

        // ObservationTypeDataPoint CRUD
        public Task<int> AddObservationTypeDataPointAsync(ObservationTypeDataPoint dataPoint) => _database.InsertAsync(dataPoint);
        public Task<int> UpdateObservationTypeDataPointAsync(ObservationTypeDataPoint dataPoint) => _database.UpdateAsync(dataPoint);
        public Task<int> DeleteObservationTypeDataPointAsync(ObservationTypeDataPoint dataPoint) => _database.DeleteAsync(dataPoint);

        public async Task<List<ObservationTypeDataPoint>> GetDataPointsForObservationTypeAsync(Guid observationTypeId)
        {
            try
            {
                var dataPoints = await _database.Table<ObservationTypeDataPoint>()
                    .Where(d => d.ObservationTypeId == observationTypeId && d.IsActive)
                    .OrderBy(d => d.SortOrder)
                    .ThenBy(d => d.Label)
                    .ToListAsync();
                App.Log($"Retrieved {dataPoints.Count} data points for observation type {observationTypeId}");
                return dataPoints;
            }
            catch (Exception ex)
            {
                App.Log($"Error retrieving data points for observation type: {ex.Message}");
                throw;
            }
        }

        // ObservationMetadata CRUD
        public Task<int> AddObservationMetadataAsync(ObservationMetadata metadata) => _database.InsertAsync(metadata);
        public Task<int> UpdateObservationMetadataAsync(ObservationMetadata metadata) => _database.UpdateAsync(metadata);
        public Task<int> DeleteObservationMetadataAsync(ObservationMetadata metadata) => _database.DeleteAsync(metadata);

        public async Task<List<ObservationMetadata>> GetMetadataForObservationAsync(Guid observationId)
        {
            try
            {
                var metadata = await _database.Table<ObservationMetadata>()
                    .Where(m => m.ObservationId == observationId)
                    .ToListAsync();
                App.Log($"Retrieved {metadata.Count} metadata items for observation {observationId}");
                return metadata;
            }
            catch (Exception ex)
            {
                App.Log($"Error retrieving metadata for observation: {ex.Message}");
                throw;
            }
        }

        public async Task<List<ObservationMetadata>> GetMetadataForObservationAndTypeAsync(Guid observationId, Guid observationTypeId)
        {
            try
            {
                var metadata = await _database.Table<ObservationMetadata>()
                    .Where(m => m.ObservationId == observationId && m.ObservationTypeId == observationTypeId)
                    .ToListAsync();
                App.Log($"Retrieved {metadata.Count} metadata items for observation {observationId} and type {observationTypeId}");
                return metadata;
            }
            catch (Exception ex)
            {
                App.Log($"Error retrieving metadata for observation and type: {ex.Message}");
                throw;
            }
        }

        private async Task SeedObservationTypesAsync()
        {
            try
            {
                // Check if data already exists
                var existingCount = await _database.Table<ObservationType>().CountAsync();
                if (existingCount > 0)
                {
                    App.Log($"ObservationType table already has {existingCount} items, skipping seed");
                    return;
                }

                App.Log("Seeding observation types with initial data...");

                var observationTypes = new List<ObservationType>
                {
                    new() { Name = "Disease", Description = "Plant disease observations", Icon = "🦠", Color = "#F44336", SortOrder = 1 },
                    new() { Name = "Dead Plant", Description = "Dead or dying plant observations", Icon = "💀", Color = "#9E9E9E", SortOrder = 2 },
                    new() { Name = "Pest", Description = "Pest infestation observations", Icon = "🐛", Color = "#FF9800", SortOrder = 3 },
                    new() { Name = "Damage", Description = "Plant damage observations", Icon = "💥", Color = "#795548", SortOrder = 4 },
                    new() { Name = "Growth", Description = "Plant growth observations", Icon = "🌱", Color = "#4CAF50", SortOrder = 5 },
                    new() { Name = "Harvest", Description = "Harvest observations", Icon = "🌾", Color = "#FFC107", SortOrder = 6 },
                    new() { Name = "Weather", Description = "Weather condition observations", Icon = "🌤️", Color = "#2196F3", SortOrder = 7 },
                    new() { Name = "Soil", Description = "Soil condition observations", Icon = "🌍", Color = "#8D6E63", SortOrder = 8 },
                    new() { Name = "Soil Moisture", Description = "Soil moisture observations", Icon = "💧", Color = "#00BCD4", SortOrder = 9 }
                };

                foreach (var type in observationTypes)
                {
                    await AddObservationTypeAsync(type);
                }

                // Seed data points for each observation type
                await SeedDataPointsAsync(observationTypes);

                App.Log($"Successfully seeded {observationTypes.Count} observation types");
            }
            catch (Exception ex)
            {
                App.Log($"Error seeding observation types: {ex.Message}");
                // Don't throw - seeding failure shouldn't prevent app startup
            }
        }

        private async Task SeedDataPointsAsync(List<ObservationType> observationTypes)
        {
            try
            {
                var diseaseType = observationTypes.First(t => t.Name == "Disease");
                var pestType = observationTypes.First(t => t.Name == "Pest");
                var harvestType = observationTypes.First(t => t.Name == "Harvest");
                var weatherType = observationTypes.First(t => t.Name == "Weather");
                var soilType = observationTypes.First(t => t.Name == "Soil");

                var dataPoints = new List<ObservationTypeDataPoint>
                {
                    // Disease data points
                    new() { ObservationTypeId = diseaseType.Id, Code = "disease_name", Label = "Disease Name", DataType = DataTypes.Lookup, LookupGroupName = "Diseases", IsRequired = true, SortOrder = 1 },
                    new() { ObservationTypeId = diseaseType.Id, Code = "affected_area", Label = "Affected Area %", DataType = DataTypes.Long, IsRequired = false, SortOrder = 2 },
                    new() { ObservationTypeId = diseaseType.Id, Code = "plant_count", Label = "Plant Count", DataType = DataTypes.Long, IsRequired = false, SortOrder = 3 },
                    new() { ObservationTypeId = diseaseType.Id, Code = "symptoms", Label = "Symptoms", DataType = DataTypes.String, IsRequired = false, SortOrder = 4 },

                    // Pest data points
                    new() { ObservationTypeId = pestType.Id, Code = "pest_name", Label = "Pest Name", DataType = DataTypes.Lookup, LookupGroupName = "Pests", IsRequired = true, SortOrder = 1 },
                    new() { ObservationTypeId = pestType.Id, Code = "pest_count", Label = "Pest Count", DataType = DataTypes.Long, IsRequired = false, SortOrder = 2 },
                    new() { ObservationTypeId = pestType.Id, Code = "damage_level", Label = "Damage Level", DataType = DataTypes.Long, IsRequired = false, SortOrder = 3 },
                    new() { ObservationTypeId = pestType.Id, Code = "infestation_area", Label = "Infestation Area", DataType = DataTypes.String, IsRequired = false, SortOrder = 4 },

                    // Harvest data points
                    new() { ObservationTypeId = harvestType.Id, Code = "crop_type", Label = "Crop Type", DataType = DataTypes.Lookup, LookupGroupName = "Crop Types", IsRequired = true, SortOrder = 1 },
                    new() { ObservationTypeId = harvestType.Id, Code = "weight_kg", Label = "Weight (kg)", DataType = DataTypes.Long, IsRequired = false, SortOrder = 2 },
                    new() { ObservationTypeId = harvestType.Id, Code = "quality", Label = "Quality", DataType = DataTypes.String, IsRequired = false, SortOrder = 3 },
                    new() { ObservationTypeId = harvestType.Id, Code = "yield_per_area", Label = "Yield per Area", DataType = DataTypes.Long, IsRequired = false, SortOrder = 4 },

                    // Weather data points
                    new() { ObservationTypeId = weatherType.Id, Code = "temperature", Label = "Temperature (°C)", DataType = DataTypes.Long, IsRequired = false, SortOrder = 1 },
                    new() { ObservationTypeId = weatherType.Id, Code = "humidity", Label = "Humidity (%)", DataType = DataTypes.Long, IsRequired = false, SortOrder = 2 },
                    new() { ObservationTypeId = weatherType.Id, Code = "wind_speed", Label = "Wind Speed", DataType = DataTypes.Long, IsRequired = false, SortOrder = 3 },
                    new() { ObservationTypeId = weatherType.Id, Code = "precipitation", Label = "Precipitation", DataType = DataTypes.Long, IsRequired = false, SortOrder = 4 },

                    // Soil data points
                    new() { ObservationTypeId = soilType.Id, Code = "ph_level", Label = "pH Level", DataType = DataTypes.Long, IsRequired = false, SortOrder = 1 },
                    new() { ObservationTypeId = soilType.Id, Code = "moisture", Label = "Moisture %", DataType = DataTypes.Long, IsRequired = false, SortOrder = 2 },
                    new() { ObservationTypeId = soilType.Id, Code = "temperature", Label = "Temperature (°C)", DataType = DataTypes.Long, IsRequired = false, SortOrder = 3 },
                    new() { ObservationTypeId = soilType.Id, Code = "nutrient_level", Label = "Nutrient Level", DataType = DataTypes.Long, IsRequired = false, SortOrder = 4 }
                };

                foreach (var dataPoint in dataPoints)
                {
                    await AddObservationTypeDataPointAsync(dataPoint);
                }

                App.Log($"Successfully seeded {dataPoints.Count} data points");
            }
            catch (Exception ex)
            {
                App.Log($"Error seeding data points: {ex.Message}");
                // Don't throw - seeding failure shouldn't prevent app startup
            }
        }
    }
} 
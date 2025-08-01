using SQLite;
using FarmScout.Models;
using System.Text.Json;
using System.Globalization;
using System.Text;

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

                App.Log("Creating LookupGroup table...");
                await _database.CreateTableAsync<LookupGroup>();
                App.Log("LookupGroup table created successfully");

                App.Log("Creating LookupSubGroup table...");
                await _database.CreateTableAsync<LookupSubGroup>();
                App.Log("LookupSubGroup table created successfully");

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


                // Create report groups table
                await _database.CreateTableAsync<ReportGroup>();
                App.Log("ReportGroup table created successfully");

                // Create markdown reports table
                await _database.CreateTableAsync<MarkdownReport>();
                App.Log("MarkdownReport table created successfully");
        
                // Seed initial report groups
                await SeedReportGroupsAsync();

                // Seed observations from CSV file
                await SeedObservationsFromCsvAsync();

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

        public async Task<List<Observation>> GetObservationsAsync(int skip, int take, FilterParameters filterParams)
        {
            try
            {
                App.Log($"GetObservationsAsync called with skip={skip}, take={take}, filters={JsonSerializer.Serialize(filterParams)}");
                
                var query = _database.Table<Observation>();

                // Apply filters
                if (filterParams.StartDate.HasValue)
                {
                    query = query.Where(o => o.Timestamp >= filterParams.StartDate.Value);
                }

                if (filterParams.EndDate.HasValue)
                {
                    query = query.Where(o => o.Timestamp <= filterParams.EndDate.Value);
                }

                if (!string.IsNullOrWhiteSpace(filterParams.SearchText))
                {
                    var searchText = filterParams.SearchText.ToLower();
                    query = query.Where(o =>
                        o.Notes.Contains(searchText, StringComparison.CurrentCultureIgnoreCase) ||
                        o.Summary.Contains(searchText, StringComparison.CurrentCultureIgnoreCase) ||
                        o.Severity.Contains(searchText, StringComparison.CurrentCultureIgnoreCase));
                }

                if (filterParams.FieldId.HasValue)
                {
                    query = query.Where(o => o.FarmLocationId == filterParams.FieldId.Value);
                }

                if (filterParams.ObservationTypeId.HasValue)
                {
                    // Filter by observation type using ObservationMetadata join
                    var observationIdsWithType = await _database.Table<ObservationMetadata>()
                        .Where(om => om.ObservationTypeId == filterParams.ObservationTypeId.Value)
                        .ToListAsync();
                    
                    var observationIds = observationIdsWithType.Select(om => om.ObservationId).ToList();
                    
                    if (observationIds.Count > 0)
                    {
                        query = query.Where(o => observationIds.Contains(o.Id));
                    }
                    else
                    {
                        // If no observations have this type, return empty result
                        query = query.Where(o => false);
                    }
                }

                // Apply sorting
                query = filterParams.SortBy.ToLower() switch
                {
                    "timestamp" => filterParams.SortAscending 
                        ? query.OrderBy(o => o.Timestamp)
                        : query.OrderByDescending(o => o.Timestamp),
                    "severity" => filterParams.SortAscending 
                        ? query.OrderBy(o => o.Severity)
                        : query.OrderByDescending(o => o.Severity),
                    "summary" => filterParams.SortAscending 
                        ? query.OrderBy(o => o.Summary)
                        : query.OrderByDescending(o => o.Summary),
                    _ => filterParams.SortAscending 
                        ? query.OrderBy(o => o.Timestamp)
                        : query.OrderByDescending(o => o.Timestamp)
                };

                var observations = await query
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync();
                
                App.Log($"Retrieved {observations.Count} observations from database with filters (skip={skip}, take={take})");

                return observations;
            }
            catch (Exception ex)
            {
                App.Log($"Error retrieving observations with filters: {ex.Message}");
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

        public async Task<int> GetObservationsCountAsync(FilterParameters filterParams)
        {
            try
            {
                App.Log($"GetObservationsCountAsync called with filters: {JsonSerializer.Serialize(filterParams)}");
                
                var query = _database.Table<Observation>();

                // Apply filters
                if (filterParams.StartDate.HasValue)
                {
                    query = query.Where(o => o.Timestamp >= filterParams.StartDate.Value);
                }

                if (filterParams.EndDate.HasValue)
                {
                    query = query.Where(o => o.Timestamp <= filterParams.EndDate.Value);
                }

                if (!string.IsNullOrWhiteSpace(filterParams.SearchText))
                {
                    var searchText = filterParams.SearchText.ToLower();
                    query = query.Where(o =>
                        o.Notes.Contains(searchText, StringComparison.CurrentCultureIgnoreCase) ||
                        o.Summary.Contains(searchText, StringComparison.CurrentCultureIgnoreCase) ||
                        o.Severity.Contains(searchText, StringComparison.CurrentCultureIgnoreCase));
                }

                if (filterParams.FieldId.HasValue)
                {
                    query = query.Where(o => o.FarmLocationId == filterParams.FieldId.Value);
                }

                if (filterParams.ObservationTypeId.HasValue)
                {
                    // Filter by observation type using ObservationMetadata join
                    var observationIdsWithType = await _database.Table<ObservationMetadata>()
                        .Where(om => om.ObservationTypeId == filterParams.ObservationTypeId.Value)
                        .ToListAsync();
                    
                    var observationIds = observationIdsWithType.Select(om => om.ObservationId).ToList();
                    
                    if (observationIds.Count > 0)
                    {
                        query = query.Where(o => observationIds.Contains(o.Id));
                    }
                    else
                    {
                        // If no observations have this type, return empty result
                        query = query.Where(o => false);
                    }
                }

                var count = await query.CountAsync();
                App.Log($"Filtered observations count: {count}");
                return count;
            }
            catch (Exception ex)
            {
                App.Log($"Error getting filtered observations count: {ex.Message}");
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

        // FarmLocation CRUD
        public async Task<int> AddFarmLocationAsync(FarmLocation farmLocation)
        {
            try
            {
                farmLocation.LastUpdated = DateTime.Now;
                var result = await _database.InsertAsync(farmLocation);
                App.Log($"FarmLocation added with ID: {farmLocation.Id}");
                return result;
            }
            catch (Exception ex)
            {
                App.Log($"Error adding farm location: {ex.Message}");
                throw;
            }
        }

        public async Task<List<FarmLocation>> GetFarmLocationsAsync()
        {
            try
            {
                var locations = await _database.Table<FarmLocation>()
                    .OrderBy(l => l.Name)
                    .ToListAsync();
                App.Log($"Retrieved {locations.Count} farm locations from database");
                return locations;
            }
            catch (Exception ex)
            {
                App.Log($"Error retrieving farm locations: {ex.Message}");
                throw;
            }
        }

        public async Task<FarmLocation?> GetFarmLocationByIdAsync(Guid id)
        {
            try
            {
                var location = await _database.Table<FarmLocation>()
                    .Where(l => l.Id == id)
                    .FirstOrDefaultAsync();
                return location;
            }
            catch (Exception ex)
            {
                App.Log($"Error retrieving farm location by ID: {ex.Message}");
                throw;
            }
        }

        public async Task<FarmLocation?> GetFarmLocationByNameAsync(string name)
        {
            try
            {
                var location = await _database.Table<FarmLocation>()
                    .Where(l => l.Name == name)
                    .FirstOrDefaultAsync();
                return location;
            }
            catch (Exception ex)
            {
                App.Log($"Error retrieving farm location by name: {ex.Message}");
                throw;
            }
        }

        public async Task<int> UpdateFarmLocationAsync(FarmLocation farmLocation)
        {
            try
            {
                farmLocation.LastUpdated = DateTime.Now;
                var result = await _database.UpdateAsync(farmLocation);
                App.Log($"FarmLocation updated with ID: {farmLocation.Id}");
                return result;
            }
            catch (Exception ex)
            {
                App.Log($"Error updating farm location: {ex.Message}");
                throw;
            }
        }

        public async Task<int> DeleteFarmLocationAsync(FarmLocation farmLocation)
        {
            try
            {
                var result = await _database.DeleteAsync(farmLocation);
                App.Log($"FarmLocation deleted with ID: {farmLocation.Id}");
                return result;
            }
            catch (Exception ex)
            {
                App.Log($"Error deleting farm location: {ex.Message}");
                throw;
            }
        }

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
                    .OrderBy(l => l.Name)
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

        public async Task<List<LookupItem>> GetLookupItemsByGroupAsync(string groupName)
        {
            try
            {
                App.Log($"GetLookupItemsByGroupAsync called for group: {groupName}");
                
                // Get the group ID for the group name
                var group = await GetLookupGroupByNameAsync(groupName);
                if (group == null)
                {
                    App.Log($"Group '{groupName}' not found");
                    return [];
                }

                var items = await _database.Table<LookupItem>()
                    .Where(l => l.GroupId == group.Id && l.IsActive)
                    .OrderBy(l => l.Name)
                    .ToListAsync();
                
                App.Log($"Retrieved {items.Count} lookup items for group {groupName}");
                return items;
            }
            catch (Exception ex)
            {
                App.Log($"Error retrieving lookup items by group: {ex.Message}");
                throw;
            }
        }

        // Lookup Groups CRUD
        public async Task<int> AddLookupGroupAsync(LookupGroup group)
        {
            try
            {
                App.Log($"AddLookupGroupAsync called: {group.Name}");
                var result = await _database.InsertAsync(group);
                App.Log($"LookupGroup added with ID: {group.Id}");
                return result;
            }
            catch (Exception ex)
            {
                App.Log($"Error adding lookup group: {ex.Message}");
                throw;
            }
        }

        public async Task<int> UpdateLookupGroupAsync(LookupGroup group)
        {
            try
            {
                group.UpdatedAt = DateTime.Now;
                var result = await _database.UpdateAsync(group);
                App.Log($"LookupGroup updated with ID: {group.Id}");
                return result;
            }
            catch (Exception ex)
            {
                App.Log($"Error updating lookup group: {ex.Message}");
                throw;
            }
        }

        public async Task<int> DeleteLookupGroupAsync(LookupGroup group)
        {
            try
            {
                // Soft delete by setting IsActive to false
                group.IsActive = false;
                group.UpdatedAt = DateTime.Now;
                var result = await _database.UpdateAsync(group);
                App.Log($"LookupGroup soft deleted with ID: {group.Id}");
                return result;
            }
            catch (Exception ex)
            {
                App.Log($"Error deleting lookup group: {ex.Message}");
                throw;
            }
        }

        public async Task<List<LookupGroup>> GetLookupGroupsAsync()
        {
            try
            {
                App.Log("GetLookupGroupsAsync called");
                var groups = await _database.Table<LookupGroup>()
                    .Where(g => g.IsActive)
                    .OrderBy(g => g.SortOrder)
                    .ThenBy(g => g.Name)
                    .ToListAsync();
                
                App.Log($"Retrieved {groups.Count} lookup groups");
                return groups;
            }
            catch (Exception ex)
            {
                App.Log($"Error retrieving lookup groups: {ex.Message}");
                throw;
            }
        }

        public async Task<LookupGroup?> GetLookupGroupByIdAsync(Guid id)
        {
            try
            {
                App.Log($"GetLookupGroupByIdAsync called: {id}");
                var group = await _database.Table<LookupGroup>()
                    .Where(g => g.Id == id && g.IsActive)
                    .FirstOrDefaultAsync();
                
                App.Log($"Retrieved lookup group: {group?.Name ?? "null"}");
                return group;
            }
            catch (Exception ex)
            {
                App.Log($"Error retrieving lookup group by ID: {ex.Message}");
                throw;
            }
        }

        public async Task<LookupGroup?> GetLookupGroupByNameAsync(string name)
        {
            try
            {
                App.Log($"GetLookupGroupByNameAsync called: {name}");
                var group = await _database.Table<LookupGroup>()
                    .Where(g => g.Name == name && g.IsActive)
                    .FirstOrDefaultAsync();
                
                App.Log($"Retrieved lookup group: {group?.Name ?? "null"}");
                return group;
            }
            catch (Exception ex)
            {
                App.Log($"Error retrieving lookup group by name: {ex.Message}");
                throw;
            }
        }

        // Lookup SubGroups CRUD
        public async Task<int> AddLookupSubGroupAsync(LookupSubGroup subGroup)
        {
            try
            {
                App.Log($"AddLookupSubGroupAsync called: {subGroup.Name}");
                var result = await _database.InsertAsync(subGroup);
                App.Log($"LookupSubGroup added with ID: {subGroup.Id}");
                return result;
            }
            catch (Exception ex)
            {
                App.Log($"Error adding lookup subgroup: {ex.Message}");
                throw;
            }
        }

        public async Task<int> UpdateLookupSubGroupAsync(LookupSubGroup subGroup)
        {
            try
            {
                subGroup.UpdatedAt = DateTime.Now;
                var result = await _database.UpdateAsync(subGroup);
                App.Log($"LookupSubGroup updated with ID: {subGroup.Id}");
                return result;
            }
            catch (Exception ex)
            {
                App.Log($"Error updating lookup subgroup: {ex.Message}");
                throw;
            }
        }

        public async Task<int> DeleteLookupSubGroupAsync(LookupSubGroup subGroup)
        {
            try
            {
                // Soft delete by setting IsActive to false
                subGroup.IsActive = false;
                subGroup.UpdatedAt = DateTime.Now;
                var result = await _database.UpdateAsync(subGroup);
                App.Log($"LookupSubGroup soft deleted with ID: {subGroup.Id}");
                return result;
            }
            catch (Exception ex)
            {
                App.Log($"Error deleting lookup subgroup: {ex.Message}");
                throw;
            }
        }

        public async Task<List<LookupSubGroup>> GetLookupSubGroupsAsync(Guid groupId)
        {
            try
            {
                App.Log($"GetLookupSubGroupsAsync called for group ID: {groupId}");
                var subgroups = await _database.Table<LookupSubGroup>()
                    .Where(sg => sg.GroupId == groupId && sg.IsActive)
                    .OrderBy(sg => sg.SortOrder)
                    .ThenBy(sg => sg.Name)
                    .ToListAsync();
                
                App.Log($"Retrieved {subgroups.Count} lookup subgroups for group {groupId}");
                return subgroups;
            }
            catch (Exception ex)
            {
                App.Log($"Error retrieving lookup subgroups: {ex.Message}");
                throw;
            }
        }

        public async Task<List<string>> GetLookupSubGroupNamesAsync(Guid groupId)
        {
            try
            {
                App.Log($"GetLookupSubGroupNamesAsync called for group ID: {groupId}");
                var subgroups = await _database.Table<LookupSubGroup>()
                    .Where(sg => sg.GroupId == groupId && sg.IsActive)
                    .OrderBy(sg => sg.SortOrder)
                    .ThenBy(sg => sg.Name)
                    .ToListAsync();
                
                var subgroupNames = subgroups.Select(sg => sg.Name).ToList();
                App.Log($"Retrieved {subgroupNames.Count} subgroup names for group {groupId}");
                return subgroupNames;
            }
            catch (Exception ex)
            {
                App.Log($"Error retrieving lookup subgroup names: {ex.Message}");
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

        public async Task<bool> LookupItemExistsAsync(string name, string groupName, Guid? excludeId = null)
        {
            try
            {
                // Get the group ID for the group name
                var group = await GetLookupGroupByNameAsync(groupName);
                if (group == null)
                {
                    return false;
                }

                var query = _database.Table<LookupItem>()
                    .Where(l => l.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase) &&
                               l.GroupId == group.Id &&
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
                // Check if groups already exist
                var existingGroupsCount = await _database.Table<LookupGroup>().CountAsync();
                if (existingGroupsCount > 0)
                {
                    App.Log($"Lookup groups already exist ({existingGroupsCount} groups), skipping seed");
                    return;
                }

                App.Log("Seeding lookup groups and subgroups with initial data...");

                // Create groups with their icons and colors
                var groups = new List<LookupGroup>
                {
                    new() { Name = "Crop Types", Icon = "🌾", Color = "#4CAF50", SortOrder = 1 },
                    new() { Name = "Diseases", Icon = "🦠", Color = "#F44336", SortOrder = 2 },
                    new() { Name = "Pests", Icon = "🐛", Color = "#FF9800", SortOrder = 3 },
                    new() { Name = "Chemicals", Icon = "🧪", Color = "#9C27B0", SortOrder = 4 },
                    new() { Name = "Fertilizers", Icon = "🌱", Color = "#8BC34A", SortOrder = 5 },
                    new() { Name = "Soil Types", Icon = "🌍", Color = "#8D6E63", SortOrder = 6 },
                    new() { Name = "Weather Conditions", Icon = "🌤️", Color = "#2196F3", SortOrder = 7 },
                    new() { Name = "Growth Stages", Icon = "📈", Color = "#00BCD4", SortOrder = 8 },
                    new() { Name = "Damage Types", Icon = "💥", Color = "#795548", SortOrder = 9 },
                    new() { Name = "Treatment Methods", Icon = "💊", Color = "#607D8B", SortOrder = 10 }
                };

                // Add groups to database
                foreach (var group in groups)
                {
                    await AddLookupGroupAsync(group);
                }

                // Create subgroups for each group
                var subgroups = new List<LookupSubGroup>();

                // Chemicals subgroups
                var chemicalsGroup = await GetLookupGroupByNameAsync("Chemicals");
                if (chemicalsGroup != null)
                {
                    subgroups.AddRange(
                    [
                        new() { Name = "Herbicide", GroupId = chemicalsGroup.Id, SortOrder = 1 },
                        new() { Name = "Fungicide", GroupId = chemicalsGroup.Id, SortOrder = 2 },
                        new() { Name = "Insecticide", GroupId = chemicalsGroup.Id, SortOrder = 3 },
                        new() { Name = "Fertilizer", GroupId = chemicalsGroup.Id, SortOrder = 4 },
                        new() { Name = "Growth Regulator", GroupId = chemicalsGroup.Id, SortOrder = 5 }
                    ]);
                }

                // Diseases subgroups
                var diseasesGroup = await GetLookupGroupByNameAsync("Diseases");
                if (diseasesGroup != null)
                {
                    subgroups.AddRange(
                    [
                        new() { Name = "Fungal", GroupId = diseasesGroup.Id, SortOrder = 1 },
                        new() { Name = "Bacterial", GroupId = diseasesGroup.Id, SortOrder = 2 },
                        new() { Name = "Viral", GroupId = diseasesGroup.Id, SortOrder = 3 },
                        new() { Name = "Nematode", GroupId = diseasesGroup.Id, SortOrder = 4 },
                        new() { Name = "Other", GroupId = diseasesGroup.Id, SortOrder = 5 }
                    ]);
                }

                // Pests subgroups
                var pestsGroup = await GetLookupGroupByNameAsync("Pests");
                if (pestsGroup != null)
                {
                    subgroups.AddRange(
                    [
                        new() { Name = "Insects", GroupId = pestsGroup.Id, SortOrder = 1 },
                        new() { Name = "Mites", GroupId = pestsGroup.Id, SortOrder = 2 },
                        new() { Name = "Nematodes", GroupId = pestsGroup.Id, SortOrder = 3 },
                        new() { Name = "Birds", GroupId = pestsGroup.Id, SortOrder = 4 },
                        new() { Name = "Mammals", GroupId = pestsGroup.Id, SortOrder = 5 }
                    ]);
                }

                // Fertilizers subgroups
                var fertilizersGroup = await GetLookupGroupByNameAsync("Fertilizers");
                if (fertilizersGroup != null)
                {
                    subgroups.AddRange(
                    [
                        new() { Name = "Nitrogen", GroupId = fertilizersGroup.Id, SortOrder = 1 },
                        new() { Name = "Phosphorus", GroupId = fertilizersGroup.Id, SortOrder = 2 },
                        new() { Name = "Potassium", GroupId = fertilizersGroup.Id, SortOrder = 3 },
                        new() { Name = "Micronutrients", GroupId = fertilizersGroup.Id, SortOrder = 4 },
                        new() { Name = "Organic", GroupId = fertilizersGroup.Id, SortOrder = 5 }
                    ]);
                }

                // Soil Types subgroups
                var soilTypesGroup = await GetLookupGroupByNameAsync("Soil Types");
                if (soilTypesGroup != null)
                {
                    subgroups.AddRange(
                    [
                        new() { Name = "Mineral", GroupId = soilTypesGroup.Id, SortOrder = 1 },
                        new() { Name = "Organic", GroupId = soilTypesGroup.Id, SortOrder = 2 },
                        new() { Name = "Mixed", GroupId = soilTypesGroup.Id, SortOrder = 3 }
                    ]);
                }

                // Weather Conditions subgroups
                var weatherGroup = await GetLookupGroupByNameAsync("Weather Conditions");
                if (weatherGroup != null)
                {
                    subgroups.AddRange(
                    [
                        new() { Name = "Temperature", GroupId = weatherGroup.Id, SortOrder = 1 },
                        new() { Name = "Precipitation", GroupId = weatherGroup.Id, SortOrder = 2 },
                        new() { Name = "Wind", GroupId = weatherGroup.Id, SortOrder = 3 },
                        new() { Name = "Humidity", GroupId = weatherGroup.Id, SortOrder = 4 },
                        new() { Name = "Pressure", GroupId = weatherGroup.Id, SortOrder = 5 }
                    ]);
                }

                // Growth Stages subgroups
                var growthStagesGroup = await GetLookupGroupByNameAsync("Growth Stages");
                if (growthStagesGroup != null)
                {
                    subgroups.AddRange(
                    [
                        new() { Name = "Vegetative", GroupId = growthStagesGroup.Id, SortOrder = 1 },
                        new() { Name = "Reproductive", GroupId = growthStagesGroup.Id, SortOrder = 2 },
                        new() { Name = "Maturity", GroupId = growthStagesGroup.Id, SortOrder = 3 }
                    ]);
                }

                // Damage Types subgroups
                var damageTypesGroup = await GetLookupGroupByNameAsync("Damage Types");
                if (damageTypesGroup != null)
                {
                    subgroups.AddRange(
                    [
                        new() { Name = "Environmental", GroupId = damageTypesGroup.Id, SortOrder = 1 },
                        new() { Name = "Biological", GroupId = damageTypesGroup.Id, SortOrder = 2 },
                        new() { Name = "Mechanical", GroupId = damageTypesGroup.Id, SortOrder = 3 },
                        new() { Name = "Chemical", GroupId = damageTypesGroup.Id, SortOrder = 4 }
                    ]);
                }

                // Treatment Methods subgroups
                var treatmentMethodsGroup = await GetLookupGroupByNameAsync("Treatment Methods");
                if (treatmentMethodsGroup != null)
                {
                    subgroups.AddRange(
                    [
                        new() { Name = "Chemical", GroupId = treatmentMethodsGroup.Id, SortOrder = 1 },
                        new() { Name = "Biological", GroupId = treatmentMethodsGroup.Id, SortOrder = 2 },
                        new() { Name = "Cultural", GroupId = treatmentMethodsGroup.Id, SortOrder = 3 },
                        new() { Name = "Mechanical", GroupId = treatmentMethodsGroup.Id, SortOrder = 4 },
                        new() { Name = "Integrated", GroupId = treatmentMethodsGroup.Id, SortOrder = 5 }
                    ]);
                }

                // Add subgroups to database
                foreach (var subgroup in subgroups)
                {
                    await AddLookupSubGroupAsync(subgroup);
                }

                App.Log($"Successfully seeded {groups.Count} groups and {subgroups.Count} subgroups");

                // Now seed the lookup items
                await SeedLookupItemsAsync();
            }
            catch (Exception ex)
            {
                App.Log($"Error seeding lookup data: {ex.Message}");
                // Don't throw - seeding failure shouldn't prevent app startup
            }
        }

        private async Task SeedLookupItemsAsync()
        {
            try
            {
                // Check if lookup items already exist
                var existingItemsCount = await _database.Table<LookupItem>().CountAsync();
                if (existingItemsCount > 0)
                {
                    App.Log($"Lookup items already exist ({existingItemsCount} items), skipping seed");
                    return;
                }

                App.Log("Seeding lookup items with initial data...");

                var seedData = new List<LookupItem>();

                // Crop Types (no subgroups)
                var cropTypesGroup = await GetLookupGroupByNameAsync("Crop Types");
                if (cropTypesGroup != null)
                {
                    seedData.AddRange(
                    [
                        new() { Name = "Corn", GroupId = cropTypesGroup.Id, Description = "Maize crop for grain or silage" },
                        new() { Name = "Soybeans", GroupId = cropTypesGroup.Id, Description = "Legume crop for oil and protein" },
                        new() { Name = "Wheat", GroupId = cropTypesGroup.Id, Description = "Cereal grain crop" },
                        new() { Name = "Cotton", GroupId = cropTypesGroup.Id, Description = "Fiber crop" },
                        new() { Name = "Rice", GroupId = cropTypesGroup.Id, Description = "Staple grain crop" }
                    ]);
                }

                // Diseases
                var diseasesGroup = await GetLookupGroupByNameAsync("Diseases");
                if (diseasesGroup != null)
                {
                    var diseasesSubgroups = await GetLookupSubGroupsAsync(diseasesGroup.Id);
                    var fungalSubgroup = diseasesSubgroups.FirstOrDefault(sg => sg.Name == "Fungal");
                    var bacterialSubgroup = diseasesSubgroups.FirstOrDefault(sg => sg.Name == "Bacterial");

                    seedData.AddRange(
                    [
                        new() { Name = "Rust", GroupId = diseasesGroup.Id, SubGroupId = fungalSubgroup?.Id, Description = "Fungal disease affecting leaves and stems" },
                        new() { Name = "Blight", GroupId = diseasesGroup.Id, SubGroupId = bacterialSubgroup?.Id, Description = "Rapid plant disease causing wilting" },
                        new() { Name = "Mildew", GroupId = diseasesGroup.Id, SubGroupId = fungalSubgroup?.Id, Description = "Fungal growth on plant surfaces" },
                        new() { Name = "Root Rot", GroupId = diseasesGroup.Id, SubGroupId = fungalSubgroup?.Id, Description = "Fungal disease affecting plant roots" },
                        new() { Name = "Leaf Spot", GroupId = diseasesGroup.Id, SubGroupId = fungalSubgroup?.Id, Description = "Fungal disease causing spots on leaves" }
                    ]);
                }

                // Pests
                var pestsGroup = await GetLookupGroupByNameAsync("Pests");
                if (pestsGroup != null)
                {
                    var pestsSubgroups = await GetLookupSubGroupsAsync(pestsGroup.Id);
                    var insectsSubgroup = pestsSubgroups.FirstOrDefault(sg => sg.Name == "Insects");
                    var mitesSubgroup = pestsSubgroups.FirstOrDefault(sg => sg.Name == "Mites");

                    seedData.AddRange(
                    [
                        new() { Name = "Aphids", GroupId = pestsGroup.Id, SubGroupId = insectsSubgroup?.Id, Description = "Small sap-sucking insects" },
                        new() { Name = "Corn Borer", GroupId = pestsGroup.Id, SubGroupId = insectsSubgroup?.Id, Description = "Larva that bores into corn stalks" },
                        new() { Name = "Spider Mites", GroupId = pestsGroup.Id, SubGroupId = mitesSubgroup?.Id, Description = "Tiny arachnids that feed on plant sap" },
                        new() { Name = "Cutworms", GroupId = pestsGroup.Id, SubGroupId = insectsSubgroup?.Id, Description = "Caterpillars that cut plant stems" },
                        new() { Name = "Wireworms", GroupId = pestsGroup.Id, SubGroupId = insectsSubgroup?.Id, Description = "Click beetle larvae that damage roots" }
                    ]);
                }

                // Chemicals
                var chemicalsGroup = await GetLookupGroupByNameAsync("Chemicals");
                if (chemicalsGroup != null)
                {
                    var chemicalsSubgroups = await GetLookupSubGroupsAsync(chemicalsGroup.Id);
                    var herbicideSubgroup = chemicalsSubgroups.FirstOrDefault(sg => sg.Name == "Herbicide");
                    var fungicideSubgroup = chemicalsSubgroups.FirstOrDefault(sg => sg.Name == "Fungicide");
                    var insecticideSubgroup = chemicalsSubgroups.FirstOrDefault(sg => sg.Name == "Insecticide");
                    var growthRegulatorSubgroup = chemicalsSubgroups.FirstOrDefault(sg => sg.Name == "Growth Regulator");

                    seedData.AddRange(
                    [
                        new() { Name = "Glyphosate", GroupId = chemicalsGroup.Id, SubGroupId = herbicideSubgroup?.Id, Description = "Broad-spectrum herbicide" },
                        new() { Name = "Atrazine", GroupId = chemicalsGroup.Id, SubGroupId = herbicideSubgroup?.Id, Description = "Selective herbicide for corn" },
                        new() { Name = "2,4-D", GroupId = chemicalsGroup.Id, SubGroupId = herbicideSubgroup?.Id, Description = "Selective herbicide for broadleaf weeds" },
                        new() { Name = "Paraquat", GroupId = chemicalsGroup.Id, SubGroupId = herbicideSubgroup?.Id, Description = "Contact herbicide" },
                        new() { Name = "Dicamba", GroupId = chemicalsGroup.Id, SubGroupId = herbicideSubgroup?.Id, Description = "Selective herbicide for broadleaf weeds" },
                        new() { Name = "Chlorothalonil", GroupId = chemicalsGroup.Id, SubGroupId = fungicideSubgroup?.Id, Description = "Protectant fungicide for foliar diseases" },
                        new() { Name = "Azoxystrobin", GroupId = chemicalsGroup.Id, SubGroupId = fungicideSubgroup?.Id, Description = "Systemic fungicide for disease control" },
                        new() { Name = "Malathion", GroupId = chemicalsGroup.Id, SubGroupId = insecticideSubgroup?.Id, Description = "Organophosphate insecticide" },
                        new() { Name = "Carbaryl", GroupId = chemicalsGroup.Id, SubGroupId = insecticideSubgroup?.Id, Description = "Carbamate insecticide for pest control" },
                        new() { Name = "Gibberellic Acid", GroupId = chemicalsGroup.Id, SubGroupId = growthRegulatorSubgroup?.Id, Description = "Plant growth regulator" }
                    ]);
                }

                // Fertilizers
                var fertilizersGroup = await GetLookupGroupByNameAsync("Fertilizers");
                if (fertilizersGroup != null)
                {
                    var fertilizersSubgroups = await GetLookupSubGroupsAsync(fertilizersGroup.Id);
                    var nitrogenSubgroup = fertilizersSubgroups.FirstOrDefault(sg => sg.Name == "Nitrogen");
                    var phosphorusSubgroup = fertilizersSubgroups.FirstOrDefault(sg => sg.Name == "Phosphorus");
                    var potassiumSubgroup = fertilizersSubgroups.FirstOrDefault(sg => sg.Name == "Potassium");
                    var organicSubgroup = fertilizersSubgroups.FirstOrDefault(sg => sg.Name == "Organic");

                    seedData.AddRange(
                    [
                        new() { Name = "Urea", GroupId = fertilizersGroup.Id, SubGroupId = nitrogenSubgroup?.Id, Description = "Nitrogen fertilizer (46-0-0)" },
                        new() { Name = "Ammonium Nitrate", GroupId = fertilizersGroup.Id, SubGroupId = nitrogenSubgroup?.Id, Description = "Nitrogen fertilizer (34-0-0)" },
                        new() { Name = "Triple Superphosphate", GroupId = fertilizersGroup.Id, SubGroupId = phosphorusSubgroup?.Id, Description = "Phosphorus fertilizer (0-46-0)" },
                        new() { Name = "Potassium Chloride", GroupId = fertilizersGroup.Id, SubGroupId = potassiumSubgroup?.Id, Description = "Potassium fertilizer (0-0-60)" },
                        new() { Name = "NPK 10-10-10", GroupId = fertilizersGroup.Id, SubGroupId = nitrogenSubgroup?.Id, Description = "Balanced fertilizer" },
                        new() { Name = "Compost", GroupId = fertilizersGroup.Id, SubGroupId = organicSubgroup?.Id, Description = "Organic soil amendment" }
                    ]);
                }

                // Soil Types
                var soilTypesGroup = await GetLookupGroupByNameAsync("Soil Types");
                if (soilTypesGroup != null)
                {
                    var soilTypesSubgroups = await GetLookupSubGroupsAsync(soilTypesGroup.Id);
                    var mineralSubgroup = soilTypesSubgroups.FirstOrDefault(sg => sg.Name == "Mineral");
                    var organicSubgroup = soilTypesSubgroups.FirstOrDefault(sg => sg.Name == "Organic");
                    var mixedSubgroup = soilTypesSubgroups.FirstOrDefault(sg => sg.Name == "Mixed");

                    seedData.AddRange(
                    [
                        new() { Name = "Clay", GroupId = soilTypesGroup.Id, SubGroupId = mineralSubgroup?.Id, Description = "Fine-grained soil with high water retention" },
                        new() { Name = "Silt", GroupId = soilTypesGroup.Id, SubGroupId = mineralSubgroup?.Id, Description = "Medium-grained soil" },
                        new() { Name = "Sandy", GroupId = soilTypesGroup.Id, SubGroupId = mineralSubgroup?.Id, Description = "Coarse-grained soil with good drainage" },
                        new() { Name = "Loam", GroupId = soilTypesGroup.Id, SubGroupId = mixedSubgroup?.Id, Description = "Well-balanced soil mixture" },
                        new() { Name = "Peat", GroupId = soilTypesGroup.Id, SubGroupId = organicSubgroup?.Id, Description = "Organic-rich soil" }
                    ]);
                }

                // Weather Conditions
                var weatherGroup = await GetLookupGroupByNameAsync("Weather Conditions");
                if (weatherGroup != null)
                {
                    var weatherSubgroups = await GetLookupSubGroupsAsync(weatherGroup.Id);
                    var temperatureSubgroup = weatherSubgroups.FirstOrDefault(sg => sg.Name == "Temperature");
                    var precipitationSubgroup = weatherSubgroups.FirstOrDefault(sg => sg.Name == "Precipitation");
                    var windSubgroup = weatherSubgroups.FirstOrDefault(sg => sg.Name == "Wind");
                    var humiditySubgroup = weatherSubgroups.FirstOrDefault(sg => sg.Name == "Humidity");
                    var pressureSubgroup = weatherSubgroups.FirstOrDefault(sg => sg.Name == "Pressure");

                    seedData.AddRange(
                    [
                        new() { Name = "Sunny", GroupId = weatherGroup.Id, SubGroupId = temperatureSubgroup?.Id, Description = "Clear skies with full sun" },
                        new() { Name = "Cloudy", GroupId = weatherGroup.Id, SubGroupId = pressureSubgroup?.Id, Description = "Overcast conditions" },
                        new() { Name = "Rainy", GroupId = weatherGroup.Id, SubGroupId = precipitationSubgroup?.Id, Description = "Precipitation occurring" },
                        new() { Name = "Windy", GroupId = weatherGroup.Id, SubGroupId = windSubgroup?.Id, Description = "High wind conditions" },
                        new() { Name = "Foggy", GroupId = weatherGroup.Id, SubGroupId = humiditySubgroup?.Id, Description = "Low visibility due to fog" }
                    ]);
                }

                // Growth Stages
                var growthStagesGroup = await GetLookupGroupByNameAsync("Growth Stages");
                if (growthStagesGroup != null)
                {
                    var growthStagesSubgroups = await GetLookupSubGroupsAsync(growthStagesGroup.Id);
                    var vegetativeSubgroup = growthStagesSubgroups.FirstOrDefault(sg => sg.Name == "Vegetative");
                    var reproductiveSubgroup = growthStagesSubgroups.FirstOrDefault(sg => sg.Name == "Reproductive");
                    var maturitySubgroup = growthStagesSubgroups.FirstOrDefault(sg => sg.Name == "Maturity");

                    seedData.AddRange(
                    [
                        new() { Name = "Germination", GroupId = growthStagesGroup.Id, SubGroupId = vegetativeSubgroup?.Id, Description = "Seed sprouting and root development" },
                        new() { Name = "Vegetative", GroupId = growthStagesGroup.Id, SubGroupId = vegetativeSubgroup?.Id, Description = "Leaf and stem growth" },
                        new() { Name = "Flowering", GroupId = growthStagesGroup.Id, SubGroupId = reproductiveSubgroup?.Id, Description = "Flower development and pollination" },
                        new() { Name = "Fruiting", GroupId = growthStagesGroup.Id, SubGroupId = reproductiveSubgroup?.Id, Description = "Fruit or grain development" },
                        new() { Name = "Maturity", GroupId = growthStagesGroup.Id, SubGroupId = maturitySubgroup?.Id, Description = "Full development and harvest ready" }
                    ]);
                }

                // Damage Types
                var damageTypesGroup = await GetLookupGroupByNameAsync("Damage Types");
                if (damageTypesGroup != null)
                {
                    var damageTypesSubgroups = await GetLookupSubGroupsAsync(damageTypesGroup.Id);
                    var environmentalSubgroup = damageTypesSubgroups.FirstOrDefault(sg => sg.Name == "Environmental");

                    seedData.AddRange(
                    [
                        new() { Name = "Hail Damage", GroupId = damageTypesGroup.Id, SubGroupId = environmentalSubgroup?.Id, Description = "Physical damage from hail stones" },
                        new() { Name = "Wind Damage", GroupId = damageTypesGroup.Id, SubGroupId = environmentalSubgroup?.Id, Description = "Damage from high winds" },
                        new() { Name = "Drought Stress", GroupId = damageTypesGroup.Id, SubGroupId = environmentalSubgroup?.Id, Description = "Damage from lack of water" },
                        new() { Name = "Flood Damage", GroupId = damageTypesGroup.Id, SubGroupId = environmentalSubgroup?.Id, Description = "Damage from excess water" },
                        new() { Name = "Frost Damage", GroupId = damageTypesGroup.Id, SubGroupId = environmentalSubgroup?.Id, Description = "Damage from freezing temperatures" }
                    ]);
                }

                // Treatment Methods
                var treatmentMethodsGroup = await GetLookupGroupByNameAsync("Treatment Methods");
                if (treatmentMethodsGroup != null)
                {
                    var treatmentMethodsSubgroups = await GetLookupSubGroupsAsync(treatmentMethodsGroup.Id);
                    var chemicalSubgroup = treatmentMethodsSubgroups.FirstOrDefault(sg => sg.Name == "Chemical");
                    var biologicalSubgroup = treatmentMethodsSubgroups.FirstOrDefault(sg => sg.Name == "Biological");
                    var culturalSubgroup = treatmentMethodsSubgroups.FirstOrDefault(sg => sg.Name == "Cultural");
                    var mechanicalSubgroup = treatmentMethodsSubgroups.FirstOrDefault(sg => sg.Name == "Mechanical");
                    var integratedSubgroup = treatmentMethodsSubgroups.FirstOrDefault(sg => sg.Name == "Integrated");

                    seedData.AddRange(
                    [
                        new() { Name = "Chemical Treatment", GroupId = treatmentMethodsGroup.Id, SubGroupId = chemicalSubgroup?.Id, Description = "Application of pesticides or herbicides" },
                        new() { Name = "Biological Control", GroupId = treatmentMethodsGroup.Id, SubGroupId = biologicalSubgroup?.Id, Description = "Use of natural predators or beneficial organisms" },
                        new() { Name = "Cultural Control", GroupId = treatmentMethodsGroup.Id, SubGroupId = culturalSubgroup?.Id, Description = "Management practices to prevent problems" },
                        new() { Name = "Mechanical Control", GroupId = treatmentMethodsGroup.Id, SubGroupId = mechanicalSubgroup?.Id, Description = "Physical removal or barriers" },
                        new() { Name = "Integrated Pest Management", GroupId = treatmentMethodsGroup.Id, SubGroupId = integratedSubgroup?.Id, Description = "Combined approach using multiple methods" }
                    ]);
                }

                // Add all lookup items to database
                foreach (var item in seedData)
                {
                    await AddLookupItemAsync(item);
                }

                App.Log($"Successfully seeded {seedData.Count} lookup items");
            }
            catch (Exception ex)
            {
                App.Log($"Error seeding lookup items: {ex.Message}");
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

        public async Task<ObservationTypeDataPoint?> GetDataPointByIdAsync(Guid id)
        {
            try
            {
                var dataPoint = await _database.Table<ObservationTypeDataPoint>()
                    .Where(d => d.Id == id && d.IsActive)
                    .FirstOrDefaultAsync();
                return dataPoint;
            }
            catch (Exception ex)
            {
                App.Log($"Error retrieving data point by ID: {ex.Message}");
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

        // MarkdownReport CRUD
        public async Task<int> AddMarkdownReportAsync(MarkdownReport report)
        {
            try
            {
                report.CreatedAt = DateTime.Now;
                report.UpdatedAt = DateTime.Now;
                var result = await _database.InsertAsync(report);
                App.Log($"MarkdownReport added with ID: {report.Id}");
                return result;
            }
            catch (Exception ex)
            {
                App.Log($"Error adding markdown report: {ex.Message}");
                throw;
            }
        }

        public async Task<int> UpdateMarkdownReportAsync(MarkdownReport report)
        {
            try
            {
                report.UpdatedAt = DateTime.Now;
                var result = await _database.UpdateAsync(report);
                App.Log($"MarkdownReport updated with ID: {report.Id}");
                return result;
            }
            catch (Exception ex)
            {
                App.Log($"Error updating markdown report: {ex.Message}");
                throw;
            }
        }

        public async Task<int> DeleteMarkdownReportAsync(MarkdownReport report)
        {
            try
            {
                var result = await _database.DeleteAsync(report);
                App.Log($"MarkdownReport deleted with ID: {report.Id}");
                return result;
            }
            catch (Exception ex)
            {
                App.Log($"Error deleting markdown report: {ex.Message}");
                throw;
            }
        }

        public async Task<List<MarkdownReport>> GetMarkdownReportsByGroupAsync(Guid reportGroupId)
        {
            try
            {
                var reports = await _database.Table<MarkdownReport>()
                    .Where(r => r.ReportGroupId == reportGroupId && r.IsActive)
                    .OrderByDescending(r => r.DateProduced)
                    .ToListAsync();
                App.Log($"Retrieved {reports.Count} markdown reports for group ID '{reportGroupId}'");
                return reports;
            }
            catch (Exception ex)
            {
                App.Log($"Error retrieving markdown reports by group: {ex.Message}");
                throw;
            }
        }

        public async Task<List<MarkdownReport>> GetMarkdownReportsAsync()
        {
            try
            {
                var reports = await _database.Table<MarkdownReport>()
                    .Where(r => r.IsActive)
                    .OrderByDescending(r => r.DateProduced)
                    .ToListAsync();
                App.Log($"Retrieved {reports.Count} markdown reports from database");
                return reports;
            }
            catch (Exception ex)
            {
                App.Log($"Error retrieving markdown reports: {ex.Message}");
                throw;
            }
        }

        public async Task<List<MarkdownReport>> GetMarkdownReportsAsync(int skip, int take)
        {
            try
            {
                var reports = await _database.Table<MarkdownReport>()
                    .Where(r => r.IsActive)
                    .OrderByDescending(r => r.DateProduced)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync();
                App.Log($"Retrieved {reports.Count} markdown reports (skip={skip}, take={take})");
                return reports;
            }
            catch (Exception ex)
            {
                App.Log($"Error retrieving markdown reports with pagination: {ex.Message}");
                throw;
            }
        }

        public async Task<int> GetMarkdownReportsCountAsync()
        {
            try
            {
                var count = await _database.Table<MarkdownReport>()
                    .Where(r => r.IsActive)
                    .CountAsync();
                App.Log($"Total markdown reports count: {count}");
                return count;
            }
            catch (Exception ex)
            {
                App.Log($"Error getting markdown reports count: {ex.Message}");
                throw;
            }
        }

        public async Task<MarkdownReport?> GetMarkdownReportByIdAsync(Guid id)
        {
            try
            {
                var report = await _database.Table<MarkdownReport>()
                    .Where(r => r.Id == id && r.IsActive)
                    .FirstOrDefaultAsync();
                return report;
            }
            catch (Exception ex)
            {
                App.Log($"Error retrieving markdown report by ID: {ex.Message}");
                throw;
            }
        }

        public async Task<List<MarkdownReport>> SearchMarkdownReportsAsync(string searchTerm)
        {
            try
            {
                var reports = await _database.Table<MarkdownReport>()
                    .Where(r => r.IsActive && 
                               (r.Title.Contains(searchTerm) || r.ReportMarkdown.Contains(searchTerm)))
                    .OrderByDescending(r => r.DateProduced)
                    .ToListAsync();
                App.Log($"Found {reports.Count} markdown reports matching '{searchTerm}'");
                return reports;
            }
            catch (Exception ex)
            {
                App.Log($"Error searching markdown reports: {ex.Message}");
                throw;
            }
        }

        // ReportGroup CRUD
        public async Task<int> AddReportGroupAsync(ReportGroup group)
        {
            try
            {
                group.CreatedAt = DateTime.Now;
                group.UpdatedAt = DateTime.Now;
                var result = await _database.InsertAsync(group);
                App.Log($"ReportGroup added with ID: {group.Id}");
                return result;
            }
            catch (Exception ex)
            {
                App.Log($"Error adding report group: {ex.Message}");
                throw;
            }
        }

        public async Task<int> UpdateReportGroupAsync(ReportGroup group)
        {
            try
            {
                group.UpdatedAt = DateTime.Now;
                var result = await _database.UpdateAsync(group);
                App.Log($"ReportGroup updated with ID: {group.Id}");
                return result;
            }
            catch (Exception ex)
            {
                App.Log($"Error updating report group: {ex.Message}");
                throw;
            }
        }

        public async Task<int> DeleteReportGroupAsync(ReportGroup group)
        {
            try
            {
                var result = await _database.DeleteAsync(group);
                App.Log($"ReportGroup deleted with ID: {group.Id}");
                return result;
            }
            catch (Exception ex)
            {
                App.Log($"Error deleting report group: {ex.Message}");
                throw;
            }
        }

        public async Task<List<ReportGroup>> GetReportGroupsAsync()
        {
            try
            {
                var groups = await _database.Table<ReportGroup>()
                    .Where(g => g.IsActive)
                    .OrderBy(g => g.SortOrder)
                    .ThenBy(g => g.Name)
                    .ToListAsync();
                App.Log($"Retrieved {groups.Count} report groups from database");
                return groups;
            }
            catch (Exception ex)
            {
                App.Log($"Error retrieving report groups: {ex.Message}");
                throw;
            }
        }

        public async Task<ReportGroup?> GetReportGroupByIdAsync(Guid id)
        {
            try
            {
                var group = await _database.Table<ReportGroup>()
                    .Where(g => g.Id == id && g.IsActive)
                    .FirstOrDefaultAsync();
                return group;
            }
            catch (Exception ex)
            {
                App.Log($"Error retrieving report group by ID: {ex.Message}");
                throw;
            }
        }

        public async Task<ReportGroup?> GetReportGroupByNameAsync(string name)
        {
            try
            {
                var group = await _database.Table<ReportGroup>()
                    .Where(g => g.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase) && g.IsActive)
                    .FirstOrDefaultAsync();
                return group;
            }
            catch (Exception ex)
            {
                App.Log($"Error retrieving report group by name: {ex.Message}");
                throw;
            }
        }

        private async Task SeedReportGroupsAsync()
        {
            try
            {
                // Check if report groups already exist
                var existingGroupsCount = await _database.Table<ReportGroup>().CountAsync();
                if (existingGroupsCount > 0)
                {
                    App.Log($"Report groups already exist ({existingGroupsCount} groups), skipping seed");
                    return;
                }

                App.Log("Seeding report groups with initial data...");

                var groups = new List<ReportGroup>
                {
                    new() { Name = "Moisture Reports", Description = "Soil moisture analysis and monitoring reports", Icon = "💧", Color = "#00BCD4", SortOrder = 1 },
                    new() { Name = "Scout Reports", Description = "Overall section health and condition reports", Icon = "🏥", Color = "#4CAF50", SortOrder = 2 },
                    new() { Name = "Harvest Reports", Description = "Harvest yield and quality analysis", Icon = "🌾", Color = "#FFC107", SortOrder = 3 },
                    new() { Name = "Warehouse Reports", Description = "Warehouse stock reports ", Icon = "🌤️", Color = "#2196F3", SortOrder = 4 },
                    new() { Name = "Vehicle Reports", Description = "Vehicle fuel and service reports", Icon = "🐛", Color = "#FF9800", SortOrder = 5 },
                };

                foreach (var group in groups)
                {
                    await AddReportGroupAsync(group);
                }

                App.Log($"Successfully seeded {groups.Count} report groups");
            }
            catch (Exception ex)
            {
                App.Log($"Error seeding report groups: {ex.Message}");
                // Don't throw - seeding failure shouldn't prevent app startup
            }
        }

        private async Task SeedObservationsFromCsvAsync()
        {
            try
            {
                // Check if observations already exist
                var existingCount = await _database.Table<Observation>().CountAsync();
                if (existingCount > 0)
                {
                    App.Log($"Database already has {existingCount} observations, skipping CSV seed");
                    return;
                }

                App.Log("Starting CSV seeding process...");

                // Get the path to scout.csv in the app bundle
                var csvPath = GetCsvFilePath();
                if (string.IsNullOrEmpty(csvPath))
                {
                    App.Log("scout.csv not found, skipping CSV seed");
                    return;
                }

                App.Log($"Reading CSV file from: {csvPath}");

                // Read and parse CSV
                var observations = await ParseCsvFileAsync(csvPath);
                if (observations.Count == 0)
                {
                    App.Log("No observations found in CSV file");
                    return;
                }

                App.Log($"Found {observations.Count} observations in CSV file");

                // Create Scout observation type with data points
                var scoutObservationType = await CreateScoutObservationTypeAsync(observations);

                // Ensure farm locations exist for all sections
                await EnsureFarmLocationsExistAsync(observations);

                // Link observations to farm locations
                await LinkObservationsToFarmLocationsAsync(observations);

                // Link observations to Scout observation type and create metadata
                await LinkObservationsToScoutTypeAsync(observations, scoutObservationType);

                // Import observations
                var importedCount = 0;
                foreach (var observation in observations)
                {
                    try
                    {
                        await AddObservationAsync(observation);
                        importedCount++;
                        
                        if (importedCount % 100 == 0)
                        {
                            App.Log($"Imported {importedCount} observations...");
                        }
                    }
                    catch (Exception ex)
                    {
                        App.Log($"Error importing observation {observation.Id}: {ex.Message}");
                    }
                }

                App.Log($"Successfully imported {importedCount} observations from CSV");
            }
            catch (Exception ex)
            {
                App.Log($"Error during CSV seeding: {ex.Message}");
                App.Log($"Exception details: {ex}");
                // Don't throw - seeding failure shouldn't prevent app startup
            }
        }

        private static string? GetCsvFilePath()
        {
            try
            {
                // Try to find scout.csv in the app bundle
                var possiblePaths = new[]
                {
                    "scout.csv",
                    "Reports/scout.csv",
                    "Resources/scout.csv",
                    "Assets/scout.csv"
                };
                foreach (var path in possiblePaths.Where(File.Exists))
                {
                    App.Log($"Found scout.csv at: {path}");
                    return path;
                }

                // Try to copy from app bundle to app data directory
                var appDataPath = Path.Combine(FileSystem.AppDataDirectory, "scout.csv");
                if (File.Exists(appDataPath))
                {
                    App.Log($"Found scout.csv in app data: {appDataPath}");
                    return appDataPath;
                }

                App.Log("scout.csv not found in any expected location");
                return null;
            }
            catch (Exception ex)
            {
                App.Log($"Error finding CSV file: {ex.Message}");
                return null;
            }
        }

        private static async Task<List<Observation>> ParseCsvFileAsync(string csvPath)
        {
            var observations = new List<Observation>();

            try
            {
                var lines = await File.ReadAllLinesAsync(csvPath);
                
                // Skip header line
                for (int i = 1; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line))
                        continue;

                    var observation = ParseCsvLine(line, i);
                    if (observation != null)
                    {
                        observations.Add(observation);
                    }
                }
            }
            catch (Exception ex)
            {
                App.Log($"Error parsing CSV file: {ex.Message}");
            }

            return observations;
        }

        private static Observation? ParseCsvLine(string line, int lineNumber)
        {
            try
            {
                // Split by comma, but handle quoted fields
                var fields = ParseCsvFields(line);
                
                if (fields.Length < 6)
                {
                    App.Log($"Line {lineNumber}: Invalid number of fields ({fields.Length})");
                    return null;
                }

                // Parse fields
                var id = Guid.NewGuid(); // Generate a new ID for the observation
                var dateStr = fields[1];
                var section = fields[2];
                var metric = fields[3];
                var condition = fields[4];
                var notes = fields[5];

                

                // Parse date
                if (!DateTime.TryParseExact(dateStr, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    App.Log($"Line {lineNumber}: Invalid date format: {dateStr}");
                    return null;
                }

                // Map condition to severity
                var severity = MapConditionToSeverity(condition);

                // Create observation
                var observation = new Observation
                {
                    Id = id,
                    Summary = $"{metric} - {section}",
                    Timestamp = date,
                    Notes = notes,
                    Severity = severity,
                    Latitude = 0.0, // Default coordinates - could be enhanced with actual farm coordinates
                    Longitude = 0.0,
                    PhotoPath = string.Empty
                };

                return observation;
            }
            catch (Exception ex)
            {
                App.Log($"Line {lineNumber}: Error parsing line: {ex.Message}");
                return null;
            }
        }

        private static string[] ParseCsvFields(string line)
        {
            var fields = new List<string>();
            var currentField = new StringBuilder();
            var inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                var c = line[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    fields.Add(currentField.ToString());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }

            // Add the last field
            fields.Add(currentField.ToString());

            return [.. fields];
        }

        private static string MapConditionToSeverity(string condition)
        {
            return condition.ToLower() switch
            {
                "pass" => "Information",
                "partial" => "Warning",
                "fail" => "Fail",
                _ => "Information"
            };
        }

        private async Task EnsureFarmLocationsExistAsync(List<Observation> observations)
        {
            try
            {
                // Get unique sections from observations
                var sections = observations.Select(o => ExtractSectionFromSummary(o.Summary)).Distinct().ToList();

                // Get existing farm locations
                var existingLocations = await GetFarmLocationsAsync();
                var existingSectionNames = existingLocations.Select(l => l.Name).ToHashSet();
                // Create missing farm locations
                foreach (var section in sections.Where(section => !existingSectionNames.Contains(section)))
                {
                    var farmLocation = new FarmLocation
                    {
                        Name = section,
                        Description = $"Farm section {section}",
                        FieldType = "Macadamia",
                        Area = 0.0, // Could be enhanced with actual area data
                        Owner = "Farm Owner",
                        Geometry = "POLYGON((0 0, 1 0, 1 1, 0 1, 0 0))" // Default geometry
                    };
                    await AddFarmLocationAsync(farmLocation);
                    App.Log($"Created farm location for section: {section}");
                }

                App.Log($"Ensured farm locations exist for {sections.Count} sections");
            }
            catch (Exception ex)
            {
                App.Log($"Error ensuring farm locations exist: {ex.Message}");
            }
        }

        private static string ExtractSectionFromSummary(string summary)
        {
            // Summary format is "metric - section"
            var parts = summary.Split(" - ");
            return parts.Length > 1 ? parts[1] : summary;
        }

        private async Task LinkObservationsToFarmLocationsAsync(List<Observation> observations)
        {
            try
            {
                // Get all farm locations
                var farmLocations = await GetFarmLocationsAsync();
                var locationByName = farmLocations.ToDictionary(l => l.Name, l => l.Id);

                // Link each observation to its corresponding farm location
                foreach (var observation in observations)
                {
                    var section = ExtractSectionFromSummary(observation.Summary);
                    if (locationByName.TryGetValue(section, out var locationId))
                    {
                        observation.FarmLocationId = locationId;
                        App.Log($"Linked observation {observation.Id} to farm location {section} ({locationId})");
                    }
                    else
                    {
                        App.Log($"Warning: Could not find farm location for section {section}");
                    }
                }

                App.Log($"Linked {observations.Count} observations to farm locations");
            }
            catch (Exception ex)
            {
                App.Log($"Error linking observations to farm locations: {ex.Message}");
            }
        }

        private async Task<ObservationType> CreateScoutObservationTypeAsync(List<Observation> observations)
        {
            try
            {
                // Check if Scout observation type already exists
                var existingScoutType = await GetObservationTypeByNameAsync("Scout");
                if (existingScoutType != null)
                {
                    App.Log("Scout observation type already exists");
                    return existingScoutType;
                }

                // Create Scout observation type
                var scoutType = new ObservationType
                {
                    Name = "Scout",
                    Description = "Farm scouting observations for various metrics",
                    Icon = "🔍",
                    Color = "#4CAF50",
                    IsActive = true,
                    SortOrder = 1
                };

                await AddObservationTypeAsync(scoutType);
                App.Log($"Created Scout observation type with ID: {scoutType.Id}");

                // Extract unique metrics from observations
                var metrics = observations
                    .Select(o => ExtractMetricFromSummary(o.Summary))
                    .Distinct()
                    .OrderBy(m => m)
                    .ToList();

                App.Log($"Found {metrics.Count} unique metrics: {string.Join(", ", metrics)}");

                // Create data points for each metric
                var sortOrder = 1;
                foreach (var metric in metrics)
                {
                    var dataPoint = new ObservationTypeDataPoint
                    {
                        ObservationTypeId = scoutType.Id,
                        Code = metric.ToLower().Replace(" ", "_"),
                        Label = metric,
                        DataType = "Lookup",
                        LookupGroupName = "Scout Conditions",
                        Description = $"Scout observation for {metric}",
                        SortOrder = sortOrder++,
                        IsRequired = true,
                        IsActive = true
                    };

                    await AddObservationTypeDataPointAsync(dataPoint);
                    App.Log($"Created data point for metric: {metric}");
                }

                return scoutType;
            }
            catch (Exception ex)
            {
                App.Log($"Error creating Scout observation type: {ex.Message}");
                throw;
            }
        }

        private async Task LinkObservationsToScoutTypeAsync(List<Observation> observations, ObservationType scoutType)
        {
            try
            {
                // Get data points for the Scout observation type
                var dataPoints = await GetDataPointsForObservationTypeAsync(scoutType.Id);
                var dataPointByLabel = dataPoints.ToDictionary(dp => dp.Label, dp => dp.Id);

                // Create lookup items for conditions if they don't exist
                await EnsureScoutConditionsExistAsync();

                // Link each observation to the Scout observation type and create metadata
                foreach (var observation in observations)
                {
                    var metric = ExtractMetricFromSummary(observation.Summary);
                    
                    if (dataPointByLabel.TryGetValue(metric, out var dataPointId))
                    {
                        // Create observation metadata
                        var metadata = new ObservationMetadata
                        {
                            ObservationId = observation.Id,
                            ObservationTypeId = scoutType.Id,
                            DataPointId = dataPointId,
                            Value = observation.Severity, // Use severity as the value
                            CreatedAt = observation.Timestamp,
                            UpdatedAt = observation.Timestamp
                        };

                        await AddObservationMetadataAsync(metadata);
                        App.Log($"Created metadata for observation {observation.Id} - {metric}: {observation.Severity}");
                    }
                    else
                    {
                        App.Log($"Warning: Could not find data point for metric {metric}");
                    }
                }

                App.Log($"Linked {observations.Count} observations to Scout observation type");
            }
            catch (Exception ex)
            {
                App.Log($"Error linking observations to Scout type: {ex.Message}");
            }
        }

        private async Task EnsureScoutConditionsExistAsync()
        {
            try
            {
                // Check if Scout Conditions lookup group exists
                var existingGroup = await GetLookupGroupByNameAsync("Scout Conditions");
                if (existingGroup != null)
                {
                    App.Log("Scout Conditions lookup group already exists");
                    return;
                }

                // Create Scout Conditions lookup group
                var scoutConditionsGroup = new LookupGroup
                {
                    Name = "Scout Conditions",
                    Icon = "🔍",
                    Color = "#4CAF50",
                    IsActive = true,
                    SortOrder = 1
                };

                await AddLookupGroupAsync(scoutConditionsGroup);
                App.Log($"Created Scout Conditions lookup group with ID: {scoutConditionsGroup.Id}");

                // Create lookup items for conditions
                var conditions = new[]
                {
                    new LookupItem { Name = "Pass", Description = "Condition is acceptable", GroupId = scoutConditionsGroup.Id, IsActive = true },
                    new LookupItem { Name = "Partial", Description = "Condition needs attention", GroupId = scoutConditionsGroup.Id, IsActive = true },
                    new LookupItem { Name = "Fail", Description = "Condition requires immediate action", GroupId = scoutConditionsGroup.Id, IsActive = true }
                };

                foreach (var condition in conditions)
                {
                    await AddLookupItemAsync(condition);
                    App.Log($"Created lookup item: {condition.Name}");
                }
            }
            catch (Exception ex)
            {
                App.Log($"Error ensuring Scout conditions exist: {ex.Message}");
            }
        }

        private static string ExtractMetricFromSummary(string summary)
        {
            // Summary format is "metric - section"
            var parts = summary.Split(" - ");
            return parts.Length > 0 ? parts[0] : summary;
        }
    }
} 
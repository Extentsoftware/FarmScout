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

                // Create report groups table
                await _database.CreateTableAsync<ReportGroup>();
                App.Log("ReportGroup table created successfully");

                // Create markdown reports table
                await _database.CreateTableAsync<MarkdownReport>();
                App.Log("MarkdownReport table created successfully");
        
                // Seed all data using the DatabaseSeeder
                var seeder = new DatabaseSeeder(_database);
                await seeder.SeedAllDataAsync();

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
                    .Where(t => t.Name == name && t.IsActive)
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

        #region Database Reset Methods

        public async Task<bool> ResetDatabaseAsync()
        {
            try
            {
                App.Log("Starting database reset...");
                IsReady = false;

                // Delete all data from all tables
                await _database.DeleteAllAsync<MarkdownReport>();
                await _database.DeleteAllAsync<ReportGroup>();
                await _database.DeleteAllAsync<ObservationMetadata>();
                await _database.DeleteAllAsync<ObservationTypeDataPoint>();
                await _database.DeleteAllAsync<ObservationType>();
                await _database.DeleteAllAsync<LookupItem>();
                await _database.DeleteAllAsync<LookupSubGroup>();
                await _database.DeleteAllAsync<LookupGroup>();
                await _database.DeleteAllAsync<FarmLocation>();
                await _database.DeleteAllAsync<ObservationLocation>();
                await _database.DeleteAllAsync<ObservationPhoto>();
                await _database.DeleteAllAsync<TaskItem>();
                await _database.DeleteAllAsync<Observation>();

                App.Log("Database reset completed successfully");
                IsReady = true;
                return true;
                    }
                    catch (Exception ex)
                    {
                App.Log($"Error during database reset: {ex.Message}");
                IsReady = false;
                return false;
            }
        }

        public async Task<bool> ResetDatabaseWithSeedingAsync()
        {
            try
            {
                App.Log("Starting database reset with seeding...");
                
                // First reset the database
                var resetSuccess = await ResetDatabaseAsync();
                if (!resetSuccess)
                {
                    App.Log("Database reset failed, aborting seeding");
                    return false;
                }

                // Then re-seed the data
                var seeder = new DatabaseSeeder(_database);
                await seeder.SeedAllDataAsync();

                App.Log("Database reset with seeding completed successfully");
                IsReady = true;
                return true;
            }
            catch (Exception ex)
            {
                App.Log($"Error during database reset with seeding: {ex.Message}");
                IsReady = false;
                return false;
            }
        }

        public async Task<DatabaseResetInfo> GetDatabaseInfoAsync()
        {
            var info = new DatabaseResetInfo
            {
                IsReady = IsReady
            };

            try
            {
                // Get database file info
                string dbPath = Path.Combine(FileSystem.AppDataDirectory, "farmscout.db3");
                info.DatabasePath = dbPath;

                if (File.Exists(dbPath))
                {
                    var fileInfo = new FileInfo(dbPath);
                    info.DatabaseSizeBytes = fileInfo.Length;
                    info.LastModified = fileInfo.LastWriteTime;
                }

                // Get record counts
                info.ObservationCount = await _database.Table<Observation>().CountAsync();
                info.TaskCount = await _database.Table<TaskItem>().CountAsync();
                info.PhotoCount = await _database.Table<ObservationPhoto>().CountAsync();
                info.LocationCount = await _database.Table<ObservationLocation>().CountAsync();
                info.FarmLocationCount = await _database.Table<FarmLocation>().CountAsync();
                info.LookupGroupCount = await _database.Table<LookupGroup>().CountAsync();
                info.LookupSubGroupCount = await _database.Table<LookupSubGroup>().CountAsync();
                info.LookupItemCount = await _database.Table<LookupItem>().CountAsync();
                info.ObservationTypeCount = await _database.Table<ObservationType>().CountAsync();
                info.ObservationTypeDataPointCount = await _database.Table<ObservationTypeDataPoint>().CountAsync();
                info.ObservationMetadataCount = await _database.Table<ObservationMetadata>().CountAsync();
                info.ReportGroupCount = await _database.Table<ReportGroup>().CountAsync();
                info.MarkdownReportCount = await _database.Table<MarkdownReport>().CountAsync();

                // Calculate total
                info.TotalRecordCount = info.ObservationCount + info.TaskCount + info.PhotoCount + 
                                       info.LocationCount + info.FarmLocationCount + info.LookupGroupCount + 
                                       info.LookupSubGroupCount + info.LookupItemCount + info.ObservationTypeCount + 
                                       info.ObservationTypeDataPointCount + info.ObservationMetadataCount + 
                                       info.ReportGroupCount + info.MarkdownReportCount;

                App.Log($"Database info retrieved: {info.TotalRecordCount} total records");
            }
            catch (Exception ex)
            {
                App.Log($"Error getting database info: {ex.Message}");
            }

            return info;
        }

        #endregion
    }
} 
using SQLite;
using FarmScout.Models;
using System.Text.Json;
using System.Globalization;
using System.Text;

namespace FarmScout.Services
{
    public class DatabaseSeeder
    {
        private readonly SQLiteAsyncConnection _database;

        public DatabaseSeeder(SQLiteAsyncConnection database)
        {
            _database = database;
        }

        public async Task SeedAllDataAsync()
        {
            try
            {
                App.Log("Starting database seeding process...");
                
                // Seed initial lookup data
                await SeedLookupDataAsync();
                
                // Seed initial observation types and data points
                await SeedObservationTypesAsync();

                // Seed initial report groups
                await SeedReportGroupsAsync();

                // Seed observations from CSV file
                await SeedObservationsFromCsvAsync();

                App.Log("Database seeding completed successfully");
            }
            catch (Exception ex)
            {
                App.Log($"Error during database seeding: {ex.Message}");
                // Don't throw - seeding failure shouldn't prevent app startup
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

                App.Log("Seeding lookup data...");

                // Try to load from JSON first, then CSV, then fallback
                var jsonData = await ParseLookupDataJsonFileAsync();
                
                if (jsonData != null && jsonData.LookupGroups.Count > 0)
                {
                    App.Log($"Loading {jsonData.LookupGroups.Count} lookup groups from JSON...");
                    await SeedLookupDataFromJsonAsync(jsonData);
                }
                else
                {
                    App.Log("JSON not found or empty, trying CSV...");
                    // Parse lookup data from CSV
                    var lookupData = await ParseLookupCsvFileAsync();
                    if (lookupData == null || !lookupData.Any())
                    {
                        App.Log("No lookup data found in CSV file, using fallback data");
                        await SeedLookupDataFallbackAsync();
                        return;
                    }

                    App.Log("Seeding lookup data from CSV file...");
                    await SeedLookupDataFromCsvAsync(lookupData);
                }


            }
            catch (Exception ex)
            {
                App.Log($"Error seeding lookup data: {ex.Message}");
            }
        }

        private async Task SeedLookupDataFromJsonAsync(LookupDataJsonData jsonData)
        {
            try
            {
                App.Log("Seeding lookup data from JSON...");

                foreach (var groupJson in jsonData.LookupGroups)
                {
                    if (!Guid.TryParse(groupJson.Id, out var groupId))
                    {
                        App.Log($"Invalid GUID for lookup group: {groupJson.Id}");
                        continue;
                    }

                    // Create and seed the group
                    var group = new LookupGroup
                    {
                        Id = groupId,
                        Name = groupJson.Name,
                        Icon = groupJson.Icon,
                        Color = groupJson.Color,
                        SortOrder = groupJson.SortOrder,
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };

                    await _database.InsertAsync(group);
                    App.Log($"Seeded group from JSON: {group.Name} (ID: {group.Id})");

                    // Seed subgroups for this group
                    foreach (var subGroupJson in groupJson.SubGroups)
                    {
                        if (!Guid.TryParse(subGroupJson.Id, out var subGroupId))
                        {
                            App.Log($"Invalid GUID for lookup subgroup: {subGroupJson.Id}");
                            continue;
                        }

                        var subGroup = new LookupSubGroup
                        {
                            Id = subGroupId,
                            Name = subGroupJson.Name,
                            GroupId = groupId,
                            SortOrder = subGroupJson.SortOrder,
                            IsActive = true,
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        };

                        await _database.InsertAsync(subGroup);
                        App.Log($"Seeded subgroup from JSON: {subGroup.Name} (ID: {subGroup.Id}) in group {group.Name}");

                        // Seed items for this subgroup
                        foreach (var itemJson in subGroupJson.Items)
                        {
                            if (!Guid.TryParse(itemJson.Id, out var itemId))
                            {
                                App.Log($"Invalid GUID for lookup item: {itemJson.Id}");
                                continue;
                            }

                            var item = new LookupItem
                            {
                                Id = itemId,
                                Name = itemJson.Name,
                                Description = itemJson.Description,
                                GroupId = groupId,
                                SubGroupId = subGroupId,
                                IsActive = true,
                                CreatedAt = DateTime.Now,
                                UpdatedAt = DateTime.Now
                            };

                            await _database.InsertAsync(item);
                            App.Log($"Seeded item from JSON: {item.Name} (ID: {item.Id}) in subgroup {subGroup.Name}");
                        }
                    }
                }

                App.Log($"Successfully seeded {jsonData.LookupGroups.Count} lookup groups with their subgroups and items from JSON");
            }
            catch (Exception ex)
            {
                App.Log($"Error seeding lookup data from JSON: {ex.Message}");
                throw;
            }
        }

        private async Task SeedLookupDataFromCsvAsync(List<LookupCsvRow> lookupData)
        {
            try
            {
                App.Log("Seeding lookup data from CSV...");

                // Extract unique groups from CSV data
                var groups = lookupData
                    .Where(row => row.GroupId.HasValue && !string.IsNullOrEmpty(row.Group))
                    .GroupBy(row => new { row.GroupId, row.Group, row.Icon, row.Color, row.SortOrder })
                    .Select(g => new LookupGroup
                    {
                        Id = g.Key.GroupId!.Value,
                        Name = g.Key.Group,
                        Icon = g.Key.Icon,
                        Color = g.Key.Color,
                        SortOrder = g.Key.SortOrder,
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    })
                    .OrderBy(g => g.SortOrder)
                    .ToList();

                // Add groups to database
                foreach (var group in groups)
                {
                    await _database.InsertAsync(group);
                    App.Log($"Seeded group from CSV: {group.Name} (ID: {group.Id})");
                }

                App.Log($"Successfully seeded {groups.Count} groups from CSV");

                // Extract unique subgroups from CSV data
                var subgroups = lookupData
                    .Where(row => row.SubGroupId.HasValue && !string.IsNullOrEmpty(row.SubGroup))
                    .GroupBy(row => new { row.SubGroupId, row.SubGroup, row.GroupId })
                    .Select(g => new LookupSubGroup
                    {
                        Id = g.Key.SubGroupId!.Value,
                        Name = g.Key.SubGroup,
                        GroupId = g.Key.GroupId!.Value,
                        SortOrder = g.Min(row => row.SortOrder), // Use the minimum sort order from the group
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    })
                    .OrderBy(sg => sg.GroupId)
                    .ThenBy(sg => sg.SortOrder)
                    .ToList();

                // Add subgroups to database
                foreach (var subgroup in subgroups)
                {
                    await _database.InsertAsync(subgroup);
                    var groupName = groups.FirstOrDefault(g => g.Id == subgroup.GroupId)?.Name ?? "Unknown";
                    App.Log($"Seeded subgroup from CSV: {subgroup.Name} (ID: {subgroup.Id}) in group {groupName}");
                }

                App.Log($"Successfully seeded {subgroups.Count} subgroups from CSV");

                // Now seed the lookup items
                await SeedLookupItemsAsync(lookupData);
            }
            catch (Exception ex)
            {
                App.Log($"Error seeding lookup data from CSV: {ex.Message}");
                throw;
            }
        }

        private async Task SeedLookupDataFallbackAsync()
        {
            try
            {
                App.Log("Seeding lookup data with hardcoded fallback...");

                // Create basic fallback groups
                var groups = new List<LookupGroup>
                {
                    new() { Name = "Diseases", Icon = "🦠", Color = "#F44336", SortOrder = 1 },
                    new() { Name = "Pests", Icon = "🐛", Color = "#FF9800", SortOrder = 2 },
                    new() { Name = "Crop Types", Icon = "🌾", Color = "#FFC107", SortOrder = 3 },
                    new() { Name = "Conditions", Icon = "🏥", Color = "#4CAF50", SortOrder = 4 }
                };

                foreach (var group in groups)
                {
                    group.IsActive = true;
                    group.CreatedAt = DateTime.Now;
                    group.UpdatedAt = DateTime.Now;
                    await _database.InsertAsync(group);
                    App.Log($"Seeded fallback group: {group.Name} (ID: {group.Id})");
                }

                App.Log($"Successfully seeded {groups.Count} fallback lookup groups");
            }
            catch (Exception ex)
            {
                App.Log($"Error seeding fallback lookup data: {ex.Message}");
                throw;
            }
        }

        private async Task<LookupGroup?> GetLookupGroupByNameAsync(string name)
        {
            try
            {
                var group = await _database.Table<LookupGroup>()
                    .Where(g => g.Name == name && g.IsActive)
                    .FirstOrDefaultAsync();
                return group;
            }
            catch (Exception ex)
            {
                App.Log($"Error retrieving lookup group by name: {ex.Message}");
                return null;
            }
        }

        private static async Task<List<LookupCsvRow>?> ParseLookupCsvFileAsync()
        {
            try
            {
                var csvPath = GetLookupCsvFilePath();
                if (string.IsNullOrEmpty(csvPath) || !File.Exists(csvPath))
                {
                    App.Log("Lookup CSV file not found");
                    return null;
                }

                App.Log($"Reading lookup data from: {csvPath}");

                var lines = await File.ReadAllLinesAsync(csvPath);
                if (lines.Length < 2) // Need header + at least one data row
                {
                    App.Log("Lookup CSV file is empty or has no data rows");
                    return null;
                }

                var lookupData = new List<LookupCsvRow>();

                // Skip header row (line 0)
                for (int i = 1; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line)) continue;

                    var row = ParseLookupCsvLine(line, i + 1);
                    if (row != null)
                    {
                        lookupData.Add(row);
                    }
                }

                App.Log($"Successfully parsed {lookupData.Count} lookup data rows from CSV");
                return lookupData;
            }
            catch (Exception ex)
            {
                App.Log($"Error parsing lookup CSV file: {ex.Message}");
                return null;
            }
        }

        private static string? GetLookupCsvFilePath()
        {
            try
            {
                // Look for the lookup CSV file in the same directory as the scout.csv file
                var baseDir = AppContext.BaseDirectory;
                var possiblePaths = new[]
                {
                    Path.Combine(baseDir, "lookup_data_spreadsheet.csv"),
                    Path.Combine(baseDir, "Resources", "Raw", "lookup_data_spreadsheet.csv"),
                    Path.Combine(baseDir, "..", "lookup_data_spreadsheet.csv"),
                    Path.Combine(baseDir, "..", "..", "lookup_data_spreadsheet.csv")
                };

                foreach (var path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        return path;
                    }
                }

                App.Log("Lookup CSV file not found in any expected location");
                return null;
            }
            catch (Exception ex)
            {
                App.Log($"Error finding lookup CSV file: {ex.Message}");
                return null;
            }
        }

        private static LookupCsvRow? ParseLookupCsvLine(string line, int lineNumber)
        {
            try
            {
                var fields = ParseCsvFields(line);
                if (fields.Length < 10)
                {
                    App.Log($"Warning: Line {lineNumber} has insufficient fields ({fields.Length}), expected 10, skipping");
                    return null;
                }

                // Parse GUIDs - they can be empty for group-only or subgroup-only rows
                Guid? groupId = null;
                Guid? subGroupId = null;
                Guid? itemId = null;

                if (!string.IsNullOrEmpty(fields[0]) && Guid.TryParse(fields[0].Trim(), out var parsedGroupId))
                {
                    groupId = parsedGroupId;
                }

                if (!string.IsNullOrEmpty(fields[1]) && Guid.TryParse(fields[1].Trim(), out var parsedSubGroupId))
                {
                    subGroupId = parsedSubGroupId;
                }

                if (!string.IsNullOrEmpty(fields[2]) && Guid.TryParse(fields[2].Trim(), out var parsedItemId))
                {
                    itemId = parsedItemId;
                }

                return new LookupCsvRow
                {
                    GroupId = groupId,
                    SubGroupId = subGroupId,
                    ItemId = itemId,
                    Group = fields[3].Trim(),
                    SubGroup = fields[4].Trim(),
                    ItemName = fields[5].Trim(),
                    Description = fields[6].Trim(),
                    Icon = fields[7].Trim(),
                    Color = fields[8].Trim(),
                    SortOrder = int.TryParse(fields[9].Trim(), out var sortOrder) ? sortOrder : 0
                };
            }
            catch (Exception ex)
            {
                App.Log($"Error parsing lookup CSV line {lineNumber}: {ex.Message}");
                return null;
            }
        }

        private class LookupCsvRow
        {
            public Guid? GroupId { get; set; }
            public Guid? SubGroupId { get; set; }
            public Guid? ItemId { get; set; }
            public string Group { get; set; } = string.Empty;
            public string SubGroup { get; set; } = string.Empty;
            public string ItemName { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Icon { get; set; } = string.Empty;
            public string Color { get; set; } = string.Empty;
            public int SortOrder { get; set; }
        }

        private async Task SeedLookupItemsAsync(List<LookupCsvRow>? lookupData = null)
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

                App.Log("Seeding lookup items...");

                var seedData = new List<LookupItem>();

                if (lookupData != null && lookupData.Count != 0)
                {
                    // Use CSV data to seed items
                    await SeedLookupItemsFromCsvAsync(lookupData, seedData);
                }
                else
                {
                    seedData = [];
                }

                // Add all lookup items to database
                foreach (var item in seedData)
                {
                    await _database.InsertAsync(item);
                }

                App.Log($"Successfully seeded {seedData.Count} lookup items");
            }
            catch (Exception ex)
            {
                App.Log($"Error seeding lookup items: {ex.Message}");
                // Don't throw - seeding failure shouldn't prevent app startup
            }
        }

        private async Task SeedLookupItemsFromCsvAsync(List<LookupCsvRow> lookupData, List<LookupItem> seedData)
        {
            try
            {
                App.Log("Seeding lookup items from CSV data...");

                // Get all groups and subgroups for efficient lookup
                var groups = await _database.Table<LookupGroup>().ToListAsync();
                var subgroups = await _database.Table<LookupSubGroup>().ToListAsync();

                foreach (var row in lookupData)
                {
                    // Skip rows without item names or item IDs (these are just group/subgroup placeholders)
                    if (string.IsNullOrEmpty(row.ItemName) || !row.ItemId.HasValue) continue;

                    // Verify that the group ID exists
                    if (!row.GroupId.HasValue)
                    {
                        App.Log($"Warning: No group ID specified for item '{row.ItemName}'");
                        continue;
                    }

                    var group = groups.FirstOrDefault(g => g.Id == row.GroupId.Value);
                    if (group == null)
                    {
                        App.Log($"Warning: Group ID '{row.GroupId}' not found for item '{row.ItemName}'");
                        continue;
                    }

                    Guid? subgroupId = null;
                    if (row.SubGroupId.HasValue)
                    {
                        var subgroup = subgroups.FirstOrDefault(sg => sg.Id == row.SubGroupId.Value);
                        if (subgroup != null)
                        {
                            subgroupId = subgroup.Id;
                        }
                        else
                        {
                            App.Log($"Warning: Subgroup ID '{row.SubGroupId}' not found for item '{row.ItemName}'");
                        }
                    }

                    var lookupItem = new LookupItem
                    {
                        Id = row.ItemId.Value,
                        Name = row.ItemName,
                        GroupId = group.Id,
                        SubGroupId = subgroupId,
                        Description = row.Description,
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };

                    seedData.Add(lookupItem);
                    App.Log($"Processed lookup item from CSV: {lookupItem.Name} (ID: {lookupItem.Id}) in group {group.Name}");
                }

                App.Log($"Successfully processed {seedData.Count} items from CSV data");
            }
            catch (Exception ex)
            {
                App.Log($"Error seeding lookup items from CSV: {ex.Message}");
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

                App.Log("Seeding observation types...");

                // Try to load from JSON first, then CSV, then fallback
                var jsonData = await ParseObservationTypesJsonFileAsync();
                List<ObservationType> observationTypes;

                if (jsonData != null && jsonData.ObservationTypes.Count > 0)
                {
                    App.Log($"Loading {jsonData.ObservationTypes.Count} observation types from JSON...");
                    observationTypes = await SeedObservationTypesFromJsonAsync(jsonData);
                }
                else
                {
                    App.Log("JSON not found or empty, trying CSV...");
                    var csvData = await ParseObservationTypesCsvFileAsync();
                    
                    if (csvData != null && csvData.Count > 0)
                    {
                        App.Log($"Loading {csvData.Count} observation types from CSV...");
                        observationTypes = await SeedObservationTypesFromCsvAsync(csvData);
                        // Seed data points separately for CSV
                        await SeedDataPointsAsync(observationTypes);
                    }
                    else
                    {
                        App.Log("CSV not found or empty, using fallback hardcoded data...");
                        observationTypes = await SeedObservationTypesFallbackAsync();
                        // Seed data points separately for fallback
                        await SeedDataPointsAsync(observationTypes);
                    }
                }

                App.Log($"Successfully seeded {observationTypes.Count} observation types");
            }
            catch (Exception ex)
            {
                App.Log($"Error seeding observation types: {ex.Message}");
                // Don't throw - seeding failure shouldn't prevent app startup
            }
        }

        private async Task<List<ObservationType>> SeedObservationTypesFromJsonAsync(ObservationTypesJsonData jsonData)
        {
            var observationTypes = new List<ObservationType>();

            foreach (var typeJson in jsonData.ObservationTypes)
            {
                if (!Guid.TryParse(typeJson.Id, out var typeId))
                {
                    App.Log($"Invalid GUID for observation type: {typeJson.Id}");
                    continue;
                }

                var observationType = new ObservationType
                {
                    Id = typeId,
                    Name = typeJson.Name,
                    Description = typeJson.Description,
                    Icon = typeJson.Icon,
                    Color = typeJson.Color,
                    SortOrder = typeJson.SortOrder,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    IsActive = true
                };

                await _database.InsertAsync(observationType);
                observationTypes.Add(observationType);
                App.Log($"Seeded observation type from JSON: {observationType.Name} (ID: {observationType.Id})");

                // Seed data points for this observation type
                foreach (var dataPointJson in typeJson.DataPoints)
                {
                    if (!Guid.TryParse(dataPointJson.Id, out var dataPointId))
                    {
                        App.Log($"Invalid GUID for data point: {dataPointJson.Id}");
                        continue;
                    }

                    var dataPoint = new ObservationTypeDataPoint
                    {
                        Id = dataPointId,
                        ObservationTypeId = typeId,
                        Code = dataPointJson.Code,
                        Label = dataPointJson.Label,
                        DataType = dataPointJson.DataType,
                        LookupGroupName = dataPointJson.LookupGroupName ?? string.Empty,
                        IsRequired = dataPointJson.IsRequired,
                        SortOrder = dataPointJson.SortOrder,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now,
                        IsActive = true
                    };

                    await _database.InsertAsync(dataPoint);
                    App.Log($"Seeded data point from JSON: {dataPoint.Code} (ID: {dataPoint.Id}) for observation type {observationType.Name}");
                }
            }

            App.Log($"Successfully seeded {observationTypes.Count} observation types with their data points from JSON");
            return observationTypes;
        }

        private async Task<List<ObservationType>> SeedObservationTypesFromCsvAsync(List<ObservationTypeCsvRow> csvData)
        {
            var observationTypes = new List<ObservationType>();

            foreach (var row in csvData)
            {
                var observationType = new ObservationType
                {
                    Id = row.Id,
                    Name = row.Name,
                    Description = row.Description,
                    Icon = row.Icon,
                    Color = row.Color,
                    SortOrder = row.SortOrder,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    IsActive = true
                };

                await _database.InsertAsync(observationType);
                observationTypes.Add(observationType);
                App.Log($"Seeded observation type from CSV: {observationType.Name} (ID: {observationType.Id})");
            }

            return observationTypes;
        }

        private async Task<List<ObservationType>> SeedObservationTypesFallbackAsync()
        {
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
                await _database.InsertAsync(type);
            }

            return observationTypes;
        }

        private async Task SeedDataPointsAsync(List<ObservationType> observationTypes)
        {
            try
            {
                App.Log("Seeding observation type data points...");

                // Try to load from CSV first
                var csvData = await ParseObservationTypeDataPointsCsvFileAsync();
                
                if (csvData != null && csvData.Count > 0)
                {
                    App.Log($"Loading {csvData.Count} data points from CSV...");
                    await SeedDataPointsFromCsvAsync(csvData, observationTypes);
                }
                else
                {
                    App.Log("CSV not found or empty, using fallback hardcoded data...");
                    await SeedDataPointsFallbackAsync(observationTypes);
                }
            }
            catch (Exception ex)
            {
                App.Log($"Error seeding data points: {ex.Message}");
                // Don't throw - seeding failure shouldn't prevent app startup
            }
        }

        private async Task SeedDataPointsFromCsvAsync(List<ObservationTypeDataPointCsvRow> csvData, List<ObservationType> observationTypes)
        {
            var dataPointsSeeded = 0;
            var observationTypeDict = observationTypes.ToDictionary(ot => ot.Id, ot => ot);

            foreach (var row in csvData)
            {
                // Verify that the observation type exists
                if (!observationTypeDict.ContainsKey(row.ObservationTypeId))
                {
                    App.Log($"Warning: Observation type ID {row.ObservationTypeId} not found for data point {row.Code}");
                    continue;
                }

                var dataPoint = new ObservationTypeDataPoint
                {
                    Id = row.Id,
                    ObservationTypeId = row.ObservationTypeId,
                    Code = row.Code,
                    Label = row.Label,
                    DataType = row.DataType,
                    LookupGroupName = row.LookupGroupName ?? string.Empty,
                    IsRequired = row.IsRequired,
                    SortOrder = row.SortOrder,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    IsActive = true
                };

                await _database.InsertAsync(dataPoint);
                dataPointsSeeded++;
                App.Log($"Seeded data point from CSV: {dataPoint.Code} (ID: {dataPoint.Id}) for observation type {observationTypeDict[row.ObservationTypeId].Name}");
            }

            App.Log($"Successfully seeded {dataPointsSeeded} data points from CSV");
        }

        private async Task SeedDataPointsFallbackAsync(List<ObservationType> observationTypes)
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
                await _database.InsertAsync(dataPoint);
            }

            App.Log($"Successfully seeded {dataPoints.Count} data points from fallback data");
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
                    group.CreatedAt = DateTime.Now;
                    group.UpdatedAt = DateTime.Now;
                    await _database.InsertAsync(group);
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
                        await _database.InsertAsync(observation);
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
                var existingLocations = await _database.Table<FarmLocation>().ToListAsync();
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
                    await _database.InsertAsync(farmLocation);
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
                var farmLocations = await _database.Table<FarmLocation>().ToListAsync();
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
                var existingScoutType = await _database.Table<ObservationType>()
                    .Where(t => t.Name == "Scout" && t.IsActive)
                    .FirstOrDefaultAsync();
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

                await _database.InsertAsync(scoutType);
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

                    await _database.InsertAsync(dataPoint);
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
                var dataPoints = await _database.Table<ObservationTypeDataPoint>()
                    .Where(d => d.ObservationTypeId == scoutType.Id && d.IsActive)
                    .ToListAsync();
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

                        await _database.InsertAsync(metadata);
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
                var existingGroup = await _database.Table<LookupGroup>()
                    .Where(g => g.Name == "Scout Conditions" && g.IsActive)
                    .FirstOrDefaultAsync();
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

                await _database.InsertAsync(scoutConditionsGroup);
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
                    condition.CreatedAt = DateTime.Now;
                    condition.UpdatedAt = DateTime.Now;
                    await _database.InsertAsync(condition);
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

        #region JSON Parsing for Observation Types and Data Points

        private static async Task<ObservationTypesJsonData?> ParseObservationTypesJsonFileAsync()
        {
            try
            {
                var jsonPath = GetObservationTypesJsonFilePath();
                if (string.IsNullOrEmpty(jsonPath))
                {
                    App.Log("observation_types_seeding.json not found");
                    return null;
                }

                App.Log($"Reading observation types JSON from: {jsonPath}");
                
                var jsonContent = await File.ReadAllTextAsync(jsonPath);
                if (string.IsNullOrEmpty(jsonContent))
                {
                    App.Log("Observation types JSON file is empty");
                    return null;
                }

                var jsonData = JsonSerializer.Deserialize<ObservationTypesJsonData>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                });

                if (jsonData?.ObservationTypes == null || jsonData.ObservationTypes.Count == 0)
                {
                    App.Log("No observation types found in JSON file");
                    return null;
                }

                App.Log($"Successfully parsed {jsonData.ObservationTypes.Count} observation types from JSON");
                return jsonData;
            }
            catch (Exception ex)
            {
                App.Log($"Error reading observation types JSON file: {ex.Message}");
                return null;
            }
        }

        private static string? GetObservationTypesJsonFilePath()
        {
            var fileName = "observation_types_seeding.json";
            var searchPaths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Raw", fileName),
                Path.Combine(FileSystem.AppDataDirectory, fileName),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), fileName),
                fileName // Current directory
            };

            foreach (var path in searchPaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }

        private class ObservationTypesJsonData
        {
            public List<ObservationTypeJson> ObservationTypes { get; set; } = new();
        }

        private class ObservationTypeJson
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Icon { get; set; } = string.Empty;
            public string Color { get; set; } = string.Empty;
            public int SortOrder { get; set; }
            public List<ObservationTypeDataPointJson> DataPoints { get; set; } = new();
        }

        private class ObservationTypeDataPointJson
        {
            public string Id { get; set; } = string.Empty;
            public string Code { get; set; } = string.Empty;
            public string Label { get; set; } = string.Empty;
            public string DataType { get; set; } = string.Empty;
            public string? LookupGroupName { get; set; }
            public bool IsRequired { get; set; }
            public int SortOrder { get; set; }
        }

        #endregion

        #region JSON Parsing for Lookup Data

        private static async Task<LookupDataJsonData?> ParseLookupDataJsonFileAsync()
        {
            try
            {
                var jsonPath = GetLookupDataJsonFilePath();
                if (string.IsNullOrEmpty(jsonPath))
                {
                    App.Log("lookup_data_seeding.json not found");
                    return null;
                }

                App.Log($"Reading lookup data JSON from: {jsonPath}");
                
                var jsonContent = await File.ReadAllTextAsync(jsonPath);
                if (string.IsNullOrEmpty(jsonContent))
                {
                    App.Log("Lookup data JSON file is empty");
                    return null;
                }

                var jsonData = JsonSerializer.Deserialize<LookupDataJsonData>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                });

                if (jsonData?.LookupGroups == null || jsonData.LookupGroups.Count == 0)
                {
                    App.Log("No lookup groups found in JSON file");
                    return null;
                }

                App.Log($"Successfully parsed {jsonData.LookupGroups.Count} lookup groups from JSON");
                return jsonData;
            }
            catch (Exception ex)
            {
                App.Log($"Error reading lookup data JSON file: {ex.Message}");
                return null;
            }
        }

        private static string? GetLookupDataJsonFilePath()
        {
            var fileName = "lookup_data_seeding.json";
            var searchPaths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Raw", fileName),
                Path.Combine(FileSystem.AppDataDirectory, fileName),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), fileName),
                fileName // Current directory
            };

            foreach (var path in searchPaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }

        private class LookupDataJsonData
        {
            public List<LookupGroupJson> LookupGroups { get; set; } = new();
        }

        private class LookupGroupJson
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Icon { get; set; } = string.Empty;
            public string Color { get; set; } = string.Empty;
            public int SortOrder { get; set; }
            public List<LookupSubGroupJson> SubGroups { get; set; } = new();
        }

        private class LookupSubGroupJson
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Icon { get; set; } = string.Empty;
            public string Color { get; set; } = string.Empty;
            public int SortOrder { get; set; }
            public List<LookupItemJson> Items { get; set; } = new();
        }

        private class LookupItemJson
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Icon { get; set; } = string.Empty;
            public string Color { get; set; } = string.Empty;
            public int SortOrder { get; set; }
        }

        #endregion

        #region CSV Parsing for Observation Types and Data Points

        private static async Task<List<ObservationTypeCsvRow>?> ParseObservationTypesCsvFileAsync()
        {
            try
            {
                var csvPath = GetObservationTypesCsvFilePath();
                if (string.IsNullOrEmpty(csvPath))
                {
                    App.Log("observation_types_seeding.csv not found");
                    return null;
                }

                App.Log($"Reading observation types CSV from: {csvPath}");
                
                var lines = await File.ReadAllLinesAsync(csvPath);
                if (lines.Length <= 1)
                {
                    App.Log("Observation types CSV file is empty or only contains header");
                    return null;
                }

                var observationTypes = new List<ObservationTypeCsvRow>();
                
                // Skip header row
                for (int i = 1; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line)) continue;

                    try
                    {
                        var row = ParseObservationTypeCsvLine(line, i + 1);
                        if (row != null)
                        {
                            observationTypes.Add(row);
                        }
                    }
                    catch (Exception ex)
                    {
                        App.Log($"Error parsing observation type CSV line {i + 1}: {ex.Message}");
                    }
                }

                App.Log($"Successfully parsed {observationTypes.Count} observation types from CSV");
                return observationTypes;
            }
            catch (Exception ex)
            {
                App.Log($"Error reading observation types CSV file: {ex.Message}");
                return null;
            }
        }

        private static async Task<List<ObservationTypeDataPointCsvRow>?> ParseObservationTypeDataPointsCsvFileAsync()
        {
            try
            {
                var csvPath = GetObservationTypeDataPointsCsvFilePath();
                if (string.IsNullOrEmpty(csvPath))
                {
                    App.Log("observation_type_datapoints_seeding.csv not found");
                    return null;
                }

                App.Log($"Reading observation type data points CSV from: {csvPath}");
                
                var lines = await File.ReadAllLinesAsync(csvPath);
                if (lines.Length <= 1)
                {
                    App.Log("Observation type data points CSV file is empty or only contains header");
                    return null;
                }

                var dataPoints = new List<ObservationTypeDataPointCsvRow>();
                
                // Skip header row
                for (int i = 1; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line)) continue;

                    try
                    {
                        var row = ParseObservationTypeDataPointCsvLine(line, i + 1);
                        if (row != null)
                        {
                            dataPoints.Add(row);
                        }
                    }
                    catch (Exception ex)
                    {
                        App.Log($"Error parsing observation type data point CSV line {i + 1}: {ex.Message}");
                    }
                }

                App.Log($"Successfully parsed {dataPoints.Count} observation type data points from CSV");
                return dataPoints;
            }
            catch (Exception ex)
            {
                App.Log($"Error reading observation type data points CSV file: {ex.Message}");
                return null;
            }
        }

        private static string? GetObservationTypesCsvFilePath()
        {
            var fileName = "observation_types_seeding.csv";
            var searchPaths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Raw", fileName),
                Path.Combine(FileSystem.AppDataDirectory, fileName),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), fileName),
                fileName // Current directory
            };

            foreach (var path in searchPaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }

        private static string? GetObservationTypeDataPointsCsvFilePath()
        {
            var fileName = "observation_type_datapoints_seeding.csv";
            var searchPaths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Raw", fileName),
                Path.Combine(FileSystem.AppDataDirectory, fileName),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), fileName),
                fileName // Current directory
            };

            foreach (var path in searchPaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }

        private static ObservationTypeCsvRow? ParseObservationTypeCsvLine(string line, int lineNumber)
        {
            try
            {
                var fields = ParseCsvFields(line);
                if (fields.Length < 6)
                {
                    App.Log($"Observation type CSV line {lineNumber} has insufficient fields ({fields.Length} found, 6 expected)");
                    return null;
                }

                if (!Guid.TryParse(fields[0], out var id))
                {
                    App.Log($"Invalid GUID in observation type CSV line {lineNumber}: {fields[0]}");
                    return null;
                }

                if (!int.TryParse(fields[5], out var sortOrder))
                {
                    App.Log($"Invalid sort order in observation type CSV line {lineNumber}: {fields[5]}");
                    sortOrder = 0;
                }

                return new ObservationTypeCsvRow
                {
                    Id = id,
                    Name = fields[1],
                    Description = fields[2],
                    Icon = fields[3],
                    Color = fields[4],
                    SortOrder = sortOrder
                };
            }
            catch (Exception ex)
            {
                App.Log($"Error parsing observation type CSV line {lineNumber}: {ex.Message}");
                return null;
            }
        }

        private static ObservationTypeDataPointCsvRow? ParseObservationTypeDataPointCsvLine(string line, int lineNumber)
        {
            try
            {
                var fields = ParseCsvFields(line);
                if (fields.Length < 8)
                {
                    App.Log($"Observation type data point CSV line {lineNumber} has insufficient fields ({fields.Length} found, 8 expected)");
                    return null;
                }

                if (!Guid.TryParse(fields[0], out var id))
                {
                    App.Log($"Invalid GUID in observation type data point CSV line {lineNumber}: {fields[0]}");
                    return null;
                }

                if (!Guid.TryParse(fields[1], out var observationTypeId))
                {
                    App.Log($"Invalid observation type GUID in data point CSV line {lineNumber}: {fields[1]}");
                    return null;
                }

                if (!bool.TryParse(fields[6], out var isRequired))
                {
                    App.Log($"Invalid boolean value for IsRequired in data point CSV line {lineNumber}: {fields[6]}");
                    isRequired = false;
                }

                if (!int.TryParse(fields[7], out var sortOrder))
                {
                    App.Log($"Invalid sort order in data point CSV line {lineNumber}: {fields[7]}");
                    sortOrder = 0;
                }

                return new ObservationTypeDataPointCsvRow
                {
                    Id = id,
                    ObservationTypeId = observationTypeId,
                    Code = fields[2],
                    Label = fields[3],
                    DataType = fields[4],
                    LookupGroupName = string.IsNullOrEmpty(fields[5]) ? null : fields[5],
                    IsRequired = isRequired,
                    SortOrder = sortOrder
                };
            }
            catch (Exception ex)
            {
                App.Log($"Error parsing observation type data point CSV line {lineNumber}: {ex.Message}");
                return null;
            }
        }

        private class ObservationTypeCsvRow
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Icon { get; set; } = string.Empty;
            public string Color { get; set; } = string.Empty;
            public int SortOrder { get; set; }
        }

        private class ObservationTypeDataPointCsvRow
        {
            public Guid Id { get; set; }
            public Guid ObservationTypeId { get; set; }
            public string Code { get; set; } = string.Empty;
            public string Label { get; set; } = string.Empty;
            public string DataType { get; set; } = string.Empty;
            public string? LookupGroupName { get; set; }
            public bool IsRequired { get; set; }
            public int SortOrder { get; set; }
        }

        #endregion
    }
} 
# CSV-Based Lookup Data Seeding Implementation

## Overview

The FarmScout application has been updated to use CSV files for seeding lookup data instead of hardcoded values. This provides better maintainability and allows for easy updates to the lookup data without code changes.

## Changes Made

### 1. Modified `DatabaseSeeder.cs`

The `SeedLookupDataAsync()` method has been updated to:
- First attempt to read lookup data from a CSV file
- Fall back to hardcoded data if the CSV file is not found or cannot be parsed
- Extract groups, subgroups, and items from the CSV data
- Maintain backward compatibility

### 2. New CSV Parsing Methods

Added several new methods to handle CSV file processing:

#### `ParseLookupCsvFileAsync()`
- Reads and parses the lookup CSV file
- Returns a list of `LookupCsvRow` objects
- Handles file not found scenarios gracefully

#### `GetLookupCsvFilePath()`
- Searches for the CSV file in multiple locations:
  - Application base directory
  - Resources/Raw directory
  - Parent directories
- Returns the first found path or null

#### `ParseLookupCsvLine()`
- Parses individual CSV lines into `LookupCsvRow` objects
- Handles CSV field parsing with proper escaping
- Validates required fields

#### `LookupCsvRow` Class
- Internal class to represent CSV data rows
- Contains properties for Group, SubGroup, ItemName, Description, Icon, Color, and SortOrder

### 3. Enhanced Seeding Methods

#### `SeedLookupItemsAsync()`
- Now accepts an optional `List<LookupCsvRow>` parameter
- Uses CSV data when available, falls back to hardcoded data
- Maintains the same database insertion logic

#### `SeedLookupItemsFromCsvAsync()`
- Processes CSV data to create lookup items
- Maps groups and subgroups correctly
- Handles missing or invalid data gracefully

#### `SeedLookupItemsFallbackAsync()`
- Contains the original hardcoded lookup data
- Used when CSV file is not available or parsing fails
- Ensures the application always has lookup data

## CSV File Format

The lookup data CSV file (`lookup_data_spreadsheet.csv`) should have the following structure:

```csv
Group,SubGroup,Item Name,Description,Icon,Color,Sort Order
Crop Types,,Corn,Maize crop for grain or silage,🌾,#4CAF50,1
Diseases,Fungal,Rust,Fungal disease affecting leaves and stems,🦠,#F44336,2
```

### Required Columns:
- **Group**: Main category (e.g., "Crop Types", "Diseases")
- **SubGroup**: Subcategory (e.g., "Fungal", "Herbicide") - may be empty
- **Item Name**: Specific item name (e.g., "Corn", "Rust") - may be empty for subgroup placeholders
- **Description**: Detailed description of the item
- **Icon**: Emoji icon representing the group
- **Color**: Hex color code for the group
- **Sort Order**: Numeric order for display purposes

## File Location

The CSV file is searched in the following locations (in order):
1. `{AppBaseDirectory}/lookup_data_spreadsheet.csv`
2. `{AppBaseDirectory}/Resources/Raw/lookup_data_spreadsheet.csv`
3. `{AppBaseDirectory}/../lookup_data_spreadsheet.csv`
4. `{AppBaseDirectory}/../../lookup_data_spreadsheet.csv`

## Benefits

### 1. **Maintainability**
- Lookup data can be updated without code changes
- Non-developers can modify the CSV file
- Version control friendly for data changes

### 2. **Flexibility**
- Easy to add new groups, subgroups, and items
- Can be used for different environments (dev, staging, production)
- Supports bulk data imports

### 3. **Backward Compatibility**
- Falls back to hardcoded data if CSV is not available
- No breaking changes to existing functionality
- Graceful error handling

### 4. **Data Management**
- Clear separation between code and data
- Easy to export/import lookup data
- Can be integrated with external data sources

## Usage

### Adding New Lookup Data

1. **Edit the CSV file** (`lookup_data_spreadsheet.csv`)
2. **Add new rows** following the established format
3. **Restart the application** or clear the database to trigger re-seeding

### Example: Adding a New Crop Type

```csv
Crop Types,,Barley,Barley crop for brewing and feed,🌾,#4CAF50,1
```

### Example: Adding a New Disease Subgroup

```csv
Diseases,Protozoan,,,🦠,#F44336,2
Diseases,Protozoan,Downy Mildew,Protozoan disease affecting leaves,🦠,#F44336,2
```

## Error Handling

The implementation includes comprehensive error handling:

- **File not found**: Falls back to hardcoded data
- **Invalid CSV format**: Logs warnings and skips invalid rows
- **Missing required fields**: Logs warnings and skips incomplete rows
- **Database errors**: Logs errors but doesn't prevent app startup

## Logging

The seeding process provides detailed logging:

```
Seeding lookup data from CSV file...
Reading lookup data from: C:\path\to\lookup_data_spreadsheet.csv
Successfully parsed 85 lookup data rows from CSV
Successfully seeded 10 groups from CSV
Successfully seeded 35 subgroups from CSV
Successfully processed 40 items from CSV data
Successfully seeded 40 lookup items
```

## Migration from Hardcoded Data

Existing applications will automatically migrate to CSV-based seeding:

1. **First run**: Uses hardcoded data (if CSV not found)
2. **Add CSV file**: Subsequent runs will use CSV data
3. **No data loss**: Existing database records are preserved
4. **Seamless transition**: No user intervention required

## Future Enhancements

Potential improvements for future versions:

1. **Real-time CSV monitoring**: Auto-reload when CSV changes
2. **Multiple CSV files**: Support for different data sets
3. **Web-based CSV editor**: Built-in data management interface
4. **Data validation**: Enhanced CSV format validation
5. **Import/Export tools**: Utilities for data migration 
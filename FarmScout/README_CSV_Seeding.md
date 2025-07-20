# CSV Seeding for FarmScout

This document explains how the CSV seeding functionality works in the FarmScout application.

## Overview

The FarmScout application now includes automatic seeding of observations from the `scout.csv` file at startup. This allows the app to be pre-populated with real farm observation data.

## How It Works

### 1. Database Initialization
When the app starts, the `FarmScoutDatabase.InitializeDatabaseAsync()` method is called, which:
- Creates all necessary database tables
- Seeds lookup data (groups, subgroups, items)
- Seeds observation types and data points
- **Seeds observations from CSV** (new functionality)
- Seeds report groups

### 2. CSV Seeding Process
The `SeedObservationsFromCsvAsync()` method:

1. **Checks for existing data**: If observations already exist in the database, seeding is skipped
2. **Locates CSV file**: Looks for `scout.csv` in multiple locations:
   - `scout.csv` (current directory)
   - `Reports/scout.csv`
   - `Resources/scout.csv`
   - `Assets/scout.csv`
   - App data directory
3. **Parses CSV data**: Reads and parses the CSV file, handling quoted fields
4. **Creates farm locations**: Automatically creates farm locations for each unique section
5. **Imports observations**: Adds all observations to the database

### 3. Data Mapping

The CSV data is mapped to the Observation model as follows:

| CSV Field | Observation Property | Notes |
|-----------|---------------------|-------|
| id | Id | Parsed as GUID |
| date | Timestamp | Parsed as DateTime (dd/MM/yyyy format) |
| section | Summary | Combined with metric as "metric - section" |
| metric | Summary | Combined with section as "metric - section" |
| condition | Severity | Mapped: pass→Information, partial→Warning, fail→Fail |
| notes | Notes | Direct copy |

### 4. Farm Location Creation

For each unique section found in the CSV data, a FarmLocation is automatically created with:
- **Name**: Section name (e.g., "B1S1", "B2S7")
- **Description**: "Farm section {section}"
- **FieldType**: "Macadamia"
- **Area**: 0.0 (placeholder)
- **Owner**: "Farm Owner"
- **Geometry**: Default polygon geometry

## File Requirements

### CSV Format
The `scout.csv` file must have the following format:
```csv
id,date,section,metric,condition,notes
1,20/11/2024,B2S7,Insect pests,partial,"1 nut borer larvae, black citrus aphids, broad mites and few thrips"
2,20/11/2024,B2S7,diseases,pass,2 plants with sterm canker
...
```

### Required Fields
- **id**: Unique identifier (GUID format)
- **date**: Date in dd/MM/yyyy format
- **section**: Farm section identifier
- **metric**: Observation metric/type
- **condition**: Status (pass/partial/fail)
- **notes**: Optional notes/description

## Implementation Details

### Key Methods Added

#### FarmScoutDatabase.cs
- `SeedObservationsFromCsvAsync()`: Main seeding method
- `GetCsvFilePathAsync()`: Locates CSV file
- `ParseCsvFileAsync()`: Reads and parses CSV
- `ParseCsvLine()`: Parses individual CSV lines
- `ParseCsvFields()`: Handles quoted CSV fields
- `MapConditionToSeverity()`: Maps conditions to severity levels
- `EnsureFarmLocationsExistAsync()`: Creates farm locations
- `ExtractSectionFromSummary()`: Extracts section from summary

#### FarmLocation CRUD Methods
- `AddFarmLocationAsync()`
- `GetFarmLocationsAsync()`
- `GetFarmLocationByIdAsync()`
- `GetFarmLocationByNameAsync()`
- `UpdateFarmLocationAsync()`
- `DeleteFarmLocationAsync()`

### Error Handling
- **Graceful degradation**: If CSV seeding fails, the app continues to start
- **Detailed logging**: All operations are logged for debugging
- **Validation**: CSV data is validated before import
- **Duplicate prevention**: Seeding only occurs if no observations exist

## Usage

### Automatic Seeding
The seeding happens automatically when the app starts for the first time (when no observations exist in the database).

### Manual Testing
You can test the seeding functionality using the `TestCsvSeeding` class:

```csharp
await TestCsvSeeding.TestCsvSeedingAsync();
```

### Logging
All seeding operations are logged to the app's log system. Check the logs to see:
- Whether CSV file was found
- Number of observations imported
- Farm locations created
- Any errors that occurred

## Benefits

1. **Immediate Data**: App starts with real farm data
2. **Testing**: Provides realistic data for testing features
3. **Demonstration**: Shows app capabilities with actual data
4. **Development**: Reduces need to manually create test data
5. **User Experience**: Users see meaningful data immediately

## Troubleshooting

### Common Issues

1. **CSV file not found**
   - Ensure `scout.csv` is in the correct location
   - Check file permissions
   - Verify file name spelling

2. **Parsing errors**
   - Check CSV format matches expected structure
   - Verify date format is dd/MM/yyyy
   - Ensure IDs are valid GUIDs

3. **Database errors**
   - Check database permissions
   - Verify database schema is correct
   - Check available disk space

### Debug Information
Enable detailed logging to see:
- CSV file location
- Number of lines parsed
- Farm locations created
- Import progress
- Any errors encountered

## Future Enhancements

Potential improvements to the CSV seeding system:

1. **Coordinate mapping**: Add actual GPS coordinates for farm sections
2. **Area calculation**: Calculate actual areas for farm locations
3. **Metadata support**: Import observation metadata
4. **Photo linking**: Link observations to existing photos
5. **Incremental updates**: Support for updating existing data
6. **Multiple file support**: Import from multiple CSV files
7. **Data validation**: Enhanced validation and error reporting
8. **Progress reporting**: Real-time progress updates during import

## File Locations

- **CSV file**: `FarmScout/scout.csv`
- **Implementation**: `FarmScout/Services/FarmScoutDatabase.cs`
- **Interface**: `FarmScout/Services/IFarmScoutDatabase.cs`
- **Test**: `FarmScout/TestCsvSeeding.cs`
- **Documentation**: `FarmScout/README_CSV_Seeding.md`

The CSV seeding functionality provides a robust way to populate the FarmScout application with real farm data, making it immediately useful for users and developers. 
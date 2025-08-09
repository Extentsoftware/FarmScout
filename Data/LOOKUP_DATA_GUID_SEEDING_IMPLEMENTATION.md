# Lookup Data GUID-Based Seeding Implementation

## Overview

The FarmScout database seeder has been enhanced to support GUID primary keys for lookup data (Groups, SubGroups, and Items) from the CSV file. This ensures consistent referential integrity across environments and deployments while maintaining the flexibility of external configuration.

## Changes Made

### 1. CSV File Structure Update

#### New lookup_data_spreadsheet.csv Format
The CSV file now includes GUID columns for primary keys:

```csv
GroupId,SubGroupId,ItemId,Group,SubGroup,Item Name,Description,Icon,Color,Sort Order
750e8400-e29b-41d4-a716-446655440001,,,Diseases,,,Disease types and conditions,🦠,#F44336,1
750e8400-e29b-41d4-a716-446655440001,850e8400-e29b-41d4-a716-446655440001,,Diseases,Fungal,,,🍄,#8D6E63,1
750e8400-e29b-41d4-a716-446655440001,850e8400-e29b-41d4-a716-446655440001,950e8400-e29b-41d4-a716-446655440001,Diseases,Fungal,Powdery Mildew,White powdery coating on leaves,🍄,#FFEB3B,1
...
```

**Column Structure:**
- `GroupId`: GUID primary key for lookup groups (required for groups)
- `SubGroupId`: GUID primary key for lookup subgroups (optional, only for subgroup rows)
- `ItemId`: GUID primary key for lookup items (optional, only for item rows)
- `Group`: Group name (required)
- `SubGroup`: Subgroup name (optional)
- `Item Name`: Item name (optional, only for item rows)
- `Description`: Description text
- `Icon`: Unicode emoji icon
- `Color`: Hex color code
- `Sort Order`: Integer sort order

### 2. Enhanced CSV Parsing

#### Updated LookupCsvRow Class
```csharp
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
```

#### Enhanced ParseLookupCsvLine Method
- **GUID Validation**: Proper GUID parsing with null handling
- **Field Count Validation**: Expects 10 fields (increased from 7)
- **Flexible GUID Handling**: GUIDs can be empty for group-only or subgroup-only rows
- **Error Handling**: Comprehensive error logging for invalid data

### 3. Updated Seeding Methods

#### SeedLookupDataAsync Method
**Group Seeding:**
- Uses `GroupId` from CSV as primary key
- Filters for rows with valid `GroupId` and `Group` name
- Sets all required fields including `IsActive`, `CreatedAt`, `UpdatedAt`
- Groups by unique `GroupId` to avoid duplicates

**Subgroup Seeding:**
- Uses `SubGroupId` from CSV as primary key
- Links to parent group using `GroupId`
- Validates parent group existence
- Maintains proper sort order and metadata

#### SeedLookupItemsFromCsvAsync Method
**Item Seeding:**
- Uses `ItemId` from CSV as primary key
- Validates both `GroupId` and optional `SubGroupId` references
- Ensures referential integrity with existing groups and subgroups
- Comprehensive logging for missing references

### 4. Key Features

#### GUID Primary Key Support
- **Consistent IDs**: Same GUIDs across different environments
- **Referential Integrity**: Proper foreign key relationships
- **Predictable Testing**: Known IDs for unit and integration tests
- **Data Migration**: Easier data migration between environments

#### Robust Error Handling
- **GUID Validation**: Invalid GUIDs are logged and skipped
- **Reference Validation**: Missing parent records are logged
- **Field Validation**: Insufficient field counts are handled gracefully
- **Continued Operation**: Parsing errors don't stop the seeding process

#### Comprehensive Logging
- **Detailed Progress**: Each entity creation is logged with its GUID
- **Error Reporting**: Clear error messages for troubleshooting
- **Relationship Tracking**: Logs show parent-child relationships
- **Statistics**: Summary counts for each entity type

## Benefits

### 1. Referential Integrity
- Consistent primary keys across environments
- Reliable foreign key relationships
- Predictable data structure for testing

### 2. External Configuration
- Lookup data managed outside of code
- Easy updates without recompilation
- Version control for data changes

### 3. Deployment Consistency
- Same GUIDs in development, staging, and production
- Reliable data migrations
- Consistent test data

### 4. Backward Compatibility
- Fallback to hardcoded data if CSV is unavailable
- Graceful handling of missing or invalid data
- Non-breaking changes to existing functionality

## CSV File Management

### 1. GUID Generation
Use consistent GUID format: `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`

**Recommended GUID Ranges:**
- Groups: `750e8400-e29b-41d4-a716-44665544xxxx`
- SubGroups: `850e8400-e29b-41d4-a716-44665544xxxx`
- Items: `950e8400-e29b-41d4-a716-44665544xxxx`

### 2. Row Types
**Group-Only Rows:**
- `GroupId`: Required GUID
- `SubGroupId`, `ItemId`: Empty
- `Group`: Required name
- `SubGroup`, `Item Name`: Empty

**Subgroup-Only Rows:**
- `GroupId`: Parent group GUID
- `SubGroupId`: Required GUID
- `ItemId`: Empty
- `Group`: Parent group name
- `SubGroup`: Required name
- `Item Name`: Empty

**Item Rows:**
- `GroupId`: Parent group GUID
- `SubGroupId`: Parent subgroup GUID (optional)
- `ItemId`: Required GUID
- `Group`: Parent group name
- `SubGroup`: Parent subgroup name (optional)
- `Item Name`: Required name

### 3. Data Validation
- Ensure all GUIDs are unique within their type
- Verify parent-child relationships are correct
- Check that required fields are populated
- Validate GUID format consistency

## Usage

### 1. Creating New Lookup Data
1. Generate unique GUIDs for each entity
2. Follow the CSV structure with proper parent-child relationships
3. Test the CSV file with small datasets first
4. Validate all references are correct

### 2. Updating Existing Data
1. Keep existing GUIDs to maintain references
2. Add new entities with new GUIDs
3. Update descriptions, icons, colors as needed
4. Maintain sort order consistency

### 3. Testing
1. Use known GUIDs for predictable test results
2. Verify all relationships are properly seeded
3. Check that fallback mechanism works when CSV is missing
4. Validate error handling with malformed data

## Error Scenarios and Handling

### 1. Invalid GUID Format
- **Error**: Malformed GUID strings
- **Handling**: Logged and skipped, processing continues
- **Resolution**: Fix GUID format in CSV

### 2. Missing Parent References
- **Error**: SubGroup references non-existent Group
- **Handling**: Logged warning, item skipped
- **Resolution**: Ensure parent entities exist in CSV

### 3. Insufficient Fields
- **Error**: CSV row has fewer than 10 fields
- **Handling**: Logged and skipped
- **Resolution**: Fix CSV structure

### 4. Duplicate GUIDs
- **Error**: Same GUID used multiple times
- **Handling**: Database constraint violation
- **Resolution**: Ensure all GUIDs are unique

## Files Created/Modified

### New Files
- `lookup_data_spreadsheet.csv` - GUID-based lookup data configuration
- `LOOKUP_DATA_GUID_SEEDING_IMPLEMENTATION.md` - This documentation

### Modified Files
- `Services/DatabaseSeeder.cs` - Enhanced with GUID-based CSV parsing and seeding

## Testing Recommendations

1. **Unit Tests**: Test CSV parsing with various data scenarios
2. **Integration Tests**: Verify complete seeding process with known GUIDs
3. **Error Handling Tests**: Test with malformed CSV data
4. **Fallback Tests**: Ensure fallback mechanism works when CSV is missing
5. **Performance Tests**: Validate seeding performance with large datasets

The implementation provides a robust, flexible system for managing lookup data with consistent GUID primary keys while maintaining backward compatibility and comprehensive error handling.

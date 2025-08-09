# Lookup Data JSON-Based Seeding Implementation

## Overview

The FarmScout database seeder has been enhanced to support JSON-based seeding for lookup data with a nested hierarchical structure. This replaces the flat CSV format with a more intuitive JSON structure where subgroups are nested within groups, and items are nested within subgroups, providing a natural representation of the data relationships.

## Changes Made

### 1. Nested JSON File Structure

#### New lookup_data_seeding.json Format
The JSON file contains lookup groups with nested subgroups and items:

```json
{
  "lookupGroups": [
    {
      "id": "750e8400-e29b-41d4-a716-446655440001",
      "name": "Diseases",
      "description": "Disease types and conditions",
      "icon": "🦠",
      "color": "#F44336",
      "sortOrder": 1,
      "subGroups": [
        {
          "id": "850e8400-e29b-41d4-a716-446655440001",
          "name": "Fungal",
          "icon": "🍄",
          "color": "#8D6E63",
          "sortOrder": 1,
          "items": [
            {
              "id": "950e8400-e29b-41d4-a716-446655440001",
              "name": "Powdery Mildew",
              "description": "White powdery coating on leaves",
              "icon": "🍄",
              "color": "#FFEB3B",
              "sortOrder": 1
            }
          ]
        }
      ]
    }
  ]
}
```

### 2. Enhanced JSON Parsing

#### New JSON Classes
```csharp
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
```

### 3. Updated Seeding Logic

#### Priority-Based Loading
The seeder now follows a priority-based approach:
1. **JSON First**: Attempts to load from `lookup_data_seeding.json`
2. **CSV Fallback**: Falls back to `lookup_data_spreadsheet.csv` if JSON not available
3. **Hardcoded Fallback**: Uses basic hardcoded groups if neither JSON nor CSV available

#### SeedLookupDataFromJsonAsync Method
- **Hierarchical Processing**: Processes groups, subgroups, and items in natural order
- **GUID Validation**: Validates all GUID fields before processing
- **Atomic Operations**: Each complete hierarchy is seeded together
- **Comprehensive Logging**: Detailed logging for each entity created

### 4. Key Benefits

#### Natural Hierarchy
- **Intuitive Structure**: Data relationships are immediately clear
- **Logical Nesting**: Items belong to subgroups, subgroups belong to groups
- **Single Source**: All related data in one cohesive structure

#### Improved Maintainability
- **Easier Updates**: Adding items to a subgroup is straightforward
- **Better Organization**: Related data is grouped together
- **Version Control**: Better diff visualization for hierarchical changes

#### Enhanced Processing
- **Atomic Operations**: Complete hierarchies processed together
- **Referential Integrity**: Parent-child relationships are implicit
- **Reduced Complexity**: No need to manage foreign key relationships manually

## JSON File Structure

### Root Object
```json
{
  "lookupGroups": [...]
}
```

### Lookup Group Object
```json
{
  "id": "GUID",
  "name": "string",
  "description": "string",
  "icon": "emoji",
  "color": "#hexcode",
  "sortOrder": number,
  "subGroups": [...]
}
```

### Lookup SubGroup Object
```json
{
  "id": "GUID",
  "name": "string",
  "icon": "emoji",
  "color": "#hexcode",
  "sortOrder": number,
  "items": [...]
}
```

### Lookup Item Object
```json
{
  "id": "GUID",
  "name": "string",
  "description": "string",
  "icon": "emoji",
  "color": "#hexcode",
  "sortOrder": number
}
```

## Data Structure Example

The JSON contains 4 main lookup groups:

1. **Diseases** (🦠)
   - **Fungal** (🍄): Powdery Mildew, Rust, Blight
   - **Bacterial** (🦠): Fire Blight, Crown Gall
   - **Viral** (🔬): Mosaic Virus

2. **Pests** (🐛)
   - **Insects** (🦗): Aphids, Caterpillars, Spider Mites, Thrips
   - **Mammals** (🐭): Rabbits, Deer
   - **Birds** (🐦): Crows

3. **Crop Types** (🌾)
   - **Grains** (🌾): Wheat, Corn, Barley, Oats
   - **Vegetables** (🥕): Tomatoes, Carrots, Lettuce, Potatoes
   - **Fruits** (🍎): Apples, Grapes, Berries

4. **Conditions** (🏥)
   - **Health** (💚): Excellent, Good, Fair
   - **Stress** (⚠️): Drought Stress, Nutrient Deficiency, Heat Stress

## Usage

### 1. Creating New Lookup Groups
1. Add new group object to the `lookupGroups` array
2. Include all required fields with unique GUID
3. Add `subGroups` array with nested subgroup objects
4. Ensure sort orders are appropriate

### 2. Adding Subgroups
1. Locate the parent group in the JSON
2. Add new subgroup object to the `subGroups` array
3. Include `items` array for nested items
4. Use unique GUID and appropriate sort order

### 3. Adding Items
1. Locate the parent subgroup in the JSON
2. Add new item object to the `items` array
3. Use unique GUID and appropriate sort order
4. Include description and visual properties

### 4. Updating Existing Data
1. Modify properties directly in the JSON file
2. Keep existing GUIDs to maintain data integrity
3. Update sort orders as needed
4. Test JSON validity before deployment

## File Locations

The seeder searches for `lookup_data_seeding.json` in:
1. Application base directory
2. `Resources/Raw` folder
3. App data directory
4. User's Documents folder
5. Current directory

## Error Handling

### JSON Parsing Errors
- **Invalid JSON**: Logged and falls back to CSV/hardcoded data
- **Missing File**: Logged and continues with fallback options
- **Schema Errors**: Individual items with errors are skipped

### GUID Validation
- **Invalid GUIDs**: Logged and item is skipped
- **Duplicate GUIDs**: Database constraint will prevent insertion
- **Missing GUIDs**: Item is skipped with warning

### Relationship Integrity
- **Orphaned Items**: Items without valid parent subgroups are skipped
- **Missing Parents**: Subgroups without valid parent groups are skipped
- **Processing Order**: Parents are always processed before children

## Migration from CSV

### Automatic Migration
The system maintains backward compatibility:
- Existing CSV files continue to work
- JSON takes priority when both are present
- No breaking changes to existing functionality

### Manual Migration Steps
1. Create `lookup_data_seeding.json` file
2. Structure groups with nested subgroups and items
3. Preserve all existing GUIDs
4. Validate JSON structure and relationships
5. Test with small dataset first

## Performance Considerations

### Advantages
- **Single File Read**: Reduces I/O operations
- **Batch Processing**: Complete hierarchies processed together
- **Atomic Operations**: All related data inserted in sequence
- **Memory Efficiency**: Structured data loaded once

### Memory Usage
- **Full Load**: Entire JSON structure loaded into memory
- **Processing Order**: Parents processed before children
- **Validation**: GUIDs validated before database operations

## Testing

### Validation Scenarios
1. **Valid JSON**: Complete hierarchical structure
2. **Invalid JSON**: Malformed JSON syntax
3. **Missing File**: No JSON file present
4. **Partial Hierarchy**: Groups without subgroups or items
5. **Invalid GUIDs**: Malformed GUID strings
6. **Mixed Sources**: JSON, CSV, and fallback combinations

### Data Integrity Tests
- **GUID Uniqueness**: All GUIDs are unique within their type
- **Relationship Validity**: All parent-child relationships are correct
- **Sort Order**: Items are properly ordered within their containers
- **Required Fields**: All mandatory fields are populated

## Future Enhancements

### Schema Validation
- JSON Schema validation for structure
- Required field enforcement
- Data type validation for all properties
- Relationship integrity checks

### Import/Export Tools
- Export current database to JSON format
- Import validation with detailed error reporting
- Bulk update capabilities with change tracking
- Data transformation utilities

### UI Integration
- Hierarchical JSON editor with tree view
- Real-time validation with error highlighting
- Preview changes before applying
- Backup and restore functionality

## Files Created/Modified

### New Files
- `lookup_data_seeding.json` - Hierarchical lookup data structure
- `LOOKUP_DATA_JSON_SEEDING_IMPLEMENTATION.md` - This documentation

### Modified Files
- `Services/DatabaseSeeder.cs` - Enhanced with JSON parsing and hierarchical processing

## Benefits Summary

1. **Hierarchical Structure**: Natural representation of data relationships
2. **Improved Maintainability**: Logical grouping and easier updates
3. **Better Performance**: Reduced I/O and atomic processing
4. **Enhanced Validation**: JSON schema validation capabilities
5. **Backward Compatibility**: Maintains support for existing CSV files
6. **Future-Proof**: Extensible structure for additional metadata

The JSON-based approach provides a more intuitive, maintainable, and efficient way to manage lookup data configuration while preserving all existing functionality and providing seamless migration paths. The hierarchical structure eliminates the complexity of managing flat CSV relationships and makes the data structure immediately understandable.

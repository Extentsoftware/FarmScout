# Markdown Reports Database

This document describes the database schema and functionality for storing markdown reports generated by backend systems in the FarmScout application.

## Overview

The markdown reports system allows you to store, retrieve, and manage markdown-formatted reports with embedded graphics. Each report contains a title, date produced, report group, and the complete markdown content.

## Database Schema

### Tables

#### `markdown_reports`
Stores individual markdown reports with the following fields:

| Field | Type | Description |
|-------|------|-------------|
| `id` | UUID | Primary key |
| `title` | VARCHAR(255) | Report title |
| `report_group` | VARCHAR(100) | Report category/group |
| `date_produced` | TIMESTAMP | When the report was generated |
| `report_markdown` | TEXT | Complete markdown content |
| `file_name` | VARCHAR(255) | Optional original filename |
| `file_size` | BIGINT | Size of markdown content in bytes |
| `is_active` | BOOLEAN | Whether the report is active |
| `created_at` | TIMESTAMP | Record creation time |
| `updated_at` | TIMESTAMP | Last update time |

#### `report_groups`
Lookup table for standardized report groups:

| Field | Type | Description |
|-------|------|-------------|
| `id` | UUID | Primary key |
| `name` | VARCHAR(100) | Group name (unique) |
| `description` | TEXT | Group description |
| `icon` | VARCHAR(10) | Emoji icon |
| `color` | VARCHAR(7) | Hex color code |
| `sort_order` | INTEGER | Display order |
| `is_active` | BOOLEAN | Whether the group is active |
| `created_at` | TIMESTAMP | Record creation time |
| `updated_at` | TIMESTAMP | Last update time |

## C# Models

### MarkdownReport
```csharp
[Table("markdown_reports")]
public class MarkdownReport
{
    [PrimaryKey]
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(255), NotNull]
    public string Title { get; set; } = string.Empty;

    [MaxLength(100), NotNull]
    public string ReportGroup { get; set; } = string.Empty;

    [NotNull]
    public DateTime DateProduced { get; set; } = DateTime.Now;

    [NotNull]
    public string ReportMarkdown { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? FileName { get; set; }

    public long? FileSize { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
```

### ReportGroup
```csharp
[Table("report_groups")]
public class ReportGroup
{
    [PrimaryKey]
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(100), Unique, NotNull]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [MaxLength(10)]
    public string Icon { get; set; } = "📊";

    [MaxLength(7)]
    public string Color { get; set; } = "#607D8B";

    public int SortOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
```

## Database Operations

### Interface Methods

The `IFarmScoutDatabase` interface includes these methods for markdown reports:

#### MarkdownReport CRUD
- `AddMarkdownReportAsync(MarkdownReport report)`
- `UpdateMarkdownReportAsync(MarkdownReport report)`
- `DeleteMarkdownReportAsync(MarkdownReport report)`
- `GetMarkdownReportsAsync()`
- `GetMarkdownReportsByGroupAsync(string reportGroup)`
- `GetMarkdownReportsAsync(int skip, int take)`
- `GetMarkdownReportsCountAsync()`
- `GetMarkdownReportByIdAsync(Guid id)`
- `SearchMarkdownReportsAsync(string searchTerm)`

#### ReportGroup CRUD
- `AddReportGroupAsync(ReportGroup group)`
- `UpdateReportGroupAsync(ReportGroup group)`
- `DeleteReportGroupAsync(ReportGroup group)`
- `GetReportGroupsAsync()`
- `GetReportGroupByIdAsync(Guid id)`
- `GetReportGroupByNameAsync(string name)`

## Usage Examples

### Basic Operations

```csharp
// Store a new report
var report = new MarkdownReport
{
    Title = "Monthly Soil Moisture Analysis",
    ReportGroup = "Moisture Reports",
    DateProduced = DateTime.Now,
    ReportMarkdown = "# Monthly Report\n\nContent here...",
    FileName = "monthly_moisture_report.md"
};

await database.AddMarkdownReportAsync(report);

// Retrieve reports by group
var moistureReports = await database.GetMarkdownReportsByGroupAsync("Moisture Reports");

// Search reports
var searchResults = await database.SearchMarkdownReportsAsync("moisture");

// Get paginated results
var (reports, totalCount) = await database.GetMarkdownReportsAsync(0, 10);
```

### Using the MarkdownReportService

```csharp
var reportService = new MarkdownReportService(database);

// Store a report
var report = await reportService.StoreReportAsync(
    "Weekly Health Check",
    "Section Health",
    markdownContent,
    "weekly_health.md"
);

// Store from file
var report = await reportService.StoreReportFromFileAsync(
    "Monthly Analysis",
    "Moisture Reports",
    "path/to/report.md"
);

// Get latest report for a group
var latestReport = await reportService.GetLatestReportByGroupAsync("Moisture Reports");

// Export to file
await reportService.ExportReportToFileAsync(reportId, "exported_report.md");

// Get statistics
var stats = await reportService.GetReportStatisticsAsync();
```

## Predefined Report Groups

The system comes with these predefined report groups:

1. **Moisture Reports** 💧 - Soil moisture analysis and monitoring
2. **Section Health** 🏥 - Overall section health and condition
3. **Harvest Reports** 🌾 - Harvest yield and quality analysis
4. **Weather Reports** 🌤️ - Weather condition and impact analysis
5. **Pest Reports** 🐛 - Pest infestation and control
6. **Disease Reports** 🦠 - Plant disease monitoring and treatment
7. **Soil Reports** 🌍 - Soil condition and nutrient analysis
8. **Growth Reports** 📈 - Plant growth and development tracking

## PostgreSQL Schema

For PostgreSQL deployments, use the `report_tables.sql` file which includes:

- Table definitions with proper constraints
- Indexes for performance
- Triggers for updated_at timestamps
- Sample data for report groups
- Views for common queries
- Functions for advanced operations

### Key PostgreSQL Features

```sql
-- Views for easy querying
CREATE VIEW active_reports_with_groups AS
SELECT 
    mr.id,
    mr.title,
    mr.report_group,
    rg.description as group_description,
    rg.icon as group_icon,
    rg.color as group_color,
    mr.date_produced,
    mr.file_name,
    mr.file_size
FROM markdown_reports mr
LEFT JOIN report_groups rg ON mr.report_group = rg.name
WHERE mr.is_active = true
ORDER BY mr.date_produced DESC;

-- Functions for pagination and search
CREATE OR REPLACE FUNCTION get_reports_by_group(
    group_name VARCHAR(100),
    limit_count INTEGER DEFAULT 10,
    offset_count INTEGER DEFAULT 0
) RETURNS TABLE (...);

CREATE OR REPLACE FUNCTION search_reports(
    search_term TEXT,
    limit_count INTEGER DEFAULT 20
) RETURNS TABLE (...);
```

## Integration with Report Generation

### From Python Scripts

```python
# After generating a markdown report with moisture_report.py
import sqlite3
import datetime

def store_report_in_database(title, report_group, markdown_content, db_path):
    conn = sqlite3.connect(db_path)
    cursor = conn.cursor()
    
    cursor.execute("""
        INSERT INTO markdown_reports 
        (id, title, report_group, date_produced, report_markdown, file_size, is_active, created_at, updated_at)
        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
    """, (
        str(uuid.uuid4()),
        title,
        report_group,
        datetime.datetime.now().isoformat(),
        markdown_content,
        len(markdown_content.encode('utf-8')),
        True,
        datetime.datetime.now().isoformat(),
        datetime.datetime.now().isoformat()
    ))
    
    conn.commit()
    conn.close()

# Usage
with open('soil_moisture_report.md', 'r') as f:
    content = f.read()
    store_report_in_database(
        "Soil Moisture Analysis - January 2024",
        "Moisture Reports",
        content,
        "farmscout.db3"
    )
```

### From Backend Systems

```csharp
// In your backend API
[HttpPost("reports")]
public async Task<IActionResult> StoreReport([FromBody] StoreReportRequest request)
{
    var report = new MarkdownReport
    {
        Title = request.Title,
        ReportGroup = request.ReportGroup,
        DateProduced = DateTime.Now,
        ReportMarkdown = request.MarkdownContent,
        FileName = request.FileName,
        FileSize = Encoding.UTF8.GetByteCount(request.MarkdownContent)
    };

    await _database.AddMarkdownReportAsync(report);
    
    return Ok(new { ReportId = report.Id });
}
```

## Best Practices

### Storage
- Use meaningful titles that describe the report content
- Group reports logically using predefined report groups
- Include file size for monitoring storage usage
- Set appropriate file names for easy identification

### Retrieval
- Use pagination for large report collections
- Implement search functionality for finding specific reports
- Cache frequently accessed reports
- Use report groups for organization

### Performance
- Index frequently queried fields
- Use pagination to limit result sets
- Consider archiving old reports
- Monitor database size growth

## Migration Notes

### From File-Based Storage
If migrating from file-based report storage:

1. Scan existing report files
2. Extract metadata (title, date, group)
3. Read markdown content
4. Insert into database using the provided methods
5. Verify data integrity
6. Update application code to use database instead of file system

### Database Schema Updates
When updating the schema:

1. Backup existing data
2. Run migration scripts
3. Update C# models if needed
4. Test with sample data
5. Deploy to production

## Troubleshooting

### Common Issues

**Large Report Files**
- Monitor file size limits
- Consider compression for very large reports
- Implement cleanup for old reports

**Search Performance**
- Ensure proper indexing
- Use full-text search for large content
- Consider external search engines for complex queries

**Database Growth**
- Implement archival strategy
- Monitor storage usage
- Consider partitioning for very large datasets

### Performance Optimization

```sql
-- Add indexes for better performance
CREATE INDEX idx_markdown_reports_date_produced ON markdown_reports(date_produced DESC);
CREATE INDEX idx_markdown_reports_report_group ON markdown_reports(report_group);
CREATE INDEX idx_markdown_reports_title ON markdown_reports(title);

-- For full-text search (PostgreSQL)
CREATE INDEX idx_markdown_reports_content_fts ON markdown_reports USING gin(to_tsvector('english', report_markdown));
```

## Security Considerations

- Validate markdown content before storage
- Implement access controls for sensitive reports
- Use parameterized queries to prevent injection
- Encrypt sensitive report content if needed
- Implement audit logging for report access 
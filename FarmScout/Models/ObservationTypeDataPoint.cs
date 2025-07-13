using SQLite;

namespace FarmScout.Models
{
    [Table("ObservationTypeDataPoints")]
    public class ObservationTypeDataPoint
    {
        [PrimaryKey]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Indexed]
        public Guid ObservationTypeId { get; set; }
        
        [MaxLength(50), NotNull]
        public string Code { get; set; } = string.Empty;
        
        [MaxLength(100), NotNull]
        public string Label { get; set; } = string.Empty;
        
        [MaxLength(20), NotNull]
        public string DataType { get; set; } = string.Empty; // "long", "string", "lookup"
        
        [MaxLength(100)]
        public string LookupGroupName { get; set; } = string.Empty; // Only used when DataType is "lookup"
        
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;
        
        public bool IsRequired { get; set; } = false;
        
        public bool IsActive { get; set; } = true;
        
        public int SortOrder { get; set; } = 0;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
    
    public static class DataTypes
    {
        public const string Long = "long";
        public const string String = "string";
        public const string Lookup = "lookup";
        
        public static readonly string[] AvailableTypes = [Long, String, Lookup];
        
        public static string GetDataTypeIcon(string dataType)
        {
            return dataType switch
            {
                Long => "🔢",
                String => "📝",
                Lookup => "📋",
                _ => "❓"
            };
        }
        
        public static string GetDataTypeColor(string dataType)
        {
            return dataType switch
            {
                Long => "#2196F3",    // Blue
                String => "#4CAF50",   // Green
                Lookup => "#FF9800",   // Orange
                _ => "#607D8B"         // Default
            };
        }
    }
} 
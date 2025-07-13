using SQLite;

namespace FarmScout.Models
{
    [Table("LookupGroups")]
    public class LookupGroup
    {
        [PrimaryKey]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [MaxLength(50), NotNull, Unique]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(10)]
        public string Icon { get; set; } = "📝";
        
        [MaxLength(7)]
        public string Color { get; set; } = "#607D8B";
        
        public int SortOrder { get; set; } = 0;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        
        public bool IsActive { get; set; } = true;
    }
} 
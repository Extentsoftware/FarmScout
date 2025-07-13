using SQLite;

namespace FarmScout.Models
{
    [Table("LookupSubGroups")]
    public class LookupSubGroup
    {
        [PrimaryKey]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [MaxLength(50), NotNull]
        public string Name { get; set; } = string.Empty;
        
        [NotNull]
        public Guid GroupId { get; set; }
        
        public int SortOrder { get; set; } = 0;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        
        public bool IsActive { get; set; } = true;
    }
} 
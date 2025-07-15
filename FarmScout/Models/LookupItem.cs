using SQLite;

namespace FarmScout.Models
{
    [Table("LookupItems")]
    public class LookupItem
    {
        [PrimaryKey]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [MaxLength(100), NotNull]
        public string Name { get; set; } = string.Empty;
        
        [NotNull]
        public Guid GroupId { get; set; }
        
        public Guid? SubGroupId { get; set; }
        
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        
        public bool IsActive { get; set; } = true;
    }
} 
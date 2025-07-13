using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace FarmScout.Models
{
    [Table("ObservationTypes")]
    public class ObservationType : ObservableObject
    {
        [PrimaryKey]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [MaxLength(100), NotNull]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;
        
        [MaxLength(10)]
        public string Icon { get; set; } = string.Empty;
        
        [MaxLength(7)]
        public string Color { get; set; } = "#607D8B";
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        
        public int SortOrder { get; set; } = 0;
    }
} 
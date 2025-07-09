using SQLite;
using System;

namespace FarmScout.Models
{
    [Table("ObservationMetadata")]
    public class ObservationMetadata
    {
        [PrimaryKey]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Indexed]
        public Guid ObservationId { get; set; }
        
        [Indexed]
        public Guid ObservationTypeId { get; set; }
        
        [Indexed]
        public Guid DataPointId { get; set; }
        
        [MaxLength(1000)]
        public string Value { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
} 
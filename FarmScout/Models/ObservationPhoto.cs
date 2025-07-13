using SQLite;

namespace FarmScout.Models
{
    public class ObservationPhoto
    {
        [PrimaryKey]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Indexed]
        public Guid ObservationId { get; set; }
        
        public string PhotoPath { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        public DateTime Timestamp { get; set; }
    }
} 
using SQLite;

namespace FarmScout.Models
{
    public class ObservationLocation
    {
        [PrimaryKey]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Indexed]
        public Guid ObservationId { get; set; }
        
        public double Latitude { get; set; }
        
        public double Longitude { get; set; }
        
        public string Description { get; set; } = string.Empty;
        
        public DateTime Timestamp { get; set; }
    }
} 
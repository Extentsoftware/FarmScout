using SQLite;
using System;

namespace FarmScout.Models
{
    public class ObservationLocation
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        
        [Indexed]
        public int ObservationId { get; set; }
        
        public double Latitude { get; set; }
        
        public double Longitude { get; set; }
        
        public string Description { get; set; } = string.Empty;
        
        public DateTime Timestamp { get; set; }
    }
} 
using SQLite;
using System;

namespace FarmScout.Models
{
    public class ObservationPhoto
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        
        [Indexed]
        public int ObservationId { get; set; }
        
        public string PhotoPath { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        public DateTime Timestamp { get; set; }
    }
} 
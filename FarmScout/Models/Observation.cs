using SQLite;
using System;
using System.Collections.Generic;

namespace FarmScout.Models
{
    public class Observation
    {
        [PrimaryKey]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public string PhotoPath { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime Timestamp { get; set; }
        public string Notes { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public Guid? FarmLocationId { get; set; }
        
        // Metadata will be linked by ObservationId in ObservationMetadata
        // Tasks will be linked by ObservationId in TaskItem
        // Photos will be linked by ObservationId in ObservationPhoto
        // Locations will be linked by ObservationId in ObservationLocation
    }
} 
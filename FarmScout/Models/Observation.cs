using SQLite;
using System;
using System.Collections.Generic;

namespace FarmScout.Models
{
    public class Observation
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Disease { get; set; } = string.Empty;
        public double SoilMoisture { get; set; }
        public string PhotoPath { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime Timestamp { get; set; }
        public string ObservationTypes { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public int? FarmLocationId { get; set; }
        
        // Additional optional metrics
        public string DiseaseName { get; set; } = string.Empty;
        public string PestName { get; set; } = string.Empty;
        public int? PlantCount { get; set; }
        public int? PestCount { get; set; }
        public double? AffectedAreaPercentage { get; set; }
        public double? DamageLevel { get; set; }
        public string DamageType { get; set; } = string.Empty;
        public string GrowthStage { get; set; } = string.Empty;
        public double? HeightCm { get; set; }
        public double? WeightKg { get; set; }
        public string CropType { get; set; } = string.Empty;
        public double? TemperatureCelsius { get; set; }
        public double? HumidityPercentage { get; set; }
        public double? WindSpeed { get; set; }
        public double? Precipitation { get; set; }
        public double? PhLevel { get; set; }
        public double? NutrientLevel { get; set; }
        public string Symptoms { get; set; } = string.Empty;
        public string Cause { get; set; } = string.Empty;
        public string Quality { get; set; } = string.Empty;
        public double? HealthScore { get; set; }
        public double? YieldPerArea { get; set; }
        public string InfestationArea { get; set; } = string.Empty;
        
        // Tasks will be linked by ObservationId in TaskItem
    }
} 
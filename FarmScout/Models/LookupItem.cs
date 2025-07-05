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
        
        [MaxLength(50), NotNull]
        public string Group { get; set; } = string.Empty;
        
        [MaxLength(50)]
        public string SubGroup { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        
        public bool IsActive { get; set; } = true;
    }

    public static class LookupGroups
    {
        public static readonly string[] AvailableGroups = 
        [
            "Crop Types",
            "Diseases",
            "Pests",
            "Chemicals",
            "Fertilizers",
            "Soil Types",
            "Weather Conditions",
            "Growth Stages",
            "Damage Types",
            "Treatment Methods"
        ];

        public static string[] GetSubGroupsForGroup(string group)
        {
            return group switch
            {
                "Chemicals" => ["Herbicide", "Fungicide", "Insecticide", "Fertilizer", "Growth Regulator"],
                "Diseases" => ["Fungal", "Bacterial", "Viral", "Nematode", "Other"],
                "Pests" => ["Insects", "Mites", "Nematodes", "Birds", "Mammals"],
                "Fertilizers" => ["Nitrogen", "Phosphorus", "Potassium", "Micronutrients", "Organic"],
                "Soil Types" => ["Mineral", "Organic", "Mixed"],
                "Weather Conditions" => ["Temperature", "Precipitation", "Wind", "Humidity", "Pressure"],
                "Growth Stages" => ["Vegetative", "Reproductive", "Maturity"],
                "Damage Types" => ["Environmental", "Biological", "Mechanical", "Chemical"],
                "Treatment Methods" => ["Chemical", "Biological", "Cultural", "Mechanical", "Integrated"],
                _ => []
            };
        }

        public static string GetGroupIcon(string group)
        {
            return group switch
            {
                "Crop Types" => "🌾",
                "Diseases" => "🦠",
                "Pests" => "🐛",
                "Chemicals" => "🧪",
                "Fertilizers" => "🌱",
                "Soil Types" => "🌍",
                "Weather Conditions" => "🌤️",
                "Growth Stages" => "📈",
                "Damage Types" => "💥",
                "Treatment Methods" => "💊",
                _ => "📝"
            };
        }

        public static string GetGroupColor(string group)
        {
            return group switch
            {
                "Crop Types" => "#4CAF50",      // Green
                "Diseases" => "#F44336",         // Red
                "Pests" => "#FF9800",            // Orange
                "Chemicals" => "#9C27B0",        // Purple
                "Fertilizers" => "#8BC34A",      // Light Green
                "Soil Types" => "#8D6E63",       // Brown
                "Weather Conditions" => "#2196F3", // Blue
                "Growth Stages" => "#00BCD4",    // Cyan
                "Damage Types" => "#795548",     // Dark Brown
                "Treatment Methods" => "#607D8B", // Blue Gray
                _ => "#607D8B"                   // Default
            };
        }
    }
} 
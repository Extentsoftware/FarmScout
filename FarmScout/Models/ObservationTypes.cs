namespace FarmScout.Models;

public static class ObservationTypes
{
    public static readonly string[] AvailableTypes = 
    {
        "Disease",
        "Dead Plant", 
        "Pest",
        "Damage",
        "Growth",
        "Harvest",
        "Weather",
        "Soil",
        "Soil Moisture"
    };

    public static string GetTypeIcon(string observationType)
    {
        return observationType switch
        {
            "Disease" => "🦠",
            "Dead Plant" => "💀",
            "Pest" => "🐛",
            "Damage" => "💥",
            "Growth" => "🌱",
            "Harvest" => "🌾",
            "Weather" => "🌤️",
            "Soil" => "🌍",
            "Soil Moisture" => "��",
            _ => "📝"
        };
    }

    public static string GetTypeColor(string observationType)
    {
        return observationType switch
        {
            "Disease" => "#F44336",    // Red
            "Dead Plant" => "#9E9E9E",  // Gray
            "Pest" => "#FF9800",        // Orange
            "Damage" => "#795548",      // Brown
            "Growth" => "#4CAF50",      // Green
            "Harvest" => "#FFC107",     // Amber
            "Weather" => "#2196F3",     // Blue
            "Soil" => "#8D6E63",        // Brown
            "Soil Moisture" => "#00BCD4", // Cyan
            _ => "#607D8B"              // Blue Gray
        };
    }

    public static List<string> GetMetricsForType(string observationType)
    {
        return observationType switch
        {
            "Disease" => ["Disease Name", "Affected Area %", "Plant Count", "Symptoms"],
            "Dead Plant" => ["Plant Count", "Area Affected", "Cause"],
            "Pest" => ["Pest Name", "Pest Count", "Damage Level", "Infestation Area"],
            "Damage" => ["Damage Type", "Severity", "Area Affected", "Cause"],
            "Growth" => ["Growth Stage", "Height (cm)", "Plant Count", "Health Score"],
            "Harvest" => ["Crop Type", "Weight (kg)", "Quality", "Yield per Area"],
            "Weather" => ["Temperature (°C)", "Humidity (%)", "Wind Speed", "Precipitation"],
            "Soil" => ["pH Level", "Moisture %", "Temperature (°C)", "Nutrient Level"],
            "Soil Moisture" => ["Moisture Level", "Area Affected", "Last Measured"],
            _ => []
        };
    }

    public static string JoinTypes(IEnumerable<string> types)
    {
        return string.Join(",", types);
    }

    public static List<string> SplitTypes(string typesString)
    {
        if (string.IsNullOrWhiteSpace(typesString))
            return [];
        
        return [.. typesString.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim())];
    }
}

public static class SeverityLevels
{
    public static readonly string[] AvailableSeverities = 
    {
        "Information",
        "Warning", 
        "Fail"
    };

    public static string GetSeverityColor(string severity)
    {
        return severity switch
        {
            "Information" => "#2196F3", // Blue
            "Warning" => "#FF9800",      // Orange
            "Fail" => "#F44336",         // Red
            _ => "#2196F3"               // Default blue
        };
    }

    public static string GetSeverityIcon(string severity)
    {
        return severity switch
        {
            "Information" => "ℹ️",
            "Warning" => "⚠️", 
            "Fail" => "❌",
            _ => "ℹ️"
        };
    }
} 
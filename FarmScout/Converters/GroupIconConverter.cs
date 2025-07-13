using System.Globalization;

namespace FarmScout.Converters
{
    public class GroupIconConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string groupName)
            {
                // For now, return a default icon since we need to access the database
                // In a real implementation, you might want to pass the group object instead
                return groupName switch
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
            return "📝";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 
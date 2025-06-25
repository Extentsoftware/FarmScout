using System.Globalization;

namespace FarmScout.Converters;

public class BoolToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue && parameter is string stringParameter)
        {
            var strings = stringParameter.Split('|');
            if (strings.Length == 2)
            {
                var trueString = strings[0].Trim();
                var falseString = strings[1].Trim();
                
                return boolValue ? trueString : falseString;
            }
        }
        
        // Default fallback
        return value?.ToString() ?? string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
} 
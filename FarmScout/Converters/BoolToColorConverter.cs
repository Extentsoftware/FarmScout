using System.Globalization;
using Microsoft.Maui.Graphics;

namespace FarmScout.Converters;

public class BoolToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue && parameter is string colorString)
        {
            var colors = colorString.Split('|');
            if (colors.Length == 2)
            {
                var trueColor = colors[0].Trim();
                var falseColor = colors[1].Trim();
                
                return boolValue ? Color.FromArgb(trueColor) : Color.FromArgb(falseColor);
            }
        }
        
        // Default fallback
        return Colors.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
} 
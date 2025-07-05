using System.Globalization;

namespace FarmScout.Converters
{
    public class StringToBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                if (parameter is string colorParam && targetType == typeof(Color))
                {
                    var colors = colorParam.Split('|');
                    if (colors.Length >= 2)
                    {
                        var hasValue = !string.IsNullOrWhiteSpace(str);
                        return hasValue ? Color.FromArgb(colors[0]) : Color.FromArgb(colors[1]);
                    }
                }
                return !string.IsNullOrWhiteSpace(str);
            }
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 
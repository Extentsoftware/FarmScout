using System.Globalization;

namespace FarmScout.Converters;

public class StringContainsConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is System.Collections.ObjectModel.ObservableCollection<string> collection && parameter is string searchTerm)
        {
            return collection.Contains(searchTerm);
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
} 
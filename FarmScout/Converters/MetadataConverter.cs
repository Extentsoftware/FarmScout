using System.Globalization;

namespace FarmScout.Converters;

public class MetadataConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Dictionary<Guid, Dictionary<Guid, object>> metadataByType && parameter is Guid observationTypeId)
        {
            if (metadataByType.TryGetValue(observationTypeId, out var metadata))
            {
                return metadata;
            }
        }
        return new Dictionary<Guid, object>();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
} 
using System.Globalization;

namespace FarmScout.Converters
{
    public class GroupIconConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string groupName)
            {
                return FarmScout.Models.LookupGroups.GetGroupIcon(groupName);
            }
            return "📝";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 
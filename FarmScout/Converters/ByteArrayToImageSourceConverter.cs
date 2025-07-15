using System.Globalization;

namespace FarmScout.Converters
{
    public class ByteArrayToImageSourceConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is byte[] imageBytes && imageBytes.Length > 0)
            {
                try
                {
                    return ImageSource.FromStream(() => new MemoryStream(imageBytes));
                }
                catch (Exception ex)
                {
                    App.Log($"Error converting byte array to image: {ex.Message}");
                    return null;
                }
            }
            
            return null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 
namespace FarmScout.Services
{
    public class LocationService
    {
        public async Task<(double Latitude, double Longitude)?> GetCurrentLocationAsync()
        {
            try
            {
                var location = await Geolocation.Default.GetLocationAsync();
                if (location != null)
                {
                    return (location.Latitude, location.Longitude);
                }
            }
            catch
            {
                // Handle exceptions (permissions, etc.)
            }
            return null;
        }
    }
} 
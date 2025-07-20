using FarmScout.Services;

namespace FarmScout.Tests
{
    public class TestCsvSeeding
    {
        public static async Task TestCsvSeedingAsync()
        {
            try
            {
                App.Log("Starting CSV seeding test...");

                // Create database instance
                var database = new FarmScoutDatabase();
                
                // Wait for database to be ready
                while (!database.IsReady)
                {
                    await Task.Delay(100);
                }

                App.Log("Database is ready, checking observations...");

                // Check if observations were seeded
                var observations = await database.GetObservationsAsync();
                App.Log($"Found {observations.Count} observations in database");

                if (observations.Count > 0)
                {
                    App.Log("CSV seeding appears to be working!");
                    App.Log($"Sample observation: {observations[0].Summary} - {observations[0].Timestamp}");
                }
                else
                {
                    App.Log("No observations found - CSV seeding may not have worked");
                }

                // Check farm locations
                var farmLocations = await database.GetFarmLocationsAsync();
                App.Log($"Found {farmLocations.Count} farm locations in database");

                if (farmLocations.Count > 0)
                {
                    App.Log("Farm locations were created successfully!");
                    foreach (var location in farmLocations.Take(5))
                    {
                        App.Log($"  - {location.Name}: {location.Description}");
                    }
                }

                App.Log("CSV seeding test completed");
            }
            catch (Exception ex)
            {
                App.Log($"Error during CSV seeding test: {ex.Message}");
                App.Log($"Exception details: {ex}");
            }
        }
    }
} 
# Farm Scout - MAUI Mobile App

A comprehensive mobile application for farm scouting observations, built with .NET MAUI for Android.

## Features

### 🌱 Core Functionality
- **Record Observations**: Capture disease/issue descriptions and soil moisture levels
- **Camera Integration**: Take photos of affected areas
- **GPS Location**: Automatically record the location of each observation
- **Task Management**: Create and track tasks resulting from observations
- **Offline Support**: All data is stored locally using SQLite database
- **Dashboard**: Overview of farm activities and recent observations
- **Observations**: Record and track farm observations with photos and location data
- **Tasks**: Manage farm tasks and track completion status
- **Lookup Tables**: Comprehensive reference system for farm-related data

### 📱 User Interface
- **Dashboard**: Overview of observations and tasks with quick action buttons
- **Add Observation**: Comprehensive form with camera and GPS integration
- **Observations List**: View all recorded observations with photos and details
- **Tasks List**: Manage tasks with completion status tracking
- **Observation Details**: Full view of individual observations with associated tasks

### 🔧 Technical Features
- **Local Database**: SQLite for offline data storage
- **Camera Access**: Built-in camera integration for photo capture
- **GPS Services**: Location services for precise field mapping
- **Modern UI**: Clean, intuitive interface with material design principles
- **Cross-Platform**: Built with .NET MAUI for Android compatibility

## Getting Started

### Prerequisites
- .NET 9.0 SDK
- Visual Studio 2022 or Visual Studio Code
- Android SDK (for Android deployment)
- Android device or emulator

### Installation

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd FarmScout
   ```

2. **Install MAUI workloads**
   ```bash
   dotnet workload install maui-android
   ```

3. **Restore dependencies**
   ```bash
   dotnet restore FarmScout/FarmScout.csproj
   ```

### Running the App

#### On Windows (for testing)
```bash
dotnet build FarmScout/FarmScout.csproj -t:Run -f net9.0-windows10.0.19041.0
```

#### On Android
1. Connect an Android device or start an emulator
2. Run the app:
   ```bash
   dotnet build FarmScout/FarmScout.csproj -t:Run -f net9.0-android
   ```

### Building for Release
```bash
dotnet build FarmScout/FarmScout.csproj -c Release -f net9.0-android
```

## Project Structure

```
FarmScout/
├── Models/
│   ├── Observation.cs          # Data model for farm observations
│   └── TaskItem.cs             # Data model for tasks
├── Services/
│   ├── FarmScoutDatabase.cs    # SQLite database operations
│   ├── PhotoService.cs         # Camera/photo capture service
│   └── LocationService.cs      # GPS location service
├── Views/
│   ├── DashboardPage.xaml      # Main dashboard
│   ├── ObservationsPage.xaml   # List all observations
│   ├── TasksPage.xaml          # Manage tasks
├── Platforms/
│   └── Android/
│       └── AndroidManifest.xml # Android permissions
└── MauiProgram.cs              # App configuration and DI
```

## Permissions

The app requires the following Android permissions:
- `CAMERA`: For taking photos of observations
- `ACCESS_FINE_LOCATION`: For precise GPS location
- `ACCESS_COARSE_LOCATION`: For approximate location
- `INTERNET`: For future sync functionality
- `ACCESS_NETWORK_STATE`: For network status checking

## Database Schema

### Observation Table
- `Id` (Primary Key, Auto Increment)
- `Disease` (Text): Description of disease or issue
- `SoilMoisture` (Real): Soil moisture percentage
- `PhotoPath` (Text): Path to captured photo
- `Latitude` (Real): GPS latitude
- `Longitude` (Real): GPS longitude
- `Timestamp` (DateTime): When observation was recorded

### TaskItem Table
- `Id` (Primary Key, Auto Increment)
- `ObservationId` (Integer): Foreign key to Observation
- `Description` (Text): Task description
- `IsCompleted` (Boolean): Task completion status

### Lookup Tables System

The lookup tables feature provides a centralized reference system for various farm-related data categories:

#### Supported Categories
- **Crop Types**: Corn, Soybeans, Wheat, Cotton, Rice, etc.
- **Diseases**: Rust, Blight, Mildew, Root Rot, Leaf Spot, etc.
- **Pests**: Aphids, Corn Borer, Spider Mites, Cutworms, etc.
- **Chemicals**: Glyphosate, Atrazine, 2,4-D, Paraquat, Dicamba, etc.
- **Fertilizers**: Urea, Ammonium Nitrate, Triple Superphosphate, etc.
- **Soil Types**: Clay, Silt, Sandy, Loam, Peat, etc.
- **Weather Conditions**: Sunny, Cloudy, Rainy, Windy, Foggy, etc.
- **Growth Stages**: Germination, Vegetative, Flowering, Fruiting, Maturity
- **Damage Types**: Hail Damage, Wind Damage, Drought Stress, etc.
- **Treatment Methods**: Chemical Treatment, Biological Control, etc.

#### Features
- **Add/Edit Items**: Users can add new items or edit existing ones
- **Search & Filter**: Search by name or description, filter by category
- **Data Validation**: Prevents duplicate entries within the same category
- **Soft Delete**: Items are marked as inactive rather than permanently deleted
- **Initial Data**: Comes pre-populated with common farm-related items

#### Usage
1. Navigate to "📚 Lookup Tables" from the main menu
2. Use the search bar to find specific items
3. Use the group filter to view items by category
4. Click "Add New Item" to create new entries
5. Use Edit/Delete buttons to modify existing items

## Usage Guide

### Adding an Observation
1. Open the app and tap "➕ Add New Observation"
2. Enter the disease or issue description
3. Adjust soil moisture using the slider
4. Tap "📷 Take Photo" to capture an image
5. Tap "📍 Get Current Location" to record GPS coordinates
6. Add any tasks that need to be completed
7. Tap "💾 Save Observation"

### Viewing Observations
1. From the dashboard, tap "📋 View Observations"
2. Browse through all recorded observations
3. Tap the eye icon to view full details
4. Tap the X icon to delete an observation

### Managing Tasks
1. From the dashboard, tap "✅ View Tasks"
2. Check/uncheck tasks to mark them complete
3. Tap the X icon to delete tasks
4. Tasks show which observation they're associated with

## Future Enhancements

- **Cloud Sync**: Upload observations to cloud storage when online
- **Offline Maps**: Download field maps for offline use
- **Data Export**: Export observations to CSV or PDF
- **Push Notifications**: Reminders for incomplete tasks
- **Field Boundaries**: Draw and save field boundaries
- **Weather Integration**: Include weather data with observations

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly
5. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For issues and questions, please create an issue in the repository or contact the development team. 
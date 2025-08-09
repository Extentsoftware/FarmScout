using FarmScout.Models;

namespace FarmScout.Services
{
    public interface IFarmScoutDatabase
    {
        bool IsReady { get; set; }
        Task InitializeDatabaseAsync();

        // Observation CRUD
        Task<int> AddObservationAsync(Observation obs);
        Task<List<Observation>> GetObservationsAsync();
        Task<List<Observation>> GetObservationsAsync(int skip, int take);
        Task<List<Observation>> GetObservationsAsync(int skip, int take, FilterParameters filterParams);
        Task<int> GetObservationsCountAsync();
        Task<int> GetObservationsCountAsync(FilterParameters filterParams);
        Task<int> UpdateObservationAsync(Observation obs);
        Task<int> DeleteObservationAsync(Observation obs);

        // TaskItem CRUD
        Task<int> AddTaskAsync(TaskItem task);
        Task<List<TaskItem>> GetTasksForObservationAsync(Guid observationId);
        Task<int> UpdateTaskAsync(TaskItem task);
        Task<int> DeleteTaskAsync(TaskItem task);

        // ObservationPhoto CRUD
        Task<int> AddPhotoAsync(ObservationPhoto photo);
        Task<List<ObservationPhoto>> GetPhotosForObservationAsync(Guid observationId);
        Task<int> UpdatePhotoAsync(ObservationPhoto photo);
        Task<int> DeletePhotoAsync(ObservationPhoto photo);

        // ObservationLocation CRUD
        Task<int> AddLocationAsync(ObservationLocation location);
        Task<List<ObservationLocation>> GetLocationsForObservationAsync(Guid observationId);
        Task<int> UpdateLocationAsync(ObservationLocation location);
        Task<int> DeleteLocationAsync(ObservationLocation location);

        // FarmLocation CRUD
        Task<int> AddFarmLocationAsync(FarmLocation farmLocation);
        Task<List<FarmLocation>> GetFarmLocationsAsync();
        Task<FarmLocation?> GetFarmLocationByIdAsync(Guid id);
        Task<FarmLocation?> GetFarmLocationByNameAsync(string name);
        Task<int> UpdateFarmLocationAsync(FarmLocation farmLocation);
        Task<int> DeleteFarmLocationAsync(FarmLocation farmLocation);

        // LookupItem CRUD
        Task<int> AddLookupItemAsync(LookupItem item);
        Task<List<LookupItem>> GetLookupItemsAsync();
        Task<List<LookupItem>> GetLookupItemsByGroupAsync(string groupName);
        Task<int> UpdateLookupItemAsync(LookupItem item);
        Task<int> DeleteLookupItemAsync(LookupItem item);
        Task<bool> LookupItemExistsAsync(string name, string groupName, Guid? excludeId = null);

        // LookupGroup CRUD
        Task<int> AddLookupGroupAsync(LookupGroup group);
        Task<int> UpdateLookupGroupAsync(LookupGroup group);
        Task<int> DeleteLookupGroupAsync(LookupGroup group);
        Task<List<LookupGroup>> GetLookupGroupsAsync();
        Task<LookupGroup?> GetLookupGroupByIdAsync(Guid id);
        Task<LookupGroup?> GetLookupGroupByNameAsync(string name);

        // LookupSubGroup CRUD
        Task<int> AddLookupSubGroupAsync(LookupSubGroup subGroup);
        Task<int> UpdateLookupSubGroupAsync(LookupSubGroup subGroup);
        Task<int> DeleteLookupSubGroupAsync(LookupSubGroup subGroup);
        Task<List<LookupSubGroup>> GetLookupSubGroupsAsync(Guid groupId);
        Task<List<string>> GetLookupSubGroupNamesAsync(Guid groupId);

        // ObservationType CRUD
        Task<int> AddObservationTypeAsync(ObservationType observationType);
        Task<int> UpdateObservationTypeAsync(ObservationType observationType);
        Task<int> DeleteObservationTypeAsync(ObservationType observationType);
        Task<List<ObservationType>> GetObservationTypesAsync();
        Task<ObservationType?> GetObservationTypeByIdAsync(Guid id);
        Task<ObservationType?> GetObservationTypeByNameAsync(string name);

        // ObservationTypeDataPoint CRUD
        Task<int> AddObservationTypeDataPointAsync(ObservationTypeDataPoint dataPoint);
        Task<int> UpdateObservationTypeDataPointAsync(ObservationTypeDataPoint dataPoint);
        Task<int> DeleteObservationTypeDataPointAsync(ObservationTypeDataPoint dataPoint);
        Task<List<ObservationTypeDataPoint>> GetDataPointsForObservationTypeAsync(Guid observationTypeId);
        Task<ObservationTypeDataPoint?> GetDataPointByIdAsync(Guid id);

        // ObservationMetadata CRUD
        Task<int> AddObservationMetadataAsync(ObservationMetadata metadata);
        Task<int> UpdateObservationMetadataAsync(ObservationMetadata metadata);
        Task<int> DeleteObservationMetadataAsync(ObservationMetadata metadata);
        Task<List<ObservationMetadata>> GetMetadataForObservationAsync(Guid observationId);
        Task<List<ObservationMetadata>> GetMetadataForObservationAndTypeAsync(Guid observationId, Guid observationTypeId);

        // MarkdownReport CRUD
        Task<int> AddMarkdownReportAsync(MarkdownReport report);
        Task<int> UpdateMarkdownReportAsync(MarkdownReport report);
        Task<int> DeleteMarkdownReportAsync(MarkdownReport report);
        Task<List<MarkdownReport>> GetMarkdownReportsAsync();
        Task<List<MarkdownReport>> GetMarkdownReportsByGroupAsync(Guid reportGroupId);
        Task<List<MarkdownReport>> GetMarkdownReportsAsync(int skip, int take);
        Task<int> GetMarkdownReportsCountAsync();
        Task<MarkdownReport?> GetMarkdownReportByIdAsync(Guid id);
        Task<List<MarkdownReport>> SearchMarkdownReportsAsync(string searchTerm);

        // ReportGroup CRUD
        Task<int> AddReportGroupAsync(ReportGroup group);
        Task<int> UpdateReportGroupAsync(ReportGroup group);
        Task<int> DeleteReportGroupAsync(ReportGroup group);
        Task<List<ReportGroup>> GetReportGroupsAsync();
        Task<ReportGroup?> GetReportGroupByIdAsync(Guid id);
        Task<ReportGroup?> GetReportGroupByNameAsync(string name);

        // Database Reset
        Task<bool> ResetDatabaseAsync();
        Task<bool> ResetDatabaseWithSeedingAsync();
        Task<DatabaseResetInfo> GetDatabaseInfoAsync();
    }
}
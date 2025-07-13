using FarmScout.Models;

namespace FarmScout.Services
{
    public interface IFarmScoutDatabase
    {
        bool IsReady { get; set; }
        Task InitializeDatabaseAsync();

        // Existing methods
        Task<int> AddLocationAsync(ObservationLocation location);
        Task<int> AddLookupItemAsync(LookupItem item);
        Task<int> AddObservationAsync(Observation obs);
        Task<int> AddPhotoAsync(ObservationPhoto photo);
        Task<int> AddTaskAsync(TaskItem task);
        Task<int> DeleteLocationAsync(ObservationLocation location);
        Task<int> DeleteLookupItemAsync(LookupItem item);
        Task<int> DeleteObservationAsync(Observation obs);
        Task<int> DeletePhotoAsync(ObservationPhoto photo);
        Task<int> DeleteTaskAsync(TaskItem task);
        Task<List<ObservationLocation>> GetLocationsForObservationAsync(Guid observationId);
        // Lookup Groups and SubGroups
        Task<int> AddLookupGroupAsync(LookupGroup group);
        Task<int> UpdateLookupGroupAsync(LookupGroup group);
        Task<int> DeleteLookupGroupAsync(LookupGroup group);
        Task<List<LookupGroup>> GetLookupGroupsAsync();
        Task<LookupGroup?> GetLookupGroupByIdAsync(Guid id);
        Task<LookupGroup?> GetLookupGroupByNameAsync(string name);
        
        Task<int> AddLookupSubGroupAsync(LookupSubGroup subGroup);
        Task<int> UpdateLookupSubGroupAsync(LookupSubGroup subGroup);
        Task<int> DeleteLookupSubGroupAsync(LookupSubGroup subGroup);
        Task<List<LookupSubGroup>> GetLookupSubGroupsAsync(Guid groupId);
        Task<List<string>> GetLookupSubGroupNamesAsync(Guid groupId);
        Task<List<LookupItem>> GetLookupItemsAsync();
        Task<List<LookupItem>> GetLookupItemsByGroupAsync(string group);
        Task<List<Observation>> GetObservationsAsync();
        Task<List<Observation>> GetObservationsAsync(int skip, int take);
        Task<int> GetObservationsCountAsync();
        Task<List<ObservationPhoto>> GetPhotosForObservationAsync(Guid observationId);
        Task<List<TaskItem>> GetTasksForObservationAsync(Guid observationId);
        Task<bool> LookupItemExistsAsync(string name, string group, Guid? excludeId = null);
        Task<int> UpdateLocationAsync(ObservationLocation location);
        Task<int> UpdateLookupItemAsync(LookupItem item);
        Task<int> UpdateObservationAsync(Observation obs);
        Task<int> UpdatePhotoAsync(ObservationPhoto photo);
        Task<int> UpdateTaskAsync(TaskItem task);
        
        // New metadata system methods
        Task<int> AddObservationTypeAsync(ObservationType observationType);
        Task<int> AddObservationTypeDataPointAsync(ObservationTypeDataPoint dataPoint);
        Task<int> AddObservationMetadataAsync(ObservationMetadata metadata);
        Task<int> DeleteObservationTypeAsync(ObservationType observationType);
        Task<int> DeleteObservationTypeDataPointAsync(ObservationTypeDataPoint dataPoint);
        Task<int> DeleteObservationMetadataAsync(ObservationMetadata metadata);
        Task<List<ObservationType>> GetObservationTypesAsync();
        Task<ObservationType?> GetObservationTypeByIdAsync(Guid id);
        Task<ObservationType?> GetObservationTypeByNameAsync(string name);
        Task<List<ObservationTypeDataPoint>> GetDataPointsForObservationTypeAsync(Guid observationTypeId);
        Task<ObservationTypeDataPoint?> GetDataPointByIdAsync(Guid id);
        Task<List<ObservationMetadata>> GetMetadataForObservationAsync(Guid observationId);
        Task<List<ObservationMetadata>> GetMetadataForObservationAndTypeAsync(Guid observationId, Guid observationTypeId);
        Task<int> UpdateObservationTypeAsync(ObservationType observationType);
        Task<int> UpdateObservationTypeDataPointAsync(ObservationTypeDataPoint dataPoint);
        Task<int> UpdateObservationMetadataAsync(ObservationMetadata metadata);
    }
}
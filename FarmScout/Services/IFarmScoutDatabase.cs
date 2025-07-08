using FarmScout.Models;

namespace FarmScout.Services
{
    public interface IFarmScoutDatabase
    {
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
        Task<List<string>> GetLookupGroupsAsync();
        Task<List<LookupItem>> GetLookupItemsAsync();
        Task<List<LookupItem>> GetLookupItemsByGroupAsync(string group);
        Task<List<Observation>> GetObservationsAsync();
        Task<List<ObservationPhoto>> GetPhotosForObservationAsync(Guid observationId);
        Task<List<TaskItem>> GetTasksForObservationAsync(Guid observationId);
        Task<bool> LookupItemExistsAsync(string name, string group, Guid? excludeId = null);
        Task<int> UpdateLocationAsync(ObservationLocation location);
        Task<int> UpdateLookupItemAsync(LookupItem item);
        Task<int> UpdateObservationAsync(Observation obs);
        Task<int> UpdatePhotoAsync(ObservationPhoto photo);
        Task<int> UpdateTaskAsync(TaskItem task);
    }
}
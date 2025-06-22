using System.Collections.ObjectModel;
using System.Windows.Input;
using FarmScout.Models;
using FarmScout.Services;

namespace FarmScout.ViewModels;

public class ObservationDetailViewModel : BaseViewModel
{
    private readonly FarmScoutDatabase _database;
    private readonly INavigationService _navigationService;

    public ObservationDetailViewModel(FarmScoutDatabase database, INavigationService navigationService)
    {
        _database = database;
        _navigationService = navigationService;
        Title = "Observation Details";
        
        EditObservationCommand = new Command(async () => await EditObservation());
    }

    public ICommand EditObservationCommand { get; }

    public Observation? Observation { get; set; }
    public string ObservationTypesText 
    {
        get
        {
            if (Observation == null) return "No type specified";
            var types = ObservationTypes.SplitTypes(Observation.ObservationTypes);
            return types.Count > 0 ? string.Join(", ", types) : "No type specified";
        }
    }
    public string SeverityText 
    {
        get
        {
            if (Observation == null) return "";
            return $"{SeverityLevels.GetSeverityIcon(Observation.Severity)} {Observation.Severity}";
        }
    }
    public string SeverityColor 
    {
        get
        {
            if (Observation == null) return "#2196F3";
            return SeverityLevels.GetSeverityColor(Observation.Severity);
        }
    }
    public string LocationText => "Multiple locations available"; // Will be updated when we load locations
    public string PhotoText => "Multiple photos available"; // Will be updated when we load photos
    public bool HasPhoto => false; // Will be updated when we load photos
    public bool NoPhoto => true; // Will be updated when we load photos

    // Additional properties for the new UI
    public string SoilMoistureText 
    {
        get
        {
            if (Observation == null) return "N/A";
            return $"Soil: {Observation.SoilMoisture:F0}%";
        }
    }
    
    public string TimestampText 
    {
        get
        {
            if (Observation == null) return "N/A";
            return Observation.Timestamp.ToString("MMM dd, yyyy HH:mm");
        }
    }
    
    public string NotesText 
    {
        get
        {
            if (Observation == null || string.IsNullOrEmpty(Observation.Notes)) return "No notes";
            return Observation.Notes;
        }
    }

    public async Task LoadObservationAsync(int observationId)
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            var observations = await _database.GetObservationsAsync();
            Observation = observations.FirstOrDefault(o => o.Id == observationId);
            
            if (Observation != null)
            {
                OnPropertyChanged(nameof(ObservationTypesText));
                OnPropertyChanged(nameof(SeverityText));
                OnPropertyChanged(nameof(SeverityColor));
                OnPropertyChanged(nameof(LocationText));
                OnPropertyChanged(nameof(PhotoText));
                OnPropertyChanged(nameof(HasPhoto));
                OnPropertyChanged(nameof(NoPhoto));
                OnPropertyChanged(nameof(SoilMoistureText));
                OnPropertyChanged(nameof(TimestampText));
                OnPropertyChanged(nameof(NotesText));
            }
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlert("Error", "Failed to load observation details", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task EditObservation()
    {
        if (Observation != null)
        {
            var parameters = new Dictionary<string, object>
            {
                { "ObservationId", Observation.Id }
            };
            await _navigationService.NavigateToAsync("EditObservation", parameters);
        }
    }
}

public class TaskDetailViewModel
{
    public TaskItem TaskItem { get; }
    public TextDecorations TextDecoration => TaskItem.IsCompleted ? TextDecorations.Strikethrough : TextDecorations.None;

    public TaskDetailViewModel(TaskItem taskItem)
    {
        TaskItem = taskItem;
    }
} 
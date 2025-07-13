using CommunityToolkit.Mvvm.ComponentModel;
using FarmScout.Models;

namespace FarmScout.ViewModels;

public partial class ObservationTypeViewModel : ObservableObject
{
    [ObservableProperty]
    public partial ObservationType ObservationType { get; set; }

    [ObservableProperty]
    public partial Dictionary<Guid, object> Metadata { get; set; } = [];

    public ObservationTypeViewModel(ObservationType observationType)
    {
        ObservationType = observationType;
    }

    public Guid Id => ObservationType.Id;
    public string Name => ObservationType.Name;
    public string Icon => ObservationType.Icon;
    public string Description => ObservationType.Description;

    public void AddMetadata(Guid dataPointId, object value)
    {
        Metadata[dataPointId] = value;
        OnPropertyChanged(nameof(Metadata));
    }

    public void RemoveMetadata(Guid dataPointId)
    {
        Metadata.Remove(dataPointId);
        OnPropertyChanged(nameof(Metadata));
    }

    public void ClearMetadata()
    {
        Metadata.Clear();
        OnPropertyChanged(nameof(Metadata));
    }

    public void SetMetadata(Dictionary<Guid, object> metadata)
    {
        Metadata.Clear();
        foreach (var kvp in metadata)
        {
            Metadata[kvp.Key] = kvp.Value;
        }
        OnPropertyChanged(nameof(Metadata));
    }
} 
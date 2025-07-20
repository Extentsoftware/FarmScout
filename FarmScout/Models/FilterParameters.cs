namespace FarmScout.Models
{
    public class FilterParameters
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? SearchText { get; set; }
        public Guid? FieldId { get; set; }
        public Guid? ObservationTypeId { get; set; }
        public string SortBy { get; set; } = "Timestamp";
        public bool SortAscending { get; set; } = false;
    }
} 
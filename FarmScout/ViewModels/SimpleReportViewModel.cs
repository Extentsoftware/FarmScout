using FarmScout.Models;

namespace FarmScout.ViewModels;

public class SimpleReportViewModel
{
    public MarkdownReport Report { get; }
    public ReportGroup? Group { get; }

    public SimpleReportViewModel(MarkdownReport report, ReportGroup? group = null)
    {
        Report = report;
        Group = group;
    }

    public string Title => Report.Title;
    public string ReportGroup => Group?.Name ?? "Unknown Group";
    public DateTime DateProduced => Report.DateProduced;
    public string DateText => DateProduced.ToString("MMM dd, yyyy HH:mm");
    public string GroupIcon => GetGroupIcon(Group?.Name ?? "");

    private string GetGroupIcon(string reportGroup)
    {
        return reportGroup.ToLower() switch
        {
            "moisture reports" => "💧",
            "scout reports" => "🏥",
            "harvest reports" => "🌾",
            "warehouse reports" => "🌤️",
            "vehicle reports" => "🚗",
            _ => "📄"
        };
    }
} 
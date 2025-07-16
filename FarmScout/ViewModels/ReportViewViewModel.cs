using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmScout.Models;
using FarmScout.Services;
using System.Text.RegularExpressions;

namespace FarmScout.ViewModels;

public partial class ReportViewViewModel(INavigationService navigationService) : ObservableObject
{
    [ObservableProperty]
    private string reportTitle = string.Empty;

    [ObservableProperty]
    private DateTime reportDate = DateTime.Now;

    [ObservableProperty]
    private string formattedContent = string.Empty;

    [ObservableProperty]
    private MarkdownReport? report;

    [RelayCommand]
    private async Task Close()
    {
        await navigationService.GoBackAsync();
    }

    public void LoadReport(MarkdownReport markdownReport)
    {
        Report = markdownReport;
        ReportTitle = markdownReport.Title;
        ReportDate = markdownReport.DateProduced;
        FormattedContent = ConvertMarkdownToHtml(markdownReport.ReportMarkdown);
    }

    private string ConvertMarkdownToHtml(string markdown)
    {
        if (string.IsNullOrEmpty(markdown))
            return "<p>No content available</p>";

        var html = markdown;

        // Convert headers
        html = Regex.Replace(html, @"^### (.*$)", "<h3 style='color: #2E7D32; margin-top: 20px; margin-bottom: 10px;'>$1</h3>", RegexOptions.Multiline);
        html = Regex.Replace(html, @"^## (.*$)", "<h2 style='color: #2E7D32; margin-top: 25px; margin-bottom: 15px; border-bottom: 2px solid #E8F5E8; padding-bottom: 5px;'>$1</h2>", RegexOptions.Multiline);
        html = Regex.Replace(html, @"^# (.*$)", "<h1 style='color: #2E7D32; margin-top: 30px; margin-bottom: 20px; font-size: 24px;'>$1</h1>", RegexOptions.Multiline);

        // Convert bold text
        html = Regex.Replace(html, @"\*\*(.*?)\*\*", "<strong style='color: #1976D2;'>$1</strong>");

        // Convert italic text
        html = Regex.Replace(html, @"\*(.*?)\*", "<em>$1</em>");

        // Convert bullet points
        html = Regex.Replace(html, @"^- (.*$)", "<li style='margin: 5px 0;'>$1</li>", RegexOptions.Multiline);
        html = Regex.Replace(html, @"^(\s*)- (.*$)", "<li style='margin: 5px 0; padding-left: 20px;'>$2</li>", RegexOptions.Multiline);

        // Wrap lists
        html = Regex.Replace(html, @"(<li.*?</li>)", "<ul style='margin: 10px 0; padding-left: 20px;'>$1</ul>", RegexOptions.Singleline);

        // Convert line breaks to paragraphs
        html = Regex.Replace(html, @"\n\n", "</p><p style='margin: 10px 0; line-height: 1.6;'>");
        html = "<p style='margin: 10px 0; line-height: 1.6;'>" + html + "</p>";

        // Clean up empty paragraphs
        html = Regex.Replace(html, @"<p[^>]*>\s*</p>", "");

        // Convert emojis and special characters
        html = html.Replace("🟢", "<span style='color: #4CAF50; font-size: 16px;'>🟢</span>")
                   .Replace("🟡", "<span style='color: #FF9800; font-size: 16px;'>🟡</span>")
                   .Replace("🔴", "<span style='color: #F44336; font-size: 16px;'>🔴</span>")
                   .Replace("📊", "<span style='font-size: 16px;'>📊</span>")
                   .Replace("📈", "<span style='font-size: 16px;'>📈</span>")
                   .Replace("🎯", "<span style='font-size: 16px;'>🎯</span>");

        return html;
    }
} 
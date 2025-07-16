using FarmScout.Models;
using System.Text;

namespace FarmScout.Services
{
    public class MarkdownReportService
    {
        private readonly IFarmScoutDatabase _database;

        public MarkdownReportService(IFarmScoutDatabase database)
        {
            _database = database;
        }

        /// <summary>
        /// Stores a markdown report in the database
        /// </summary>
        public async Task<MarkdownReport> StoreReportAsync(string title, Guid reportGroupId, string markdownContent, string? fileName = null)
        {
            var report = new MarkdownReport
            {
                Title = title,
                ReportGroupId = reportGroupId,
                DateProduced = DateTime.Now,
                ReportMarkdown = markdownContent,
                FileName = fileName,
                FileSize = Encoding.UTF8.GetByteCount(markdownContent)
            };

            await _database.AddMarkdownReportAsync(report);
            return report;
        }

        /// <summary>
        /// Stores a markdown report from a file
        /// </summary>
        public async Task<MarkdownReport> StoreReportFromFileAsync(string title, Guid reportGroupId, string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Report file not found: {filePath}");

            var markdownContent = await File.ReadAllTextAsync(filePath);
            var fileName = Path.GetFileName(filePath);

            return await StoreReportAsync(title, reportGroupId, markdownContent, fileName);
        }

        /// <summary>
        /// Retrieves all reports for a specific group
        /// </summary>
        public async Task<List<MarkdownReport>> GetReportsByGroupAsync(Guid reportGroupId)
        {
            return await _database.GetMarkdownReportsByGroupAsync(reportGroupId);
        }

        /// <summary>
        /// Retrieves the latest report for a specific group
        /// </summary>
        public async Task<MarkdownReport?> GetLatestReportByGroupAsync(Guid reportGroupId)
        {
            var reports = await _database.GetMarkdownReportsByGroupAsync(reportGroupId);
            return reports.FirstOrDefault();
        }

        /// <summary>
        /// Searches reports by title or content
        /// </summary>
        public async Task<List<MarkdownReport>> SearchReportsAsync(string searchTerm)
        {
            return await _database.SearchMarkdownReportsAsync(searchTerm);
        }

        /// <summary>
        /// Gets paginated reports
        /// </summary>
        public async Task<(List<MarkdownReport> Reports, int TotalCount)> GetReportsPaginatedAsync(int page, int pageSize)
        {
            var skip = (page - 1) * pageSize;
            var reports = await _database.GetMarkdownReportsAsync(skip, pageSize);
            var totalCount = await _database.GetMarkdownReportsCountAsync();
            
            return (reports, totalCount);
        }

        /// <summary>
        /// Exports a report to a file
        /// </summary>
        public async Task ExportReportToFileAsync(Guid reportId, string outputPath)
        {
            var report = await _database.GetMarkdownReportByIdAsync(reportId);
            if (report == null)
                throw new ArgumentException($"Report with ID {reportId} not found");

            await File.WriteAllTextAsync(outputPath, report.ReportMarkdown);
        }

        /// <summary>
        /// Gets report statistics
        /// </summary>
        public async Task<ReportStatistics> GetReportStatisticsAsync()
        {
            var allReports = await _database.GetMarkdownReportsAsync();
            var groups = await _database.GetReportGroupsAsync();

            // Create a lookup for group names
            var groupLookup = groups.ToDictionary(g => g.Id, g => g.Name);

            var stats = new ReportStatistics
            {
                TotalReports = allReports.Count,
                TotalGroups = groups.Count,
                ReportsByGroup = allReports.GroupBy(r => groupLookup.GetValueOrDefault(r.ReportGroupId, "Unknown"))
                    .ToDictionary(g => g.Key, g => g.Count()),
                LatestReportDate = allReports.Max(r => r.DateProduced),
                TotalFileSize = allReports.Sum(r => r.FileSize ?? 0)
            };

            return stats;
        }

        /// <summary>
        /// Creates a sample moisture report and stores it
        /// </summary>
        public async Task<MarkdownReport> CreateSampleMoistureReportAsync()
        {
            var moistureGroup = await _database.GetReportGroupByNameAsync("Moisture Reports");
            if (moistureGroup == null)
                throw new InvalidOperationException("Moisture Reports group not found");

            var markdownContent = @"# Sample Soil Moisture Report

**Generated:** 2024-01-15 14:30:25  
**Data Range:** 2024-01-01 to 2024-01-15  
**Total Observations:** 45  

## 📊 Summary

This is a sample moisture report demonstrating the markdown report storage functionality.

## 📈 Key Findings

- **Good Condition:** 6 sections (75.0%)
- **Fair Condition:** 1 sections (12.5%)
- **Poor Condition:** 1 sections (12.5%)

## 🎯 Recommendations

🟢 **Maintain Standards:**
- Continue current management practices for good sections
- Use as benchmarks for improvement in other sections

---

*This is a sample report for testing purposes.*";

            return await StoreReportAsync(
                "Sample Soil Moisture Report", 
                moistureGroup.Id, 
                markdownContent,
                "sample_moisture_report.md"
            );
        }

        /// <summary>
        /// Creates sample reports for testing the dashboard functionality
        /// </summary>
        public async Task<List<MarkdownReport>> CreateSampleReportsAsync()
        {
            var reports = new List<MarkdownReport>();

            // Get all report groups from the database
            var reportGroups = await _database.GetReportGroupsAsync();
            var groupLookup = reportGroups.ToDictionary(g => g.Name, g => g);

            // Verify all required groups exist
            var requiredGroups = new[] { "Moisture Reports", "Scout Reports", "Harvest Reports", "Warehouse Reports", "Vehicle Reports" };
            foreach (var groupName in requiredGroups)
            {
                if (!groupLookup.ContainsKey(groupName))
                    throw new InvalidOperationException($"Required report group '{groupName}' not found");
            }

            // Sample 1: Moisture Reports
            var moistureReport = await StoreReportAsync(
                "Soil Moisture Condition Report",
                groupLookup["Moisture Reports"].Id,
                @"# Soil Moisture Condition Report

**Report Generated:** 2025-07-15 19:35:04  
**Data Range:** 2024-11-20 to 2025-07-02  
**Total Moisture Observations:** 58  

## 📊 Comprehensive Moisture Analysis Dashboard

This report provides a comprehensive analysis of soil moisture conditions across all monitored sections.

## 📈 Key Findings

- **Good Condition:** 6 sections (75.0%)
- **Fair Condition:** 1 sections (12.5%)
- **Poor Condition:** 1 sections (12.5%)

## 🎯 Recommendations

🟢 **Maintain Standards:**
- Continue current management practices for good sections
- Use as benchmarks for improvement in other sections

🟡 **Monitor Closely:**
- Section 3 shows declining moisture levels
- Implement additional monitoring for at-risk areas

🔴 **Immediate Action Required:**
- Section 7 requires immediate irrigation
- Review drainage systems in affected areas

---

*Report generated automatically by FarmScout system.*",
                "soil_moisture_report.md"
            );
            reports.Add(moistureReport);

            // Sample 2: Scout Reports
            var scoutReport = await StoreReportAsync(
                "Section Health Assessment Report",
                groupLookup["Scout Reports"].Id,
                @"# Section Health Assessment Report

**Report Generated:** 2025-07-14 15:20:30  
**Assessment Period:** 2025-07-01 to 2025-07-14  
**Sections Analyzed:** 12  

## 🏥 Overall Health Status

The section health assessment shows generally good conditions with some areas requiring attention.

## 📊 Health Metrics

- **Excellent Health:** 8 sections (66.7%)
- **Good Health:** 3 sections (25.0%)
- **Fair Health:** 1 section (8.3%)

## 🔍 Key Observations

- **Pest Pressure:** Low across most sections
- **Disease Incidence:** Minimal, primarily in Section 5
- **Nutrient Status:** Adequate levels maintained

## 🎯 Action Items

- Monitor Section 5 for disease progression
- Schedule foliar application for Section 8
- Continue current pest management program

---

*Health assessment completed by field team.*",
                "section_health_report.md"
            );
            reports.Add(scoutReport);

            // Sample 3: Harvest Reports
            var harvestReport = await StoreReportAsync(
                "Harvest Yield Analysis Report",
                groupLookup["Harvest Reports"].Id,
                @"# Harvest Yield Analysis Report

**Report Generated:** 2025-07-13 10:45:15  
**Harvest Period:** 2025-08-15 to 2025-09-30  
**Data Sources:** Historical + Current Conditions  

## 🌾 Yield Projections

Based on current conditions and historical data, projected yields are:

- **Section 1-4:** 95-105% of historical average
- **Section 5-8:** 85-95% of historical average
- **Section 9-12:** 90-100% of historical average

## 🎯 Factors Influencing Yield

**Positive Factors:**
- Adequate soil moisture in most sections
- Good pest management program
- Favorable weather conditions

**Risk Factors:**
- Potential drought conditions in Sections 5-6
- Late season disease pressure possible

## 📊 Recommendations

1. **Irrigation Management:** Focus on Sections 5-6
2. **Fertilizer Application:** Maintain current program
3. **Harvest Planning:** Prepare for staggered harvest

---

*Predictions based on AI analysis and field data.*",
                "harvest_yield_report.md"
            );
            reports.Add(harvestReport);

            // Sample 4: Warehouse Reports
            var warehouseReport = await StoreReportAsync(
                "Warehouse Stock Status Report",
                groupLookup["Warehouse Reports"].Id,
                @"# Warehouse Stock Status Report

**Report Generated:** 2025-07-12 08:30:00  
**Reporting Period:** 2025-07-01 to 2025-07-12  
**Warehouse Locations:** 3  

## 📦 Stock Overview

**Current Inventory Levels:**
- **Fertilizers:** 85% of capacity
- **Pesticides:** 72% of capacity
- **Seeds:** 45% of capacity
- **Equipment Parts:** 90% of capacity

## 📊 Key Metrics

**Stock Turnover:**
- **High Turnover:** Fertilizers, Pesticides
- **Medium Turnover:** Seeds
- **Low Turnover:** Equipment Parts

## 🎯 Recommendations

1. **Replenish Seeds:** Order additional seed stock for next season
2. **Monitor Parts:** Continue current parts inventory levels
3. **Optimize Storage:** Reorganize warehouse layout for efficiency

---

*Inventory data from warehouse management system.*",
                "warehouse_stock_report.md"
            );
            reports.Add(warehouseReport);

            // Sample 5: Vehicle Reports
            var vehicleReport = await StoreReportAsync(
                "Vehicle Fleet Status Report",
                groupLookup["Vehicle Reports"].Id,
                @"# Vehicle Fleet Status Report

**Report Generated:** 2025-07-11 16:15:45  
**Fleet Count:** 15 vehicles  
**Reporting Period:** 2025-07-01 to 2025-07-11  

## 🚗 Fleet Status Overview

**Operational Status:**
- **Fully Operational:** 12 vehicles (80%)
- **Minor Issues:** 2 vehicles (13%)
- **Maintenance Required:** 1 vehicle (7%)

## ⛽ Fuel Consumption

**Average Fuel Efficiency:**
- **Tractors:** 8.5 MPG
- **Trucks:** 12.3 MPG
- **Utility Vehicles:** 18.7 MPG

## 🔧 Maintenance Schedule

**Completed This Period:**
- Tractor #3: Oil change and filter replacement
- Truck #1: Brake inspection
- Utility Vehicle #2: Tire rotation

**Scheduled Maintenance:**
- Tractor #1: Engine tune-up (due next week)
- Truck #3: Transmission service (due in 2 weeks)

## 🎯 Recommendations

1. **Preventive Maintenance:** Continue scheduled maintenance
2. **Fuel Monitoring:** Implement fuel tracking system
3. **Driver Training:** Schedule refresher training for new drivers

---

*Fleet data from vehicle management system.*",
                "vehicle_fleet_report.md"
            );
            reports.Add(vehicleReport);

            return reports;
        }
    }

    public class ReportStatistics
    {
        public int TotalReports { get; set; }
        public int TotalGroups { get; set; }
        public Dictionary<string, int> ReportsByGroup { get; set; } = new();
        public DateTime LatestReportDate { get; set; }
        public long TotalFileSize { get; set; }
    }
} 
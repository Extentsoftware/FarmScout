using System;

namespace FarmScout.Models
{
    public class DatabaseResetInfo
    {
        public string DatabasePath { get; set; } = string.Empty;
        public long DatabaseSizeBytes { get; set; }
        public DateTime LastModified { get; set; }
        public int ObservationCount { get; set; }
        public int TaskCount { get; set; }
        public int PhotoCount { get; set; }
        public int LocationCount { get; set; }
        public int FarmLocationCount { get; set; }
        public int LookupGroupCount { get; set; }
        public int LookupSubGroupCount { get; set; }
        public int LookupItemCount { get; set; }
        public int ObservationTypeCount { get; set; }
        public int ObservationTypeDataPointCount { get; set; }
        public int ObservationMetadataCount { get; set; }
        public int ReportGroupCount { get; set; }
        public int MarkdownReportCount { get; set; }
        public int TotalRecordCount { get; set; }
        public bool IsReady { get; set; }

        public string DatabaseSizeFormatted
        {
            get
            {
                if (DatabaseSizeBytes < 1024)
                    return $"{DatabaseSizeBytes} B";
                else if (DatabaseSizeBytes < 1024 * 1024)
                    return $"{DatabaseSizeBytes / 1024.0:F1} KB";
                else
                    return $"{DatabaseSizeBytes / (1024.0 * 1024.0):F1} MB";
            }
        }

        public string LastModifiedFormatted => LastModified.ToString("yyyy-MM-dd HH:mm:ss");
    }
} 
using System;
using SQLite;

namespace FarmScout.Models
{
    [Table("markdown_reports")]
    public class MarkdownReport
    {
        [PrimaryKey]
        public Guid Id { get; set; } = Guid.NewGuid();

        [MaxLength(255), NotNull]
        public string Title { get; set; } = string.Empty;

        [NotNull]
        public Guid ReportGroupId { get; set; }

        [NotNull]
        public DateTime DateProduced { get; set; } = DateTime.Now;

        [NotNull]
        public string ReportMarkdown { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? FileName { get; set; }

        public long? FileSize { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

    }

    [Table("report_groups")]
    public class ReportGroup
    {
        [PrimaryKey]
        public Guid Id { get; set; } = Guid.NewGuid();

        [MaxLength(100), Unique, NotNull]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [MaxLength(10)]
        public string Icon { get; set; } = "📊";

        [MaxLength(7)]
        public string Color { get; set; } = "#607D8B";

        public int SortOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
} 
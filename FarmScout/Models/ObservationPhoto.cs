using SQLite;

namespace FarmScout.Models
{
    public class ObservationPhoto
    {
        [PrimaryKey]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Indexed]
        public Guid ObservationId { get; set; }
        
        /// <summary>
        /// The actual photo data stored as bytes in the database
        /// </summary>
        [MaxLength(10485760)] // 10MB max size
        public byte[] PhotoData { get; set; } = Array.Empty<byte>();
        
        /// <summary>
        /// MIME type of the image (e.g., "image/jpeg", "image/png")
        /// </summary>
        [MaxLength(50)]
        public string MimeType { get; set; } = "image/jpeg";
        
        /// <summary>
        /// Original filename (for reference)
        /// </summary>
        [MaxLength(255)]
        public string OriginalFileName { get; set; } = string.Empty;
        
        /// <summary>
        /// File size in bytes
        /// </summary>
        public long FileSize { get; set; }
        
        /// <summary>
        /// Image width in pixels
        /// </summary>
        public int Width { get; set; }
        
        /// <summary>
        /// Image height in pixels
        /// </summary>
        public int Height { get; set; }
        
        /// <summary>
        /// User-provided description of the photo
        /// </summary>
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// When the photo was taken/created
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// When the photo was added to the database
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        /// <summary>
        /// When the photo was last modified
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        
        /// <summary>
        /// Whether the photo is active/deleted
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// Gets the file extension based on MIME type
        /// </summary>
        public string FileExtension => MimeType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/gif" => ".gif",
            "image/webp" => ".webp",
            "image/bmp" => ".bmp",
            _ => ".jpg"
        };
        
        /// <summary>
        /// Gets a display-friendly file size
        /// </summary>
        public string FileSizeDisplay
        {
            get
            {
                if (FileSize < 1024) return $"{FileSize} B";
                if (FileSize < 1024 * 1024) return $"{FileSize / 1024.0:F1} KB";
                return $"{FileSize / (1024.0 * 1024.0):F1} MB";
            }
        }
        
        /// <summary>
        /// Gets a display-friendly image dimensions
        /// </summary>
        public string DimensionsDisplay => $"{Width} × {Height}";
    }
} 
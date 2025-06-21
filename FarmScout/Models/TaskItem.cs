using SQLite;
using System;

namespace FarmScout.Models
{
    public class TaskItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int ObservationId { get; set; } // Foreign key
        public string Description { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public string Status => IsCompleted ? "Completed" : "Pending";
    }
} 
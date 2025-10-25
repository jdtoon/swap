using System.ComponentModel.DataAnnotations;

namespace habits.Data.Models
{
    public class CalendarEvent
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = null!;

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public DateTime? ReminderDateTime { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; } = null!;

        public int CalendarEventTypeId { get; set; }

        public CalendarEventType EventType { get; set; } = null!;

        public string? CreatedBy { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public TimeSpan? StartTime { get; set; }

        public TimeSpan? EndTime { get; set; }

        public bool NotificationSent { get; set; } = false;
    }
}

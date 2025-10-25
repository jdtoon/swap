using System.ComponentModel.DataAnnotations;

namespace habits.Data.Models
{
    public class CalendarEventType
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Color { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string IconPath { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

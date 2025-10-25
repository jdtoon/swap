using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace habits.Data.Models
{
    public class TaskList
    {
        public TaskList()
        {
            TaskListItems = [];
        }

        [Key]
        public int Id { get; set; }

        [MaxLength(200)]
        [Required]
        public string Name { get; set; } = null!;
        
        [MaxLength(1000)]
        public string? Description { get; set; } = null!;
        
        public int Order { get; set; } = 0;

        public required DateTime CreatedDateUTC { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDateUTC { get; set; }

        public required AppUser CreatedBy { get; set; }
        public required AppUser UpdatedBy { get; set; }

        public ICollection<TaskListItem> TaskListItems { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace habits.Data.Models
{
    public class TaskListItem
    {
        public TaskListItem()
        {
            AssignedUsers = new List<TaskUser>();
        }

        [Key]
        public int Id { get; set; }

        [MaxLength(1000)]
        [Required]
        public string Task { get; set; } = null!;

        public bool IsHeader { get; set; } = false;

        public bool IsCompleted { get; set; } = false;

        public int Order { get; set; }

        public TaskList TaskList { get; set; } = null!;

        public required DateTime CreatedDateUTC { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDateUTC { get; set; }

        public required AppUser CreatedBy { get; set; }
        public required AppUser UpdatedBy { get; set; }

        public ICollection<TaskUser> AssignedUsers { get; set; }
    }
}

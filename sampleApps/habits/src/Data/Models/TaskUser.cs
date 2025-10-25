using System.ComponentModel.DataAnnotations;

namespace habits.Data.Models
{
    public class TaskUser
    {
        [Key]
        public int Id { get; set; }

        public required TaskListItem Task { get; set; }
        public required AppUser User { get; set; }
    }
}

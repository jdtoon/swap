using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using habits.Data.Models;

namespace habits.Data
{
    public class ApplicationDbContext : IdentityDbContext<AppUser, AppRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public required DbSet<CalendarEvent> CalendarEvent { get; set; }
        public required DbSet<CalendarEventType> CalendarEventTypes { get; set; }
        public required DbSet<Document> Document { get; set; }
        public required DbSet<MealPlan> MealPlan { get; set; }
        public required DbSet<TaskList> TaskList { get; set; }
        public required DbSet<TaskListItem> TaskListItem { get; set; }
        public required DbSet<TaskUser> TaskUser { get; set; }
        public DbSet<LoyaltyCard> LoyaltyCards { get; set; }
    }
}

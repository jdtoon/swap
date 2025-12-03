using Microsoft.EntityFrameworkCore;
//#if (IncludeSampleModule)
using SwapModularMonolith.Modules.Notes.Entities;
//#endif

namespace SwapModularMonolith.Data.Seeding;

public static class MasterDataSeeder
{
    public static void Seed(ModelBuilder modelBuilder)
    {
//#if (IncludeSampleModule)
        SeedNotes(modelBuilder);
//#endif
    }

//#if (IncludeSampleModule)
    private static void SeedNotes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Note>().HasData(
            new Note
            {
                Id = 1,
                Title = "Welcome to SwapModularMonolith!",
                Content = "This is your first note. Edit or delete it to get started.",
                Color = "blue",
                CreatedAt = DateTime.UtcNow
            },
            new Note
            {
                Id = 2,
                Title = "About Swap.Htmx",
                Content = "Swap.Htmx is a server-driven UI library for ASP.NET Core. It enables reactive UIs without writing JavaScript.",
                Color = "green",
                CreatedAt = DateTime.UtcNow
            }
        );
    }
//#endif
}

using Microsoft.EntityFrameworkCore;
using SwapSmallPartials.Modules.Notes.Entities;

namespace SwapSmallPartials.Data.Seeding;

public static class MasterDataSeeder
{
    public static void Seed(ModelBuilder modelBuilder)
    {
        SeedNotes(modelBuilder);
    }

    private static void SeedNotes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Note>().HasData(
            new Note
            {
                Id = 1,
                Title = "Welcome to SwapSmallPartials!",
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
}

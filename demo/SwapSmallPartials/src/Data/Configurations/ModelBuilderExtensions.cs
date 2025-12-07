using Microsoft.EntityFrameworkCore;
using SwapSmallPartials.Modules.Notes.Entities;

namespace SwapSmallPartials.Data.Configurations;

public static class ModelBuilderExtensions
{
    public static ModelBuilder ApplyAppConfigurations(this ModelBuilder modelBuilder)
    {
        ConfigureNote(modelBuilder);

        return modelBuilder;
    }

    private static void ConfigureNote(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Note>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Content).HasMaxLength(4000);
            entity.Property(e => e.Color).HasMaxLength(20);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ModularMonolithDemo.Modules.Todos.Module.Domain;

namespace ModularMonolithDemo.Modules.Todos.Module.Persistence.Configurations;

public class TodoItemConfiguration : IEntityTypeConfiguration<TodoItem>
{
    public void Configure(EntityTypeBuilder<TodoItem> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(256);

        // For providers without schema (SQLite), prefix table name. Schema will be set at model level when supported by provider.
        builder.ToTable(TodosDbContext.TablePrefix + "todo_items");
    }
}

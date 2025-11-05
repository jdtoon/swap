using Microsoft.EntityFrameworkCore;
using ModularMonolithDemo.Modules.Todos.Module.Domain;

namespace ModularMonolithDemo.Modules.Todos.Module.Persistence;

public class TodosDbContext : DbContext
{
    public const string DefaultSchema = "todos";
    public const string TablePrefix = "todos_"; // used for providers without schemas (SQLite)

    public TodosDbContext(DbContextOptions<TodosDbContext> options) : base(options)
    {
    }

    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TodosDbContext).Assembly);
    }
}

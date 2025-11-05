using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ModularMonolithDemo.Modules.Todos.Module.Persistence;

namespace Todos.Migrations.Postgres;

public class DesignTimeTodosDbContextFactory : IDesignTimeDbContextFactory<TodosDbContext>
{
    public TodosDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TodosDbContext>();

        // Placeholder: requires Npgsql.EntityFrameworkCore.PostgreSQL package
        // To enable, install the provider and replace the UseSqlite line with:
        // optionsBuilder.UseNpgsql(conn, b => b.MigrationsAssembly(typeof(DesignTimeTodosDbContextFactory).Assembly.FullName));

        var conn = Environment.GetEnvironmentVariable("TODOS_POSTGRES_CONNECTION")
                   ?? "Host=localhost;Port=5432;Database=todosdb;Username=postgres;Password=postgres";

        // Fallback keeps project compilable until provider is added
        optionsBuilder.UseSqlite("Data Source=todos.db");

        return new TodosDbContext(optionsBuilder.Options);
    }
}

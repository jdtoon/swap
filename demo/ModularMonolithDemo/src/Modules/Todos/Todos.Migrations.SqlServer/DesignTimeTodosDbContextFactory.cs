using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ModularMonolithDemo.Modules.Todos.Module.Persistence;

namespace Todos.Migrations.SqlServer;

public class DesignTimeTodosDbContextFactory : IDesignTimeDbContextFactory<TodosDbContext>
{
    public TodosDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TodosDbContext>();

        // Allow override via environment variable for local generation
        var conn = Environment.GetEnvironmentVariable("TODOS_SQLSERVER_CONNECTION")
                   ?? "Server=localhost,1433;Database=TodosDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True";

        optionsBuilder.UseSqlServer(conn, b => b.MigrationsAssembly(typeof(DesignTimeTodosDbContextFactory).Assembly.FullName));

        return new TodosDbContext(optionsBuilder.Options);
    }
}

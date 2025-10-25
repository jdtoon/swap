using Swap.CLI.Infrastructure;
using Spectre.Console;

namespace Swap.CLI.Commands;

/// <summary>
/// Database management commands (Rails-inspired: db:migrate, db:rollback, etc.)
/// </summary>
public class DbCommand
{
    private readonly string _subCommand;
    private readonly string? _migrationName;
    private readonly string? _projectPath;

    public DbCommand(string subCommand, string? migrationName = null, string? projectPath = null)
    {
        _subCommand = subCommand;
        _migrationName = migrationName;
        _projectPath = projectPath ?? Directory.GetCurrentDirectory();
    }

    public async Task<int> ExecuteAsync()
    {
        try
        {
            // Check if EF Core tools are installed
            var efInstalled = await MigrationRunner.IsEfCoreInstalledAsync();
            if (!efInstalled)
            {
                AnsiConsole.MarkupLine("[red]❌ EF Core tools not installed[/]");
                AnsiConsole.MarkupLine("[yellow]Install with:[/]");
                AnsiConsole.MarkupLine("[blue]   dotnet tool install --global dotnet-ef[/]");
                return 1;
            }

            return _subCommand switch
            {
                "migrate" => await MigrateAsync(),
                "update" => await UpdateAsync(),
                "rollback" => await RollbackAsync(),
                "reset" => await ResetAsync(),
                "seed" => await SeedAsync(),
                "status" => await StatusAsync(),
                _ => InvalidCommand()
            };
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]❌ Error: {ex.Message}[/]");
            return 1;
        }
    }

    private async Task<int> MigrateAsync()
    {
        if (string.IsNullOrEmpty(_migrationName))
        {
            AnsiConsole.MarkupLine("[red]❌ Migration name required[/]");
            AnsiConsole.MarkupLine("[yellow]Usage: netmx db migrate <MigrationName>[/]");
            return 1;
        }

        AnsiConsole.MarkupLine($"[cyan]🔄 Creating migration: {_migrationName}[/]");

        var success = await MigrationRunner.CreateMigrationAsync(_migrationName, _projectPath);

        if (success)
        {
            AnsiConsole.MarkupLine($"[green]✅ Migration '{_migrationName}' created successfully[/]");
            AnsiConsole.MarkupLine("[yellow]💡 Run 'netmx db update' to apply the migration[/]");
            return 0;
        }
        else
        {
            AnsiConsole.MarkupLine("[red]❌ Failed to create migration[/]");
            return 1;
        }
    }

    private async Task<int> UpdateAsync()
    {
        AnsiConsole.MarkupLine("[cyan]🔄 Applying pending migrations...[/]");

        var success = await MigrationRunner.UpdateDatabaseAsync(_projectPath);

        if (success)
        {
            AnsiConsole.MarkupLine("[green]✅ Database updated successfully[/]");
            return 0;
        }
        else
        {
            AnsiConsole.MarkupLine("[red]❌ Failed to update database[/]");
            return 1;
        }
    }

    private async Task<int> RollbackAsync()
    {
        AnsiConsole.MarkupLine("[yellow]⚠️  Rolling back last migration...[/]");
        AnsiConsole.MarkupLine("[dim]This will undo the last migration and update the database[/]");

        var success = await MigrationRunner.RemoveMigrationAsync(_projectPath);

        if (success)
        {
            AnsiConsole.MarkupLine("[green]✅ Last migration rolled back successfully[/]");
            return 0;
        }
        else
        {
            AnsiConsole.MarkupLine("[red]❌ Failed to rollback migration[/]");
            return 1;
        }
    }

    private async Task<int> ResetAsync()
    {
        AnsiConsole.MarkupLine("[red]⚠️  WARNING: This will delete all data in the database![/]");
        var confirm = AnsiConsole.Confirm("Are you sure you want to reset the database?", false);

        if (!confirm)
        {
            AnsiConsole.MarkupLine("[yellow]Database reset cancelled[/]");
            return 0;
        }

        AnsiConsole.Status()
            .Start("Resetting database...", ctx =>
            {
                ctx.Status("[yellow]Dropping database...[/]");
                // Note: This is a simplified version. Full implementation would:
                // 1. Drop database
                // 2. Recreate database
                // 3. Apply all migrations
                // 4. Run seeders
                AnsiConsole.MarkupLine("[dim]   DROP DATABASE[/]");

                ctx.Status("[yellow]Recreating database...[/]");
                AnsiConsole.MarkupLine("[dim]   CREATE DATABASE[/]");

                ctx.Status("[yellow]Applying migrations...[/]");
                AnsiConsole.MarkupLine("[dim]   APPLYING MIGRATIONS[/]");
            });

        AnsiConsole.MarkupLine("[yellow]⚠️  Database reset not fully implemented yet[/]");
        AnsiConsole.MarkupLine("[dim]For now, manually drop database and run 'netmx db update'[/]");
        return 1;
    }

    private async Task<int> SeedAsync()
    {
        AnsiConsole.MarkupLine("[cyan]🌱 Running seeders...[/]");

        // TODO: Implement seeder discovery and execution
        // 1. Find all classes implementing ISeeder
        // 2. Execute them in order
        // 3. Track which seeders have run

        AnsiConsole.MarkupLine("[yellow]⚠️  Seeder execution not implemented yet[/]");
        AnsiConsole.MarkupLine("[dim]Seeders will be available in CLI Phase 2D (Week 4)[/]");
        return 1;
    }

    private async Task<int> StatusAsync()
    {
        AnsiConsole.MarkupLine("[cyan]📊 Migration Status[/]");
        AnsiConsole.WriteLine();

        var migrations = await MigrationRunner.ListMigrationsAsync(_projectPath);

        if (migrations.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No migrations found[/]");
            return 0;
        }

        var table = new Table();
        table.AddColumn("Migration");
        table.AddColumn("Status");

        foreach (var migration in migrations)
        {
            // Simple heuristic: migrations with dates are likely applied
            var status = migration.Contains("(applied)") || !migration.Contains("(pending)")
                ? "[green]✅ Applied[/]"
                : "[yellow]⏳ Pending[/]";

            table.AddRow(migration, status);
        }

        AnsiConsole.Write(table);
        return 0;
    }

    private int InvalidCommand()
    {
        AnsiConsole.MarkupLine("[red]❌ Invalid command[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]Available commands:[/]");
        AnsiConsole.MarkupLine("[blue]   netmx db migrate <name>[/]  - Create a new migration");
        AnsiConsole.MarkupLine("[blue]   netmx db update[/]          - Apply pending migrations");
        AnsiConsole.MarkupLine("[blue]   netmx db rollback[/]        - Undo last migration");
        AnsiConsole.MarkupLine("[blue]   netmx db reset[/]           - Drop and recreate database");
        AnsiConsole.MarkupLine("[blue]   netmx db seed[/]            - Run database seeders");
        AnsiConsole.MarkupLine("[blue]   netmx db status[/]          - Show migration status");
        return 1;
    }
}


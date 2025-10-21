using System.CommandLine;

namespace NetMX.CLI.Commands.Database;

/// <summary>
/// Base command for database operations.
/// Provides subcommands for migration management and database operations.
/// </summary>
public static class DatabaseCommand
{
    public static Command Create()
    {
        var dbCommand = new Command("db", "Database management commands");

        // Add subcommands
        dbCommand.Subcommands.Add(MigrateCommand.Create());
        dbCommand.Subcommands.Add(UpdateCommand.Create());
        dbCommand.Subcommands.Add(RollbackCommand.Create());
        dbCommand.Subcommands.Add(ResetCommand.Create());
        dbCommand.Subcommands.Add(StatusCommand.Create());
        dbCommand.Subcommands.Add(SeedCommand.Create());

        return dbCommand;
    }
}

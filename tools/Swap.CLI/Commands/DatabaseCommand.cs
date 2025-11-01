using System.CommandLine;

namespace Swap.CLI.Commands;

public static class DatabaseCommand
{
    public static Command Create()
    {
        var command = new Command("database", "Database management commands");
        command.AddAlias("db");
        
        // Add subcommands
        command.AddCommand(DatabaseMigrationCommand.Create());
        command.AddCommand(DatabaseResetCommand.Create());
        command.AddCommand(DatabaseSeedCommand.Create());
        command.AddCommand(DatabaseInfoCommand.Create());
        
        // Keep old 'migrate' command for backward compatibility (deprecated)
        command.AddCommand(DatabaseMigrateCommand.Create());
        
        return command;
    }
}

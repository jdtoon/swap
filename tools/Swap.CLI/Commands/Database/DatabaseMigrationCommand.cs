using System.CommandLine;

namespace Swap.CLI.Commands;

public static class DatabaseMigrationCommand
{
    public static Command Create()
    {
        var command = new Command("migration", "Manage Entity Framework migrations");
        
        command.AddCommand(DatabaseMigrationAddCommand.Create());
        command.AddCommand(DatabaseMigrationApplyCommand.Create());
        command.AddCommand(DatabaseMigrationListCommand.Create());
        command.AddCommand(DatabaseMigrationRemoveCommand.Create());
        
        return command;
    }
}

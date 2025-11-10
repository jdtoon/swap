using System.CommandLine;

namespace Swap.CLI.Commands;

public static class GenerateCommand
{
    public static Command Create()
    {
        var command = new Command("generate", "Generate framework-related code");
        command.AddAlias("g");
        
        // Add subcommands
        command.AddCommand(GenerateHtmxShellCommand.Create());
        
        return command;
    }
}

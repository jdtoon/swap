using System.CommandLine;
using Swap.CLI.Commands.Relationships;

namespace Swap.CLI.Commands;

public static class GenerateCommand
{
    public static Command Create()
    {
        var command = new Command("generate", "Generate code (controller, model, etc.)");
        command.AddAlias("g");
        
        // Add subcommands
        command.AddCommand(GenerateAuthCommand.Create());
        command.AddCommand(GenerateControllerCommand.Create());
        command.AddCommand(GenerateModelCommand.Create());
        command.AddCommand(GenerateResourceCommand.Create());
        command.AddCommand(GenerateSeedCommand.Create());
        command.AddCommand(GenerateTestCommand.Create());
        command.AddCommand(GenerateFactoryCommand.Create());
        command.AddCommand(GeneratePatternCommand.Create());
        command.AddCommand(GenerateHtmxShellCommand.Create());
        command.AddCommand(GenerateRelationshipCommand.Create());
        
        return command;
    }
}

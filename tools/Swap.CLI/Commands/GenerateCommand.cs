using System.CommandLine;

namespace Swap.CLI.Commands;

public static class GenerateCommand
{
    public static Command Create()
    {
        var command = new Command("generate", "Generate code (controller, model, etc.)");
        command.AddAlias("g");
        
        // Add subcommands
        command.AddCommand(GenerateControllerCommand.Create());
        command.AddCommand(GenerateModelCommand.Create());
        command.AddCommand(GenerateResourceCommand.Create());
        command.AddCommand(GenerateSeedCommand.Create());
        command.AddCommand(GenerateTestCommand.Create());
        command.AddCommand(GenerateFactoryCommand.Create());
        
        return command;
    }
}

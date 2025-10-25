using System.CommandLine;
using Swap.CLI.Commands;

var rootCommand = new RootCommand("Swap CLI - The Rails of .NET");

// Add commands
rootCommand.AddCommand(NewCommand.Create());
rootCommand.AddCommand(GenerateCommand.Create());

return await rootCommand.InvokeAsync(args);

using System.CommandLine;
using Swap.CLI.Commands;

var rootCommand = new RootCommand("Swap CLI - The Rails of .NET");
rootCommand.AddCommand(NewCommand.Create());

return rootCommand.Invoke(args);

using System.CommandLine;
using Swap.CLI.Commands;

var rootCommand = new RootCommand("Swap CLI - Generate production-ready ASP.NET + HTMX applications");

// Add commands
rootCommand.AddCommand(NewCommand.Create());
rootCommand.AddCommand(GenerateCommand.Create());

return await rootCommand.InvokeAsync(args);

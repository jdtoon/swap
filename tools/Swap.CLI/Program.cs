using System.CommandLine;
using Swap.CLI.Commands;

var rootCommand = new RootCommand(@"Swap CLI - HTMX-first framework for ASP.NET Core

Examples:
  swap new MyApp                          Create a new project
  swap g htmx-shell                       Add HTMX shell middleware
  swap events list                        List event chains");

// Add commands
rootCommand.AddCommand(NewCommand.Create());
rootCommand.AddCommand(GenerateCommand.Create());
rootCommand.AddCommand(EventsCommand.Create());

return await rootCommand.InvokeAsync(args);

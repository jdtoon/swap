using System.CommandLine;
using Swap.CLI.Commands;

var rootCommand = new RootCommand(@"Swap CLI - Generate production-ready ASP.NET + HTMX applications

Examples:
  swap new MyApp                          Create a new project
  swap g m Product --fields ""Name:string Price:decimal""
                                          Generate a model
  swap g c Product --fields ""Name:string Price:decimal""
                                          Generate a controller
  swap g r Product --fields ""Name:string Price:decimal InStock:bool""
                                          Generate full CRUD resource
  swap g seed all --count 100             Generate seeders for all entities

Field Flags:
  :sortable, :s       Enable sorting on this column
  :filterable, :f     Enable filtering on this column
  :nosort, :ns        Disable sorting (not sortable)");

// Add commands
rootCommand.AddCommand(NewCommand.Create());
rootCommand.AddCommand(GenerateCommand.Create());

return await rootCommand.InvokeAsync(args);

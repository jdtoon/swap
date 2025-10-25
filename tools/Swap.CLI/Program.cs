using System.CommandLine;

var rootCommand = new RootCommand("Swap CLI - The Rails of .NET");

// TODO: Add commands based on THE-PRODUCT.md and HTMX-PATTERNS-LEARNED.md
// Examples:
// - swap new <projectName>
// - swap generate feature <name>
// - swap add toasts
// - swap db migrate

return rootCommand.Invoke(args);

using System.CommandLine;

var rootCommand = new RootCommand("Swap CLI - The Rails of .NET");

rootCommand.SetAction(context =>
{
    // TODO: Add commands based on THE-PRODUCT.md and HTMX-PATTERNS-LEARNED.md
    // Examples:
    // - swap new <projectName>
    // - swap generate feature <name>
    // - swap add toasts
    // - swap db migrate
    
    Console.WriteLine("Swap CLI - Use --help for usage");
    return Task.FromResult(0);
});

return await rootCommand.Parse(args).InvokeAsync();

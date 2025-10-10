using System.CommandLine;
using System.CommandLine.Parsing;
using LibGit2Sharp;

class Program
{
    static int Main(string[] args)
    {
        // --- Define Root Command ---
        var rootCommand = new RootCommand("Command-Line Interface for the NetMX Framework");

        // --- Define 'new' Command ---
        var newCommand = new Command("new", "Create a new NetMX solution from a startup template.");
        
        // --- Define Arguments and Options for 'new' ---
        var outputArgument = new Argument<string>("output") 
        {
            Description = "The name of the solution and the directory to create it in."
        };

        var templateOption = new Option<string>("--template", "-t")
        {
            Description = "The name of the startup template to use."
        };
        
        // --- Add Symbols to Commands ---
        newCommand.Arguments.Add(outputArgument);
        newCommand.Options.Add(templateOption);
        rootCommand.Subcommands.Add(newCommand);

        // --- Set the Action for the 'new' Command ---
        newCommand.SetAction((parseResult) =>
        {
            // CORRECTED: Use the GetValue<T>(symbol) method directly on ParseResult
            string outputValue = parseResult.GetValue(outputArgument);
            string templateValue = parseResult.GetValue(templateOption) ?? "modular";

            CreateNewSolution(outputValue, templateValue);
        });

        // --- Invoke the parser ---
        return rootCommand.Parse(args).Invoke();
    }

    // --- Logic Methods ---
    internal static void CreateNewSolution(string outputDirectory, string templateName)
{
    Console.WriteLine($"🚀 Creating new NetMX solution '{outputDirectory}' using the '{templateName}' template...");

    // In the future, this can come from a config file.
    // We will create this repository in the next Epic.
    var templateRepoUrl = $"https://github.com/netmx-framework/template-{templateName}.git";

    try
    {
        // The actual cloning operation.
        Repository.Clone(templateRepoUrl, outputDirectory);

        Console.WriteLine($"✅ Successfully cloned template into '{outputDirectory}'.");
        Console.WriteLine("Next steps: ");
        Console.WriteLine($"   cd {outputDirectory}");
        Console.WriteLine("   (Restore dependencies, etc.)");

        // TODO:
        // - Delete the .git folder from the cloned repo.
        // - Run 'git init' to create a new, clean repository for the user.
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"❌ ERROR: Could not create the project.");
        Console.WriteLine($"   Please ensure you are online and that the template repository exists.");
        Console.WriteLine($"   Details: {ex.Message}");
        Console.ResetColor();
    }
}
}
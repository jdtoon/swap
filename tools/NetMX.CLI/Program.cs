using System.CommandLine;
using System.CommandLine.Parsing;

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
        Console.WriteLine("-- NetMX CLI 'new' Command --");
        Console.WriteLine($"Solution Name: {outputDirectory}");
        Console.WriteLine($"Template: {templateName}");
        Console.WriteLine("✅ Command parsing successful. Git logic will be added next.");
    }
}
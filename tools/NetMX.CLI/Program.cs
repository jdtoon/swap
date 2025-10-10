using System.CommandLine;
using System.CommandLine.Parsing;
using LibGit2Sharp;

class Program
{
    static int Main(string[] args)
    {
        // --- Define Root Command ---
        var rootCommand = new RootCommand("Command-Line Interface for the NetMX Framework");

        // =================================================================
        // === 'new' COMMAND DEFINITION (Existing Code)
        // =================================================================
        var newCommand = new Command("new", "Create a new NetMX solution from a startup template.");
        
        var outputArgument = new Argument<string>("output") 
        {
            Description = "The name of the solution and the directory to create it in."
        };

        var templateOption = new Option<string>("--template", "-t")
        {
            Description = "The name of the startup template to use."
        };
        
        newCommand.Arguments.Add(outputArgument);
        newCommand.Options.Add(templateOption);
        rootCommand.Subcommands.Add(newCommand);

        newCommand.SetAction((parseResult) =>
        {
            string outputValue = parseResult.GetValue(outputArgument);
            string templateValue = parseResult.GetValue(templateOption) ?? "modular";

            CreateNewSolution(outputValue, templateValue);
        });


        // =================================================================
        // === START: NEW CODE FOR THE 'add module' COMMAND
        // =================================================================

        // 1. Define a parent 'add' command for grouping.
        var addCommand = new Command("add", "Add a new item to the NetMX solution.");
        rootCommand.Subcommands.Add(addCommand);

        // 2. Define the 'module' subcommand.
        var moduleCommand = new Command("module", "Add a new application module.");
        addCommand.Subcommands.Add(moduleCommand);

        // 3. Define arguments and options for 'module'.
        var moduleNameArgument = new Argument<string>("name");
        moduleCommand.Arguments.Add(moduleNameArgument);

        var proOption = new Option<bool>("--pro")
        {
            Description = "Flag to create the module in the 'pro' directory."
        };
        moduleCommand.Options.Add(proOption);

        // 4. Set the Action for the 'module' subcommand.
        moduleCommand.SetAction((parseResult) =>
        {
            string moduleName = parseResult.GetValue(moduleNameArgument);
            bool isPro = parseResult.GetValue(proOption);

            AddModule(moduleName, isPro);
        });

        // =================================================================
        // === END: NEW CODE FOR THE 'add module' COMMAND
        // =================================================================


        // --- Invoke the parser ---
        return rootCommand.Parse(args).Invoke();
    }

    // --- Logic Methods ---
    internal static void CreateNewSolution(string outputDirectory, string templateName)
    {
        Console.WriteLine($"🚀 Creating new NetMX solution '{outputDirectory}' using the '{templateName}' template...");
        var templateRepoUrl = $"https://github.com/netmx-framework/template-{templateName}.git";
        try
        {
            Repository.Clone(templateRepoUrl, outputDirectory);
            Console.WriteLine($"✅ Successfully cloned template into '{outputDirectory}'.");
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

    internal static void AddModule(string moduleName, bool isPro)
    {
        Console.WriteLine("-- NetMX CLI 'add module' Command --");
        Console.WriteLine($"Module Name: {moduleName}");
        Console.WriteLine($"Is Pro Module: {isPro}");
        Console.WriteLine("✅ Command parsing successful.");
        Console.WriteLine("TODO: Implement scaffolding logic here (create projects, add to solution, etc.).");
    }
}
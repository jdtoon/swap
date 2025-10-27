using System.CommandLine;
using System.CommandLine.Invocation;
using Spectre.Console;
using Swap.CLI.Infrastructure;

namespace Swap.CLI.Commands;

public static class NewCommand
{
    public static Command Create()
    {
        var command = new Command("new", "Create a new Swap project");
        
        var nameArg = new Argument<string>("name", "The name of the project (e.g., MyApp)");
        var dbOption = new Option<string>("--database", () => "sqlite", "Database provider (sqlite|sqlserver|postgres)");
        var outOption = new Option<string?>("--output", "Output directory (default: ./{name})");
        
        command.AddArgument(nameArg);
        command.AddOption(dbOption);
        command.AddOption(outOption);
        
        command.SetHandler(async (InvocationContext context) =>
        {
            var name = context.ParseResult.GetValueForArgument(nameArg);
            var database = context.ParseResult.GetValueForOption(dbOption);
            var output = context.ParseResult.GetValueForOption(outOption);
            
            context.ExitCode = await ExecuteAsync(name, database!, output);
        });
        
        return command;
    }
    
    private static async Task<int> ExecuteAsync(string name, string database, string? output)
    {
        // Validate project name
        if (string.IsNullOrWhiteSpace(name))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Project name cannot be empty.");
            return 1;
        }
        
        if (name.Contains(' '))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Project name cannot contain spaces. Use PascalCase (e.g., 'MyApp' instead of 'My App').");
            return 1;
        }
        
        if (!char.IsLetter(name[0]))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Project name must start with a letter.");
            return 1;
        }
        
        // Validate database option
        if (database != "sqlite" && database != "sqlserver" && database != "postgres")
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Invalid database option '{database}'. Must be: sqlite, sqlserver, or postgres");
            return 1;
        }
        
        var projectPath = Path.GetFullPath(output ?? name);
        
        AnsiConsole.MarkupLine($"[bold cyan]Creating new Swap project:[/] {name}");
        AnsiConsole.MarkupLine($"[dim]Database:[/] {database}");
        AnsiConsole.MarkupLine($"[dim]Location:[/] {projectPath}");
        AnsiConsole.WriteLine();
        
        // Check if directory exists
        if (Directory.Exists(projectPath))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Directory '{projectPath}' already exists.");
            return 1;
        }
        
        try
        {
            await GenerateProjectAsync(name, database, projectPath);
            
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[green]✓[/] Project created successfully!");
            AnsiConsole.WriteLine();
            
            // Run setup commands automatically
            var hasNpm = await IsCommandAvailableAsync("npm");
            var hasLibman = await IsCommandAvailableAsync("libman");
            
            if (!hasNpm && !hasLibman)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[yellow]Note:[/] npm and libman not found in PATH. Skipping automatic setup.");
                AnsiConsole.MarkupLine("[dim]You'll need to run setup commands manually (see instructions below).[/]");
            }
            else
            {
                try
                {
                    await AnsiConsole.Status()
                        .StartAsync("Running setup commands...", async ctx =>
                        {
                            if (hasNpm)
                            {
                                try
                                {
                                    ctx.Status("Running npm install...");
                                    await RunCommandAsync("npm", "install", projectPath);
                                    AnsiConsole.MarkupLine("[green]✓[/] npm install completed");
                                }
                                catch (Exception ex)
                                {
                                    AnsiConsole.MarkupLine($"[yellow]Warning:[/] npm install failed: {ex.Message}");
                                }
                            }
                            else
                            {
                                AnsiConsole.MarkupLine("[yellow]⊘[/] npm not found - skipping npm install");
                            }
                            
                            if (hasLibman)
                            {
                                try
                                {
                                    ctx.Status("Running libman restore...");
                                    await RunCommandAsync("libman", "restore", projectPath);
                                    AnsiConsole.MarkupLine("[green]✓[/] libman restore completed");
                                }
                                catch (Exception ex)
                                {
                                    AnsiConsole.MarkupLine($"[yellow]Warning:[/] libman restore failed: {ex.Message}");
                                }
                            }
                            else
                            {
                                AnsiConsole.MarkupLine("[yellow]⊘[/] libman not found - skipping libman restore");
                            }
                            
                            if (hasNpm)
                            {
                                try
                                {
                                    ctx.Status("Building CSS...");
                                    await RunCommandAsync("npm", "run build:css", projectPath);
                                    AnsiConsole.MarkupLine("[green]✓[/] CSS build completed");
                                }
                                catch (Exception ex)
                                {
                                    AnsiConsole.MarkupLine($"[yellow]Warning:[/] CSS build failed: {ex.Message}");
                                }
                            }
                            else
                            {
                                AnsiConsole.MarkupLine("[yellow]⊘[/] npm not found - skipping CSS build");
                            }
                        });
                    
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine("[green]✓[/] Setup completed!");
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[yellow]Warning:[/] Some setup steps failed: {ex.Message}");
                }
            }
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Next steps:[/]");
            AnsiConsole.MarkupLine($"  cd {name}");
            
            if (!hasNpm || !hasLibman)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[yellow]⚠ Manual setup required:[/]");
                if (!hasNpm)
                {
                    AnsiConsole.MarkupLine("[yellow]  npm install[/]          [dim]# Install Node.js packages[/]");
                    AnsiConsole.MarkupLine("[yellow]  npm run build:css[/]    [dim]# Build Tailwind CSS[/]");
                }
                if (!hasLibman)
                {
                    AnsiConsole.MarkupLine("[yellow]  libman restore[/]       [dim]# Restore client libraries (HTMX, DaisyUI)[/]");
                }
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[dim]Then continue with:[/]");
            }
            
            AnsiConsole.MarkupLine("  dotnet ef migrations add InitialCreate");
            AnsiConsole.MarkupLine("  dotnet ef database update");
            AnsiConsole.MarkupLine("  dotnet run");
            
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }
    
    private static async Task GenerateProjectAsync(string projectName, string database, string projectPath)
    {
        var templatePath = Path.Combine(AppContext.BaseDirectory, "templates", "monolith");
        
        if (!Directory.Exists(templatePath))
        {
            throw new DirectoryNotFoundException($"Template directory not found: {templatePath}");
        }
        
        // Create project directory
        Directory.CreateDirectory(projectPath);
        
        // Setup template variables
        var variables = new Dictionary<string, string>
        {
            { "ProjectName", projectName },
            { "ProjectNameLower", projectName.ToLowerInvariant() },
            { "DatabaseProvider", database }
        };
        
        await AnsiConsole.Status()
            .StartAsync("Generating project...", async ctx =>
            {
                // Copy and process all template files
                await ProcessTemplateDirectoryAsync(templatePath, projectPath, variables, ctx);
            });
    }
    
    private static async Task ProcessTemplateDirectoryAsync(
        string sourcePath,
        string targetPath,
        Dictionary<string, string> variables,
        StatusContext ctx)
    {
        foreach (var file in Directory.GetFiles(sourcePath, "*.template", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourcePath, file);
            var targetFileName = relativePath.Replace(".template", "");
            
            // Special case: rename Project.csproj to {ProjectName}.csproj
            if (targetFileName == "Project.csproj")
            {
                targetFileName = $"{variables["ProjectName"]}.csproj";
            }
            
            var targetFile = Path.Combine(targetPath, targetFileName);
            
            ctx.Status($"Creating {targetFileName}...");
            
            // Create target directory if needed
            var targetDir = Path.GetDirectoryName(targetFile)!;
            Directory.CreateDirectory(targetDir);
            
            // Read template, process, and write
            var templateContent = await File.ReadAllTextAsync(file);
            var processedContent = TemplateEngine.Process(templateContent, variables);
            await File.WriteAllTextAsync(targetFile, processedContent);
            
            await Task.Delay(50); // Small delay for visual feedback
        }
    }
    
    private static async Task<bool> IsCommandAvailableAsync(string command)
    {
        try
        {
            var processStartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = command,
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            using var process = System.Diagnostics.Process.Start(processStartInfo);
            if (process == null) return false;
            
            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
    
    private static async Task RunCommandAsync(string command, string arguments, string workingDirectory)
    {
        var processStartInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        using var process = System.Diagnostics.Process.Start(processStartInfo);
        if (process == null)
        {
            throw new InvalidOperationException($"Failed to start {command}");
        }
        
        await process.WaitForExitAsync();
        
        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"{command} failed: {error}");
        }
    }
}

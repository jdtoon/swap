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
        var skipSetupOption = new Option<bool>("--skip-setup", description: "Skip prerequisites check, npm/libman steps, and initial migration (useful for CI/tests)");
        var noHtmxShellOption = new Option<bool>("--no-htmx-shell", description: "Do not include the HTMX shell middleware by default");
        
        command.AddArgument(nameArg);
        command.AddOption(dbOption);
        command.AddOption(outOption);
        command.AddOption(skipSetupOption);
        command.AddOption(noHtmxShellOption);
        
        command.SetHandler(async (InvocationContext context) =>
        {
            var name = context.ParseResult.GetValueForArgument(nameArg);
            var database = context.ParseResult.GetValueForOption(dbOption);
            var output = context.ParseResult.GetValueForOption(outOption);
            var skipSetup = context.ParseResult.GetValueForOption(skipSetupOption);
            var noHtmxShell = context.ParseResult.GetValueForOption(noHtmxShellOption);
            
            context.ExitCode = await ExecuteAsync(name, database!, output, skipSetup, noHtmxShell);
        });
        
        return command;
    }
    
    private static async Task<int> ExecuteAsync(string name, string database, string? output, bool skipSetup, bool noHtmxShell)
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
        
        if (!skipSetup)
        {
            // Check prerequisites upfront
            AnsiConsole.Status()
                .Start("Checking prerequisites...", ctx => { });
            
            var hasNpm = await IsCommandAvailableAsync("npm");
            var hasLibman = await IsCommandAvailableAsync("libman");
            
            if (!hasNpm || !hasLibman)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[red]✗ Prerequisites check failed[/]");
                AnsiConsole.WriteLine();
                
                if (!hasNpm)
                {
                    AnsiConsole.MarkupLine("[red]  ✗ npm not found[/]");
                    AnsiConsole.MarkupLine("    [dim]npm is required for Tailwind CSS and frontend dependencies[/]");
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine("    [bold]Install Node.js (includes npm):[/]");
                    AnsiConsole.MarkupLine("      • Download: [link]https://nodejs.org/[/] (LTS version recommended)");
                    AnsiConsole.MarkupLine("      • Windows (winget): [cyan]winget install OpenJS.NodeJS.LTS[/]");
                    AnsiConsole.MarkupLine("      • Windows (chocolatey): [cyan]choco install nodejs-lts[/]");
                    AnsiConsole.MarkupLine("      • macOS (homebrew): [cyan]brew install node[/]");
                    AnsiConsole.MarkupLine("      • Linux: Use your package manager (apt, yum, etc.)");
                }
                
                if (!hasLibman)
                {
                    AnsiConsole.MarkupLine("[red]  ✗ libman not found[/]");
                    AnsiConsole.MarkupLine("    [dim]libman manages client libraries (HTMX, DaisyUI)[/]");
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine("    [bold]Install libman:[/]");
                    AnsiConsole.MarkupLine("      [cyan]dotnet tool install -g Microsoft.Web.LibraryManager.Cli[/]");
                }
                
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[yellow]After installing the above tools:[/]");
                AnsiConsole.MarkupLine("  1. [bold]Restart your terminal[/] (to refresh PATH)");
                AnsiConsole.MarkupLine("  2. Verify installations:");
                if (!hasNpm)
                    AnsiConsole.MarkupLine("     [cyan]npm --version[/]");
                if (!hasLibman)
                    AnsiConsole.MarkupLine("     [cyan]libman --version[/]");
                AnsiConsole.MarkupLine($"  3. Run [cyan]swap new {name}[/] again");
                AnsiConsole.WriteLine();
                
                return 1;
            }
            
            AnsiConsole.MarkupLine("[green]✓[/] Prerequisites check passed");
            AnsiConsole.WriteLine();
        }
        
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
            
            if (!skipSetup)
            {
                // Run setup commands automatically (prerequisites already checked)
                try
                {
                    await AnsiConsole.Status()
                        .StartAsync("Running setup commands...", async ctx =>
                        {
                            try
                            {
                                ctx.Status("Running npm install...");
                                await RunCommandAsync("npm", "install", projectPath);
                                AnsiConsole.MarkupLine("[green]✓[/] npm install completed");
                            }
                            catch (Exception ex)
                            {
                                AnsiConsole.MarkupLine($"[red]✗[/] npm install failed: {ex.Message}");
                                throw;
                            }
                            
                            try
                            {
                                ctx.Status("Running libman restore...");
                                await RunCommandAsync("libman", "restore", projectPath);
                                AnsiConsole.MarkupLine("[green]✓[/] libman restore completed");
                            }
                            catch (Exception ex)
                            {
                                AnsiConsole.MarkupLine($"[red]✗[/] libman restore failed: {ex.Message}");
                                throw;
                            }
                            
                            try
                            {
                                ctx.Status("Building CSS...");
                                await RunCommandAsync("npm", "run build:css", projectPath);
                                AnsiConsole.MarkupLine("[green]✓[/] CSS build completed");
                            }
                            catch (Exception ex)
                            {
                        AnsiConsole.MarkupLine($"[red]✗[/] CSS build failed: {ex.Message}");
                        throw;
                    }
                });
                
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[green]✓[/] Setup completed!");
                
                // Build-first, then create initial migration (no DB update)
                AnsiConsole.WriteLine();
                await AnsiConsole.Status()
                    .StartAsync("Creating initial migration...", async ctx =>
                    {
                        try
                        {
                            ctx.Status("Building project before migration...");
                            await RunCommandAsync("dotnet", "build", projectPath);

                            ctx.Status("Creating initial migration...");
                            await RunCommandAsync("dotnet", "ef migrations add InitialCreate", projectPath);
                            AnsiConsole.MarkupLine("[green]✓[/] Migration created");
                        }
                        catch (Exception ex)
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] Migration creation failed: {ex.Message}");
                            throw;
                        }
                    });
                    
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[green]✓[/] Migration ready!");
                }
                catch (Exception)
                {
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine("[red]✗ Setup failed. Please run the setup commands manually:[/]");
                    AnsiConsole.MarkupLine($"  cd {name}");
                    AnsiConsole.MarkupLine("  npm install");
                    AnsiConsole.MarkupLine("  libman restore");
                    AnsiConsole.MarkupLine("  npm run build:css");
                    AnsiConsole.MarkupLine("  dotnet ef migrations add InitialCreate");
                    return 1;
                }
            }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold green]🎉 Project ready![/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Run your application:[/]");
        AnsiConsole.MarkupLine($"  cd {name}");
        AnsiConsole.MarkupLine("  dotnet run");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Then visit: http://localhost:5000[/]");
        return 0;
    }
    catch (Exception)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[red]✗ Setup failed. Please run the setup commands manually:[/]");
        AnsiConsole.MarkupLine($"  cd {name}");
        AnsiConsole.MarkupLine("  npm install");
        AnsiConsole.MarkupLine("  libman restore");
        AnsiConsole.MarkupLine("  npm run build:css");
        AnsiConsole.MarkupLine("  dotnet ef migrations add InitialCreate");
        return 1;
    }
    }
    
    private static async Task AddHtmxShellAsync(string workingDir, string projectName)
    {
        // Create middleware file
        var middlewareDir = Path.Combine(workingDir, "Middleware");
        Directory.CreateDirectory(middlewareDir);
        var filePath = Path.Combine(middlewareDir, "HtmxShellMiddleware.cs");
        if (!File.Exists(filePath))
        {
            var code = $@"namespace {projectName}.Middleware;

public class HtmxShellMiddleware
{{
    private readonly RequestDelegate _next;

    // Adjust allowlist for controllers that render HTMX partial routes
    private static readonly HashSet<string> Allowlist = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {{
        // e.g., ""/Posts"", ""/Categories""
    }};

    public HtmxShellMiddleware(RequestDelegate next)
    {{
        _next = next;
    }}

    public async Task InvokeAsync(HttpContext context)
    {{
        var isGet = HttpMethods.IsGet(context.Request.Method);
        var isHtmx = context.Request.Headers.ContainsKey(""HX-Request"");
        var path = context.Request.Path.Value ?? string.Empty;

        if (isGet && !isHtmx)
        {{
            foreach (var baseRoute in Allowlist)
            {{
                if (path.StartsWith(baseRoute + ""/"", StringComparison.OrdinalIgnoreCase))
                {{
                    context.Response.Redirect(baseRoute);
                    return;
                }}
            }}
        }}

        await _next(context);
    }}
}}
";
            await File.WriteAllTextAsync(filePath, code);
        }

        // Wire Program.cs
        var programPath = Path.Combine(workingDir, "Program.cs");
        if (File.Exists(programPath))
        {
            var content = await File.ReadAllTextAsync(programPath);
            var usingLine = $"using {projectName}.Middleware;";
            if (!content.Contains(usingLine))
            {
                var firstUsingEnd = content.IndexOf(";\n");
                if (firstUsingEnd > 0) content = content.Insert(firstUsingEnd + 2, usingLine + "\n");
                else content = usingLine + "\n" + content;
            }

            var buildIdx = content.IndexOf("var app = builder.Build();", StringComparison.Ordinal);
            if (buildIdx >= 0 && !content.Contains("UseMiddleware<HtmxShellMiddleware>"))
            {
                var insertPos = content.IndexOf('\n', buildIdx);
                if (insertPos > 0)
                {
                    content = content.Insert(insertPos + 1, "app.UseMiddleware<HtmxShellMiddleware>();\n");
                }
            }

            await File.WriteAllTextAsync(programPath, content);
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
            // On Windows, npm is a PowerShell script, so we need to use cmd or pwsh to run it
            var isWindows = OperatingSystem.IsWindows();
            var fileName = isWindows ? "cmd.exe" : command;
            var arguments = isWindows ? $"/c {command} --version" : "--version";
            
            var processStartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
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
        // On Windows, npm/npx are PowerShell scripts, so we need to use cmd to run them
        var isWindows = OperatingSystem.IsWindows();
        var fileName = command;
        var args = arguments;
        
        if (isWindows && (command == "npm" || command == "npx"))
        {
            fileName = "cmd.exe";
            args = $"/c {command} {arguments}";
        }
        
        var processStartInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = fileName,
            Arguments = args,
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

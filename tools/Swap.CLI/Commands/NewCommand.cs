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
    var templateOption = new Option<string>("--template", () => "monolith", "Project template (monolith|swap-monolith|layered|swap-layered)");
        var outOption = new Option<string?>("--output", "Output directory (default: ./{name})");
        var skipSetupOption = new Option<bool>("--skip-setup", description: "Skip prerequisites check, npm/libman steps, and initial migration (useful for CI/tests)");
        var localNugetOption = new Option<bool>("--local-nuget", description: "Use local NuGet feed for Swap packages (for framework development only)");
        
        command.AddArgument(nameArg);
        command.AddOption(dbOption);
    command.AddOption(outOption);
    command.AddOption(skipSetupOption);
    command.AddOption(localNugetOption);
    command.AddOption(templateOption);
        
        command.SetHandler(async (InvocationContext context) =>
        {
            var name = context.ParseResult.GetValueForArgument(nameArg);
            var database = context.ParseResult.GetValueForOption(dbOption);
            var output = context.ParseResult.GetValueForOption(outOption);
            var skipSetup = context.ParseResult.GetValueForOption(skipSetupOption);
            var localNuget = context.ParseResult.GetValueForOption(localNugetOption);
            var template = context.ParseResult.GetValueForOption(templateOption);
            
            context.ExitCode = await ExecuteAsync(name, database!, output, skipSetup, localNuget, template!);
        });
        
        return command;
    }
    
    private static async Task<int> ExecuteAsync(string name, string database, string? output, bool skipSetup, bool localNuget, string template)
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
    AnsiConsole.MarkupLine($"[dim]Template:[/] {template}");
        if (localNuget)
        {
            AnsiConsole.MarkupLine($"[dim]NuGet Source:[/] [yellow]Local feed (development mode)[/]");
        }
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
            await GenerateProjectAsync(name, database, projectPath, localNuget, template);

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
                            var isLayered = string.Equals(template, "layered", StringComparison.OrdinalIgnoreCase) ||
                                            string.Equals(template, "swap-layered", StringComparison.OrdinalIgnoreCase);
                            // New folder layout uses src/ for app
                            var webDir = isLayered ? Path.Combine(projectPath, "src", "Web") : Path.Combine(projectPath, "src");
                            try
                            {
                                ctx.Status("Running npm install...");
                                await RunCommandAsync("npm", "install", webDir);
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
                                await RunCommandAsync("libman", "restore", webDir);
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
                                await RunCommandAsync("npm", "run build:css", webDir);
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
                
                // Build-first, then create initial migration and update the database
                AnsiConsole.WriteLine();
                await AnsiConsole.Status()
                    .StartAsync("Creating initial migration...", async ctx =>
                    {
                        try
                        {
                            ctx.Status("Building project before migration...");
                            await RunCommandAsync("dotnet", "build", projectPath);

                            var isLayered = string.Equals(template, "layered", StringComparison.OrdinalIgnoreCase) ||
                                            string.Equals(template, "swap-layered", StringComparison.OrdinalIgnoreCase);
                            ctx.Status("Creating initial migration...");
                            if (isLayered)
                            {
                                await RunCommandAsync("dotnet", "ef migrations add InitialCreate -p src/Infrastructure -s src/Web", projectPath);
                            }
                            else
                            {
                                await RunCommandAsync("dotnet", "ef migrations add InitialCreate", Path.Combine(projectPath, "src"));
                            }
                            AnsiConsole.MarkupLine("[green]✓[/] Migration created");

                            ctx.Status("Updating database...");
                            if (isLayered)
                            {
                                await RunCommandAsync("dotnet", "ef database update -p src/Infrastructure -s src/Web", projectPath);
                            }
                            else
                            {
                                await RunCommandAsync("dotnet", "ef database update", Path.Combine(projectPath, "src"));
                            }
                            AnsiConsole.MarkupLine("[green]✓[/] Database updated");
                        }
                        catch (Exception ex)
                        {
                            AnsiConsole.MarkupLine($"[red]✗[/] Migration creation failed: {ex.Message}");
                            throw;
                        }
                    });
                    
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[green]✓[/] Migrations applied and database ready!");
                }
                catch (Exception)
                {
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine("[red]✗ Setup failed. Please run the setup commands manually:[/]");
                    var isLayered = string.Equals(template, "layered", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(template, "swap-layered", StringComparison.OrdinalIgnoreCase);
                    if (isLayered)
                    {
                        AnsiConsole.MarkupLine($"  cd {name}/src/Web");
                        AnsiConsole.MarkupLine("  npm install");
                        AnsiConsole.MarkupLine("  libman restore");
                        AnsiConsole.MarkupLine("  npm run build:css");
                        AnsiConsole.MarkupLine($"  cd ../..");
                        AnsiConsole.MarkupLine("  dotnet ef migrations add InitialCreate -p src/Infrastructure -s src/Web");
                        AnsiConsole.MarkupLine("  dotnet ef database update -p src/Infrastructure -s src/Web");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"  cd {name}/src");
                        AnsiConsole.MarkupLine("  npm install");
                        AnsiConsole.MarkupLine("  libman restore");
                        AnsiConsole.MarkupLine("  npm run build:css");
                        AnsiConsole.MarkupLine("  dotnet ef migrations add InitialCreate");
                        AnsiConsole.MarkupLine("  dotnet ef database update");
                    }
                    return 1;
                }
            }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold green]🎉 Project ready![/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Run your application:[/]");
        var layered = string.Equals(template, "layered", StringComparison.OrdinalIgnoreCase) ||
                      string.Equals(template, "swap-layered", StringComparison.OrdinalIgnoreCase);
        if (layered)
        {
            AnsiConsole.MarkupLine($"  cd {name}/src/Web");
            AnsiConsole.MarkupLine("  dotnet run");
        }
        else
        {
            AnsiConsole.MarkupLine($"  cd {name}/src");
            AnsiConsole.MarkupLine("  dotnet run");
        }
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Then visit: http://localhost:5000[/]");
        return 0;
    }
    catch (Exception)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[red]✗ Setup failed. Please run the setup commands manually:[/]");
        var isLayered = string.Equals(template, "layered", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(template, "swap-layered", StringComparison.OrdinalIgnoreCase);
        if (isLayered)
        {
            AnsiConsole.MarkupLine($"  cd {name}/src/Web");
            AnsiConsole.MarkupLine("  npm install");
            AnsiConsole.MarkupLine("  libman restore");
            AnsiConsole.MarkupLine("  npm run build:css");
            AnsiConsole.MarkupLine($"  cd ../..");
            AnsiConsole.MarkupLine("  dotnet ef migrations add InitialCreate -p src/Infrastructure -s src/Web");
            AnsiConsole.MarkupLine("  dotnet ef database update -p src/Infrastructure -s src/Web");
        }
        else
        {
            AnsiConsole.MarkupLine($"  cd {name}/src");
            AnsiConsole.MarkupLine("  npm install");
            AnsiConsole.MarkupLine("  libman restore");
            AnsiConsole.MarkupLine("  npm run build:css");
            AnsiConsole.MarkupLine("  dotnet ef migrations add InitialCreate");
            AnsiConsole.MarkupLine("  dotnet ef database update");
        }
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
    
    private static async Task GenerateProjectAsync(string projectName, string database, string projectPath, bool localNuget, string template)
    {
        var raw = (template ?? "monolith").Trim().ToLowerInvariant();
        var selected = raw switch
        {
            "monolith" => "monolith",
            "swap-monolith" => "swap-monolith",
            "layered" => "swap-layered",
            "swap-layered" => "swap-layered",
            _ => throw new ArgumentException($"Unknown template '{template}'. Use 'monolith', 'swap-monolith', 'layered', or 'swap-layered'.")
        };
        // Allow tests and custom packaging to override templates base directory
        var templatesBase = Environment.GetEnvironmentVariable("SWAP_TEMPLATES_DIR");
        var templatePath = string.IsNullOrWhiteSpace(templatesBase)
            ? Path.Combine(AppContext.BaseDirectory, "templates", selected)
            : Path.Combine(templatesBase!, selected);
        
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
            { "DatabaseProvider", database },
            { "DatabaseType", database }, // Alias for use in display/UI
            { "UseLocalNuget", localNuget.ToString().ToLowerInvariant() }
        };
        
        await AnsiConsole.Status()
            .StartAsync("Generating project...", async ctx =>
            {
                // Decide folder layout: place app under src, tests under test
                var useSrcLayout = string.Equals(selected, "swap-monolith", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(selected, "swap-layered", StringComparison.OrdinalIgnoreCase);

                // Copy and process all template files
                await ProcessTemplateDirectoryAsync(templatePath, projectPath, variables, ctx, useSrcLayout ? "src" : null);
                
                // If using local NuGet, ensure packages are built and create nuget.config
                if (localNuget)
                {
                    // Check if local feed exists, if not, offer to create it
                    var swapRootPath = Path.GetFullPath(Path.Combine(projectPath, "..", ".."));
                    var localFeedPath = Path.Combine(swapRootPath, ".nuget", "local");
                    
                    if (!Directory.Exists(localFeedPath) || !Directory.GetFiles(localFeedPath, "*.nupkg").Any())
                    {
                        ctx.Status("Local NuGet feed not found. Building packages...");
                        
                        // Run pack-local script
                        var isWindows = OperatingSystem.IsWindows();
                        var packScript = isWindows ? "pack-local.ps1" : "pack-local.sh";
                        var packScriptPath = Path.Combine(swapRootPath, "scripts", packScript);
                        
                        if (File.Exists(packScriptPath))
                        {
                            AnsiConsole.MarkupLine("\n[yellow]Building local NuGet packages...[/]");
                            try
                            {
                                if (isWindows)
                                {
                                    await RunCommandAsync("pwsh", $"-File \"{packScriptPath}\"", swapRootPath);
                                }
                                else
                                {
                                    await RunCommandAsync("bash", $"\"{packScriptPath}\"", swapRootPath);
                                }
                                AnsiConsole.MarkupLine("[green]✓[/] Local packages built successfully!");
                            }
                            catch (Exception ex)
                            {
                                AnsiConsole.MarkupLine($"[yellow]Warning:[/] Could not build local packages: {ex.Message}");
                                AnsiConsole.MarkupLine($"[yellow]Run manually:[/] {packScript}");
                            }
                        }
                    }
                    
                    await CreateLocalNugetConfigAsync(projectPath, ctx);
                }
            });
    }
    
    private static async Task ProcessTemplateDirectoryAsync(
        string sourcePath,
        string targetPath,
        Dictionary<string, string> variables,
        StatusContext ctx,
        string? prefixFolder)
    {
        foreach (var file in Directory.GetFiles(sourcePath, "*.template", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourcePath, file);
            var targetRelativePath = relativePath.Replace(".template", "");

            // Skip legacy root-level PaginationDto; use Dtos/PaginationDto.cs instead
            if (string.Equals(relativePath, "PaginationDto.cs.template", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Apply variable substitution to the path (e.g., {{ProjectName}}.sln)
            targetRelativePath = TemplateEngine.Process(targetRelativePath, variables);

            // If requested, place most content under a 'src' folder; keep root items and tests at root/test
            if (!string.IsNullOrEmpty(prefixFolder))
            {
                var relLower = relativePath.Replace('\\','/').ToLowerInvariant();
                var isTest = relLower.StartsWith("test/");
                var isSln = targetRelativePath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase);
                var isRootFile = string.Equals(targetRelativePath, ".gitignore", StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(targetRelativePath, "nuget.config", StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(targetRelativePath, "README.md", StringComparison.OrdinalIgnoreCase);
                if (!isTest && !isSln && !isRootFile)
                {
                    targetRelativePath = Path.Combine(prefixFolder!, targetRelativePath);
                }
            }

            // Normalize csproj filenames: Project.*.csproj => {ProjectName}.*.csproj
            if (targetRelativePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                var fileNameOnly = Path.GetFileName(targetRelativePath);
                if (fileNameOnly.StartsWith("Project.", StringComparison.OrdinalIgnoreCase))
                {
                    var newFileName = fileNameOnly.Replace("Project.", variables["ProjectName"] + ".");
                    targetRelativePath = Path.Combine(Path.GetDirectoryName(targetRelativePath) ?? string.Empty, newFileName);
                }
                else if (string.Equals(fileNameOnly, "Project.csproj", StringComparison.OrdinalIgnoreCase))
                {
                    var newFileName = $"{variables["ProjectName"]}.csproj";
                    targetRelativePath = Path.Combine(Path.GetDirectoryName(targetRelativePath) ?? string.Empty, newFileName);
                }
            }

            var targetFile = Path.Combine(targetPath, targetRelativePath);

            ctx.Status($"Creating {targetRelativePath}...");
            
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
    
    private static async Task CreateLocalNugetConfigAsync(string projectPath, StatusContext ctx)
    {
        ctx.Status("Creating nuget.config for local feed...");
        
        // Use relative path that works for testApps within swap repo
        // From testApps/ProjectName/ → ../../.nuget/local
        var relativeLocalFeedPath = "../../.nuget/local";
        
        // Verify the path exists by resolving it
        var absoluteCheckPath = Path.GetFullPath(Path.Combine(projectPath, relativeLocalFeedPath));
        if (!Directory.Exists(absoluteCheckPath))
        {
            throw new DirectoryNotFoundException(
                $"Local NuGet feed not found at: {absoluteCheckPath}\n" +
                $"The --local-nuget flag is intended for development within the Swap repository.\n" +
                $"Run pack-local.ps1 or pack-local.sh first to create local packages.");
        }
        
        var nugetConfig = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <clear />
    <add key=""local"" value=""{relativeLocalFeedPath}"" />
    <add key=""nuget.org"" value=""https://api.nuget.org/v3/index.json"" />
  </packageSources>
</configuration>";
        
        var nugetConfigPath = Path.Combine(projectPath, "nuget.config");
        await File.WriteAllTextAsync(nugetConfigPath, nugetConfig);
    }
}

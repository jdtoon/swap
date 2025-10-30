using System.CommandLine;
using System.CommandLine.Invocation;
using System.Xml.Linq;
using Spectre.Console;

namespace Swap.CLI.Commands;

public static class GenerateAuthCommand
{
    public static Command Create()
    {
        var command = new Command("auth", "Generate authentication system with ASP.NET Core Identity");
        command.AddAlias("a");
        
        var dryRunOption = new Option<bool>(
            "--dry-run",
            description: "Preview what would be generated without writing files");
        var forceOption = new Option<bool>(
            "--force",
            description: "Overwrite existing files without prompting");
        var projectOption = new Option<string?>(
            aliases: new[] { "--project", "-p" },
            description: "Path to project directory (default: current directory)");

        var noMigrationsOption = new Option<bool>(
            aliases: new[] { "--no-migrations" },
            description: "Skip automatic migration creation (you'll need to create migrations manually)"
        );
        
        command.AddOption(dryRunOption);
        command.AddOption(forceOption);
        command.AddOption(projectOption);
        command.AddOption(noMigrationsOption);
        
        command.SetHandler(async (InvocationContext context) =>
        {
            var dryRun = context.ParseResult.GetValueForOption(dryRunOption);
            var force = context.ParseResult.GetValueForOption(forceOption);
            var project = context.ParseResult.GetValueForOption(projectOption);
            var noMigrations = context.ParseResult.GetValueForOption(noMigrationsOption);
            context.ExitCode = await ExecuteAsync(dryRun, force, project, noMigrations);
        });
        
        return command;
    }
    
    private static async Task<int> ExecuteAsync(bool dryRun, bool force, string? projectPath, bool noMigrations)
    {
        // Resolve project directory
        var workingDir = string.IsNullOrWhiteSpace(projectPath) 
            ? Directory.GetCurrentDirectory() 
            : Path.GetFullPath(projectPath);
        
        if (!Directory.Exists(workingDir))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Project directory not found: {workingDir}");
            return 1;
        }
        
        // Check if we're in a project directory
        var projectFiles = Directory.GetFiles(workingDir, "*.csproj");
        if (projectFiles.Length == 0)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] No .csproj file found in {workingDir}. Run this command from your project root.");
            return 1;
        }
        
        var projectFile = projectFiles[0];
        var projectName = Path.GetFileNameWithoutExtension(projectFile);
        
        AnsiConsole.MarkupLine($"[bold cyan]{(dryRun ? "Preview" : "Generating")} authentication system[/]");
        AnsiConsole.MarkupLine($"[dim]Project:[/] {projectName}");
        AnsiConsole.WriteLine();
        
        try
        {
            // Define files to be generated
            var filesToGenerate = new[]
            {
                "Models/ApplicationUser.cs",
                "ViewModels/LoginViewModel.cs",
                "ViewModels/RegisterViewModel.cs",
                "ViewModels/ForgotPasswordViewModel.cs",
                "ViewModels/ResetPasswordViewModel.cs",
                "Controllers/AuthController.cs",
                "Views/Auth/Login.cshtml",
                "Views/Auth/Register.cshtml",
                "Views/Auth/ForgotPassword.cshtml",
                "Views/Auth/ForgotPasswordConfirmation.cshtml",
                "Views/Auth/ResetPassword.cshtml",
                "Views/Auth/ResetPasswordConfirmation.cshtml",
                "Views/Auth/AccessDenied.cshtml",
                "Views/Shared/_LoginPartial.cshtml"
            };
            
            // Check for existing files
            if (!force && !dryRun)
            {
                var existingFiles = filesToGenerate
                    .Where(f => File.Exists(Path.Combine(workingDir, f)))
                    .ToList();
                
                if (existingFiles.Any())
                {
                    AnsiConsole.MarkupLine("[yellow]⚠ Warning:[/] The following files already exist:");
                    foreach (var file in existingFiles)
                    {
                        AnsiConsole.MarkupLine($"  [dim]•[/] {file}");
                    }
                    AnsiConsole.WriteLine();
                    
                    if (!AnsiConsole.Confirm("Do you want to overwrite them?", false))
                    {
                        AnsiConsole.MarkupLine("[yellow]ℹ[/] Operation cancelled by user");
                        return 0;
                    }
                }
            }
            
            // Handle dry-run mode
            if (dryRun)
            {
                AnsiConsole.MarkupLine("[bold]Files that would be generated:[/]");
                foreach (var file in filesToGenerate)
                {
                    AnsiConsole.MarkupLine($"  [green]+[/] {file}");
                }
                AnsiConsole.WriteLine();
                
                AnsiConsole.MarkupLine("[bold]Package references that would be added:[/]");
                AnsiConsole.MarkupLine("  [green]+[/] Microsoft.AspNetCore.Identity.EntityFrameworkCore");
                AnsiConsole.WriteLine();
                
                AnsiConsole.MarkupLine("[bold]Configuration changes required:[/]");
                AnsiConsole.MarkupLine("  [yellow]~[/] Program.cs (Identity configuration)");
                AnsiConsole.MarkupLine("  [yellow]~[/] ApplicationDbContext (inherit from IdentityDbContext)");
                AnsiConsole.WriteLine();
                
                AnsiConsole.MarkupLine("[yellow]ℹ[/] Dry-run mode - no files were modified");
                return 0;
            }
            
            // Generate auth files
            await GenerateAuthFilesAsync(workingDir, projectName, filesToGenerate);
            
            // Add package reference
            await AddIdentityPackageAsync(projectFile);
            
            // Auto-configure the application
            await ConfigureDbContextAsync(workingDir, projectName);
            await ConfigureProgramCsAsync(workingDir, projectName);
            await ConfigureLayoutAsync(workingDir);
            
            // Auto-create migration for Identity (rigid: build first, no DB update)
            if (!noMigrations)
            {
                await TryCreateIdentityMigrationAsync(workingDir);
            }
            else
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[yellow]ℹ[/] Migration creation skipped (--no-migrations flag)");
                AnsiConsole.MarkupLine("[dim]Create migration manually:[/] [cyan]dotnet ef migrations add AddIdentity[/]");
            }

            // Show success and next steps
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[green]✓[/] Authentication system generated and configured successfully!");
            AnsiConsole.WriteLine();
            
            AnsiConsole.MarkupLine("[bold]Generated files:[/]");
            foreach (var file in filesToGenerate)
            {
                AnsiConsole.MarkupLine($"  [green]+[/] {file}");
            }
            AnsiConsole.WriteLine();
            
            AnsiConsole.MarkupLine("[bold]Configured files:[/]");
            AnsiConsole.MarkupLine("  [yellow]~[/] Data/AppDbContext.cs (now inherits IdentityDbContext)");
            AnsiConsole.MarkupLine("  [yellow]~[/] Program.cs (added Identity configuration)");
            AnsiConsole.MarkupLine("  [yellow]~[/] Views/Shared/_Layout.cshtml (added login partial)");
            AnsiConsole.WriteLine();
            
            ShowNextSteps(workingDir);
            
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }
    
    private static async Task GenerateAuthFilesAsync(string workingDir, string projectName, string[] files)
    {
        var templatesDir = Path.Combine(AppContext.BaseDirectory, "templates");
        var authTemplatesDir = Path.Combine(templatesDir, "generate", "auth");
        
        if (!Directory.Exists(authTemplatesDir))
        {
            throw new InvalidOperationException($"Auth templates directory not found at: {authTemplatesDir}");
        }
        
        await AnsiConsole.Status()
            .StartAsync("Generating auth files...", async ctx =>
            {
                foreach (var file in files)
                {
                    ctx.Status($"Generating {file}...");
                    
                    var templateFile = Path.Combine(authTemplatesDir, file + ".template");
                    var targetFile = Path.Combine(workingDir, file);
                    
                    if (!File.Exists(templateFile))
                    {
                        throw new FileNotFoundException($"Template not found: {templateFile}");
                    }
                    
                    // Read template
                    var content = await File.ReadAllTextAsync(templateFile);
                    
                    // Replace placeholder
                    content = content.Replace("{{ProjectName}}", projectName);
                    
                    // Ensure directory exists
                    var targetDir = Path.GetDirectoryName(targetFile);
                    if (!string.IsNullOrEmpty(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }
                    
                    // Write file
                    await File.WriteAllTextAsync(targetFile, content);
                }
            });
    }
    
    private static async Task AddIdentityPackageAsync(string projectFile)
    {
        await AnsiConsole.Status()
            .StartAsync("Adding Identity package reference...", async ctx =>
            {
                var doc = XDocument.Load(projectFile);
                var root = doc.Root;
                
                if (root == null) return;
                
                // Check if package already exists
                var existingPackage = root.Descendants("PackageReference")
                    .FirstOrDefault(p => p.Attribute("Include")?.Value == "Microsoft.AspNetCore.Identity.EntityFrameworkCore");
                
                if (existingPackage != null)
                {
                    AnsiConsole.MarkupLine("[dim]Identity package already referenced[/]");
                    return;
                }
                
                // Find or create ItemGroup
                var itemGroup = root.Elements("ItemGroup")
                    .FirstOrDefault(ig => ig.Elements("PackageReference").Any());
                
                if (itemGroup == null)
                {
                    itemGroup = new XElement("ItemGroup");
                    root.Add(itemGroup);
                }
                
                // Add package reference (pin to 9.0.10)
                itemGroup.Add(new XElement("PackageReference",
                    new XAttribute("Include", "Microsoft.AspNetCore.Identity.EntityFrameworkCore"),
                    new XAttribute("Version", "9.0.10")));
                
                doc.Save(projectFile);
                AnsiConsole.MarkupLine("[green]✓[/] Added Identity package reference");
            });
    }
    
    private static async Task ConfigureDbContextAsync(string workingDir, string projectName)
    {
        var dbContextPath = Path.Combine(workingDir, "Data", "AppDbContext.cs");
        if (!File.Exists(dbContextPath))
        {
            AnsiConsole.MarkupLine("[yellow]⚠[/] AppDbContext.cs not found, skipping auto-configuration");
            return;
        }

        var content = await File.ReadAllTextAsync(dbContextPath);
        
        // Check if already configured
        if (content.Contains("IdentityDbContext<ApplicationUser>"))
        {
            AnsiConsole.MarkupLine("[dim]DbContext already configured for Identity[/]");
            return;
        }

        // Add using statements if not present
        if (!content.Contains("using Microsoft.AspNetCore.Identity.EntityFrameworkCore;"))
        {
            var usingIndex = content.IndexOf("using Microsoft.EntityFrameworkCore;");
            if (usingIndex >= 0)
            {
                content = content.Insert(usingIndex, "using Microsoft.AspNetCore.Identity.EntityFrameworkCore;\n");
            }
        }

        // Replace DbContext with IdentityDbContext<ApplicationUser>
        content = content.Replace(
            "public class AppDbContext : DbContext",
            "public class AppDbContext : IdentityDbContext<ApplicationUser>");

        await File.WriteAllTextAsync(dbContextPath, content);
        AnsiConsole.MarkupLine("[green]✓[/] Updated AppDbContext to inherit from IdentityDbContext");
    }

    private static async Task ConfigureProgramCsAsync(string workingDir, string projectName)
    {
        var programPath = Path.Combine(workingDir, "Program.cs");
        if (!File.Exists(programPath))
        {
            AnsiConsole.MarkupLine("[yellow]⚠[/] Program.cs not found, skipping auto-configuration");
            return;
        }

        var content = await File.ReadAllTextAsync(programPath);
        
        // Check if already configured
        if (content.Contains("AddIdentity<ApplicationUser"))
        {
            AnsiConsole.MarkupLine("[dim]Program.cs already configured for Identity[/]");
            return;
        }

        // Add using statements
        if (!content.Contains("using Microsoft.AspNetCore.Identity;"))
        {
            var usingIndex = content.IndexOf("using Microsoft.EntityFrameworkCore;");
            if (usingIndex >= 0)
            {
                var insertPoint = content.IndexOf('\n', usingIndex) + 1;
                content = content.Insert(insertPoint, $"using Microsoft.AspNetCore.Identity;\nusing {projectName}.Models;\n");
            }
        }

        // Find where to insert Identity configuration (after AddDbContext)
        var dbContextIndex = content.IndexOf("builder.Services.AddDbContext");
        if (dbContextIndex >= 0)
        {
            // Find the end of AddDbContext statement (look for the closing );)
            var searchStart = dbContextIndex;
            var parenCount = 0;
            var foundStart = false;
            var insertPoint = -1;

            for (int i = searchStart; i < content.Length; i++)
            {
                if (content[i] == '(')
                {
                    parenCount++;
                    foundStart = true;
                }
                else if (content[i] == ')')
                {
                    parenCount--;
                    if (foundStart && parenCount == 0)
                    {
                        // Find the semicolon after this closing paren
                        insertPoint = content.IndexOf(';', i) + 1;
                        break;
                    }
                }
            }

            if (insertPoint > 0)
            {
                // Find the end of the line
                var lineEnd = content.IndexOf('\n', insertPoint);
                if (lineEnd > 0)
                {
                    var identityConfig = $@"

// Add Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
}})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Configure application cookie
builder.Services.ConfigureApplicationCookie(options =>
{{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.LoginPath = ""/auth/login"";
    options.LogoutPath = ""/auth/logout"";
    options.AccessDeniedPath = ""/auth/access-denied"";
    options.SlidingExpiration = true;
}});
";
                    content = content.Insert(lineEnd + 1, identityConfig);
                }
            }
        }

        // Add UseAuthentication() before UseAuthorization()
        var useAuthzIndex = content.IndexOf("app.UseAuthorization();");
        if (useAuthzIndex >= 0 && !content.Contains("app.UseAuthentication();"))
        {
            content = content.Insert(useAuthzIndex, "app.UseAuthentication();\n");
        }

        await File.WriteAllTextAsync(programPath, content);
        AnsiConsole.MarkupLine("[green]✓[/] Configured Identity in Program.cs");
    }

    private static async Task ConfigureLayoutAsync(string workingDir)
    {
        var layoutPath = Path.Combine(workingDir, "Views", "Shared", "_Layout.cshtml");
        if (!File.Exists(layoutPath))
        {
            AnsiConsole.MarkupLine("[yellow]⚠[/] _Layout.cshtml not found, skipping auto-configuration");
            return;
        }

        var content = await File.ReadAllTextAsync(layoutPath);
        
        // Check if already configured
        if (content.Contains("_LoginPartial"))
        {
            AnsiConsole.MarkupLine("[dim]Layout already includes login partial[/]");
            return;
        }

        // Find the navbar div and add the login partial
        var navbarIndex = content.IndexOf("<div class=\"flex-none\">");
        if (navbarIndex >= 0)
        {
            // Find the closing </div> of flex-none
            var searchStart = navbarIndex;
            var divCount = 1;
            var insertPoint = -1;

            for (int i = searchStart + "<div class=\"flex-none\">".Length; i < content.Length - 6; i++)
            {
                if (content.Substring(i, 5) == "<div ")
                {
                    divCount++;
                }
                else if (content.Substring(i, 6) == "</div>")
                {
                    divCount--;
                    if (divCount == 0)
                    {
                        insertPoint = i;
                        break;
                    }
                }
            }

            if (insertPoint > 0)
            {
                var loginPartial = "\n                    <div class=\"ml-4\">\n                        <partial name=\"_LoginPartial\" />\n                    </div>\n                ";
                content = content.Insert(insertPoint, loginPartial);
            }
        }

        await File.WriteAllTextAsync(layoutPath, content);
        AnsiConsole.MarkupLine("[green]✓[/] Added login partial to layout");

        // Ensure _ValidationScriptsPartial exists to prevent runtime errors in auth views
        var validationPartialPath = Path.Combine(workingDir, "Views", "Shared", "_ValidationScriptsPartial.cshtml");
        if (!File.Exists(validationPartialPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(validationPartialPath)!);
            // Minimal placeholder partial; projects may add scripts as needed
            var partialContent = "@* Validation scripts partial (optional). Add client-side validation scripts here if used). *@\n";
            await File.WriteAllTextAsync(validationPartialPath, partialContent);
            AnsiConsole.MarkupLine("[green]✓[/] Added missing [grey]_ValidationScriptsPartial.cshtml[/] to Views/Shared");
        }
    }

    private static async Task TryCreateIdentityMigrationAsync(string workingDir)
    {
        try
        {
            var build = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "build",
                WorkingDirectory = workingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using (var buildProc = System.Diagnostics.Process.Start(build))
            {
                if (buildProc != null)
                {
                    await buildProc.WaitForExitAsync();
                    if (buildProc.ExitCode != 0)
                    {
                        AnsiConsole.MarkupLine("[red]✗ Build failed before migration creation[/]");
                        var err = await buildProc.StandardError.ReadToEndAsync();
                        var outp = await buildProc.StandardOutput.ReadToEndAsync();
                        if (!string.IsNullOrWhiteSpace(outp)) AnsiConsole.WriteLine(outp);
                        if (!string.IsNullOrWhiteSpace(err)) AnsiConsole.WriteLine(err);
                        return;
                    }
                }
            }

            var dbContexts = FindDbContextCandidates(workingDir);
            string? contextName = null;
            if (dbContexts.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]ℹ[/] No DbContext found. Skipping automatic migration creation.");
                return;
            }
            else if (dbContexts.Count == 1)
            {
                contextName = dbContexts[0].className;
            }
            else
            {
                var choices = dbContexts.Select(d => $"{d.className} ({d.relativePath})").ToList();
                var selected = AnsiConsole.Prompt(
                    new Spectre.Console.SelectionPrompt<string>()
                        .Title("Multiple DbContexts found. Select one for the migration:")
                        .AddChoices(choices)
                );
                var idx = choices.IndexOf(selected);
                if (idx >= 0) contextName = dbContexts[idx].className;
            }

            var args = contextName != null
                ? $"ef migrations add AddIdentity --context {contextName}"
                : $"ef migrations add AddIdentity";

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = args,
                WorkingDirectory = workingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            AnsiConsole.MarkupLine($"[cyan]Creating migration:[/] AddIdentity {(contextName != null ? $"(Context: {contextName})" : string.Empty)}");
            using var proc = System.Diagnostics.Process.Start(psi);
            if (proc != null)
            {
                await proc.WaitForExitAsync();
                if (proc.ExitCode == 0)
                {
                    AnsiConsole.MarkupLine("[green]✓[/] Migration created");
                }
                else
                {
                    AnsiConsole.MarkupLine("[yellow]⚠[/] Failed to create migration automatically. You can run it manually:");
                    AnsiConsole.MarkupLine($"    dotnet ef migrations add AddIdentity{(contextName != null ? $" --context {contextName}" : string.Empty)}");
                }
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]⚠[/] Skipped automatic migration creation: {ex.Message}");
        }
    }

    private static List<(string className, string relativePath)> FindDbContextCandidates(string workingDir)
    {
        var results = new List<(string, string)>();
        var dataDir = Path.Combine(workingDir, "Data");
        if (!Directory.Exists(dataDir)) return results;

        foreach (var file in Directory.GetFiles(dataDir, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            var lines = text.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains(": DbContext") || line.Contains("IdentityDbContext"))
                {
                    var idx = line.IndexOf("class ");
                    if (idx >= 0)
                    {
                        var rest = line.Substring(idx + 6).Trim();
                        var name = rest.Split(new[]{' ', ':'}, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            var rel = Path.GetRelativePath(workingDir, file);
                            results.Add((name!, rel));
                        }
                    }
                }
            }
        }

        return results
            .GroupBy(r => r.Item1)
            .Select(g => g.First())
            .ToList();
    }

    private static void ShowNextSteps(string workingDir)
    {
        AnsiConsole.MarkupLine("[bold cyan]Next steps:[/]");
        AnsiConsole.WriteLine();
        
        // Step 1: Create and apply migration
        AnsiConsole.MarkupLine("[bold]1. Create and apply Identity migration:[/]");
        AnsiConsole.WriteLine();
        var migrationDir = Path.Combine(workingDir, "Migrations");
        var migrationCommand = Directory.Exists(migrationDir)
            ? "dotnet ef migrations add AddIdentity"
            : "dotnet ef migrations add InitialCreate";
        
        AnsiConsole.MarkupLine($"   [cyan]{migrationCommand}[/]");
        AnsiConsole.MarkupLine("   [cyan]dotnet ef database update[/]");
        AnsiConsole.WriteLine();
        
        // Step 2: Test the auth system
        AnsiConsole.MarkupLine("[bold]2. Run your application:[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("   [cyan]dotnet run[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("   Then visit [cyan]/auth/register[/] to create your first account.");
        AnsiConsole.WriteLine();
        
        // Optional: Email service
        AnsiConsole.MarkupLine("[bold]3. (Optional) Configure email service:[/]");
        AnsiConsole.MarkupLine("   Password reset tokens are currently logged to console.");
        AnsiConsole.MarkupLine("   For production, implement an email service and update AuthController.");
    }
}


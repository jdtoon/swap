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
        
        command.AddOption(dryRunOption);
        command.AddOption(forceOption);
        command.AddOption(projectOption);
        
        command.SetHandler(async (InvocationContext context) =>
        {
            var dryRun = context.ParseResult.GetValueForOption(dryRunOption);
            var force = context.ParseResult.GetValueForOption(forceOption);
            var project = context.ParseResult.GetValueForOption(projectOption);
            context.ExitCode = await ExecuteAsync(dryRun, force, project);
        });
        
        return command;
    }
    
    private static async Task<int> ExecuteAsync(bool dryRun, bool force, string? projectPath)
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
            
            // Show success and instructions
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[green]✓[/] Authentication system generated successfully!");
            AnsiConsole.WriteLine();
            
            AnsiConsole.MarkupLine("[bold]Generated files:[/]");
            foreach (var file in filesToGenerate)
            {
                AnsiConsole.MarkupLine($"  [green]+[/] {file}");
            }
            AnsiConsole.WriteLine();
            
            ShowManualSteps(workingDir, projectName);
            
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
                
                // Add package reference
                itemGroup.Add(new XElement("PackageReference",
                    new XAttribute("Include", "Microsoft.AspNetCore.Identity.EntityFrameworkCore"),
                    new XAttribute("Version", "9.0.0")));
                
                doc.Save(projectFile);
                AnsiConsole.MarkupLine("[green]✓[/] Added Identity package reference");
            });
    }
    
    private static void ShowManualSteps(string workingDir, string projectName)
    {
        AnsiConsole.MarkupLine("[bold yellow]⚠ Manual configuration required:[/]");
        AnsiConsole.WriteLine();
        
        // Step 1: Update DbContext
        AnsiConsole.MarkupLine("[bold]1. Update your DbContext:[/]");
        AnsiConsole.MarkupLine("   Change your ApplicationDbContext to inherit from IdentityDbContext:");
        AnsiConsole.WriteLine();
        
        var dbContextCode = $@"using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using {projectName}.Models;

namespace {projectName}.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) {{ }}
    
    // Your existing DbSets here
}}";
        
        var dbContextPanel = new Panel(Markup.Escape(dbContextCode))
        {
            Header = new PanelHeader("Data/ApplicationDbContext.cs"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Blue)
        };
        AnsiConsole.Write(dbContextPanel);
        AnsiConsole.WriteLine();
        
        // Step 2: Update Program.cs
        AnsiConsole.MarkupLine("[bold]2. Configure Identity in Program.cs:[/]");
        AnsiConsole.MarkupLine("   Add Identity services and configure options:");
        AnsiConsole.WriteLine();
        
        var programCode = $@"// Add Identity
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
.AddEntityFrameworkStores<ApplicationDbContext>()
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

// ... existing code ...

// Add these BEFORE app.MapControllerRoute()
app.UseAuthentication();
app.UseAuthorization();";
        
        var programPanel = new Panel(Markup.Escape(programCode))
        {
            Header = new PanelHeader("Program.cs"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Blue)
        };
        AnsiConsole.Write(programPanel);
        AnsiConsole.WriteLine();
        
        // Step 3: Add login partial to layout
        AnsiConsole.MarkupLine("[bold]3. Add login partial to your layout:[/]");
        AnsiConsole.MarkupLine("   In Views/Shared/_Layout.cshtml, add the partial view:");
        AnsiConsole.WriteLine();
        
        var layoutCode = @"<!-- In your navigation or header -->
<partial name=""_LoginPartial"" />";
        
        var layoutPanel = new Panel(Markup.Escape(layoutCode))
        {
            Header = new PanelHeader("Views/Shared/_Layout.cshtml"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Blue)
        };
        AnsiConsole.Write(layoutPanel);
        AnsiConsole.WriteLine();
        
        // Step 4: Create migration
        AnsiConsole.MarkupLine("[bold]4. Create and apply Identity migration:[/]");
        AnsiConsole.WriteLine();
        var migrationDir = Path.Combine(workingDir, "Migrations");
        var migrationCommand = Directory.Exists(migrationDir)
            ? "dotnet ef migrations add AddIdentity"
            : "dotnet ef migrations add InitialCreate";
        
        AnsiConsole.MarkupLine($"   [cyan]{migrationCommand}[/]");
        AnsiConsole.MarkupLine("   [cyan]dotnet ef database update[/]");
        AnsiConsole.WriteLine();
        
        // Step 5: Optional email service
        AnsiConsole.MarkupLine("[bold]5. (Optional) Configure email service for password reset:[/]");
        AnsiConsole.MarkupLine("   Currently, password reset tokens are logged to console.");
        AnsiConsole.MarkupLine("   For production, implement an email service and update AuthController.");
        AnsiConsole.WriteLine();
        
        AnsiConsole.MarkupLine("[bold green]✓[/] Authentication system is ready to use!");
        AnsiConsole.MarkupLine("   Visit [cyan]/auth/register[/] to create your first account.");
    }
}

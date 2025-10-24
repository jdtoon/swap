using System.Xml.Linq;
using NetMX.CLI.Infrastructure;
using NetMX.CLI.Models;

namespace NetMX.CLI.Commands;

/// <summary>
/// Command to add a NetMX module to the current solution
/// </summary>
public class AddModuleCommand
{
    private readonly string _moduleName;
    private readonly string? _source;
    private readonly bool _skipMigration;

    public AddModuleCommand(string moduleName, string? source = null, bool skipMigration = false)
    {
        _moduleName = moduleName;
        _source = source;
        _skipMigration = skipMigration;
    }

    public async Task<int> ExecuteAsync()
    {
        try
        {
            ConsoleHelper.WriteHeader($"Adding {_moduleName} Module");

            // Step 1: Find solution file
            var solutionPath = FindSolutionFile();
            if (solutionPath == null)
            {
                ConsoleHelper.WriteError("No .sln file found in current directory or parents");
                return 1;
            }

            var solutionDir = Path.GetDirectoryName(solutionPath)!;
            ConsoleHelper.WriteStep(1, $"Found solution: {Path.GetFileName(solutionPath)}");

            // Step 2: Find web project
            var webProjectPath = FindWebProject(solutionDir);
            if (webProjectPath == null)
            {
                ConsoleHelper.WriteError("Could not find web project (*.Web.csproj)");
                return 1;
            }

            ConsoleHelper.WriteStep(2, $"Found web project: {Path.GetFileName(webProjectPath)}");

            // Step 3: Locate module
            var modulePath = LocateModule(solutionDir);
            if (modulePath == null)
            {
                ConsoleHelper.WriteError($"Module '{_moduleName}' not found");
                ConsoleHelper.WriteInfo($"  Searched in: modules/{_moduleName}/");
                return 1;
            }

            ConsoleHelper.WriteStep(3, $"Found module at: {Path.GetRelativePath(solutionDir, modulePath)}");

            // Step 4: Load module descriptor
            var descriptorPath = Path.Combine(modulePath, "module.json");
            var descriptor = ModuleDescriptor.LoadFrom(descriptorPath);
            
            if (descriptor == null)
            {
                ConsoleHelper.WriteWarning($"No module.json found, will add project references manually");
            }
            else
            {
                ConsoleHelper.WriteStep(4, $"Loaded module descriptor: {descriptor.Name} v{descriptor.Version}");
            }

            // Step 5: Copy module source code to solution
            ConsoleHelper.WriteStep(5, $"Copying module source code");
            
            var modulesDir = Path.Combine(solutionDir, "src", $"{Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(webProjectPath))}.Modules");
            var targetModulePath = Path.Combine(modulesDir, _moduleName);
            
            if (Directory.Exists(targetModulePath))
            {
                ConsoleHelper.WriteWarning($"Module already exists at {targetModulePath}");
                ConsoleHelper.WriteInfo($"Use --force to overwrite");
                return 1;
            }
            
            Directory.CreateDirectory(modulesDir);
            CopyDirectory(modulePath, targetModulePath);
            
            ConsoleHelper.WriteSuccess($"  ✓ Copied to {Path.GetRelativePath(solutionDir, targetModulePath)}");
            
            // Step 6: Add module projects to solution
            var projectsToAdd = descriptor?.Projects ?? DiscoverModuleProjects(targetModulePath);
            
            ConsoleHelper.WriteStep(6, $"Adding {projectsToAdd.Count} project(s) to solution");
            
            foreach (var project in projectsToAdd)
            {
                var projectPath = Path.Combine(targetModulePath, project.Path);
                if (File.Exists(projectPath))
                {
                    ConsoleHelper.WriteProgress($"  Adding {project.Name}");
                    
                    // Add to solution
                    AddProjectToSolution(solutionPath, projectPath);
                    
                    // Add reference from Web project to module's Web project
                    if (project.Path.Contains(".Web"))
                    {
                        AddProjectReference(webProjectPath, projectPath);
                    }
                    
                    ConsoleHelper.WriteProgressDone();
                }
            }

            // Step 7: Update Program.cs (if module has services)
            if (descriptor?.Services != null && descriptor.Services.Any())
            {
                ConsoleHelper.WriteStep(7, "Updating Program.cs to register module");
                UpdateProgramCs(webProjectPath, descriptor);
            }

            // Step 8: Run migrations (if enabled)
            if (!_skipMigration && descriptor?.Migrations?.Enabled == true)
            {
                ConsoleHelper.WriteStep(8, "Running database migrations");
                await RunMigrationsAsync(webProjectPath, descriptor);
            }

            ConsoleHelper.WriteSuccess($"Module '{_moduleName}' added successfully!");
            ConsoleHelper.WriteInfo("Next steps:");
            ConsoleHelper.WriteInfo($"  1. Run 'dotnet build' to verify the solution builds");
            ConsoleHelper.WriteInfo($"  2. Run 'dotnet run' to start the application");
            
            if (descriptor?.Routes != null && descriptor.Routes.Any())
            {
                var firstRoute = descriptor.Routes.First();
                ConsoleHelper.WriteInfo($"  3. Navigate to {firstRoute.Pattern} to see the module");
            }

            return 0;
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"Failed to add module: {ex.Message}");
            if (ex.InnerException != null)
            {
                ConsoleHelper.WriteError($"  Details: {ex.InnerException.Message}");
            }
            return 1;
        }
    }

    private string? FindSolutionFile()
    {
        var currentDir = Directory.GetCurrentDirectory();
        while (currentDir != null)
        {
            var slnFiles = Directory.GetFiles(currentDir, "*.sln");
            if (slnFiles.Length > 0)
            {
                return slnFiles[0];
            }

            var parent = Directory.GetParent(currentDir);
            currentDir = parent?.FullName;
        }

        return null;
    }

    private string? FindWebProject(string solutionDir)
    {
        var searchPaths = new[]
        {
            Path.Combine(solutionDir, "src"),
            Path.Combine(solutionDir, "app"),
            solutionDir
        };

        foreach (var searchPath in searchPaths)
        {
            if (!Directory.Exists(searchPath)) continue;

            var webProjects = Directory.GetFiles(searchPath, "*.Web.csproj", SearchOption.AllDirectories);
            if (webProjects.Length > 0)
            {
                return webProjects[0];
            }
        }

        return null;
    }

    private string? LocateModule(string solutionDir)
    {
        // Check if custom source is provided
        if (!string.IsNullOrEmpty(_source) && Directory.Exists(_source))
        {
            return _source;
        }

        // Check in modules directory
        var modulePath = Path.Combine(solutionDir, "modules", _moduleName);
        if (Directory.Exists(modulePath))
        {
            return modulePath;
        }

        // Check in parent directory (for development scenarios)
        var parentModulePath = Path.Combine(Directory.GetParent(solutionDir)?.FullName ?? "", "modules", _moduleName);
        if (Directory.Exists(parentModulePath))
        {
            return parentModulePath;
        }

        return null;
    }

    private List<ModuleProject> DiscoverModuleProjects(string modulePath)
    {
        var projects = new List<ModuleProject>();
        var csprojFiles = Directory.GetFiles(modulePath, "*.csproj", SearchOption.AllDirectories);

        foreach (var csproj in csprojFiles)
        {
            var projectName = Path.GetFileNameWithoutExtension(csproj);
            var relativePath = Path.GetRelativePath(modulePath, csproj);

            projects.Add(new ModuleProject
            {
                Name = projectName,
                Path = relativePath,
                Type = DetermineProjectType(projectName)
            });
        }

        return projects;
    }

    private string DetermineProjectType(string projectName)
    {
        if (projectName.EndsWith(".Core")) return "core";
        if (projectName.EndsWith(".Contracts")) return "contracts";
        if (projectName.EndsWith(".Application")) return "application";
        if (projectName.EndsWith(".Web")) return "web";
        return "library";
    }

    private void AddProjectReference(string webProjectPath, string moduleProjectPath)
    {
        var doc = XDocument.Load(webProjectPath);
        var project = doc.Root!;

        // Check if reference already exists
        var existingRef = project.Descendants("ProjectReference")
            .FirstOrDefault(pr => pr.Attribute("Include")?.Value.Contains(Path.GetFileName(moduleProjectPath)) == true);

        if (existingRef != null)
        {
            return; // Already added
        }

        // Find or create ItemGroup for ProjectReferences
        var itemGroup = project.Elements("ItemGroup")
            .FirstOrDefault(ig => ig.Elements("ProjectReference").Any());

        if (itemGroup == null)
        {
            itemGroup = new XElement("ItemGroup");
            project.Add(itemGroup);
        }

        // Calculate relative path from web project to module project
        var webProjectDir = Path.GetDirectoryName(webProjectPath)!;
        var relativePath = Path.GetRelativePath(webProjectDir, moduleProjectPath);

        // Add project reference
        itemGroup.Add(new XElement("ProjectReference",
            new XAttribute("Include", relativePath)));

        doc.Save(webProjectPath);
    }

    private void UpdateProgramCs(string webProjectPath, ModuleDescriptor descriptor)
    {
        var webProjectDir = Path.GetDirectoryName(webProjectPath)!;
        var programCsPath = Path.Combine(webProjectDir, "Program.cs");

        if (!File.Exists(programCsPath))
        {
            ConsoleHelper.WriteWarning("Program.cs not found, skipping automatic module registration");
            return;
        }

        var programCs = File.ReadAllText(programCsPath);

        // Get module services
        var moduleServices = descriptor.Services!.Where(s => s.Type == "module").ToList();
        if (!moduleServices.Any())
        {
            ConsoleHelper.WriteInfo($"  No module services to register");
            return;
        }

        // Check if already registered
        var moduleClass = moduleServices.First().Class;
        if (programCs.Contains(moduleClass))
        {
            ConsoleHelper.WriteInfo($"  Module already registered in Program.cs");
            return;
        }

        // Find the builder.Services section and add module
        var searchText = "var builder = WebApplication.CreateBuilder(args);";
        if (programCs.Contains(searchText))
        {
            var insertionPoint = programCs.IndexOf(searchText) + searchText.Length;
            
            // Build registration code for all module services
            var registrationCode = new System.Text.StringBuilder();
            registrationCode.AppendLine();
            registrationCode.AppendLine($"// Add {descriptor.Name} Module");
            
            foreach (var service in moduleServices)
            {
                // Extract method name from class (e.g., NetMXIdentityWebModule -> AddIdentity)
                var className = service.Class.Split('.').Last();
                var methodName = className.Replace("NetMX", "").Replace("WebModule", "").Replace("Module", "");
                registrationCode.AppendLine($"// builder.Services.Add{methodName}();");
            }

            programCs = programCs.Insert(insertionPoint, registrationCode.ToString());
            File.WriteAllText(programCsPath, programCs);

            ConsoleHelper.WriteInfo($"  Added commented registration code to Program.cs");
            ConsoleHelper.WriteInfo($"  Uncomment the lines to enable the module");
        }
    }

    private async Task RunMigrationsAsync(string webProjectPath, ModuleDescriptor descriptor)
    {
        var webProjectDir = Path.GetDirectoryName(webProjectPath)!;
        var contextName = descriptor.Migrations!.GetContextName();
        
        if (string.IsNullOrEmpty(contextName))
        {
            ConsoleHelper.WriteWarning("  No DbContext specified in module.json migrations section");
            return;
        }

        try
        {
            // First, check if there are any migrations to apply
            var checkProcess = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"ef migrations list --context {contextName} --no-build",
                WorkingDirectory = webProjectDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (checkProcess != null)
            {
                var output = await checkProcess.StandardOutput.ReadToEndAsync();
                await checkProcess.WaitForExitAsync();
                
                if (checkProcess.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
                {
                    ConsoleHelper.WriteInfo($"  ✓ No migrations found for {contextName}");
                    return;
                }
            }

            // Now apply migrations
            var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"ef database update --context {contextName}",
                WorkingDirectory = webProjectDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process != null)
            {
                await process.WaitForExitAsync();
                
                if (process.ExitCode == 0)
                {
                    ConsoleHelper.WriteSuccess($"  ✓ Migrations applied successfully for {contextName}");
                }
                else
                {
                    var error = await process.StandardError.ReadToEndAsync();
                    ConsoleHelper.WriteWarning($"  ⚠ Migrations failed for {contextName}");
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        ConsoleHelper.WriteInfo($"    Error: {error.Split('\n')[0]}");
                    }
                    ConsoleHelper.WriteInfo($"    Run manually: dotnet ef database update --context {contextName}");
                }
            }
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteWarning($"  ⚠ Could not run migrations: {ex.Message}");
            ConsoleHelper.WriteInfo($"    Run manually: dotnet ef database update --context {contextName}");
        }
    }

    private void CopyDirectory(string sourceDir, string targetDir)
    {
        // Create target directory
        Directory.CreateDirectory(targetDir);

        // Copy all files
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var fileName = Path.GetFileName(file);
            var targetFile = Path.Combine(targetDir, fileName);
            File.Copy(file, targetFile, overwrite: true);
        }

        // Copy subdirectories recursively
        foreach (var subDir in Directory.GetDirectories(sourceDir))
        {
            var dirName = Path.GetFileName(subDir);
            
            // Skip bin, obj, .vs directories
            if (dirName.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                dirName.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
                dirName.Equals(".vs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var targetSubDir = Path.Combine(targetDir, dirName);
            CopyDirectory(subDir, targetSubDir);
        }
    }

    private void AddProjectToSolution(string solutionPath, string projectPath)
    {
        try
        {
            var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"sln \"{solutionPath}\" add \"{projectPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process != null)
            {
                process.WaitForExit();
                
                if (process.ExitCode != 0)
                {
                    var error = process.StandardError.ReadToEnd();
                    // Ignore "already in solution" errors
                    if (!error.Contains("already"))
                    {
                        ConsoleHelper.WriteWarning($"    ⚠ Could not add to solution: {error}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteWarning($"    ⚠ Could not add to solution: {ex.Message}");
        }
    }
}

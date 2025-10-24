using System.Text.RegularExpressions;
using NetMX.CLI.Infrastructure;

namespace NetMX.CLI.Commands;

/// <summary>
/// Command to create a new NetMX project from a template.
/// </summary>
public class NewCommand
{
    private readonly string _templateName;
    private readonly string _projectName;
    private readonly string _outputPath;

    public NewCommand(string templateName, string projectName, string? outputPath = null)
    {
        _templateName = templateName;
        _projectName = projectName;
        _outputPath = outputPath ?? Path.Combine(Directory.GetCurrentDirectory(), projectName);
    }

    public async Task<int> ExecuteAsync()
    {
        try
        {
            ConsoleHelper.WriteHeader($"Creating new {_templateName} project: {_projectName}");

            // Find the template directory
            var templateDir = FindTemplateDirectory(_templateName);
            if (templateDir == null)
            {
                ConsoleHelper.WriteError($"Template '{_templateName}' not found");
                ConsoleHelper.WriteInfo("Available templates: modular");
                return 1;
            }

            // Check if output directory already exists
            if (Directory.Exists(_outputPath))
            {
                ConsoleHelper.WriteError($"Directory already exists: {_outputPath}");
                ConsoleHelper.WriteInfo("Choose a different name or delete the existing directory");
                return 1;
            }

            // Step 1: Copy template files
            ConsoleHelper.WriteStep(1, "Copying template files");
            Directory.CreateDirectory(_outputPath);
            CopyDirectory(templateDir, _outputPath);
            ConsoleHelper.WriteSuccess($"  Template files copied to: {_outputPath}");

            // Step 2: Replace placeholders (NetMXApp → ProjectName)
            ConsoleHelper.WriteStep(2, "Updating project names and namespaces");
            await ReplacePlaceholdersAsync(_outputPath, _projectName);
            ConsoleHelper.WriteSuccess($"  Updated all references to: {_projectName}");

            // Step 3: Rename project files and directories
            ConsoleHelper.WriteStep(3, "Renaming project files");
            RenameProjectFiles(_outputPath, _projectName);
            ConsoleHelper.WriteSuccess("  Project structure updated");

            // Success - show template-specific info
            ConsoleHelper.WriteSuccess($"\n✨ Project '{_projectName}' created successfully!");
            ShowTemplateInfo(_templateName, _projectName, _outputPath);

            return 0;
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"Failed to create project: {ex.Message}");
            ConsoleHelper.WriteError($"Stack trace: {ex.StackTrace}");
            return 1;
        }
    }

    private string? FindTemplateDirectory(string templateName)
    {
        // Strategy 1: Check if templates are bundled with the CLI tool (installed globally)
        var assemblyLocation = typeof(NewCommand).Assembly.Location;
        var toolDirectory = Path.GetDirectoryName(assemblyLocation);
        if (toolDirectory != null)
        {
            // Try direct path first (templates bundled at root level)
            var directPath = Path.Combine(toolDirectory, templateName);
            if (Directory.Exists(directPath))
            {
                ConsoleHelper.WriteInfo($"  Using template: {directPath}");
                return directPath;
            }
            
            // Try templates subdirectory (alternative bundling structure)
            var templatesPath = Path.Combine(toolDirectory, "templates", templateName);
            if (Directory.Exists(templatesPath))
            {
                ConsoleHelper.WriteInfo($"  Using template: {templatesPath}");
                return templatesPath;
            }
        }
        
        // Strategy 2: Walk up from current directory (development scenario)
        var currentDir = Directory.GetCurrentDirectory();
        var maxDepth = 10; // Safety limit
        
        for (int i = 0; i < maxDepth; i++)
        {
            var templatesPath = Path.Combine(currentDir, "templates", templateName);
            if (Directory.Exists(templatesPath))
            {
                ConsoleHelper.WriteInfo($"  Using template: {templatesPath}");
                return templatesPath;
            }
            
            // Try parent directory
            var parentDir = Directory.GetParent(currentDir);
            if (parentDir == null)
                break;
                
            currentDir = parentDir.FullName;
        }
        
        // Strategy 3: Check if we're IN the netmx repository
        var repoRoot = FindRepositoryRoot();
        if (repoRoot != null)
        {
            var templatesPath = Path.Combine(repoRoot, "templates", templateName);
            if (Directory.Exists(templatesPath))
            {
                ConsoleHelper.WriteInfo($"  Using template: {templatesPath}");
                return templatesPath;
            }
        }

        return null;
    }
    
    private string? FindRepositoryRoot()
    {
        // Look for .git directory or framework/ folder (indicators of repo root)
        var currentDir = Directory.GetCurrentDirectory();
        var maxDepth = 10;
        
        for (int i = 0; i < maxDepth; i++)
        {
            if (Directory.Exists(Path.Combine(currentDir, ".git")) ||
                Directory.Exists(Path.Combine(currentDir, "framework")))
            {
                return currentDir;
            }
            
            var parentDir = Directory.GetParent(currentDir);
            if (parentDir == null)
                break;
                
            currentDir = parentDir.FullName;
        }
        
        return null;
    }

    private void CopyDirectory(string sourceDir, string destDir)
    {
        // Create destination directory
        Directory.CreateDirectory(destDir);

        // Copy files
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var fileName = Path.GetFileName(file);
            
            // Skip files we don't want to copy
            if (fileName == ".DS_Store" || fileName == "Thumbs.db")
                continue;

            var destFile = Path.Combine(destDir, fileName);
            File.Copy(file, destFile, overwrite: true);
        }

        // Copy subdirectories recursively
        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var dirName = Path.GetFileName(dir);
            
            // Skip directories we don't want to copy
            if (dirName == "bin" || dirName == "obj" || dirName == ".vs" || dirName == ".git")
                continue;

            var destSubDir = Path.Combine(destDir, dirName);
            CopyDirectory(dir, destSubDir);
        }
    }

    private async Task ReplacePlaceholdersAsync(string directory, string projectName)
    {
        // Files to process (text files only)
        var textExtensions = new[] { ".cs", ".csproj", ".sln", ".json", ".yml", ".yaml", ".md", ".cshtml", ".razor" };

        foreach (var file in Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories))
        {
            var extension = Path.GetExtension(file).ToLowerInvariant();
            
            // Skip binary files and specific directories
            if (!textExtensions.Contains(extension))
                continue;

            if (file.Contains("\\bin\\") || file.Contains("\\obj\\") || file.Contains("\\.vs\\"))
                continue;

            // Read file content
            var content = await File.ReadAllTextAsync(file);
            var originalContent = content;

            // Replace placeholders
            content = content.Replace("NetMXApp", projectName);
            content = content.Replace("netmxapp", projectName.ToLowerInvariant());

            // Write back if changed
            if (content != originalContent)
            {
                await File.WriteAllTextAsync(file, content);
            }
        }
    }

    private void RenameProjectFiles(string directory, string projectName)
    {
        // Rename .sln file
        var slnFiles = Directory.GetFiles(directory, "*.sln");
        foreach (var slnFile in slnFiles)
        {
            var newName = Path.Combine(directory, $"{projectName}.sln");
            if (slnFile != newName)
            {
                File.Move(slnFile, newName);
            }
        }

        // Rename project directory (src/NetMXApp.Web → src/{ProjectName}.Web)
        var srcDir = Path.Combine(directory, "src");
        if (Directory.Exists(srcDir))
        {
            var oldProjectDir = Path.Combine(srcDir, "NetMXApp.Web");
            var newProjectDir = Path.Combine(srcDir, $"{projectName}.Web");
            
            if (Directory.Exists(oldProjectDir) && oldProjectDir != newProjectDir)
            {
                Directory.Move(oldProjectDir, newProjectDir);
            }

            // Rename .csproj file
            var csprojFiles = Directory.GetFiles(newProjectDir, "*.csproj");
            foreach (var csprojFile in csprojFiles)
            {
                var newName = Path.Combine(newProjectDir, $"{projectName}.Web.csproj");
                if (csprojFile != newName)
                {
                    File.Move(csprojFile, newName);
                }
            }
        }
    }
    
    private void ShowTemplateInfo(string templateName, string projectName, string outputPath)
    {
        ConsoleHelper.WriteInfo("\n📁 Project structure:");
        ConsoleHelper.WriteInfo($"   {outputPath}/");
        
        switch (templateName.ToLower())
        {
            case "monolith":
                ConsoleHelper.WriteInfo($"   ├─ {projectName}.sln");
                ConsoleHelper.WriteInfo($"   ├─ src/{projectName}.Web/");
                ConsoleHelper.WriteInfo($"   │  ├─ Models/          (flat structure)");
                ConsoleHelper.WriteInfo($"   │  ├─ Services/");
                ConsoleHelper.WriteInfo($"   │  ├─ Controllers/");
                ConsoleHelper.WriteInfo($"   │  └─ Views/");
                ConsoleHelper.WriteInfo($"   └─ docker-compose.yml");
                
                ConsoleHelper.WriteInfo("\n🚀 Next steps:");
                ConsoleHelper.WriteInfo($"   1. cd {projectName}");
                ConsoleHelper.WriteInfo($"   2. cd src/{projectName}.Web");
                ConsoleHelper.WriteInfo($"   3. netmx generate feature Product --migrate");
                ConsoleHelper.WriteInfo($"   4. dotnet run");
                ConsoleHelper.WriteInfo($"   5. Navigate to http://localhost:5263/Product");
                break;
                
            case "vertical-slice":
                ConsoleHelper.WriteInfo($"   ├─ {projectName}.sln");
                ConsoleHelper.WriteInfo($"   ├─ src/{projectName}.Web/");
                ConsoleHelper.WriteInfo($"   │  ├─ Features/        (vertical slices)");
                ConsoleHelper.WriteInfo($"   │  │  ├─ Products/");
                ConsoleHelper.WriteInfo($"   │  │  └─ Orders/");
                ConsoleHelper.WriteInfo($"   │  └─ Data/");
                ConsoleHelper.WriteInfo($"   └─ docker-compose.yml");
                
                ConsoleHelper.WriteInfo("\n🚀 Next steps:");
                ConsoleHelper.WriteInfo($"   1. cd {projectName}");
                ConsoleHelper.WriteInfo($"   2. cd src/{projectName}.Web");
                ConsoleHelper.WriteInfo($"   3. netmx generate feature Product --migrate");
                ConsoleHelper.WriteInfo("      (Creates Features/Product/ folder)");
                ConsoleHelper.WriteInfo($"   4. dotnet run");
                break;
                
            case "modular":
                ConsoleHelper.WriteInfo($"   ├─ {projectName}.sln");
                ConsoleHelper.WriteInfo($"   ├─ src/{projectName}.Web/      (host app)");
                ConsoleHelper.WriteInfo($"   ├─ modules/                     (modules here)");
                ConsoleHelper.WriteInfo($"   │  └─ .gitkeep");
                ConsoleHelper.WriteInfo($"   └─ docker-compose.yml");
                
                ConsoleHelper.WriteInfo("\n🚀 Next steps:");
                ConsoleHelper.WriteInfo($"   1. cd {projectName}");
                ConsoleHelper.WriteInfo($"   2. netmx add module Identity");
                ConsoleHelper.WriteInfo($"   3. netmx create module Catalog");
                ConsoleHelper.WriteInfo($"   4. cd modules/Catalog/Catalog.Web");
                ConsoleHelper.WriteInfo($"   5. netmx generate feature Product --migrate");
                ConsoleHelper.WriteInfo("\n💡 Add modules:");
                ConsoleHelper.WriteInfo("   netmx add module Identity");
                ConsoleHelper.WriteInfo("   netmx add module Authorization");
                break;
                
            case "microservices":
                ConsoleHelper.WriteInfo($"   ├─ {projectName}.sln");
                ConsoleHelper.WriteInfo($"   ├─ services/                    (services here)");
                ConsoleHelper.WriteInfo($"   ├─ gateway/                     (API gateway)");
                ConsoleHelper.WriteInfo($"   ├─ shared/                      (contracts)");
                ConsoleHelper.WriteInfo($"   └─ infrastructure/");
                ConsoleHelper.WriteInfo($"       ├─ docker-compose.yml");
                ConsoleHelper.WriteInfo($"       └─ kubernetes/");
                
                ConsoleHelper.WriteInfo("\n🚀 Next steps:");
                ConsoleHelper.WriteInfo($"   1. cd {projectName}");
                ConsoleHelper.WriteInfo($"   2. netmx add service Identity");
                ConsoleHelper.WriteInfo($"   3. netmx create service Catalog");
                ConsoleHelper.WriteInfo($"   4. docker-compose up");
                ConsoleHelper.WriteInfo($"   5. Access gateway at http://localhost:8080");
                break;
        }
    }
}


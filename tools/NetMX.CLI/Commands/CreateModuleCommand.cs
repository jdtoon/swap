using System.Text.Json;
using NetMX.CLI.Infrastructure;

namespace NetMX.CLI.Commands;

/// <summary>
/// Command to create a new module with 4-layer structure
/// </summary>
public class CreateModuleCommand
{
    private readonly string _moduleName;

    public CreateModuleCommand(string moduleName)
    {
        _moduleName = moduleName;
    }

    public async Task<int> ExecuteAsync()
    {
        try
        {
            ConsoleHelper.WriteHeader($"Creating Module: {_moduleName}");

            // Step 1: Find solution
            ConsoleHelper.WriteStep(1, "Locating solution file");
            var solutionFile = FindSolutionFile();
            if (solutionFile == null)
            {
                ConsoleHelper.WriteError("No solution file (.sln) found in current directory or parent directories.");
                return 1;
            }

            var solutionDir = Path.GetDirectoryName(solutionFile)!;
            ConsoleHelper.WriteSuccess($"Found solution: {Path.GetFileName(solutionFile)}");

            // Step 2: Create module directory structure
            ConsoleHelper.WriteStep(2, "Creating module directory structure");
            
            // Determine correct modules directory
            // If in framework/, create modules in repo root; otherwise create relative to solution
            var modulesBaseDir = solutionDir;
            var solutionDirName = Path.GetFileName(solutionDir);
            
            if (solutionDirName == "framework")
            {
                // We're in framework/, so create in repo root modules/
                modulesBaseDir = Path.GetDirectoryName(solutionDir)!;
                ConsoleHelper.WriteInfo($"  Detected framework directory, creating in repository modules/");
            }
            
            var modulesDir = Path.Combine(modulesBaseDir, "modules", _moduleName);
            
            if (Directory.Exists(modulesDir))
            {
                ConsoleHelper.WriteError($"Module '{_moduleName}' already exists at: {modulesDir}");
                return 1;
            }

            Directory.CreateDirectory(modulesDir);

            // Step 3: Create 4-layer projects
            ConsoleHelper.WriteStep(3, "Creating module projects");
            
            CreateCoreProject(modulesDir);
            CreateContractsProject(modulesDir);
            CreateApplicationProject(modulesDir);
            CreateWebProject(modulesDir);

            // Step 4: Add project references
            ConsoleHelper.WriteStep(4, "Configuring project references");
            AddProjectReferences(modulesDir);

            // Step 5: Create module solution file
            ConsoleHelper.WriteStep(5, "Creating module solution");
            CreateModuleSolution(modulesDir);

            // Step 6: Create module.json
            ConsoleHelper.WriteStep(6, "Creating module descriptor");
            CreateModuleJson(modulesDir);

            // Step 7: Create README
            ConsoleHelper.WriteStep(7, "Creating module documentation");
            CreateReadme(modulesDir);

            ConsoleHelper.WriteSuccess($"Module '{_moduleName}' created successfully!");
            ConsoleHelper.WriteInfo("\nModule structure:");
            ConsoleHelper.WriteInfo($"  modules/{_moduleName}/");
            ConsoleHelper.WriteInfo($"    ├── {_moduleName}.Core/       (Domain layer)");
            ConsoleHelper.WriteInfo($"    ├── {_moduleName}.Contracts/  (DTOs & interfaces)");
            ConsoleHelper.WriteInfo($"    ├── {_moduleName}.Application/ (Services)");
            ConsoleHelper.WriteInfo($"    ├── {_moduleName}.Web/        (Controllers & views)");
            ConsoleHelper.WriteInfo($"    ├── {_moduleName}.sln         (Module solution)");
            ConsoleHelper.WriteInfo($"    ├── module.json");
            ConsoleHelper.WriteInfo($"    └── README.md");

            ConsoleHelper.WriteInfo("\nNext steps:");
            ConsoleHelper.WriteInfo($"  1. cd modules/{_moduleName}");
            ConsoleHelper.WriteInfo($"  2. cd {_moduleName}.Web");
            ConsoleHelper.WriteInfo($"  3. netmx generate crud YourEntity -m {_moduleName}");

            return 0;
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"Failed to create module: {ex.Message}");
            return 1;
        }
    }

    private string? FindSolutionFile()
    {
        var currentDir = Directory.GetCurrentDirectory();
        while (currentDir != null)
        {
            var slnFiles = Directory.GetFiles(currentDir, "*.sln");
            if (slnFiles.Length > 0) return slnFiles[0];
            currentDir = Directory.GetParent(currentDir)?.FullName;
        }
        return null;
    }

    private void CreateCoreProject(string moduleDir)
    {
        var projectDir = Path.Combine(moduleDir, $"{_moduleName}.Core");
        Directory.CreateDirectory(projectDir);

        var csproj = $$"""
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <RootNamespace>{{_moduleName}}.Core</RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="NetMX.Ddd.Domain" Version="0.1.0-*" />
  </ItemGroup>

</Project>
""";

        File.WriteAllText(Path.Combine(projectDir, $"{_moduleName}.Core.csproj"), csproj);

        // Create directories
        Directory.CreateDirectory(Path.Combine(projectDir, "Entities"));
        Directory.CreateDirectory(Path.Combine(projectDir, "ValueObjects"));

        ConsoleHelper.WriteInfo($"  ✓ Created {_moduleName}.Core");
    }

    private void CreateContractsProject(string moduleDir)
    {
        var projectDir = Path.Combine(moduleDir, $"{_moduleName}.Contracts");
        Directory.CreateDirectory(projectDir);

        var csproj = $$"""
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <RootNamespace>{{_moduleName}}.Contracts</RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="NetMX.Ddd.Application.Contracts" Version="0.1.0-*" />
    <ProjectReference Include="..\{{_moduleName}}.Core\{{_moduleName}}.Core.csproj" />
  </ItemGroup>

</Project>
""";

        File.WriteAllText(Path.Combine(projectDir, $"{_moduleName}.Contracts.csproj"), csproj);

        // Create directories
        Directory.CreateDirectory(Path.Combine(projectDir, "Dtos"));
        Directory.CreateDirectory(Path.Combine(projectDir, "Services"));

        ConsoleHelper.WriteInfo($"  ✓ Created {_moduleName}.Contracts");
    }

    private void CreateApplicationProject(string moduleDir)
    {
        var projectDir = Path.Combine(moduleDir, $"{_moduleName}.Application");
        Directory.CreateDirectory(projectDir);

        var csproj = $$"""
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <RootNamespace>{{_moduleName}}.Application</RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="NetMX.Ddd.Application" Version="0.1.0-*" />
    <PackageReference Include="NetMX.EntityFrameworkCore" Version="0.1.0-*" />
    <ProjectReference Include="..\{{_moduleName}}.Contracts\{{_moduleName}}.Contracts.csproj" />
  </ItemGroup>

</Project>
""";

        File.WriteAllText(Path.Combine(projectDir, $"{_moduleName}.Application.csproj"), csproj);

        // Create directories
        Directory.CreateDirectory(Path.Combine(projectDir, "Services"));

        ConsoleHelper.WriteInfo($"  ✓ Created {_moduleName}.Application");
    }

    private void CreateWebProject(string moduleDir)
    {
        var projectDir = Path.Combine(moduleDir, $"{_moduleName}.Web");
        Directory.CreateDirectory(projectDir);

        var csproj = $$"""
<Project Sdk="Microsoft.NET.Sdk.Razor">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <RootNamespace>{{_moduleName}}.Web</RootNamespace>
    <AddRazorSupportForMvc>true</AddRazorSupportForMvc>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="NetMX.AspNetCore.Mvc" Version="0.1.0-*" />
    <PackageReference Include="NetMX.Events" Version="0.1.0-*" />
    <ProjectReference Include="..\{{_moduleName}}.Application\{{_moduleName}}.Application.csproj" />
  </ItemGroup>

</Project>
""";

        File.WriteAllText(Path.Combine(projectDir, $"{_moduleName}.Web.csproj"), csproj);

        // Create directories
        Directory.CreateDirectory(Path.Combine(projectDir, "Controllers"));
        Directory.CreateDirectory(Path.Combine(projectDir, "Views"));
        Directory.CreateDirectory(Path.Combine(projectDir, "Models"));
        Directory.CreateDirectory(Path.Combine(projectDir, "Services"));
        Directory.CreateDirectory(Path.Combine(projectDir, "Dtos"));
        Directory.CreateDirectory(Path.Combine(projectDir, "Events"));

        ConsoleHelper.WriteInfo($"  ✓ Created {_moduleName}.Web");
    }

    private void AddProjectReferences(string moduleDir)
    {
        // Already done in project creation
        ConsoleHelper.WriteInfo("  ✓ Project references configured");
    }

    private void CreateModuleSolution(string moduleDir)
    {
        var solutionFile = Path.Combine(moduleDir, $"{_moduleName}.sln");
        
        // Create solution file
        var createSlnProcess = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"new sln -n {_moduleName} -o \"{moduleDir}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });
        createSlnProcess?.WaitForExit();

        // Add all projects to the solution
        var projects = new[]
        {
            Path.Combine(moduleDir, $"{_moduleName}.Core", $"{_moduleName}.Core.csproj"),
            Path.Combine(moduleDir, $"{_moduleName}.Contracts", $"{_moduleName}.Contracts.csproj"),
            Path.Combine(moduleDir, $"{_moduleName}.Application", $"{_moduleName}.Application.csproj"),
            Path.Combine(moduleDir, $"{_moduleName}.Web", $"{_moduleName}.Web.csproj")
        };

        foreach (var project in projects)
        {
            var relativePath = Path.GetRelativePath(moduleDir, project);
            var addProcess = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"sln \"{solutionFile}\" add \"{relativePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            addProcess?.WaitForExit();
        }

        ConsoleHelper.WriteInfo($"  ✓ Created {_moduleName}.sln");
    }

    private void CreateModuleJson(string moduleDir)
    {
        var moduleJson = new
        {
            name = _moduleName,
            version = "0.1.0",
            description = $"{_moduleName} module for NetMX",
            author = "Your Name",
            license = "MIT",
            dependencies = new
            {
                netmx = "0.1.0"
            },
            projects = new[]
            {
                $"{_moduleName}.Core",
                $"{_moduleName}.Contracts",
                $"{_moduleName}.Application",
                $"{_moduleName}.Web"
            }
        };

        var json = JsonSerializer.Serialize(moduleJson, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(Path.Combine(moduleDir, "module.json"), json);
        ConsoleHelper.WriteInfo("  ✓ Created module.json");
    }

    private void CreateReadme(string moduleDir)
    {
        var readme = $$"""
# {{_moduleName}} Module

{{_moduleName}} module for NetMX framework.

## Structure

- **{{_moduleName}}.Core** - Domain entities and value objects
- **{{_moduleName}}.Contracts** - DTOs and service interfaces
- **{{_moduleName}}.Application** - Service implementations
- **{{_moduleName}}.Web** - Controllers, views, and UI components

## Getting Started

### Generate Features

```bash
cd {{_moduleName}}.Web
netmx generate crud YourEntity
```

### Integration

Add to your application's `Program.cs`:

```csharp
// Add services
builder.Services.Add{{_moduleName}}();

// Configure middleware
app.Use{{_moduleName}}();
```

## Features

List your module's features here.

## License

MIT
""";

        File.WriteAllText(Path.Combine(moduleDir, "README.md"), readme);
        ConsoleHelper.WriteInfo("  ✓ Created README.md");
    }
}

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

            // Step 4: Create test projects
            ConsoleHelper.WriteStep(4, "Creating test projects");
            
            CreateUnitTestProject(modulesDir);
            CreateIntegrationTestProject(modulesDir);
            CreateE2ETestProject(modulesDir);

            // Step 5: Add project references
            ConsoleHelper.WriteStep(5, "Configuring project references");
            AddProjectReferences(modulesDir);
            AddTestProjectReferences(modulesDir);

            // Step 6: Create module solution file
            ConsoleHelper.WriteStep(6, "Creating module solution");
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
            ConsoleHelper.WriteInfo($"    ├── {_moduleName}.Core/           (Domain layer)");
            ConsoleHelper.WriteInfo($"    ├── {_moduleName}.Contracts/      (DTOs & interfaces)");
            ConsoleHelper.WriteInfo($"    ├── {_moduleName}.Application/    (Services)");
            ConsoleHelper.WriteInfo($"    ├── {_moduleName}.Web/            (Controllers & views)");
            ConsoleHelper.WriteInfo($"    ├── {_moduleName}.Tests/          (Unit tests)");
            ConsoleHelper.WriteInfo($"    ├── {_moduleName}.Web.Tests/      (Integration tests)");
            ConsoleHelper.WriteInfo($"    ├── {_moduleName}.E2E.Tests/      (E2E tests with Playwright)");
            ConsoleHelper.WriteInfo($"    ├── {_moduleName}.sln             (Module solution)");
            ConsoleHelper.WriteInfo($"    ├── module.json");
            ConsoleHelper.WriteInfo($"    └── README.md");

            ConsoleHelper.WriteInfo("\nNext steps:");
            ConsoleHelper.WriteInfo($"  1. cd modules/{_moduleName}");
            ConsoleHelper.WriteInfo($"  2. cd {_moduleName}.Web");
            ConsoleHelper.WriteInfo($"  3. netmx generate crud YourEntity -m {_moduleName}");
            ConsoleHelper.WriteInfo($"  4. Run tests: dotnet test");
            ConsoleHelper.WriteInfo($"  5. E2E tests: dotnet playwright install (first time only)");

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
            Path.Combine(moduleDir, $"{_moduleName}.Web", $"{_moduleName}.Web.csproj"),
            Path.Combine(moduleDir, $"{_moduleName}.Tests", $"{_moduleName}.Tests.csproj"),
            Path.Combine(moduleDir, $"{_moduleName}.Web.Tests", $"{_moduleName}.Web.Tests.csproj"),
            Path.Combine(moduleDir, $"{_moduleName}.E2E.Tests", $"{_moduleName}.E2E.Tests.csproj")
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

    private void CreateUnitTestProject(string moduleDir)
    {
        var projectName = $"{_moduleName}.Tests";
        var projectDir = Path.Combine(moduleDir, projectName);
        Directory.CreateDirectory(projectDir);

        var csproj = $$"""
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.0" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Moq" Version="4.20.70" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\{{_moduleName}}.Core\{{_moduleName}}.Core.csproj" />
    <ProjectReference Include="..\{{_moduleName}}.Application\{{_moduleName}}.Application.csproj" />
  </ItemGroup>
</Project>
""";

        File.WriteAllText(Path.Combine(projectDir, $"{projectName}.csproj"), csproj);

        // Create sample unit test
        var testClass = $$"""
using Xunit;

namespace {{_moduleName}}.Tests;

/// <summary>
/// Sample unit test class.
/// Replace with actual tests for your domain logic.
/// </summary>
public class SampleUnitTests
{
    [Fact]
    public void Sample_Test_Passes()
    {
        // Arrange
        var expected = true;
        
        // Act
        var actual = true;
        
        // Assert
        Assert.Equal(expected, actual);
    }
}
""";

        File.WriteAllText(Path.Combine(projectDir, "SampleUnitTests.cs"), testClass);
        ConsoleHelper.WriteInfo($"  ✓ Created {projectName} (Unit tests)");
    }

    private void CreateIntegrationTestProject(string moduleDir)
    {
        var projectName = $"{_moduleName}.Web.Tests";
        var projectDir = Path.Combine(moduleDir, projectName);
        Directory.CreateDirectory(projectDir);

        var csproj = $$"""
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.0" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.0.0" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="NetMX.Testing" Version="*" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\{{_moduleName}}.Web\{{_moduleName}}.Web.csproj" />
  </ItemGroup>
</Project>
""";

        File.WriteAllText(Path.Combine(projectDir, $"{projectName}.csproj"), csproj);

        // Create sample integration test
        var testClass = $$"""
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace {{_moduleName}}.Web.Tests;

/// <summary>
/// Sample integration test using WebApplicationFactory.
/// Replace with actual HTTP integration tests.
/// </summary>
public class SampleIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SampleIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Sample_Integration_Test_Passes()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        // Act
        var response = await client.GetAsync("/");
        
        // Assert
        response.EnsureSuccessStatusCode();
    }
}
""";

        File.WriteAllText(Path.Combine(projectDir, "SampleIntegrationTests.cs"), testClass);
        ConsoleHelper.WriteInfo($"  ✓ Created {projectName} (Integration tests with WebApplicationFactory)");
    }

    private void CreateE2ETestProject(string moduleDir)
    {
        var projectName = $"{_moduleName}.E2E.Tests";
        var projectDir = Path.Combine(moduleDir, projectName);
        Directory.CreateDirectory(projectDir);

        var csproj = $$"""
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.0" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="NetMX.Testing" Version="*" />
  </ItemGroup>
</Project>
""";

        File.WriteAllText(Path.Combine(projectDir, $"{projectName}.csproj"), csproj);

        // Create sample E2E test using PlaywrightTestBase
        var testClass = $$"""
using NetMX.Testing;
using Xunit;

namespace {{_moduleName}}.E2E.Tests;

/// <summary>
/// Sample E2E test using PlaywrightTestBase for HTMX testing.
/// Run 'dotnet playwright install' before running these tests.
/// </summary>
public class SampleE2ETests : PlaywrightTestBase, IAsyncLifetime
{
    private const string BaseUrl = "http://localhost:5000";

    public async Task InitializeAsync()
    {
        // Initialize Playwright with Chromium in headless mode
        await base.InitializeAsync("chromium", headless: true);
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
    }

    [Fact(Skip = "Requires running application - remove Skip attribute when ready")]
    public async Task Sample_E2E_Test_With_HTMX()
    {
        // Navigate to page
        await Page.GotoAsync($"{BaseUrl}/");

        // Example: Click button with hx-get attribute
        // await ClickAndWaitForHxSwapAsync("button[hx-get='/api/data']", "#result");

        // Example: Fill and submit HTMX form
        // await FillAndSubmitHxFormAsync("#my-form", new Dictionary<string, string>
        // {
        //     ["Name"] = "Test",
        //     ["Email"] = "test@example.com"
        // });

        // Example: Wait for HTMX event
        // await WaitForHxEventAsync("data-loaded");

        // Example: Verify text content
        // await AssertTextContainsAsync("h1", "Welcome");
        
        // Placeholder assertion
        Assert.True(true, "Replace with actual E2E test logic");
    }
}
""";

        File.WriteAllText(Path.Combine(projectDir, "SampleE2ETests.cs"), testClass);
        ConsoleHelper.WriteInfo($"  ✓ Created {projectName} (E2E tests with Playwright)");
    }

    private void AddTestProjectReferences(string moduleDir)
    {
        // Test projects are already configured with their references in the .csproj files
        // No additional work needed - just a placeholder for future enhancements
        ConsoleHelper.WriteInfo("  ✓ Test project references configured");
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

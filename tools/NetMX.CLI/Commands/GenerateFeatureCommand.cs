using System.Diagnostics;
using System.Text;
using NetMX.CLI.Infrastructure;
using NetMX.CLI.Models;

namespace NetMX.CLI.Commands;

/// <summary>
/// Command to generate a complete feature (entity with CRUD operations).
/// Orchestrates EntityGenerator, DtoGenerator, ServiceGenerator, ControllerGenerator, ViewGenerator.
/// </summary>
public class GenerateFeatureCommand
{
    private readonly EntityGenerationOptions _options;

    public GenerateFeatureCommand(
        string entityName,
        string? module = null,
        List<PropertyDefinition>? properties = null,
        int? pageSize = null,
        List<string>? searchableProperties = null,
        List<string>? filterableProperties = null,
        List<string>? sortableProperties = null,
        List<string>? exportFormats = null,
        bool autoMigrate = false,
        bool includeAuditFields = true,
        bool includeSoftDelete = false)
    {
        _options = new EntityGenerationOptions
        {
            EntityName = entityName,
            ModuleName = module,
            Properties = properties ?? new List<PropertyDefinition>(),
            PageSize = pageSize,
            SearchableProperties = searchableProperties ?? new List<string>(),
            FilterableProperties = filterableProperties ?? new List<string>(),
            SortableProperties = sortableProperties ?? new List<string>(),
            ExportFormats = exportFormats ?? new List<string>(),
            AutoMigrate = autoMigrate,
            IncludeAuditFields = includeAuditFields,
            IncludeSoftDelete = includeSoftDelete
        };
    }

    public async Task<int> ExecuteAsync()
    {
        try
        {
            ConsoleHelper.WriteHeader($"Generating Feature: {_options.EntityName}");

            var solutionPath = FindSolutionFile();
            if (solutionPath == null)
            {
                ConsoleHelper.WriteError("No .sln file found");
                return 1;
            }

            var solutionDir = Path.GetDirectoryName(solutionPath)!;
            var webProjectPath = FindWebProject(solutionDir);
            
            if (webProjectPath == null)
            {
                ConsoleHelper.WriteError("Could not find web project");
                return 1;
            }

            var webProjectDir = Path.GetDirectoryName(webProjectPath)!;

            // Step 0: Ensure required packages are installed
            ConsoleHelper.WriteStep(0, "Checking required package references");
            await EnsureRequiredPackagesAsync(webProjectPath);

            // Detect project namespace
            _options.ProjectNamespace = await DetectProjectNamespaceAsync(webProjectPath);
            if (_options.ProjectNamespace != null)
            {
                ConsoleHelper.WriteSuccess($"  Detected namespace: {_options.ProjectNamespace}");
            }

            // Step 1: Generate Entity
            ConsoleHelper.WriteStep(1, "Generating entity class (DDD patterns)");
            GenerateEntity(webProjectDir);

            // Step 2: Generate DTOs
            ConsoleHelper.WriteStep(2, "Generating DTO classes");
            GenerateDtos(webProjectDir);

            // Step 3: Generate Service
            ConsoleHelper.WriteStep(3, "Generating service interface and implementation");
            GenerateService(webProjectDir);

            // Step 4: Generate Event Constants
            ConsoleHelper.WriteStep(4, "Generating event constants (type-safe)");
            GenerateEventConstants(webProjectDir);

            // Step 5: Generate Controller
            ConsoleHelper.WriteStep(5, "Generating controller with HTMX support");
            GenerateController(webProjectDir);

            // Step 6: Generate Views
            ConsoleHelper.WriteStep(6, "Generating views with HTMX patterns");
            GenerateViews(webProjectDir);

            // Success Message
            ConsoleHelper.WriteSuccess($"Feature '{_options.EntityName}' generated successfully!");
            ConsoleHelper.WriteInfo("\nGenerated files:");
            
            ShowGeneratedFiles();

            // Auto-migration if requested (only for apps, not modules)
            if (_options.AutoMigrate && string.IsNullOrEmpty(_options.ModuleName))
            {
                await HandleAutoMigration(webProjectDir);
            }
            else
            {
                ShowNextSteps();
            }

            return 0;
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"Failed to generate feature: {ex.Message}");
            ConsoleHelper.WriteError($"Stack trace: {ex.StackTrace}");
            return 1;
        }
    }

    private void GenerateEntity(string webProjectDir)
    {
        var isModuleContext = !string.IsNullOrEmpty(_options.ModuleName);
        var entityCode = EntityGenerator.GenerateEntity(_options);
        
        string entityPath;
        if (isModuleContext)
        {
            // Module: Entity goes in Core project
            var moduleDir = Path.GetDirectoryName(webProjectDir)!;
            var entitiesDir = Path.Combine(moduleDir, $"{_options.ModuleName}.Core", "Entities");
            Directory.CreateDirectory(entitiesDir);
            entityPath = Path.Combine(entitiesDir, $"{_options.EntityName}.cs");
        }
        else
        {
            // App: Entity goes in Models folder
            var modelsDir = Path.Combine(webProjectDir, "Models");
            Directory.CreateDirectory(modelsDir);
            entityPath = Path.Combine(modelsDir, $"{_options.EntityName}.cs");
        }

        File.WriteAllText(entityPath, entityCode);
    }

    private void GenerateDtos(string webProjectDir)
    {
        var isModuleContext = !string.IsNullOrEmpty(_options.ModuleName);
        
        // Generate all 5 DTOs
        var readDto = DtoGenerator.GenerateReadDto(_options);
        var createDto = DtoGenerator.GenerateCreateDto(_options);
        var updateDto = DtoGenerator.GenerateUpdateDto(_options);
        var filterDto = _options.HasFilters || _options.HasSearch 
            ? DtoGenerator.GenerateFilterDto(_options) 
            : null;
        var pagedResultDto = _options.HasPagination 
            ? DtoGenerator.GeneratePagedResultDto(_options) 
            : null;

        string dtosDir;
        if (isModuleContext)
        {
            // Module: DTOs go in Contracts project
            var moduleDir = Path.GetDirectoryName(webProjectDir)!;
            dtosDir = Path.Combine(moduleDir, $"{_options.ModuleName}.Contracts", "Dtos");
        }
        else
        {
            // App: DTOs go in Dtos folder
            dtosDir = Path.Combine(webProjectDir, "Dtos");
        }

        Directory.CreateDirectory(dtosDir);

        // Write files
        File.WriteAllText(Path.Combine(dtosDir, $"{_options.EntityName}Dto.cs"), readDto);
        File.WriteAllText(Path.Combine(dtosDir, $"Create{_options.EntityName}Dto.cs"), createDto);
        File.WriteAllText(Path.Combine(dtosDir, $"Update{_options.EntityName}Dto.cs"), updateDto);
        
        if (filterDto != null)
            File.WriteAllText(Path.Combine(dtosDir, $"{_options.EntityName}FilterDto.cs"), filterDto);
        
        if (pagedResultDto != null)
            File.WriteAllText(Path.Combine(dtosDir, $"Paged{_options.EntityName}ResultDto.cs"), pagedResultDto);
    }

    private void GenerateService(string webProjectDir)
    {
        var isModuleContext = !string.IsNullOrEmpty(_options.ModuleName);
        
        var serviceInterface = ServiceGenerator.GenerateServiceInterface(_options);
        var serviceImplementation = ServiceGenerator.GenerateServiceImplementation(_options);

        if (isModuleContext)
        {
            // Module: Interface in Contracts, Implementation in Application
            var moduleDir = Path.GetDirectoryName(webProjectDir)!;
            var contractsDir = Path.Combine(moduleDir, $"{_options.ModuleName}.Contracts", "Services");
            var applicationDir = Path.Combine(moduleDir, $"{_options.ModuleName}.Application", "Services");
            
            Directory.CreateDirectory(contractsDir);
            Directory.CreateDirectory(applicationDir);

            File.WriteAllText(Path.Combine(contractsDir, $"I{_options.EntityName}Service.cs"), serviceInterface);
            File.WriteAllText(Path.Combine(applicationDir, $"{_options.EntityName}Service.cs"), serviceImplementation);
        }
        else
        {
            // App: Both in Services folder
            var servicesDir = Path.Combine(webProjectDir, "Services");
            Directory.CreateDirectory(servicesDir);

            File.WriteAllText(Path.Combine(servicesDir, $"I{_options.EntityName}Service.cs"), serviceInterface);
            File.WriteAllText(Path.Combine(servicesDir, $"{_options.EntityName}Service.cs"), serviceImplementation);
        }
    }

    private void GenerateEventConstants(string webProjectDir)
    {
        // Generate three files for Event Registry pattern:
        // 1. Events.{EntityName}.cs in NetMX.Events (partial Events class)
        // 2. {EntityName}EventDefinitions.cs (Register method)
        // 3. Extension method Add{EntityName}Events()
        
        GenerateEventRegistryFiles(webProjectDir);
    }

    private void GenerateController(string webProjectDir)
    {
        var controllerCode = ControllerGenerator.GenerateController(_options);
        
        var controllersDir = Path.Combine(webProjectDir, "Controllers");
        Directory.CreateDirectory(controllersDir);

        File.WriteAllText(Path.Combine(controllersDir, $"{_options.EntityName}Controller.cs"), controllerCode);
    }

    private void GenerateViews(string webProjectDir)
    {
        var indexView = ViewGenerator.GenerateIndexView(_options);
        var listView = ViewGenerator.GenerateListView(_options);
        var formView = ViewGenerator.GenerateFormView(_options);

        var viewsDir = Path.Combine(webProjectDir, "Views", _options.EntityName);
        Directory.CreateDirectory(viewsDir);

        File.WriteAllText(Path.Combine(viewsDir, "Index.cshtml"), indexView);
        File.WriteAllText(Path.Combine(viewsDir, "_List.cshtml"), listView);
        File.WriteAllText(Path.Combine(viewsDir, "_Form.cshtml"), formView);
        
        // Ensure _ViewImports.cshtml exists with correct namespaces
        EnsureViewImports(webProjectDir);
    }

    private void EnsureViewImports(string webProjectDir)
    {
        var viewsDir = Path.Combine(webProjectDir, "Views");
        var viewImportsPath = Path.Combine(viewsDir, "_ViewImports.cshtml");
        
        // Generate content with project namespace
        var projectNamespace = _options.ProjectNamespace ?? "App";
        var content = $@"@using {projectNamespace}
@using {projectNamespace}.Models
@using {projectNamespace}.Dtos
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
";

        // Create or update _ViewImports.cshtml
        Directory.CreateDirectory(viewsDir);
        
        if (File.Exists(viewImportsPath))
        {
            // Check if our namespaces are already included
            var existing = File.ReadAllText(viewImportsPath);
            if (!existing.Contains($"@using {projectNamespace}.Dtos"))
            {
                // Append our namespaces if missing
                var lines = new List<string> { existing.TrimEnd() };
                if (!existing.Contains($"@using {projectNamespace}"))
                    lines.Add($"@using {projectNamespace}");
                if (!existing.Contains($"@using {projectNamespace}.Models"))
                    lines.Add($"@using {projectNamespace}.Models");
                if (!existing.Contains($"@using {projectNamespace}.Dtos"))
                    lines.Add($"@using {projectNamespace}.Dtos");
                
                File.WriteAllText(viewImportsPath, string.Join(Environment.NewLine, lines));
            }
        }
        else
        {
            // Create new _ViewImports.cshtml
            File.WriteAllText(viewImportsPath, content);
            ConsoleHelper.WriteSuccess("  ✓ Created _ViewImports.cshtml");
        }
    }

    private void ShowGeneratedFiles()
    {
        var isModuleContext = !string.IsNullOrEmpty(_options.ModuleName);
        var entityName = _options.EntityName;
        var module = _options.ModuleName;

        if (isModuleContext)
        {
            // Module context files
            ConsoleHelper.WriteInfo($"  {module}.Core/Entities/{entityName}.cs");
            ConsoleHelper.WriteInfo($"  {module}.Contracts/Dtos/{entityName}Dto.cs");
            ConsoleHelper.WriteInfo($"  {module}.Contracts/Dtos/Create{entityName}Dto.cs");
            ConsoleHelper.WriteInfo($"  {module}.Contracts/Dtos/Update{entityName}Dto.cs");
            
            if (_options.HasFilters || _options.HasSearch)
                ConsoleHelper.WriteInfo($"  {module}.Contracts/Dtos/{entityName}FilterDto.cs");
            
            if (_options.HasPagination)
                ConsoleHelper.WriteInfo($"  {module}.Contracts/Dtos/Paged{entityName}ResultDto.cs");
            
            ConsoleHelper.WriteInfo($"  {module}.Contracts/Services/I{entityName}Service.cs");
            ConsoleHelper.WriteInfo($"  {module}.Application/Services/{entityName}Service.cs");
            // ConsoleHelper.WriteInfo($"  {module}.Web/Events/{entityName}EventDefinitions.cs");
            // ConsoleHelper.WriteInfo($"  {module}.Web/Extensions/{entityName}EventExtensions.cs");
            ConsoleHelper.WriteInfo($"  {module}.Web/Controllers/{entityName}Controller.cs");
            ConsoleHelper.WriteInfo($"  {module}.Web/Views/{entityName}/Index.cshtml");
            ConsoleHelper.WriteInfo($"  {module}.Web/Views/{entityName}/_List.cshtml");
            ConsoleHelper.WriteInfo($"  {module}.Web/Views/{entityName}/_Form.cshtml");
        }
        else
        {
            // App context files
            ConsoleHelper.WriteInfo($"  Models/{entityName}.cs");
            ConsoleHelper.WriteInfo($"  Dtos/{entityName}Dto.cs");
            ConsoleHelper.WriteInfo($"  Dtos/Create{entityName}Dto.cs");
            ConsoleHelper.WriteInfo($"  Dtos/Update{entityName}Dto.cs");
            
            if (_options.HasFilters || _options.HasSearch)
                ConsoleHelper.WriteInfo($"  Dtos/{entityName}FilterDto.cs");
            
            if (_options.HasPagination)
                ConsoleHelper.WriteInfo($"  Dtos/Paged{entityName}ResultDto.cs");
            
            ConsoleHelper.WriteInfo($"  Services/I{entityName}Service.cs");
            ConsoleHelper.WriteInfo($"  Services/{entityName}Service.cs");
            // ConsoleHelper.WriteInfo($"  Events/{entityName}EventDefinitions.cs");
            // ConsoleHelper.WriteInfo($"  Extensions/{entityName}EventExtensions.cs");
            ConsoleHelper.WriteInfo($"  Controllers/{entityName}Controller.cs");
            ConsoleHelper.WriteInfo($"  Views/{entityName}/Index.cshtml");
            ConsoleHelper.WriteInfo($"  Views/{entityName}/_List.cshtml");
            ConsoleHelper.WriteInfo($"  Views/{entityName}/_Form.cshtml");
        }

        // Show enabled features
        if (_options.HasAdvancedFeatures)
        {
            ConsoleHelper.WriteInfo("\n✨ Advanced features enabled:");
            if (_options.HasPagination)
                ConsoleHelper.WriteInfo($"  • Pagination (page size: {_options.PageSize})");
            if (_options.HasSearch)
                ConsoleHelper.WriteInfo($"  • Search ({string.Join(", ", _options.SearchableProperties)})");
            if (_options.HasFilters)
                ConsoleHelper.WriteInfo($"  • Filters ({string.Join(", ", _options.FilterableProperties)})");
            if (_options.HasSorting)
                ConsoleHelper.WriteInfo($"  • Sorting ({string.Join(", ", _options.SortableProperties)})");
            if (_options.HasExport)
                ConsoleHelper.WriteInfo($"  • Export ({string.Join(", ", _options.ExportFormats)})");
        }
    }

    private async Task HandleAutoMigration(string webProjectDir)
    {
        ConsoleHelper.WriteInfo("\n🔧 Auto-migration enabled...");
        
        try
        {
            // Use new MigrationOrchestrator for complete workflow
            var orchestrator = new MigrationOrchestrator(webProjectDir, verbose: true);
            
            ConsoleHelper.WriteStep(7, "Adding DbSet to DbContext");
            ConsoleHelper.WriteStep(8, $"Creating migration: Add{_options.EntityName}");
            ConsoleHelper.WriteStep(9, "Applying migration to database");
            
            var result = await orchestrator.AddEntityWithMigrationAsync(
                entityName: _options.EntityName,
                entityNamespace: null, // Auto-inferred from project structure
                createMigration: true,
                applyMigration: true);

            if (result.IsSuccess)
            {
                ConsoleHelper.WriteSuccess($"\n✅ {result.Message}");
                
                // Show what was done
                foreach (var step in result.Steps)
                {
                    ConsoleHelper.WriteInfo($"  {step}");
                }
                
                ConsoleHelper.WriteInfo($"\n🚀 Navigate to /{_options.EntityName} to test your feature!");
            }
            else
            {
                ConsoleHelper.WriteWarning($"\n⚠️  {result.Message}");
                
                // Show what was attempted
                foreach (var step in result.Steps)
                {
                    ConsoleHelper.WriteInfo($"  {step}");
                }
                
                ShowManualSteps();
            }
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"Auto-migration failed: {ex.Message}");
            if (_options.EntityName != null)
            {
                ConsoleHelper.WriteInfo("\nPartial failure - some steps may have completed.");
            }
            ShowManualSteps();
        }
    }

    private void ShowNextSteps()
    {
        ConsoleHelper.WriteInfo("\n📋 Next steps:");
        
        if (!string.IsNullOrEmpty(_options.ModuleName))
        {
            // Module context
            ConsoleHelper.WriteInfo("  1. Add entity to module DbContext");
            ConsoleHelper.WriteInfo("  2. Register services in DI container");
            ConsoleHelper.WriteInfo($"  3. Navigate to /{_options.EntityName} to test");
        }
        else
        {
            // App context
            ShowManualSteps();
            ConsoleHelper.WriteInfo($"\n💡 Tip: Use --migrate flag next time to automate steps 1-3!");
        }
    }

    private void ShowManualSteps()
    {
        ConsoleHelper.WriteInfo("\nManual steps:");
        ConsoleHelper.WriteInfo("  1. Add DbSet to your DbContext:");
        ConsoleHelper.WriteInfo($"     public DbSet<{_options.EntityName}> {_options.EntityName}s => Set<{_options.EntityName}>();");
        ConsoleHelper.WriteInfo($"  2. Run: dotnet ef migrations add Add{_options.EntityName}");
        ConsoleHelper.WriteInfo("  3. Run: dotnet ef database update");
        ConsoleHelper.WriteInfo($"  4. Navigate to /{_options.EntityName} to test");
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

    private string? FindWebProject(string solutionDir)
    {
        var searchPaths = new[] { Path.Combine(solutionDir, "src"), solutionDir };
        foreach (var searchPath in searchPaths)
        {
            if (!Directory.Exists(searchPath)) continue;
            var webProjects = Directory.GetFiles(searchPath, "*.Web.csproj", SearchOption.AllDirectories);
            if (webProjects.Length > 0) return webProjects[0];
        }
        return null;
    }

    private async Task EnsureRequiredPackagesAsync(string projectPath)
    {
        var projectDir = Path.GetDirectoryName(projectPath);
        if (projectDir == null)
        {
            ConsoleHelper.WriteError("  Could not determine project directory");
            return;
        }

        var requiredPackages = new[]
        {
            "NetMX.Ddd.Domain",
            "NetMX.AspNetCore.Mvc"
        };

        // Read .csproj file to check for existing package references
        var csprojContent = await File.ReadAllTextAsync(projectPath);

        foreach (var package in requiredPackages)
        {
            if (csprojContent.Contains($"<PackageReference Include=\"{package}\""))
            {
                ConsoleHelper.WriteSuccess($"  ✓ {package} already referenced");
                continue;
            }

            // Package is missing, add it
            ConsoleHelper.WriteInfo($"  Adding {package}...");
            
            var addPackageCommand = $"dotnet add \"{projectPath}\" package {package} --version \"0.1.0-*\"";
            var processInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"add \"{projectPath}\" package {package} --version \"0.1.0-*\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = projectDir
            };

            using var process = Process.Start(processInfo);
            if (process == null)
            {
                ConsoleHelper.WriteError($"  Failed to start dotnet add command for {package}");
                continue;
            }

            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                ConsoleHelper.WriteSuccess($"  ✓ Added {package}");
            }
            else
            {
                var error = await process.StandardError.ReadToEndAsync();
                ConsoleHelper.WriteError($"  Failed to add {package}: {error}");
            }
        }
    }

    private async Task<string?> DetectProjectNamespaceAsync(string projectPath)
    {
        try
        {
            var csprojContent = await File.ReadAllTextAsync(projectPath);
            
            // Try to find <RootNamespace> element
            var rootNamespaceMatch = System.Text.RegularExpressions.Regex.Match(
                csprojContent,
                @"<RootNamespace>(.*?)</RootNamespace>");
            
            if (rootNamespaceMatch.Success)
            {
                return rootNamespaceMatch.Groups[1].Value;
            }

            // Fallback: Infer from project file name
            var projectName = Path.GetFileNameWithoutExtension(projectPath);
            return projectName;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Generates Event Registry pattern files:
    /// 1. Events.{EntityName}.cs in NetMX.Events project (partial Events class)
    /// 2. {EntityName}EventDefinitions.cs in Events/ directory (Register method)
    /// 3. Extension method Add{EntityName}Events() in Extensions/ directory
    /// </summary>
    private void GenerateEventRegistryFiles(string webProjectDir)
    {
        var entityName = _options.EntityName;
        var entityNameLower = entityName.ToLower();
        var moduleName = _options.ModuleName ?? "App";
        
        // 1. Generate Events.{EntityName}.cs for NetMX.Events project
        // This file goes in framework/NetMX.Events/ directory
        var netmxEventsDir = FindNetMXEventsProjectDirectory();
        if (netmxEventsDir != null)
        {
            GenerateEventsPartialClass(netmxEventsDir, entityName, entityNameLower);
        }
        
        // TODO: Re-enable when package exports EventDirection properly
        // 2. Generate {EntityName}EventDefinitions.cs in Events/ directory
        // GenerateEventDefinitionsClass(webProjectDir, entityName, entityNameLower, moduleName);
        
        // 3. Generate extension method in Extensions/ directory
        // GenerateEventExtensionMethod(webProjectDir, entityName, moduleName);
    }

    private string? FindNetMXEventsProjectDirectory()
    {
        // Try to find NetMX.Events project from current directory
        var currentDir = Directory.GetCurrentDirectory();
        
        // Strategy 1: If we're in a module or app, go up to find framework/
        var searchDir = currentDir;
        for (int i = 0; i < 5; i++)
        {
            var frameworkDir = Path.Combine(searchDir, "framework", "NetMX.Events");
            if (Directory.Exists(frameworkDir))
            {
                return frameworkDir;
            }
            
            var parentDir = Directory.GetParent(searchDir);
            if (parentDir == null) break;
            searchDir = parentDir.FullName;
        }
        
        // Strategy 2: Check if we're already in NetMX.Events
        if (currentDir.EndsWith("NetMX.Events"))
        {
            return currentDir;
        }
        
        ConsoleHelper.WriteWarning("  ⚠️  Could not find NetMX.Events project. Skipping Events.{EntityName}.cs generation.");
        ConsoleHelper.WriteInfo("     Event constants will be generated in local Events/ directory instead.");
        return null;
    }

    private void GenerateEventsPartialClass(string netmxEventsDir, string entityName, string entityNameLower)
    {
        var eventsPartialClass = $@"namespace NetMX.Events;

/// <summary>
/// {entityName} entity events (partial extension of global Events class).
/// </summary>
/// <remarks>
/// This partial class extends the global <see cref=""Events""/> class with
/// {entityName}-specific event constants. This allows type-safe access like:
/// <code>Events.{entityName}.Created</code> from any module without project references.
/// </remarks>
public static partial class Events
{{
    /// <summary>
    /// {entityName}-related events.
    /// </summary>
    public static class {entityName}
    {{
        /// <summary>
        /// Event: ""{entityNameLower}.created""
        /// </summary>
        /// <remarks>
        /// Triggered when a new {entityName} is created.
        /// Payload: {{ {entityNameLower}Id: Guid }}
        /// </remarks>
        public const string Created = ""{entityNameLower}.created"";
        
        /// <summary>
        /// Event: ""{entityNameLower}.updated""
        /// </summary>
        /// <remarks>
        /// Triggered when a {entityName} is updated.
        /// Payload: {{ {entityNameLower}Id: Guid, changes: string[] }}
        /// </remarks>
        public const string Updated = ""{entityNameLower}.updated"";
        
        /// <summary>
        /// Event: ""{entityNameLower}.deleted""
        /// </summary>
        /// <remarks>
        /// Triggered when a {entityName} is deleted.
        /// Payload: {{ {entityNameLower}Id: Guid }}
        /// </remarks>
        public const string Deleted = ""{entityNameLower}.deleted"";
    }}
}}
";

        var filePath = Path.Combine(netmxEventsDir, $"Events.{entityName}.cs");
        File.WriteAllText(filePath, eventsPartialClass);
        ConsoleHelper.WriteSuccess($"  ✓ Generated Events.{entityName}.cs in NetMX.Events");
    }

    private void GenerateEventDefinitionsClass(string webProjectDir, string entityName, string entityNameLower, string moduleName)
    {
        var eventsDir = Path.Combine(webProjectDir, "Events");
        Directory.CreateDirectory(eventsDir);
        
        var projectNamespace = _options.ProjectNamespace ?? moduleName;
        
        var eventDefinitions = $@"using NetMX.Events;

namespace {projectNamespace}.Events;

/// <summary>
/// Defines and registers all {entityName} events.
/// </summary>
public static class {entityName}EventDefinitions
{{
    /// <summary>
    /// Registers all {entityName} events with the event registry.
    /// </summary>
    /// <param name=""registry"">The event registry to register events with.</param>
    public static void Register(IEventRegistry registry)
    {{
        registry.RegisterEvent(NetMX.Events.Events.{entityName}.Created, new EventMetadata
        {{
            Name = NetMX.Events.Events.{entityName}.Created,
            Module = ""{moduleName}"",
            Category = ""{entityName}"",
            Direction = NetMX.Events.EventDirection.Upstream,
            Description = ""Triggered when a new {entityName} is created. Payload: {{ {entityNameLower}Id: Guid }}""
        }});
        
        registry.RegisterEvent(NetMX.Events.Events.{entityName}.Updated, new EventMetadata
        {{
            Name = NetMX.Events.Events.{entityName}.Updated,
            Module = ""{moduleName}"",
            Category = ""{entityName}"",
            Direction = NetMX.Events.EventDirection.Upstream,
            Description = ""Triggered when a {entityName} is updated. Payload: {{ {entityNameLower}Id: Guid, changes: string[] }}""
        }});
        
        registry.RegisterEvent(NetMX.Events.Events.{entityName}.Deleted, new EventMetadata
        {{
            Name = NetMX.Events.Events.{entityName}.Deleted,
            Module = ""{moduleName}"",
            Category = ""{entityName}"",
            Direction = NetMX.Events.EventDirection.Terminal,
            Description = ""Triggered when a {entityName} is deleted. Payload: {{ {entityNameLower}Id: Guid }}""
        }});
    }}
}}
";

        var filePath = Path.Combine(eventsDir, $"{entityName}EventDefinitions.cs");
        File.WriteAllText(filePath, eventDefinitions);
    }

    private void GenerateEventExtensionMethod(string webProjectDir, string entityName, string moduleName)
    {
        var extensionsDir = Path.Combine(webProjectDir, "Extensions");
        Directory.CreateDirectory(extensionsDir);
        
        var projectNamespace = _options.ProjectNamespace ?? moduleName;
        var fileName = $"{entityName}EventExtensions.cs";
        var filePath = Path.Combine(extensionsDir, fileName);
        
        // Check if extensions file already exists
        if (File.Exists(filePath))
        {
            ConsoleHelper.WriteWarning($"  ⚠️  {fileName} already exists. Skipping extension method generation.");
            return;
        }
        
        var extensionMethod = $@"using NetMX.Events;
using {projectNamespace}.Events;

namespace {projectNamespace}.Extensions;

/// <summary>
/// Extension methods for registering {entityName} events.
/// </summary>
public static class {entityName}EventExtensions
{{
    /// <summary>
    /// Registers {entityName} events with the event registry.
    /// Call this during application startup after adding the event registry.
    /// </summary>
    /// <param name=""registry"">The event registry</param>
    /// <returns>The event registry for chaining</returns>
    public static IEventRegistry Add{entityName}Events(this IEventRegistry registry)
    {{
        {entityName}EventDefinitions.Register(registry);
        return registry;
    }}
}}
";

        File.WriteAllText(filePath, extensionMethod);
    }
}

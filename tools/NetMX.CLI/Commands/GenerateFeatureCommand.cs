using System.Text;
using NetMX.CLI.Infrastructure;

namespace NetMX.CLI.Commands;

/// <summary>
/// Command to generate a complete feature (entity with CRUD operations)
/// </summary>
public class GenerateFeatureCommand
{
    private readonly string _entityName;
    private readonly string? _module;
    private readonly bool _includeSearch;
    private readonly bool _includeExport;
    private readonly bool _autoMigrate;

    public GenerateFeatureCommand(string entityName, string? module = null, bool includeSearch = false, bool includeExport = false, bool autoMigrate = false)
    {
        _entityName = entityName;
        _module = module;
        _includeSearch = includeSearch;
        _includeExport = includeExport;
        _autoMigrate = autoMigrate;
    }

    public async Task<int> ExecuteAsync()
    {
        try
        {
            ConsoleHelper.WriteHeader($"Generating Feature: {_entityName}");

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

            ConsoleHelper.WriteStep(1, "Generating entity class");
            GenerateEntity(webProjectDir);

            ConsoleHelper.WriteStep(2, "Generating DTO classes");
            GenerateDtos(webProjectDir);

            ConsoleHelper.WriteStep(3, "Generating service interface and implementation");
            GenerateService(webProjectDir);

            ConsoleHelper.WriteStep(4, "Generating event constants");
            GenerateEventConstants(webProjectDir);

            ConsoleHelper.WriteStep(5, "Generating controller with HTMX support");
            GenerateController(webProjectDir);

            ConsoleHelper.WriteStep(6, "Generating views with HTMX patterns");
            GenerateViews(webProjectDir);

            ConsoleHelper.WriteSuccess($"Feature '{_entityName}' generated successfully!");
            ConsoleHelper.WriteInfo("Generated files:");
            
            if (!string.IsNullOrEmpty(_module))
            {
                // Module context
                ConsoleHelper.WriteInfo($"  - {_module}.Core/Entities/{_entityName}.cs");
                ConsoleHelper.WriteInfo($"  - {_module}.Contracts/Dtos/{_entityName}Dto.cs");
                ConsoleHelper.WriteInfo($"  - {_module}.Contracts/Dtos/Create{_entityName}Dto.cs");
                ConsoleHelper.WriteInfo($"  - {_module}.Contracts/Dtos/Update{_entityName}Dto.cs");
                ConsoleHelper.WriteInfo($"  - {_module}.Contracts/Services/I{_entityName}Service.cs");
                ConsoleHelper.WriteInfo($"  - {_module}.Application/Services/{_entityName}Service.cs");
                ConsoleHelper.WriteInfo($"  - {_module}.Web/Events/DomainEvents.{_entityName}.cs");
                ConsoleHelper.WriteInfo($"  - {_module}.Web/Controllers/{_entityName}Controller.cs");
                ConsoleHelper.WriteInfo($"  - {_module}.Web/Views/{_entityName}/Index.cshtml");
                ConsoleHelper.WriteInfo($"  - {_module}.Web/Views/{_entityName}/_List.cshtml");
                ConsoleHelper.WriteInfo($"  - {_module}.Web/Views/{_entityName}/_Form.cshtml");
            }
            else
            {
                // App context
                ConsoleHelper.WriteInfo($"  - Models/{_entityName}.cs");
                ConsoleHelper.WriteInfo($"  - Dtos/{_entityName}Dto.cs");
                ConsoleHelper.WriteInfo($"  - Dtos/Create{_entityName}Dto.cs");
                ConsoleHelper.WriteInfo($"  - Dtos/Update{_entityName}Dto.cs");
                ConsoleHelper.WriteInfo($"  - Services/I{_entityName}Service.cs");
                ConsoleHelper.WriteInfo($"  - Services/{_entityName}Service.cs");
                ConsoleHelper.WriteInfo($"  - Events/DomainEvents.{_entityName}.cs");
                ConsoleHelper.WriteInfo($"  - Controllers/{_entityName}Controller.cs");
                ConsoleHelper.WriteInfo($"  - Views/{_entityName}/Index.cshtml");
                ConsoleHelper.WriteInfo($"  - Views/{_entityName}/_List.cshtml");
                ConsoleHelper.WriteInfo($"  - Views/{_entityName}/_Form.cshtml");
            }

            // Auto-migration if requested
            if (_autoMigrate && string.IsNullOrEmpty(_module))
            {
                ConsoleHelper.WriteInfo("\n🔧 Auto-migration enabled...");
                
                // Check if EF Core tools are installed
                var efInstalled = await MigrationRunner.IsEfCoreInstalledAsync();
                if (!efInstalled)
                {
                    ConsoleHelper.WriteWarning("⚠️  EF Core tools not installed. Install with:");
                    ConsoleHelper.WriteInfo("   dotnet tool install --global dotnet-ef");
                    ConsoleHelper.WriteInfo("\nManual steps:");
                    ConsoleHelper.WriteInfo("  1. Add DbSet to your DbContext");
                    ConsoleHelper.WriteInfo($"  2. Run: dotnet ef migrations add Add{_entityName}");
                    ConsoleHelper.WriteInfo("  3. Run: dotnet ef database update");
                    return 0;
                }

                // Find DbContext
                var dbContextPath = DbContextInjector.FindDbContext();
                if (dbContextPath == null)
                {
                    ConsoleHelper.WriteWarning("⚠️  DbContext not found in current solution");
                    ConsoleHelper.WriteInfo("\nManual steps:");
                    ConsoleHelper.WriteInfo("  1. Add DbSet to your DbContext");
                    ConsoleHelper.WriteInfo($"  2. Run: dotnet ef migrations add Add{_entityName}");
                    ConsoleHelper.WriteInfo("  3. Run: dotnet ef database update");
                    return 0;
                }

                ConsoleHelper.WriteStep(7, "Injecting DbSet into DbContext");
                var dbSetAdded = await DbContextInjector.AddDbSetAsync(dbContextPath, _entityName);
                
                if (!dbSetAdded)
                {
                    ConsoleHelper.WriteWarning("⚠️  Failed to inject DbSet automatically");
                    ConsoleHelper.WriteInfo("\nManual steps:");
                    ConsoleHelper.WriteInfo($"  1. Add DbSet<{_entityName}> {_entityName}s => Set<{_entityName}>(); to your DbContext");
                    ConsoleHelper.WriteInfo($"  2. Run: dotnet ef migrations add Add{_entityName}");
                    ConsoleHelper.WriteInfo("  3. Run: dotnet ef database update");
                    return 0;
                }

                ConsoleHelper.WriteStep(8, $"Creating migration: Add{_entityName}");
                var migrationCreated = await MigrationRunner.CreateMigrationAsync($"Add{_entityName}", webProjectDir);
                
                if (!migrationCreated)
                {
                    ConsoleHelper.WriteWarning("⚠️  Failed to create migration automatically");
                    ConsoleHelper.WriteInfo($"\nDbSet added, but migration failed. Run manually:");
                    ConsoleHelper.WriteInfo($"  dotnet ef migrations add Add{_entityName}");
                    ConsoleHelper.WriteInfo("  dotnet ef database update");
                    return 0;
                }

                ConsoleHelper.WriteStep(9, "Applying migration to database");
                var dbUpdated = await MigrationRunner.UpdateDatabaseAsync(webProjectDir);
                
                if (!dbUpdated)
                {
                    ConsoleHelper.WriteWarning("⚠️  Failed to update database automatically");
                    ConsoleHelper.WriteInfo($"\nMigration created, but database update failed. Run manually:");
                    ConsoleHelper.WriteInfo("  dotnet ef database update");
                    return 0;
                }

                ConsoleHelper.WriteSuccess($"\n✅ Auto-migration complete! Database ready for {_entityName}");
                ConsoleHelper.WriteInfo($"\n🚀 Navigate to /{_entityName} to test your feature!");
            }
            else
            {
                ConsoleHelper.WriteInfo("\nNext steps:");
                if (!string.IsNullOrEmpty(_module))
                {
                    ConsoleHelper.WriteInfo("  1. Add entity to module DbContext");
                    ConsoleHelper.WriteInfo("  2. Register services in DI container");
                    ConsoleHelper.WriteInfo($"  3. Navigate to /{_entityName} to test");
                }
                else
                {
                    ConsoleHelper.WriteInfo("  1. Add DbSet to your DbContext");
                    ConsoleHelper.WriteInfo($"  2. Run: dotnet ef migrations add Add{_entityName}");
                    ConsoleHelper.WriteInfo("  3. Run: dotnet ef database update");
                    ConsoleHelper.WriteInfo($"  4. Navigate to /{_entityName} to test");
                    ConsoleHelper.WriteInfo($"\n💡 Tip: Use --migrate flag next time to automate steps 1-3!");
                }
            }

            return 0;
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"Failed to generate CRUD: {ex.Message}");
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

    private void GenerateEntity(string webProjectDir)
    {
        var isModuleContext = !string.IsNullOrEmpty(_module);
        
        if (isModuleContext)
        {
            // For modules, entity goes in Core project
            var moduleDir = Path.GetDirectoryName(webProjectDir)!; // Get parent of Audit.Web
            var entitiesDir = Path.Combine(moduleDir, $"{_module}.Core", "Entities");
            Directory.CreateDirectory(entitiesDir);

            var entityFile = Path.Combine(entitiesDir, $"{_entityName}.cs");
            
            var content = $$"""
using System.ComponentModel.DataAnnotations;
using NetMX.Ddd.Domain.Entities;

namespace {{_module}}.Core.Entities;

public class {{_entityName}} : Entity<Guid>
{
    [Required]
    [MaxLength(256)]
    public string Name { get; private set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; private set; }
    
    public bool IsActive { get; private set; } = true;
    
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; private set; }

    // EF Core requires a parameterless constructor
    private {{_entityName}}()
    {
    }

    public {{_entityName}}(Guid id, string name, string? description = null, bool isActive = true)
    {
        Id = id;
        Name = name;
        Description = description;
        IsActive = isActive;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateDetails(string name, string? description, bool isActive)
    {
        Name = name;
        Description = description;
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }
}
""";

            File.WriteAllText(entityFile, content);
        }
        else
        {
            // For apps, entity goes in Models folder
            var modelsDir = Path.Combine(webProjectDir, "Models");
            Directory.CreateDirectory(modelsDir);

            var entityFile = Path.Combine(modelsDir, $"{_entityName}.cs");
            
            var content = $$"""
using System.ComponentModel.DataAnnotations;

namespace {{Path.GetFileName(webProjectDir).Replace(".Web", "")}}.Models;

public class {{_entityName}}
{
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
}
""";

            File.WriteAllText(entityFile, content);
        }
    }

    private void GenerateDtos(string webProjectDir)
    {
        var isModuleContext = !string.IsNullOrEmpty(_module);
        
        if (isModuleContext)
        {
            // For modules, DTOs go in Contracts project
            var moduleDir = Path.GetDirectoryName(webProjectDir)!; // Get parent of Audit.Web
            var dtosDir = Path.Combine(moduleDir, $"{_module}.Contracts", "Dtos");
            Directory.CreateDirectory(dtosDir);

            var ns = $"{_module}.Contracts";

            // Read DTO
            var readDto = $$"""
using System.ComponentModel.DataAnnotations;

namespace {{ns}}.Dtos;

public class {{_entityName}}Dto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
""";

            // Create DTO
            var createDto = $$"""
using System.ComponentModel.DataAnnotations;

namespace {{ns}}.Dtos;

public class Create{{_entityName}}Dto
{
    [Required(ErrorMessage = "Name is required")]
    [MaxLength(256, ErrorMessage = "Name cannot exceed 256 characters")]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }
    
    public bool IsActive { get; set; } = true;
}
""";

            // Update DTO
            var updateDto = $$"""
using System.ComponentModel.DataAnnotations;

namespace {{ns}}.Dtos;

public class Update{{_entityName}}Dto
{
    public Guid Id { get; set; }
    
    [Required(ErrorMessage = "Name is required")]
    [MaxLength(256, ErrorMessage = "Name cannot exceed 256 characters")]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }
    
    public bool IsActive { get; set; }
}
""";

            File.WriteAllText(Path.Combine(dtosDir, $"{_entityName}Dto.cs"), readDto);
            File.WriteAllText(Path.Combine(dtosDir, $"Create{_entityName}Dto.cs"), createDto);
            File.WriteAllText(Path.Combine(dtosDir, $"Update{_entityName}Dto.cs"), updateDto);
        }
        else
        {
            // For apps, DTOs go in Dtos folder
            var dtosDir = Path.Combine(webProjectDir, "Dtos");
            Directory.CreateDirectory(dtosDir);

            var ns = Path.GetFileName(webProjectDir).Replace(".Web", "");

            // Read DTO
            var readDto = $$"""
using System.ComponentModel.DataAnnotations;

namespace {{ns}}.Dtos;

public class {{_entityName}}Dto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
""";

            // Create DTO
            var createDto = $$"""
using System.ComponentModel.DataAnnotations;

namespace {{ns}}.Dtos;

public class Create{{_entityName}}Dto
{
    [Required(ErrorMessage = "Name is required")]
    [MaxLength(256, ErrorMessage = "Name cannot exceed 256 characters")]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }
    
    public bool IsActive { get; set; } = true;
}
""";

            // Update DTO
            var updateDto = $$"""
using System.ComponentModel.DataAnnotations;

namespace {{ns}}.Dtos;

public class Update{{_entityName}}Dto
{
    public Guid Id { get; set; }
    
    [Required(ErrorMessage = "Name is required")]
    [MaxLength(256, ErrorMessage = "Name cannot exceed 256 characters")]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }
    
    public bool IsActive { get; set; }
}
""";

            File.WriteAllText(Path.Combine(dtosDir, $"{_entityName}Dto.cs"), readDto);
            File.WriteAllText(Path.Combine(dtosDir, $"Create{_entityName}Dto.cs"), createDto);
            File.WriteAllText(Path.Combine(dtosDir, $"Update{_entityName}Dto.cs"), updateDto);
        }
    }

    private void GenerateService(string webProjectDir)
    {
        var isModuleContext = !string.IsNullOrEmpty(_module);
        
        if (isModuleContext)
        {
            GenerateModuleService(webProjectDir);
        }
        else
        {
            GenerateAppService(webProjectDir);
        }
    }

    private void GenerateAppService(string webProjectDir)
    {
        var servicesDir = Path.Combine(webProjectDir, "Services");
        Directory.CreateDirectory(servicesDir);

        var ns = Path.GetFileName(webProjectDir).Replace(".Web", "");

        // Interface
        var interfaceContent = $$"""
using {{ns}}.Dtos;

namespace {{ns}}.Services;

public interface I{{_entityName}}Service
{
    Task<List<{{_entityName}}Dto>> GetAllAsync();
    Task<{{_entityName}}Dto?> GetByIdAsync(Guid id);
    Task<{{_entityName}}Dto> CreateAsync(Create{{_entityName}}Dto dto);
    Task<{{_entityName}}Dto> UpdateAsync(Update{{_entityName}}Dto dto);
    Task DeleteAsync(Guid id);
}
""";

        // Implementation
        var implContent = $$"""
using Microsoft.EntityFrameworkCore;
using {{ns}}.Data;
using {{ns}}.Dtos;
using {{ns}}.Models;

namespace {{ns}}.Services;

public class {{_entityName}}Service : I{{_entityName}}Service
{
    private readonly AppDbContext _context;

    public {{_entityName}}Service(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<{{_entityName}}Dto>> GetAllAsync()
    {
        return await _context.Set<{{_entityName}}>()
            .Select(e => new {{_entityName}}Dto
            {
                Id = e.Id,
                Name = e.Name,
                Description = e.Description,
                IsActive = e.IsActive,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<{{_entityName}}Dto?> GetByIdAsync(Guid id)
    {
        var entity = await _context.Set<{{_entityName}}>().FindAsync(id);
        if (entity == null) return null;

        return new {{_entityName}}Dto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public async Task<{{_entityName}}Dto> CreateAsync(Create{{_entityName}}Dto dto)
    {
        var entity = new {{_entityName}}
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<{{_entityName}}>().Add(entity);
        await _context.SaveChangesAsync();

        return new {{_entityName}}Dto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt
        };
    }

    public async Task<{{_entityName}}Dto> UpdateAsync(Update{{_entityName}}Dto dto)
    {
        var entity = await _context.Set<{{_entityName}}>().FindAsync(dto.Id);
        if (entity == null)
            throw new InvalidOperationException($"{{_entityName}} with ID {dto.Id} not found");

        entity.Name = dto.Name;
        entity.Description = dto.Description;
        entity.IsActive = dto.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new {{_entityName}}Dto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _context.Set<{{_entityName}}>().FindAsync(id);
        if (entity != null)
        {
            _context.Set<{{_entityName}}>().Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
""";

        File.WriteAllText(Path.Combine(servicesDir, $"I{_entityName}Service.cs"), interfaceContent);
        File.WriteAllText(Path.Combine(servicesDir, $"{_entityName}Service.cs"), implContent);
    }

    private void GenerateModuleService(string webProjectDir)
    {
        // For modules, we need to generate in multiple projects:
        // 1. Interface in Contracts project
        // 2. Implementation in Application project
        
        var moduleDir = Path.GetDirectoryName(webProjectDir)!; // Get parent of Audit.Web
        var contractsDir = Path.Combine(moduleDir, $"{_module}.Contracts", "Services");
        var applicationDir = Path.Combine(moduleDir, $"{_module}.Application", "Services");
        
        Directory.CreateDirectory(contractsDir);
        Directory.CreateDirectory(applicationDir);

        // Interface in Contracts
        var interfaceContent = $$"""
using {{_module}}.Contracts.Dtos;

namespace {{_module}}.Contracts.Services;

public interface I{{_entityName}}Service
{
    Task<List<{{_entityName}}Dto>> GetAllAsync();
    Task<{{_entityName}}Dto?> GetByIdAsync(Guid id);
    Task<{{_entityName}}Dto> CreateAsync(Create{{_entityName}}Dto dto);
    Task<{{_entityName}}Dto> UpdateAsync(Update{{_entityName}}Dto dto);
    Task DeleteAsync(Guid id);
}
""";

        // Implementation in Application using repository pattern
        var implContent = $$"""
using Microsoft.EntityFrameworkCore;
using NetMX.Ddd.Domain.Repositories;
using {{_module}}.Core.Entities;
using {{_module}}.Contracts.Dtos;
using {{_module}}.Contracts.Services;

namespace {{_module}}.Application.Services;

public class {{_entityName}}Service : I{{_entityName}}Service
{
    private readonly IQueryableRepository<{{_entityName}}, Guid> _repository;

    public {{_entityName}}Service(IQueryableRepository<{{_entityName}}, Guid> repository)
    {
        _repository = repository;
    }

    public async Task<List<{{_entityName}}Dto>> GetAllAsync()
    {
        var queryable = await _repository.GetQueryableAsync();
        return await queryable
            .Select(e => new {{_entityName}}Dto
            {
                Id = e.Id,
                Name = e.Name,
                Description = e.Description,
                IsActive = e.IsActive,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<{{_entityName}}Dto?> GetByIdAsync(Guid id)
    {
        var entity = await _repository.GetAsync(id);
        if (entity == null) return null;

        return new {{_entityName}}Dto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public async Task<{{_entityName}}Dto> CreateAsync(Create{{_entityName}}Dto dto)
    {
        var entity = new {{_entityName}}(
            Guid.NewGuid(),
            dto.Name,
            dto.Description,
            dto.IsActive
        );

        await _repository.InsertAsync(entity);

        return new {{_entityName}}Dto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt
        };
    }

    public async Task<{{_entityName}}Dto> UpdateAsync(Update{{_entityName}}Dto dto)
    {
        var entity = await _repository.GetAsync(dto.Id);
        if (entity == null)
            throw new InvalidOperationException($"{{_entityName}} with ID {dto.Id} not found");

        entity.UpdateDetails(dto.Name, dto.Description, dto.IsActive);
        await _repository.UpdateAsync(entity);

        return new {{_entityName}}Dto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public async Task DeleteAsync(Guid id)
    {
        await _repository.DeleteAsync(id);
    }
}
""";

        File.WriteAllText(Path.Combine(contractsDir, $"I{_entityName}Service.cs"), interfaceContent);
        File.WriteAllText(Path.Combine(applicationDir, $"{_entityName}Service.cs"), implContent);
    }

    private void GenerateController(string webProjectDir)
    {
        var controllersDir = Path.Combine(webProjectDir, "Controllers");
        Directory.CreateDirectory(controllersDir);

        var isModuleContext = !string.IsNullOrEmpty(_module);
        var ns = isModuleContext ? _module : Path.GetFileName(webProjectDir).Replace(".Web", "");
        
        var dtoNamespace = isModuleContext ? $"{_module}.Contracts.Dtos" : $"{ns}.Dtos";
        var serviceNamespace = isModuleContext ? $"{_module}.Contracts.Services" : $"{ns}.Services";

        var content = $$"""
using Microsoft.AspNetCore.Mvc;
using NetMX.AspNetCore.Mvc.Htmx;
using NetMX.Events;
using {{dtoNamespace}};
using {{serviceNamespace}};

namespace {{ns}}.Controllers;

public class {{_entityName}}Controller : Controller
{
    private readonly I{{_entityName}}Service _service;

    public {{_entityName}}Controller(I{{_entityName}}Service service)
    {
        _service = service;
    }

    // GET: /{{_entityName}}
    public async Task<IActionResult> Index()
    {
        var items = await _service.GetAllAsync();
        return View(items);
    }

    // GET: /{{_entityName}}/List (HTMX)
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var items = await _service.GetAllAsync();
        return PartialView("_List", items);
    }

    // GET: /{{_entityName}}/Create (HTMX)
    [HttpGet]
    public IActionResult Create()
    {
        return PartialView("_Form", new Create{{_entityName}}Dto());
    }

    // POST: /{{_entityName}}/Create (HTMX)
    [HttpPost]
    public async Task<IActionResult> Create(Create{{_entityName}}Dto dto)
    {
        if (!ModelState.IsValid)
        {
            return PartialView("_Form", dto);
        }

        var created = await _service.CreateAsync(dto);
        
        // Trigger type-safe event (extend DomainEvents with partial class for {{_entityName}})
        this.HxTrigger(DomainEvents.{{_entityName}}.Created, new { id = created.Id });
        
        return await List();
    }

    // GET: /{{_entityName}}/Edit/{id} (HTMX)
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var item = await _service.GetByIdAsync(id);
        if (item == null)
            return NotFound();

        var dto = new Update{{_entityName}}Dto
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            IsActive = item.IsActive
        };

        return PartialView("_Form", dto);
    }

    // POST: /{{_entityName}}/Edit (HTMX)
    [HttpPost]
    public async Task<IActionResult> Edit(Update{{_entityName}}Dto dto)
    {
        if (!ModelState.IsValid)
        {
            return PartialView("_Form", dto);
        }

        await _service.UpdateAsync(dto);
        
        // Trigger type-safe event (extend DomainEvents with partial class for {{_entityName}})
        this.HxTrigger(DomainEvents.{{_entityName}}.Updated, new { id = dto.Id });
        
        return await List();
    }

    // DELETE: /{{_entityName}}/Delete/{id} (HTMX)
    [HttpDelete]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        
        // Trigger type-safe event
        this.HxTrigger(DomainEvents.{{_entityName}}.Deleted, new { id });
        
        // Tell HTMX to remove the row
        this.HxReswap(HtmxSwap.Delete);
        
        return Ok();
    }
}
""";

        File.WriteAllText(Path.Combine(controllersDir, $"{_entityName}Controller.cs"), content);
    }

    private void GenerateViews(string webProjectDir)
    {
        var viewsDir = Path.Combine(webProjectDir, "Views", _entityName);
        Directory.CreateDirectory(viewsDir);

        var isModuleContext = !string.IsNullOrEmpty(_module);
        var dtoNamespace = isModuleContext ? $"{_module}.Contracts.Dtos" : $"{Path.GetFileName(webProjectDir).Replace(".Web", "")}.Dtos";

        // Index.cshtml
        var indexView = $$"""
@model List<{{_entityName}}Dto>
@using {{dtoNamespace}}
@using NetMX.Events
@{
    ViewData["Title"] = "{{_entityName}} Management";
}

<div class="hero is-primary">
    <div class="hero-body">
        <p class="title">{{_entityName}} Management</p>
        <p class="subtitle">Manage your {{_entityName.ToLower()}}s</p>
    </div>
</div>

<div class="container mt-5">
    <div class="box">
        <div class="level mb-4">
            <div class="level-left">
                <div class="level-item">
                    <h2 class="title is-4">All {{_entityName}}s</h2>
                </div>
            </div>
            <div class="level-right">
                <div class="level-item">
                    <button class="button is-primary" 
                            hx-get="/{{_entityName}}/Create" 
                            hx-target="#form-container" 
                            hx-swap="innerHTML">
                        <span class="icon">
                            <i class="fas fa-plus"></i>
                        </span>
                        <span>Add {{_entityName}}</span>
                    </button>
                </div>
            </div>
        </div>

        <!-- Form Container (hidden initially) -->
        <div id="form-container" class="mb-4"></div>

        <!-- List Container -->
        <div id="list-container" 
             hx-get="/{{_entityName}}/List" 
             hx-trigger="load, @DomainEvents.{{_entityName}}.Created from:body, @DomainEvents.{{_entityName}}.Updated from:body">
            <div class="has-text-centered">
                <span class="icon is-large">
                    <i class="fas fa-spinner fa-pulse"></i>
                </span>
            </div>
        </div>
    </div>
</div>


<style>
    .htmx-indicator {
        display: none;
    }

    .htmx-request .htmx-indicator,
    .htmx-request.htmx-indicator {
        display: inline-block;
    }
</style>
""";

        // _List.cshtml
        var listView = $$"""
@model List<{{_entityName}}Dto>
@using {{dtoNamespace}}

@if (Model.Any())
{
    <table class="table is-fullwidth is-striped is-hoverable">
        <thead>
            <tr>
                <th>Name</th>
                <th>Description</th>
                <th>Status</th>
                <th>Created</th>
                <th>Actions</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var item in Model)
            {
                <tr id="row-@item.Id">
                    <td>@item.Name</td>
                    <td>@(item.Description ?? "-")</td>
                    <td>
                        <span class="tag @(item.IsActive ? "is-success" : "is-danger")">
                            @(item.IsActive ? "Active" : "Inactive")
                        </span>
                    </td>
                    <td>@item.CreatedAt.ToString("yyyy-MM-dd")</td>
                    <td>
                        <div class="buttons are-small">
                            <button class="button is-info" 
                                    hx-get="/{{_entityName}}/Edit/@item.Id" 
                                    hx-target="#form-container" 
                                    hx-swap="innerHTML">
                                <span class="icon is-small">
                                    <i class="fas fa-edit"></i>
                                </span>
                            </button>
                            <button class="button is-danger" 
                                    hx-delete="/{{_entityName}}/Delete/@item.Id" 
                                    hx-target="#row-@item.Id" 
                                    hx-confirm="Are you sure you want to delete @item.Name?">
                                <span class="icon is-small">
                                    <i class="fas fa-trash"></i>
                                </span>
                            </button>
                        </div>
                    </td>
                </tr>
            }
        </tbody>
    </table>
}
else
{
    <div class="notification is-info has-text-centered">
        <p>No {{_entityName.ToLower()}}s found. Click "Add {{_entityName}}" to create one.</p>
    </div>
}
""";

        // _Form.cshtml
        var formView = $$"""
@model dynamic
@{
    var isCreate = Model.GetType().Name.StartsWith("Create");
    var title = isCreate ? "Add {{_entityName}}" : "Edit {{_entityName}}";
    var action = isCreate ? "/{{_entityName}}/Create" : "/{{_entityName}}/Edit";
}

<div class="box has-background-light">
    <h3 class="title is-5">@title</h3>
    
    <form hx-post="@action" 
          hx-target="#list-container" 
          hx-swap="innerHTML">
        
        @if (!isCreate)
        {
            <input type="hidden" name="Id" value="@Model.Id" />
        }

        <div class="field">
            <label class="label">Name</label>
            <div class="control">
                <input class="input" type="text" name="Name" value="@Model.Name" required>
            </div>
        </div>

        <div class="field">
            <label class="label">Description</label>
            <div class="control">
                <textarea class="textarea" name="Description" rows="3">@Model.Description</textarea>
            </div>
        </div>

        <div class="field">
            <label class="checkbox">
                <input type="checkbox" name="IsActive" value="true" @(Model.IsActive ? "checked" : "")>
                Active
            </label>
        </div>

        <div class="field is-grouped">
            <div class="control">
                <button type="submit" class="button is-primary">
                    <span class="icon">
                        <i class="fas fa-save"></i>
                    </span>
                    <span>Save</span>
                </button>
            </div>
            <div class="control">
                <button type="button" 
                        class="button is-light" 
                        onclick="document.getElementById('form-container').innerHTML = ''">
                    Cancel
                </button>
            </div>
        </div>
    </form>
</div>
""";

        File.WriteAllText(Path.Combine(viewsDir, "Index.cshtml"), indexView);
        File.WriteAllText(Path.Combine(viewsDir, "_List.cshtml"), listView);
        File.WriteAllText(Path.Combine(viewsDir, "_Form.cshtml"), formView);
    }

    private void GenerateEventConstants(string webProjectDir)
    {
        var eventsDir = Path.Combine(webProjectDir, "Events");
        Directory.CreateDirectory(eventsDir);

        var content = $$"""
namespace NetMX.Events;

/// <summary>
/// Event constants for {{_entityName}} entity.
/// </summary>
/// <remarks>
/// This partial class extends DomainEvents with {{_entityName}}-specific events.
/// Use these constants in controllers and views for type-safe event communication.
/// </remarks>
public static partial class DomainEvents
{
    /// <summary>
    /// {{_entityName}}-related events.
    /// </summary>
    public static class {{_entityName}}
    {
        /// <summary>
        /// Triggered when a new {{_entityName.ToLower()}} is created.
        /// Payload: { id: Guid }
        /// </summary>
        public const string Created = "{{_entityName.ToLower()}}:created";
        
        /// <summary>
        /// Triggered when a {{_entityName.ToLower()}} is updated.
        /// Payload: { id: Guid }
        /// </summary>
        public const string Updated = "{{_entityName.ToLower()}}:updated";
        
        /// <summary>
        /// Triggered when a {{_entityName.ToLower()}} is deleted.
        /// Payload: { id: Guid }
        /// </summary>
        public const string Deleted = "{{_entityName.ToLower()}}:deleted";
    }
}
""";

        File.WriteAllText(Path.Combine(eventsDir, $"DomainEvents.{_entityName}.cs"), content);
    }
}

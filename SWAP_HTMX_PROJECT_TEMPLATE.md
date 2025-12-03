# Swap.Htmx Project Template Guide

> **Purpose:** This document provides a complete specification for creating new ASP.NET Core projects using the Swap.Htmx modular monolith architecture. Pass this to an LLM to scaffold a production-ready project.

---

## Overview

This template creates a **modular monolith** ASP.NET Core application with:

- **Swap.Htmx** for HTMX orchestration and server-driven UI
- **SQLite** with Entity Framework Core for data persistence
- **Modular architecture** with self-contained feature modules
- **Source generators** for type-safe events, views, and elements
- **WebOptimizer** for CSS/JS bundling
- **Docker** support with docker-compose

---

## Project Structure

```
[ProjectName]/
├── [ProjectName].sln
├── docker-compose.yml
├── README.md
├── docs/
│   └── PROJECT_STRUCTURE.md
└── src/
    ├── [ProjectName].csproj
    ├── Program.cs
    ├── appsettings.json
    ├── appsettings.Development.json
    ├── libman.json
    ├── Dockerfile
    │
    ├── Controllers/                  # Shared/non-module controllers
    │   └── HomeController.cs
    │
    ├── Data/                         # Database layer
    │   ├── AppDbContext.cs
    │   ├── IAuditableEntity.cs
    │   ├── PaginatedList.cs
    │   ├── Configurations/
    │   │   └── ModelBuilderExtensions.cs
    │   └── Seeding/
    │       └── MasterDataSeeder.cs
    │
    ├── Infrastructure/               # Cross-cutting concerns
    │   ├── ServiceCollectionExtensions.cs
    │   ├── ApplicationBuilderExtensions.cs
    │   ├── MvcExtensions.cs
    │   ├── ModuleViewLocationExpander.cs
    │   ├── WebOptimizerExtensions.cs
    │   └── InvariantDecimalModelBinder.cs
    │
    ├── Modules/                      # Feature modules (self-contained)
    │   └── [ModuleName]/
    │       ├── [ModuleName]Module.cs
    │       ├── Controllers/
    │       │   └── [ModuleName]Controller.cs
    │       ├── Entities/
    │       │   └── [Entity].cs
    │       ├── Services/
    │       │   ├── [ModuleName]Service.cs
    │       │   └── [ModuleName]Dtos.cs
    │       ├── Events/
    │       │   ├── [ModuleName]Events.cs
    │       │   └── [ModuleName]EventConfig.cs
    │       └── Views/
    │           ├── ViewSources.cs
    │           ├── ElementSources.cs
    │           ├── _ViewImports.cshtml
    │           ├── _ViewStart.cshtml
    │           ├── Index.cshtml
    │           └── _*.cshtml (partials)
    │
    ├── Services/                     # Shared infrastructure services
    │
    ├── Views/                        # Shared views
    │   ├── _ViewImports.cshtml
    │   ├── _ViewStart.cshtml
    │   ├── ViewSources.cs
    │   ├── ElementSources.cs
    │   ├── Shared/
    │   │   ├── _Layout.cshtml
    │   │   └── _Toast.cshtml
    │   └── Home/
    │       └── Index.cshtml
    │
    └── wwwroot/
        ├── css/
        │   ├── base.css
        │   ├── layout.css
        │   ├── components.css
        │   ├── forms.css
        │   └── responsive.css
        ├── js/
        │   └── layout.js
        └── lib/                      # Third-party (via libman)
```

---

## File Templates

### 1. Project File ([ProjectName].csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
    <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
  </PropertyGroup>

  <!-- Required for Swap.Htmx source generators to scan views -->
  <ItemGroup>
    <AdditionalFiles Include="Views\**\*.cshtml" />
    <AdditionalFiles Include="Modules\**\Views\**\*.cshtml" />
  </ItemGroup>

  <!-- Exclude generated files from compilation (prevents duplicates) -->
  <ItemGroup>
    <Compile Remove="$(CompilerGeneratedFilesOutputPath)/**/*.cs" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="LigerShark.WebOptimizer.Core" Version="3.0.477" />
    <PackageReference Include="Swap.Htmx" Version="1.0.1" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.11" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="9.0.11" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.11">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="9.0.11">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

</Project>
```

### 2. Program.cs

```csharp
using [ProjectName].Infrastructure;
using [ProjectName].Data;
// Add module using statements as modules are created
// using [ProjectName].Modules.[ModuleName];

var builder = WebApplication.CreateBuilder(args);

// =============================================================================
// INFRASTRUCTURE SERVICES
// =============================================================================

builder.Services.AddDataProtectionConfig(builder.Environment);
builder.Services.AddCompressionConfig();

// =============================================================================
// DATABASE & CORE SERVICES
// =============================================================================

builder.Services.AddDatabaseConfig(builder.Configuration);
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database");
builder.Services.AddCoreServices();

// =============================================================================
// DOMAIN MODULES
// =============================================================================

// Register modules here as they are created:
// builder.Services.Add[ModuleName]Module();

// =============================================================================
// MVC & WEB
// =============================================================================

builder.Services.AddWebOptimizerConfig(builder.Environment);
builder.Services.AddMvcConfig();
builder.Services.AddSwapHtmxConfig();

// =============================================================================
// BUILD & RUN
// =============================================================================

var app = builder.Build();

await app.InitializeDatabaseAsync();
app.ConfigurePipeline();
app.MapEndpoints();

app.Run();
```

### 3. Infrastructure/ServiceCollectionExtensions.cs

```csharp
using System.IO.Compression;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using [ProjectName].Data;

namespace [ProjectName].Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDataProtectionConfig(this IServiceCollection services, IWebHostEnvironment environment)
    {
        var keysPath = Path.Combine(environment.ContentRootPath, "keys");
        var isDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

        if (isDocker)
        {
            keysPath = "/app/keys";
        }

        if (!Directory.Exists(keysPath))
        {
            Directory.CreateDirectory(keysPath);
        }

        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
            .SetApplicationName("[projectname]");

        return services;
    }

    public static IServiceCollection AddCompressionConfig(this IServiceCollection services)
    {
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });

        services.Configure<BrotliCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Fastest;
        });

        services.Configure<GzipCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.SmallestSize;
        });

        return services;
    }

    public static IServiceCollection AddDatabaseConfig(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection"));
        });

        return services;
    }

    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        // Add shared services here
        return services;
    }
}
```

### 4. Infrastructure/ApplicationBuilderExtensions.cs

```csharp
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using [ProjectName].Data;
using Swap.Htmx;

namespace [ProjectName].Infrastructure;

public static class ApplicationBuilderExtensions
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (db.Database.GetPendingMigrations().Any())
        {
            db.Database.Migrate();
        }

        // Enable WAL mode for better concurrent read performance
        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.UseResponseCompression();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseWebOptimizer();
        app.UseStaticFiles();
        app.UseRouting();

        app.UseSwapHtmx();

        return app;
    }

    public static WebApplication MapEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var result = new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        duration = e.Value.Duration.TotalMilliseconds
                    })
                };
                await context.Response.WriteAsJsonAsync(result);
            }
        });

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        return app;
    }
}
```

### 5. Infrastructure/MvcExtensions.cs

```csharp
using Swap.Htmx;
// Add module event config using statements as modules are created

namespace [ProjectName].Infrastructure;

public static class MvcExtensions
{
    public static IServiceCollection AddMvcConfig(this IServiceCollection services)
    {
        services.AddControllersWithViews(options =>
        {
            options.ModelBinderProviders.Insert(0, new InvariantDecimalModelBinderProvider());
        }).AddRazorOptions(options =>
        {
            options.AddModuleViewLocations();
        });

        return services;
    }

    public static IServiceCollection AddSwapHtmxConfig(this IServiceCollection services)
    {
        services.AddSwapHtmx(options =>
        {
            // Add module event configs as modules are created:
            // options.AddConfig<[ModuleName]EventConfig>();
        });

        return services;
    }
}
```

### 6. Infrastructure/ModuleViewLocationExpander.cs

```csharp
using Microsoft.AspNetCore.Mvc.Razor;

namespace [ProjectName].Infrastructure;

public class ModuleViewLocationExpander : IViewLocationExpander
{
    // Map controller names to module folders
    private static readonly Dictionary<string, ModuleViewConfig> ControllerModuleMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // Add mappings as modules are created:
        // ["ControllerName"] = new("ModuleFolderName"),
    };

    public void PopulateValues(ViewLocationExpanderContext context) { }

    public IEnumerable<string> ExpandViewLocations(
        ViewLocationExpanderContext context, 
        IEnumerable<string> viewLocations)
    {
        if (context.ControllerName != null && 
            ControllerModuleMap.TryGetValue(context.ControllerName, out var config))
        {
            var moduleLocations = config.SubFolder != null
                ? new[]
                {
                    $"/Modules/{config.ModuleName}/Views/{config.SubFolder}/{{0}}.cshtml",
                    $"/Modules/{config.ModuleName}/Views/Shared/{{0}}.cshtml"
                }
                : new[]
                {
                    $"/Modules/{config.ModuleName}/Views/{{0}}.cshtml",
                    $"/Modules/{config.ModuleName}/Views/Shared/{{0}}.cshtml"
                };

            return moduleLocations.Concat(viewLocations);
        }

        return viewLocations;
    }

    private record ModuleViewConfig(string ModuleName, string? SubFolder = null);
}

public static class ModuleViewLocationExtensions
{
    public static void AddModuleViewLocations(this RazorViewEngineOptions options)
    {
        options.ViewLocationExpanders.Add(new ModuleViewLocationExpander());
    }
}
```

### 7. Infrastructure/InvariantDecimalModelBinder.cs

```csharp
using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace [ProjectName].Infrastructure;

public class InvariantDecimalModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        
        if (valueProviderResult == ValueProviderResult.None)
            return Task.CompletedTask;

        var value = valueProviderResult.FirstValue;
        
        if (string.IsNullOrWhiteSpace(value))
            return Task.CompletedTask;

        // Try parsing with invariant culture (123.45)
        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
        {
            bindingContext.Result = ModelBindingResult.Success(result);
            return Task.CompletedTask;
        }

        // Try parsing with current culture (123,45 for European locales)
        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out result))
        {
            bindingContext.Result = ModelBindingResult.Success(result);
            return Task.CompletedTask;
        }

        bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, "Invalid decimal format");
        return Task.CompletedTask;
    }
}

public class InvariantDecimalModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (context.Metadata.ModelType == typeof(decimal) || context.Metadata.ModelType == typeof(decimal?))
        {
            return new InvariantDecimalModelBinder();
        }
        return null;
    }
}
```

### 8. Infrastructure/WebOptimizerExtensions.cs

```csharp
using WebOptimizer;

namespace [ProjectName].Infrastructure;

public static class WebOptimizerExtensions
{
    public static IServiceCollection AddWebOptimizerConfig(this IServiceCollection services, IWebHostEnvironment environment)
    {
        services.AddWebOptimizer(pipeline =>
        {
            // Main CSS bundle
            pipeline.AddCssBundle("/css/bundle.css",
                "css/base.css",
                "css/layout.css",
                "css/components.css",
                "css/forms.css",
                "css/responsive.css"
                // Add more CSS files as needed
            ).MinifyCss();

            // Main JS bundle
            pipeline.AddJavaScriptBundle("/js/bundle.js",
                "js/layout.js"
                // Add more JS files as needed
            ).MinifyJavaScript();
        },
        options =>
        {
            options.EnableTagHelperBundling = true;
            
            if (environment.IsDevelopment())
            {
                options.EnableCaching = false;
                options.EnableMemoryCache = false;
                options.EnableDiskCache = false;
            }
            else
            {
                options.EnableCaching = true;
                options.EnableMemoryCache = true;
                options.EnableDiskCache = false;
            }
        });

        return services;
    }
}
```

### 9. Data/AppDbContext.cs

```csharp
using [ProjectName].Data.Configurations;
using [ProjectName].Data.Seeding;
using Microsoft.EntityFrameworkCore;

namespace [ProjectName].Data;

public class AppDbContext : DbContext
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor httpContextAccessor) 
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    #region DbSets
    
    // Add DbSets as entities are created:
    // public DbSet<Entity> Entities { get; set; }

    #endregion

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditFields();
        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyAppConfigurations();
        MasterDataSeeder.Seed(modelBuilder);
    }

    private void ApplyAuditFields()
    {
        var userId = _httpContextAccessor?.HttpContext?.User?
            .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedByUserId = userId;
                    entry.Entity.CreatedAt = now;
                    break;

                case EntityState.Modified:
                    entry.Entity.ModifiedByUserId = userId;
                    entry.Entity.UpdatedAt = now;
                    break;
            }
        }
    }
}
```

### 10. Data/IAuditableEntity.cs

```csharp
namespace [ProjectName].Data;

public interface IAuditableEntity
{
    string? CreatedByUserId { get; set; }
    DateTime CreatedAt { get; set; }
    string? ModifiedByUserId { get; set; }
    DateTime? UpdatedAt { get; set; }
}
```

### 11. Data/PaginatedList.cs

```csharp
using Microsoft.EntityFrameworkCore;

namespace [ProjectName].Data;

public class PaginatedList<T>
{
    public List<T> Items { get; }
    public int PageIndex { get; }
    public int TotalPages { get; }
    public int TotalCount { get; }
    public int PageSize { get; }

    public PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
    {
        PageIndex = pageIndex;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        TotalCount = count;
        PageSize = pageSize;
        Items = items;
    }

    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;

    public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageIndex, int pageSize)
    {
        var count = await source.CountAsync();
        var items = await source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PaginatedList<T>(items, count, pageIndex, pageSize);
    }
}
```

### 12. Data/Configurations/ModelBuilderExtensions.cs

```csharp
using Microsoft.EntityFrameworkCore;

namespace [ProjectName].Data.Configurations;

public static class ModelBuilderExtensions
{
    public static ModelBuilder ApplyAppConfigurations(this ModelBuilder modelBuilder)
    {
        // Add configuration calls as entity configurations are created:
        // [EntityName]Configuration.Configure(modelBuilder);
        
        return modelBuilder;
    }
}
```

### 13. Data/Seeding/MasterDataSeeder.cs

```csharp
using Microsoft.EntityFrameworkCore;

namespace [ProjectName].Data.Seeding;

public static class MasterDataSeeder
{
    public static void Seed(ModelBuilder modelBuilder)
    {
        // Add seed methods as entities are created:
        // SeedEntityName(modelBuilder);
    }
    
    // Example:
    // private static void SeedEntityName(ModelBuilder modelBuilder)
    // {
    //     modelBuilder.Entity<EntityName>().HasData(
    //         new EntityName { Id = 1, Name = "Item 1" }
    //     );
    // }
}
```

---

## Module Template

When creating a new module, create these files:

### Modules/[ModuleName]/[ModuleName]Module.cs

```csharp
using [ProjectName].Modules.[ModuleName].Services;

namespace [ProjectName].Modules.[ModuleName];

public static class [ModuleName]Module
{
    public static IServiceCollection Add[ModuleName]Module(this IServiceCollection services)
    {
        services.AddScoped<I[ModuleName]Service, [ModuleName]Service>();
        return services;
    }
}
```

### Modules/[ModuleName]/Entities/[Entity].cs

```csharp
namespace [ProjectName].Modules.[ModuleName].Entities;

public class [Entity]
{
    public int Id { get; set; }
    public required string Name { get; set; }
    // Add properties as needed
}
```

### Modules/[ModuleName]/Services/I[ModuleName]Service.cs

```csharp
using [ProjectName].Modules.[ModuleName].Entities;

namespace [ProjectName].Modules.[ModuleName].Services;

public interface I[ModuleName]Service
{
    Task<List<[Entity]>> GetAllAsync();
    Task<[Entity]?> GetByIdAsync(int id);
    Task<[Entity]> CreateAsync([Entity] entity);
    Task UpdateAsync(int id, [Entity] entity);
    Task DeleteAsync(int id);
}
```

### Modules/[ModuleName]/Services/[ModuleName]Service.cs

```csharp
using Microsoft.EntityFrameworkCore;
using [ProjectName].Data;
using [ProjectName].Modules.[ModuleName].Entities;

namespace [ProjectName].Modules.[ModuleName].Services;

public class [ModuleName]Service : I[ModuleName]Service
{
    private readonly AppDbContext _db;

    public [ModuleName]Service(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<[Entity]>> GetAllAsync()
    {
        return await _db.[Entities].ToListAsync();
    }

    public async Task<[Entity]?> GetByIdAsync(int id)
    {
        return await _db.[Entities].FindAsync(id);
    }

    public async Task<[Entity]> CreateAsync([Entity] entity)
    {
        _db.[Entities].Add(entity);
        await _db.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync(int id, [Entity] entity)
    {
        var existing = await _db.[Entities].FindAsync(id) 
            ?? throw new InvalidOperationException("Entity not found");
        
        // Update properties
        existing.Name = entity.Name;
        
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _db.[Entities].FindAsync(id)
            ?? throw new InvalidOperationException("Entity not found");
        
        _db.[Entities].Remove(entity);
        await _db.SaveChangesAsync();
    }
}
```

### Modules/[ModuleName]/Events/[ModuleName]Events.cs

```csharp
using Swap.Htmx.Attributes;

namespace [ProjectName].Modules.[ModuleName].Events;

[SwapEventSource]
public static partial class [ModuleName]Events
{
    // Item lifecycle events (format: category_subcategory)
    public const string Item_Created = "item.created";
    public const string Item_Updated = "item.updated";
    public const string Item_Deleted = "item.deleted";
    
    // State change events
    public const string Data_Changed = "data.changed";
    public const string State_Changed = "state.changed";
}
```

### Modules/[ModuleName]/Events/[ModuleName]EventConfig.cs

```csharp
using Swap.Htmx;
using Swap.Htmx.Events;

namespace [ProjectName].Modules.[ModuleName].Events;

public class [ModuleName]EventConfig : ISwapEventConfiguration
{
    public void Configure(SwapEventBusOptions events)
    {
        events.When([ModuleName]Events.Item.Created)
              .AlsoTrigger([ModuleName]Events.Data.Changed)
              .SuccessToast("[Entity] created successfully");

        events.When([ModuleName]Events.Item.Updated)
              .AlsoTrigger([ModuleName]Events.Data.Changed)
              .SuccessToast("[Entity] updated");

        events.When([ModuleName]Events.Item.Deleted)
              .AlsoTrigger([ModuleName]Events.Data.Changed)
              .SuccessToast("[Entity] deleted");
    }
}
```

### Modules/[ModuleName]/Views/ViewSources.cs

```csharp
using Swap.Htmx.Attributes;

namespace [ProjectName].Modules.[ModuleName].Views;

[SwapViewSource("Modules/[ModuleName]/Views")]
public static partial class [ModuleName]Views { }
```

### Modules/[ModuleName]/Views/ElementSources.cs

```csharp
using Swap.Htmx.Attributes;

namespace [ProjectName].Modules.[ModuleName].Views;

[SwapElementSource("Modules/[ModuleName]/Views")]
public static partial class [ModuleName]Elements { }
```

### Modules/[ModuleName]/Views/_ViewImports.cshtml

```razor
@using [ProjectName]
@using [ProjectName].Modules.[ModuleName].Entities
@using [ProjectName].Modules.[ModuleName].Services
@using [ProjectName].Modules.[ModuleName].Events
@using [ProjectName].Modules.[ModuleName].Views
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@addTagHelper *, Swap.Htmx
```

### Modules/[ModuleName]/Views/_ViewStart.cshtml

```razor
@{
    Layout = "_Layout";
}
```

### Modules/[ModuleName]/Controllers/[ModuleName]Controller.cs

```csharp
using Microsoft.AspNetCore.Mvc;
using Swap.Htmx;
using [ProjectName].Modules.[ModuleName].Entities;
using [ProjectName].Modules.[ModuleName].Events;
using [ProjectName].Modules.[ModuleName].Services;
using [ProjectName].Modules.[ModuleName].Views;

namespace [ProjectName].Modules.[ModuleName].Controllers;

public class [ModuleName]Controller : SwapController
{
    private readonly I[ModuleName]Service _service;

    public [ModuleName]Controller(I[ModuleName]Service service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var items = await _service.GetAllAsync();
        return SwapView(items);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return SwapView([ModuleName]Views.Partials.CreateModal);
    }

    [HttpPost]
    public async Task<IActionResult> Create([Entity] entity)
    {
        if (!ModelState.IsValid)
        {
            return SwapResponse()
                .WithView([ModuleName]Views.Partials.CreateModal, entity)
                .WithErrorToast("Please fix the errors.")
                .Build();
        }

        await _service.CreateAsync(entity);
        return SwapEvent([ModuleName]Events.Item.Created).Build();
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await _service.GetByIdAsync(id);
        if (item == null)
        {
            return SwapResponse().WithErrorToast("Item not found.").Build();
        }
        return SwapView([ModuleName]Views.Partials.EditModal, item);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, [Entity] entity)
    {
        try
        {
            await _service.UpdateAsync(id, entity);
            return SwapEvent([ModuleName]Events.Item.Updated).Build();
        }
        catch (InvalidOperationException)
        {
            return SwapResponse().WithErrorToast("Item not found.").Build();
        }
    }

    [HttpGet]
    public async Task<IActionResult> DeleteConfirm(int id)
    {
        var item = await _service.GetByIdAsync(id);
        if (item == null)
        {
            return SwapResponse().WithErrorToast("Item not found.").Build();
        }
        return SwapView([ModuleName]Views.Partials.DeleteConfirmModal, item);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return SwapEvent([ModuleName]Events.Item.Deleted).Build();
        }
        catch (InvalidOperationException)
        {
            return SwapResponse().WithErrorToast("Item not found.").Build();
        }
    }
}
```

---

## View Templates

### Views/_ViewImports.cshtml

```razor
@using [ProjectName]
@using [ProjectName].Views
@using [ProjectName].Elements
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@addTagHelper *, Swap.Htmx
@addTagHelper *, WebOptimizer.Core
```

### Views/_ViewStart.cshtml

```razor
@{
    Layout = "_Layout";
}
```

### Views/Shared/_Layout.cshtml

```razor
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@ViewData["Title"] - [ProjectName]</title>
    <link rel="stylesheet" href="/css/bundle.css" />
    <link rel="stylesheet" href="~/_content/Swap.Htmx/css/swap.css" asp-append-version="true" />
</head>
<body>
    <!-- Sidebar -->
    <aside class="sidebar" id="sidebar">
        <div class="sidebar-header">
            <div class="brand">[ProjectName]</div>
            <button class="sidebar-collapse-btn" onclick="toggleSidebar()">☰</button>
        </div>
        <nav class="nav-menu">
            <a hx-get="@Url.Action("Index", "Home")" 
               hx-target=".main-content" 
               hx-swap="innerHTML"
               hx-push-url="true"
               class="nav-item @(ViewContext.RouteData.Values["Controller"]?.ToString() == "Home" ? "active" : "")">
                <span class="nav-icon">📊</span> Dashboard
            </a>
            <!-- Add more nav items for modules -->
        </nav>
    </aside>

    <!-- Main Content -->
    <main class="main-content">
        @RenderBody()
    </main>

    <div id="toast-container"></div>

    <script src="~/lib/htmx/dist/htmx.min.js" asp-append-version="true"></script>
    <script src="~/_content/Swap.Htmx/js/swap.client.js" asp-append-version="true"></script>
    <script src="/js/bundle.js"></script>

    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

### Views/Home/Index.cshtml

```razor
@{
    ViewData["Title"] = "Dashboard";
}

<header>
    <h1 class="page-title">Dashboard</h1>
</header>

<div class="content-scroll">
    <div class="dashboard-grid">
        <div class="card">
            <h3>Welcome to [ProjectName]</h3>
            <p>Your application is ready.</p>
        </div>
    </div>
</div>
```

---

## Configuration Files

### appsettings.json

```json
{
  "Environment": "Development",
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=[projectname].db"
  }
}
```

### appsettings.Development.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Swap.Htmx": "Debug"
    }
  }
}
```

### libman.json

```json
{
  "version": "1.0",
  "defaultProvider": "unpkg",
  "libraries": [
    {
      "library": "htmx.org@2.0.8",
      "destination": "wwwroot/lib/htmx",
      "files": [
        "dist/htmx.min.js"
      ]
    }
  ]
}
```

---

## Docker Files

### Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
RUN dotnet tool install -g Microsoft.Web.LibraryManager.Cli
ENV PATH="$PATH:/root/.dotnet/tools"

WORKDIR /src
COPY ["[ProjectName].csproj", "./"]
RUN dotnet restore "[ProjectName].csproj"

COPY ["libman.json", "./"]
RUN libman restore

COPY . .
RUN dotnet publish "[ProjectName].csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=build /src/wwwroot ./wwwroot

RUN mkdir -p /app/data /app/keys && chmod -R 700 /app/keys
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/[projectname].db"
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_HTTP_PORTS=8080

ENTRYPOINT ["dotnet", "[ProjectName].dll"]
```

### docker-compose.yml

```yaml
services:
  app:
    container_name: [projectname]_app
    build:
      context: src
      dockerfile: Dockerfile
    restart: unless-stopped
    ports:
      - "5000:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
      - DOTNET_RUNNING_IN_CONTAINER=true
      - ConnectionStrings__DefaultConnection=Data Source=/app/data/[projectname].db
    volumes:
      - sqlite-data:/app/data
      - key-storage:/app/keys

volumes:
  sqlite-data:
  key-storage:
```

---

## CSS Foundation (wwwroot/css/)

### base.css

```css
:root {
  /* Colors */
  --primary: #10B981;
  --primary-dark: #059669;
  --secondary: #6366F1;
  --danger: #EF4444;
  --warning: #F59E0B;
  --text: #1F2937;
  --text-light: #6B7280;
  --border: #E5E7EB;
  --bg: #F9FAFB;
  --bg-white: #FFFFFF;
  
  /* Spacing */
  --spacing-xs: 0.25rem;
  --spacing-sm: 0.5rem;
  --spacing-md: 1rem;
  --spacing-lg: 1.5rem;
  --spacing-xl: 2rem;
  
  /* Border Radius */
  --radius-sm: 0.25rem;
  --radius-md: 0.5rem;
  --radius-lg: 0.75rem;
  
  /* Shadows */
  --shadow-sm: 0 1px 2px rgba(0, 0, 0, 0.05);
  --shadow-md: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
}

*, *::before, *::after {
  box-sizing: border-box;
  margin: 0;
  padding: 0;
}

body {
  font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
  color: var(--text);
  background: var(--bg);
  line-height: 1.5;
}
```

### layout.css

```css
body {
  display: flex;
  min-height: 100vh;
}

.sidebar {
  width: 260px;
  background: var(--bg-white);
  border-right: 1px solid var(--border);
  display: flex;
  flex-direction: column;
  position: fixed;
  height: 100vh;
  z-index: 100;
  transition: transform 0.3s ease;
}

.sidebar-header {
  padding: var(--spacing-lg);
  border-bottom: 1px solid var(--border);
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.brand {
  font-size: 1.25rem;
  font-weight: 700;
  color: var(--primary);
}

.nav-menu {
  flex: 1;
  padding: var(--spacing-md);
  overflow-y: auto;
}

.nav-item {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  padding: var(--spacing-sm) var(--spacing-md);
  color: var(--text-light);
  text-decoration: none;
  border-radius: var(--radius-md);
  margin-bottom: var(--spacing-xs);
  transition: all 0.15s ease;
}

.nav-item:hover {
  background: var(--bg);
  color: var(--text);
}

.nav-item.active {
  background: var(--primary);
  color: white;
}

.main-content {
  flex: 1;
  margin-left: 260px;
  padding: var(--spacing-lg);
  overflow-y: auto;
}

header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--spacing-lg);
}

.page-title {
  font-size: 1.5rem;
  font-weight: 600;
}
```

### components.css

```css
/* Buttons */
.btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: var(--spacing-sm);
  padding: var(--spacing-sm) var(--spacing-md);
  border-radius: var(--radius-md);
  font-weight: 500;
  font-size: 0.875rem;
  cursor: pointer;
  border: none;
  transition: all 0.15s ease;
}

.btn-primary {
  background: var(--primary);
  color: white;
}

.btn-primary:hover {
  background: var(--primary-dark);
}

.btn-outline {
  background: transparent;
  border: 1px solid var(--border);
  color: var(--text);
}

.btn-outline:hover {
  background: var(--bg);
}

.btn-danger {
  background: var(--danger);
  color: white;
}

/* Cards */
.card {
  background: var(--bg-white);
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  padding: var(--spacing-lg);
  box-shadow: var(--shadow-sm);
}

/* Tables */
.table {
  width: 100%;
  border-collapse: collapse;
}

.table th,
.table td {
  padding: var(--spacing-sm) var(--spacing-md);
  text-align: left;
  border-bottom: 1px solid var(--border);
}

.table th {
  font-weight: 600;
  color: var(--text-light);
  font-size: 0.75rem;
  text-transform: uppercase;
}

/* Modal */
.modal-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.modal {
  background: var(--bg-white);
  border-radius: var(--radius-lg);
  max-width: 500px;
  width: 90%;
  max-height: 90vh;
  overflow: auto;
  box-shadow: var(--shadow-md);
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: var(--spacing-lg);
  border-bottom: 1px solid var(--border);
}

.modal-body {
  padding: var(--spacing-lg);
}

.modal-footer {
  display: flex;
  justify-content: flex-end;
  gap: var(--spacing-sm);
  padding: var(--spacing-lg);
  border-top: 1px solid var(--border);
}

/* Toast */
#toast-container {
  position: fixed;
  top: var(--spacing-lg);
  right: var(--spacing-lg);
  z-index: 9999;
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
}
```

### forms.css

```css
.form-group {
  margin-bottom: var(--spacing-md);
}

.form-group label {
  display: block;
  font-weight: 500;
  margin-bottom: var(--spacing-xs);
  font-size: 0.875rem;
}

.form-control {
  width: 100%;
  padding: var(--spacing-sm) var(--spacing-md);
  border: 1px solid var(--border);
  border-radius: var(--radius-md);
  font-size: 0.875rem;
  transition: border-color 0.15s ease;
}

.form-control:focus {
  outline: none;
  border-color: var(--primary);
  box-shadow: 0 0 0 3px rgba(16, 185, 129, 0.1);
}

.form-control.is-invalid {
  border-color: var(--danger);
}

.validation-message {
  color: var(--danger);
  font-size: 0.75rem;
  margin-top: var(--spacing-xs);
}

select.form-control {
  cursor: pointer;
}

textarea.form-control {
  resize: vertical;
  min-height: 100px;
}
```

### responsive.css

```css
@media (max-width: 768px) {
  .sidebar {
    transform: translateX(-100%);
  }
  
  .sidebar.open {
    transform: translateX(0);
  }
  
  .main-content {
    margin-left: 0;
  }
}
```

---

## JavaScript Foundation (wwwroot/js/)

### layout.js

```javascript
// Sidebar toggle
function toggleSidebar() {
    const sidebar = document.getElementById('sidebar');
    sidebar.classList.toggle('open');
}

// Close modal on Escape key
document.addEventListener('keydown', function(e) {
    if (e.key === 'Escape') {
        const modal = document.querySelector('.modal-overlay');
        if (modal) {
            modal.remove();
        }
    }
});

// Close modal on backdrop click
document.addEventListener('click', function(e) {
    if (e.target.classList.contains('modal-overlay')) {
        e.target.remove();
    }
});
```

---

## Module Registration Checklist

When adding a new module, update these files:

1. **Program.cs** - Add: `builder.Services.Add[ModuleName]Module();`

2. **Infrastructure/MvcExtensions.cs** - Add:
   - Using statement for events
   - Event config: `options.AddConfig<[ModuleName]EventConfig>();`

3. **Infrastructure/ModuleViewLocationExpander.cs** - Add mapping:
   ```csharp
   ["[ControllerName]"] = new("[ModuleFolderName]"),
   ```

4. **Data/AppDbContext.cs** - Add DbSet:
   ```csharp
   public DbSet<[Entity]> [Entities] { get; set; }
   ```

5. **Data/Configurations/ModelBuilderExtensions.cs** - Add configuration call (if needed)

6. **Views/_ViewImports.cshtml** - Add using statements for module entities

7. **Views/Shared/_Layout.cshtml** - Add navigation item

8. **Infrastructure/WebOptimizerExtensions.cs** - Add any module-specific CSS/JS files

---

## Commands

```bash
# Create initial migration
dotnet ef migrations add InitialCreate

# Apply migrations
dotnet ef database update

# Restore LibMan packages
libman restore

# Run in development
dotnet run

# Build Docker image
docker-compose build

# Run Docker container
docker-compose up -d
```

---

## Architecture Rules

### Server-Driven UI (MANDATORY)

All UI updates flow through the server returning HTML partials. JavaScript is allowed ONLY for:
- DOM state that cannot round-trip (scroll position, focus)
- Third-party integrations (drag-and-drop libraries)
- LocalStorage persistence

**NEVER use JavaScript for:**
- ❌ `fetch()` calls - use `hx-get`/`hx-post`
- ❌ DOM manipulation that changes content - return a partial
- ❌ Show/hide logic - use server conditionals or CSS `<details>`
- ❌ Form validation - use server validation + `<swap-validation>`
- ❌ State management - use `<swap-state>` or hidden inputs

### Controller Pattern

```csharp
public class MyController : SwapController
{
    // Full page
    public IActionResult Index() => SwapView(model);
    
    // Partial with coordination
    public IActionResult Update() => SwapResponse()
        .WithView("_Partial", model)
        .WithTrigger(MyEvents.Updated)
        .WithSuccessToast("Updated!")
        .Build();
    
    // Event trigger only
    public IActionResult Delete(int id) => SwapEvent(MyEvents.Deleted).Build();
}
```

### Event-Driven Updates

```html
<!-- Element refreshes when event fires -->
<div hx-get="/My/List" 
     hx-trigger="@MyEvents.Data.Changed from:body"
     hx-swap="innerHTML">
</div>
```

### Modal Pattern

```html
<!-- Trigger -->
<button hx-get="/My/EditModal/5" 
        hx-target="#modal-container">
    Edit
</button>

<!-- Container in layout -->
<div id="modal-container"></div>
```

---

## Quick Start

1. Create solution and project structure
2. Add NuGet packages (Swap.Htmx, EF Core, WebOptimizer)
3. Copy infrastructure files
4. Create first module following the module template
5. Add initial migration
6. Run with `dotnet run`

This template provides a production-ready foundation for building HTMX-powered ASP.NET Core applications with Swap.Htmx.

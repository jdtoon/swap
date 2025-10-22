# CLI Automation Strategy - Zero Manual Intervention

**Vision**: Developers should ONLY write business logic. All infrastructure, wiring, and boilerplate should be automated.

**Last Updated**: October 21, 2025

---

## 🎯 Core Principle

```
Developer runs CLI → Feature generated → Everything wired → Developer writes business logic → Done
```

**No manual steps for**:
- Project references
- DbContext configuration
- Dependency injection registration
- Program.cs updates
- Migrations
- Seeding setup
- Routing
- View discovery

---

## 📋 Current State vs. Target State

### Module Generation: `netmx create module Authorization`

| Step | Current | Target | Priority |
|------|---------|--------|----------|
| Create folder structure | ✅ Automated | ✅ Automated | - |
| Create 4 projects (.csproj) | ✅ Automated | ✅ Automated | - |
| Add project references | ✅ Automated | ✅ Automated | - |
| Create module.json | ✅ Automated | ✅ Automated | - |
| Create README.md | ✅ Automated | ✅ Automated | - |
| **Create DbContext** | ❌ Manual | ✅ Auto-generate | 🔥 HIGH |
| **Add to solution** | ❌ Manual | ✅ Auto-detect & add | 🔥 HIGH |
| **Sample entity** | ❌ None | ✅ Generate example | MEDIUM |

**Target Command**:
```bash
netmx create module Authorization
✨ Creating module: Authorization
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
[1/8] ✅ Created folder structure
[2/8] ✅ Created 4 projects
[3/8] ✅ Added project references
[4/8] ✅ Generated AuthorizationDbContext
[5/8] ✅ Added to solution (modules/Authorization/Authorization.sln)
[6/8] ✅ Generated module.json
[7/8] ✅ Generated README.md
[8/8] ✅ Generated sample Permission entity

📁 Module created: modules/Authorization/

💡 Next steps:
   1. Generate features: netmx generate feature Permission -m Authorization
   2. Add module to app: netmx add module Authorization
```

---

### Feature Generation: `netmx generate feature Product`

| Step | Current | Target | Priority |
|------|---------|--------|----------|
| Generate entity | ✅ Automated | ✅ Automated | - |
| Generate DTOs | ✅ Automated | ✅ Automated | - |
| Generate service | ✅ Automated | ✅ Automated | - |
| Generate controller | ✅ Automated | ✅ Automated | - |
| Generate views | ✅ Automated | ✅ Automated | - |
| **Add DbSet to DbContext** | ❌ Manual | ✅ Auto-inject | 🔥 CRITICAL |
| **Create migration** | ❌ Manual | ✅ Auto-generate | 🔥 CRITICAL |
| **Apply migration** | ❌ Manual | ✅ Auto-apply | 🔥 CRITICAL |
| **Register service (DI)** | ⚠️ Interface only | ✅ Auto via marker | MEDIUM |
| **Add navigation link** | ❌ Manual | ✅ Auto-inject | MEDIUM |
| **Generate seeder** | ❌ Manual | ✅ Auto-generate | HIGH |

**Target Command with Flags**:
```bash
netmx generate feature Product --migrate --seed --nav

✨ Generating Feature: Product
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
[1/9] ✅ Entity (Models/Product.cs)
[2/9] ✅ DTOs (Read, Create, Update)
[3/9] ✅ Service (IProductService, ProductService)
[4/9] ✅ Controller (ProductController)
[5/9] ✅ Views (Index, _List, _Form)
[6/9] ✅ Added DbSet to AppDbContext
[7/9] ✅ Migration created: 20251021_AddProduct
[8/9] ✅ Migration applied to database
[9/9] ✅ Added navigation link to _Layout.cshtml

📁 Generated Files:
   Models/Product.cs
   Dtos/ProductDto.cs, CreateProductDto.cs, UpdateProductDto.cs
   Services/IProductService.cs, ProductService.cs
   Controllers/ProductController.cs
   Views/Product/Index.cshtml, _List.cshtml, _Form.cshtml
   
📊 Database:
   ✅ Table created: Products
   ✅ Seeder available: ProductSeeder.cs (add sample data)

💡 Next steps:
   1. Add business logic to Product.cs
   2. Add validation rules to DTOs
   3. Customize views in Views/Product/
   4. Visit: https://localhost:5001/Product

⏱️  Total time: 3.2 seconds
```

---

### Module Installation: `netmx add module Authorization`

| Step | Current | Target | Priority |
|------|---------|--------|----------|
| Add project references | ⚠️ Shows code | ✅ Auto-inject | 🔥 CRITICAL |
| Update Program.cs | ⚠️ Shows code | ✅ Auto-inject | 🔥 CRITICAL |
| Add DbContext | ❌ Manual | ✅ Auto-configure | 🔥 CRITICAL |
| Run migrations | ❌ Manual | ✅ Auto-apply | HIGH |
| Seed data | ❌ Manual | ✅ Auto-seed | HIGH |
| Add navigation | ❌ Manual | ✅ Auto-inject | MEDIUM |

**Target Command**:
```bash
netmx add module Authorization

✨ Installing Module: Authorization
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
[1/6] ✅ Added project references (3 projects)
[2/6] ✅ Updated Program.cs (added module services)
[3/6] ✅ Configured AuthorizationDbContext
[4/6] ✅ Applied migrations (3 migrations)
[5/6] ✅ Seeded data (19 permissions, 3 roles)
[6/6] ✅ Added navigation links (/Permissions, /Roles)

📦 Module installed: Authorization v1.0.0

🔐 Authorization Features:
   - Permission management
   - Role management
   - Policy-based authorization
   - [RequirePermission] attribute

💡 Usage:
   // In controller
   [RequirePermission("Users.View")]
   public class UsersController : Controller { }
   
   // In code
   await _permissionChecker.IsGrantedAsync("Users.View");

📚 Documentation: modules/Authorization/README.md
⏱️  Total time: 4.7 seconds
```

---

## 🎯 Implementation Roadmap

### Week 2 (Oct 22-28): Critical Automation

**Priority 1: DbContext Auto-Management**

```csharp
// CLI detects DbContext location
var dbContextPath = DetectDbContext(projectPath);

// Inject DbSet using Roslyn
var code = File.ReadAllText(dbContextPath);
var tree = CSharpSyntaxTree.ParseText(code);
var root = tree.GetRoot();

// Add DbSet<Product> property
var newRoot = AddDbSetProperty(root, "Product");
File.WriteAllText(dbContextPath, newRoot.ToFullString());
```

**Priority 2: Auto-Migration**

```bash
# CLI internally runs
dotnet ef migrations add Add{EntityName} --context {DbContextName}
dotnet ef database update --context {DbContextName}

# With error handling
if (migration fails)
    Show error with fix suggestions
    Rollback DbSet addition
```

**Priority 3: Program.cs Auto-Injection**

```csharp
// Detect Program.cs
var programPath = Path.Combine(projectPath, "Program.cs");

// Inject module registration using Roslyn
// After: var builder = WebApplication.CreateBuilder(args);
// Add: builder.Services.AddAuthorizationModule();

// After: app.UseAuthorization();
// Add: app.UseAuthorizationModule();
```

### Week 3 (Oct 29 - Nov 4): Rich Output & UX

**Priority 4: Rich Terminal Output**

Use **Spectre.Console** for beautiful CLI:

```csharp
AnsiConsole.Progress()
    .Start(ctx => 
    {
        var task = ctx.AddTask("Generating feature...");
        
        task.Increment(12.5); // Entity
        task.Increment(12.5); // DTOs
        // ... etc
    });

// Status indicators
AnsiConsole.MarkupLine("[green]✅[/] Entity generated");
AnsiConsole.MarkupLine("[yellow]⚠️[/] Migration pending");
AnsiConsole.MarkupLine("[red]❌[/] Database connection failed");
```

**Priority 5: Interactive Prompts**

```bash
netmx generate feature

? Entity name: › Product
? Add search capability? › Yes / No
? Add export to Excel? › Yes / No
? Generate seeder with sample data? › Yes / No
? Auto-create migration? › Yes / No

✨ Generating Product feature with search, export, seeder, migration...
```

### Week 4 (Nov 5-11): Advanced Features

**Priority 6: Seeder Auto-Generation**

```bash
netmx generate feature Product --seed

# Generates ProductSeeder.cs with sample data
public class ProductSeeder : ISeeder
{
    public async Task SeedAsync()
    {
        if (await _repository.GetCountAsync() > 0)
            return; // Already seeded
            
        var products = new[]
        {
            new Product(Guid.NewGuid(), "Sample Product 1", 99.99m),
            new Product(Guid.NewGuid(), "Sample Product 2", 149.99m),
            // TODO: Add more sample data
        };
        
        foreach (var product in products)
            await _repository.InsertAsync(product);
    }
}
```

**Priority 7: Navigation Auto-Injection**

```html
<!-- CLI injects into _Layout.cshtml or _Navigation.cshtml -->
<li class="navbar-item">
    <a href="/Product">Products</a>
</li>
```

**Priority 8: Dependency Validation**

```bash
netmx generate feature Product

⚠️  Warning: Entity Framework Core not installed
💡 Run: dotnet add package Microsoft.EntityFrameworkCore
   Continue anyway? (y/N) _
```

---

## 🔧 Technical Implementation

### 1. Roslyn for Code Modification

```csharp
public class CodeInjector
{
    public void AddDbSet(string filePath, string entityName)
    {
        var code = File.ReadAllText(filePath);
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetCompilationUnitSyntax();
        
        // Find DbContext class
        var dbContextClass = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First(c => c.BaseList?.Types.Any(t => 
                t.ToString().Contains("DbContext")) ?? false);
        
        // Create new DbSet property
        var dbSetProperty = SyntaxFactory
            .PropertyDeclaration(
                SyntaxFactory.ParseTypeName($"DbSet<{entityName}>"),
                entityName + "s")
            .AddAccessorListAccessors(
                SyntaxFactory.AccessorDeclaration(
                    SyntaxKind.GetAccessorDeclaration)
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
        
        // Add to class
        var newClass = dbContextClass.AddMembers(dbSetProperty);
        var newRoot = root.ReplaceNode(dbContextClass, newClass);
        
        File.WriteAllText(filePath, newRoot.ToFullString());
    }
}
```

### 2. EF Core Migration Automation

```csharp
public class MigrationRunner
{
    public async Task<bool> CreateAndApplyMigrationAsync(
        string entityName, 
        string projectPath,
        string dbContextName)
    {
        var migrationName = $"Add{entityName}";
        
        // Create migration
        var createResult = await RunProcessAsync(
            "dotnet", 
            $"ef migrations add {migrationName} --context {dbContextName}",
            projectPath);
        
        if (!createResult.Success)
        {
            Console.WriteLine($"❌ Migration creation failed: {createResult.Error}");
            return false;
        }
        
        // Apply migration
        var applyResult = await RunProcessAsync(
            "dotnet",
            $"ef database update --context {dbContextName}",
            projectPath);
        
        if (!applyResult.Success)
        {
            Console.WriteLine($"❌ Migration apply failed: {applyResult.Error}");
            return false;
        }
        
        return true;
    }
}
```

### 3. Smart Detection

```csharp
public class ProjectAnalyzer
{
    public string DetectDbContext(string projectPath)
    {
        // Search for DbContext in project
        var files = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories);
        
        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            if (content.Contains(": DbContext") || content.Contains(": NetMXDbContext"))
                return file;
        }
        
        // Not found - should we create one?
        Console.WriteLine("⚠️  No DbContext found");
        Console.Write("💡 Create AppDbContext? (Y/n) ");
        
        if (Console.ReadLine()?.ToLower() != "n")
        {
            return CreateDbContext(projectPath);
        }
        
        return null;
    }
    
    public bool HasEfCoreInstalled(string projectPath)
    {
        var csprojPath = Directory.GetFiles(projectPath, "*.csproj").FirstOrDefault();
        if (csprojPath == null) return false;
        
        var content = File.ReadAllText(csprojPath);
        return content.Contains("Microsoft.EntityFrameworkCore");
    }
}
```

---

## 📦 CLI Package Dependencies

Add to `NetMX.CLI.csproj`:

```xml
<ItemGroup>
  <!-- Code manipulation -->
  <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.8.0" />
  
  <!-- Rich terminal UI -->
  <PackageReference Include="Spectre.Console" Version="0.49.1" />
  
  <!-- Interactive prompts -->
  <PackageReference Include="Spectre.Console.Cli" Version="0.49.1" />
  
  <!-- Process execution -->
  <PackageReference Include="CliWrap" Version="3.6.6" />
  
  <!-- File watching (for live reload) -->
  <PackageReference Include="DotNetEnv" Version="3.0.0" />
</ItemGroup>
```

---

## 🎯 Success Metrics

**Before CLI Automation**:
- Generate feature: ~2 hours (manual wiring)
- Add module: ~30 minutes (manual setup)
- Error rate: ~40% (forgot a step)

**After CLI Automation**:
- Generate feature: **<30 seconds** (zero manual work)
- Add module: **<10 seconds** (zero manual work)
- Error rate: **<5%** (validation catches issues)

**Developer Experience**:
```bash
# Before (10 manual steps)
netmx generate feature Product
# Then: Add DbSet, create migration, apply migration, add to DI, add nav, test...

# After (1 command)
netmx generate feature Product --migrate --seed --nav
# Done! Just add business logic.
```

---

## 📝 Next Steps

1. **Week 2**: Implement DbContext auto-management + auto-migration
2. **Week 3**: Add Spectre.Console for rich output
3. **Week 4**: Interactive prompts + seeder generation
4. **Week 5**: Navigation injection + validation

**Goal**: By end of Month 1, CLI should be **fully automated** with zero manual intervention needed.

---

**Remember**: The CLI should make developers **feel like magic is happening**. Every manual step removed is a win! ✨

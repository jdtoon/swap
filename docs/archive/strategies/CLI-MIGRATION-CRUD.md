# CLI Migration & CRUD Generation - Enhanced Strategy

**Ruby on Rails-inspired developer experience for NetMX**

**Last Updated**: October 21, 2025

---

## 🎯 Vision: Rails-Level Developer Experience

**Inspiration**: Ruby on Rails `rails generate` and `rails db:migrate`

```bash
# Rails way
rails generate model Product name:string price:decimal
rails db:migrate

# NetMX way (target)
netmx generate model Product name:string price:decimal --migrate
netmx db migrate
```

---

## 📋 Problem Statement

### Scenario 1: CLI-Generated Models (Already Working)
```bash
netmx generate feature Product
# ✅ Generates: Entity, DTOs, Service, Controller, Views
# ❌ Manual step: Add DbSet, create migration, apply migration
```

### Scenario 2: Manually Created Models (Not Supported)
```csharp
// Developer manually creates this
public class Category : Entity<Guid>
{
    public string Name { get; set; }
    public string Description { get; set; }
}

// Now what? How do they:
// 1. Add DbSet to DbContext?
// 2. Create migration?
// 3. Generate CRUD for it?
```

**Problem**: CLI doesn't detect manual models or provide migration helpers.

---

## 🚀 Solution: Multi-Mode CLI Commands

### Mode 1: Generate Everything (Scaffold)

**Command**: `netmx generate scaffold <EntityName> [properties]`

Generates FULL CRUD stack with migration:

```bash
netmx generate scaffold Product name:string:256 price:decimal description:text --migrate

✨ Generating Scaffold: Product
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
[1/10] ✅ Entity with properties (Models/Product.cs)
[2/10] ✅ DTOs (ProductDto, CreateProductDto, UpdateProductDto)
[3/10] ✅ Validation (FluentValidation)
[4/10] ✅ Service interface & implementation
[5/10] ✅ Controller with CRUD actions
[6/10] ✅ Views (Index, Create, Edit, Details, Delete)
[7/10] ✅ Added DbSet<Product> to AppDbContext
[8/10] ✅ Migration created: 20251021_AddProduct
[9/10] ✅ Migration applied to database
[10/10] ✅ Added navigation link to layout

📁 Generated Files: 12 files
📊 Database: Table 'Products' created with 3 columns
🌐 Visit: https://localhost:5001/Products

⏱️  Total time: 4.2 seconds
```

**Property Syntax** (Rails-inspired):
```bash
# Format: name:type[:length][:modifier]

# String with max length
name:string:256

# Decimal with precision
price:decimal:18,2

# Text (unlimited length)
description:text

# Integer
quantity:int

# Boolean
isActive:bool

# DateTime
createdAt:datetime

# Guid (default for ID)
id:guid

# Foreign key
categoryId:guid:fk:Category

# With modifiers
name:string:256:required
email:string:320:required:unique
price:decimal:18,2:required:index
```

### Mode 2: Generate Model Only

**Command**: `netmx generate model <EntityName> [properties] [--migrate]`

Generates just the entity + migration:

```bash
netmx generate model Product name:string:256 price:decimal --migrate

✨ Generating Model: Product
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
[1/4] ✅ Entity (Models/Product.cs)
[2/4] ✅ Added DbSet<Product> to AppDbContext
[3/4] ✅ Migration created: 20251021_AddProduct
[4/4] ✅ Migration applied to database

📁 Generated: Models/Product.cs
📊 Database: Table 'Products' created

💡 Generate CRUD later:
   netmx generate crud Product
```

### Mode 3: Generate CRUD for Existing Model

**Command**: `netmx generate crud <EntityName>`

Detects existing model and generates CRUD:

```bash
# Developer manually created Category.cs
netmx generate crud Category

🔍 Detecting existing model: Category
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
✅ Found: Models/Category.cs
   Properties detected: Id, Name, Description, CreatedAt

✨ Generating CRUD for: Category
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
[1/6] ✅ DTOs (CategoryDto, CreateCategoryDto, UpdateCategoryDto)
[2/6] ✅ Service interface & implementation
[3/6] ✅ Controller with CRUD actions
[4/6] ✅ Views (Index, Create, Edit, Details, Delete)
[5/6] ✅ Added DbSet<Category> to AppDbContext (if missing)
[6/6] ✅ Added navigation link to layout

💡 Need migration?
   netmx db migrate AddCategory
```

### Mode 4: Add Migration for Manual Changes

**Command**: `netmx db migrate <MigrationName>`

Creates migration for ANY pending changes (auto-detected):

```bash
# Developer manually added properties to Product.cs
# public string Sku { get; set; }
# public int StockQuantity { get; set; }

netmx db migrate AddProductSkuAndStock

🔍 Scanning for model changes...
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
✅ Detected changes in: Product
   + Sku (string, nullable)
   + StockQuantity (int, not null)

✨ Creating Migration: AddProductSkuAndStock
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
[1/3] ✅ DbSet exists in AppDbContext
[2/3] ✅ Migration created: 20251021_AddProductSkuAndStock
[3/3] ✅ Migration applied to database

📊 Database changes:
   ALTER TABLE Products ADD Sku nvarchar(max) NULL
   ALTER TABLE Products ADD StockQuantity int NOT NULL DEFAULT 0

💡 Rollback if needed:
   netmx db rollback
```

---

## 🛠️ Implementation: Model Detection

### 1. Scan for Entity Classes

```csharp
public class ModelScanner
{
    public List<ModelInfo> ScanForModels(string projectPath)
    {
        var models = new List<ModelInfo>();
        var files = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories);
        
        foreach (var file in files)
        {
            var code = File.ReadAllText(file);
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetRoot();
            
            // Find classes that inherit from Entity<T>
            var classes = root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Where(c => c.BaseList?.Types.Any(t => 
                    t.ToString().StartsWith("Entity<")) ?? false);
            
            foreach (var classDecl in classes)
            {
                var modelInfo = new ModelInfo
                {
                    Name = classDecl.Identifier.Text,
                    FilePath = file,
                    Properties = ExtractProperties(classDecl),
                    Namespace = GetNamespace(classDecl)
                };
                
                models.Add(modelInfo);
            }
        }
        
        return models;
    }
    
    private List<PropertyInfo> ExtractProperties(ClassDeclarationSyntax classDecl)
    {
        var properties = new List<PropertyInfo>();
        
        foreach (var member in classDecl.Members.OfType<PropertyDeclarationSyntax>())
        {
            var propertyInfo = new PropertyInfo
            {
                Name = member.Identifier.Text,
                Type = member.Type.ToString(),
                IsNullable = member.Type.ToString().EndsWith("?"),
                Attributes = member.AttributeLists
                    .SelectMany(al => al.Attributes)
                    .Select(a => a.Name.ToString())
                    .ToList()
            };
            
            properties.Add(propertyInfo);
        }
        
        return properties;
    }
}

public class ModelInfo
{
    public string Name { get; set; }
    public string FilePath { get; set; }
    public string Namespace { get; set; }
    public List<PropertyInfo> Properties { get; set; }
    public bool HasDbSet { get; set; }
    public bool HasMigration { get; set; }
}

public class PropertyInfo
{
    public string Name { get; set; }
    public string Type { get; set; }
    public bool IsNullable { get; set; }
    public List<string> Attributes { get; set; }
    public int? MaxLength { get; set; }
}
```

### 2. Detect Missing DbSets

```csharp
public class DbContextAnalyzer
{
    public List<string> FindMissingDbSets(string dbContextPath, List<ModelInfo> models)
    {
        var code = File.ReadAllText(dbContextPath);
        var missing = new List<string>();
        
        foreach (var model in models)
        {
            // Check if DbSet<ModelName> exists in DbContext
            if (!code.Contains($"DbSet<{model.Name}>"))
            {
                missing.Add(model.Name);
            }
        }
        
        return missing;
    }
    
    public void AddDbSet(string dbContextPath, string entityName)
    {
        var code = File.ReadAllText(dbContextPath);
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        
        // Find DbContext class
        var dbContextClass = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First(c => c.BaseList?.Types.Any(t => 
                t.ToString().Contains("DbContext")) ?? false);
        
        // Create DbSet property
        var dbSetProperty = SyntaxFactory
            .PropertyDeclaration(
                SyntaxFactory.ParseTypeName($"DbSet<{entityName}>"),
                $"{entityName}s")  // Pluralize
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .AddAccessorListAccessors(
                SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
        
        // Add to class
        var newClass = dbContextClass.AddMembers(dbSetProperty);
        var newRoot = root.ReplaceNode(dbContextClass, newClass);
        
        File.WriteAllText(dbContextPath, newRoot.NormalizeWhitespace().ToFullString());
    }
}
```

### 3. Detect Pending Model Changes

```csharp
public class MigrationDetector
{
    public async Task<List<ModelChange>> DetectChangesAsync(string projectPath)
    {
        // Run EF Core design-time check
        var result = await RunProcessAsync(
            "dotnet",
            "ef migrations has-pending-model-changes",
            projectPath);
        
        if (result.Output.Contains("No changes"))
            return new List<ModelChange>();
        
        // Parse EF Core output to detect what changed
        var changes = ParseEfCoreOutput(result.Output);
        return changes;
    }
    
    private List<ModelChange> ParseEfCoreOutput(string output)
    {
        // Parse output like:
        // "An operation was scaffolded for entity type 'Product' 
        //  that may result in the loss of data..."
        
        var changes = new List<ModelChange>();
        
        // Extract entity names and change types
        var lines = output.Split('\n');
        foreach (var line in lines)
        {
            if (line.Contains("entity type"))
            {
                var entityMatch = Regex.Match(line, @"entity type '(\w+)'");
                if (entityMatch.Success)
                {
                    changes.Add(new ModelChange
                    {
                        EntityName = entityMatch.Groups[1].Value,
                        ChangeType = DetectChangeType(line)
                    });
                }
            }
        }
        
        return changes;
    }
}
```

---

## 🎨 Property Type Mapping

### String Types
```bash
name:string          → string Name { get; set; }
name:string:256      → [MaxLength(256)] string Name { get; set; }
name:string:required → [Required] string Name { get; set; }
description:text     → string? Description { get; set; }  // No max length
```

### Numeric Types
```bash
price:decimal               → decimal Price { get; set; }
price:decimal:18,2          → [Column(TypeName = "decimal(18,2)")] decimal Price
quantity:int                → int Quantity { get; set; }
rating:double               → double Rating { get; set; }
discount:float              → float Discount { get; set; }
```

### Date/Time Types
```bash
createdAt:datetime          → DateTime CreatedAt { get; set; }
publishedAt:datetime:null   → DateTime? PublishedAt { get; set; }
birthDate:date              → DateOnly BirthDate { get; set; }
```

### Boolean Types
```bash
isActive:bool               → bool IsActive { get; set; }
isPublished:bool:default:true → bool IsPublished { get; set; } = true;
```

### Foreign Keys
```bash
categoryId:guid:fk:Category  → 
    public Guid CategoryId { get; set; }
    public Category Category { get; set; }
```

### Arrays/Collections
```bash
tags:array:string           → public List<string> Tags { get; set; } = new();
```

---

## 📋 Complete Workflow Examples

### Example 1: Quick Prototype (Scaffold)

```bash
# Generate complete CRUD in one command
netmx generate scaffold Product \
    name:string:256:required \
    sku:string:50:required:unique \
    price:decimal:18,2:required \
    description:text \
    categoryId:guid:fk:Category \
    isActive:bool:default:true \
    --migrate \
    --seed

# Generated in 5 seconds:
# ✅ Entity with 6 properties
# ✅ DTOs with validation
# ✅ Service with CRUD
# ✅ Controller with 5 actions
# ✅ Views with Bulma styling
# ✅ Migration applied
# ✅ Seeder with sample data
# ✅ Navigation link added
```

### Example 2: Incremental Development

```bash
# Step 1: Create model only
netmx generate model Category name:string:256 description:text

# Step 2: Manually add business logic to Category.cs
# public void UpdateDetails(string name, string description) { ... }

# Step 3: Add migration for model
netmx db migrate AddCategory

# Step 4: Generate CRUD later
netmx generate crud Category
```

### Example 3: Manual Model + CLI Migration

```bash
# Developer manually creates Product.cs
# public class Product : Entity<Guid>
# {
#     public string Name { get; set; }
#     public decimal Price { get; set; }
# }

# Add DbSet (if missing) and create migration
netmx db add Product
# ✅ Added DbSet<Product> to AppDbContext
# ✅ Created migration: AddProduct
# ✅ Applied to database

# Generate CRUD for it
netmx generate crud Product
# ✅ DTOs, Service, Controller, Views
```

### Example 4: Model Evolution

```bash
# Initial model
netmx generate scaffold Product name:string price:decimal --migrate

# Later: Add properties manually
# public string Sku { get; set; }
# public int StockQuantity { get; set; }

# Create migration for changes
netmx db migrate AddProductInventoryFields
# 🔍 Detected: +Sku, +StockQuantity
# ✅ Migration created and applied

# Even later: Change property type manually
# public decimal Price { get; set; } → public decimal? Price { get; set; }

# Create migration
netmx db migrate MakeProductPriceNullable
# 🔍 Detected: Price changed from decimal to decimal?
# ✅ Migration created and applied
```

---

## 🎯 CLI Command Reference

### Generate Commands

```bash
# Full scaffold (Rails-style)
netmx generate scaffold <Entity> [properties] [flags]
netmx g scaffold Product name:string price:decimal --migrate

# Model only
netmx generate model <Entity> [properties] [flags]
netmx g model Category name:string --migrate

# CRUD for existing model
netmx generate crud <Entity> [flags]
netmx g crud Product --views --api

# Controller only
netmx generate controller <Name> [actions]
netmx g controller Products Index Create Edit Delete

# Service only
netmx generate service <Name>
netmx g service ProductService

# DTOs only
netmx generate dto <Entity>
netmx g dto Product

# Seeder
netmx generate seeder <Entity>
netmx g seeder ProductSeeder
```

### Database Commands

```bash
# Create migration (auto-detect changes)
netmx db migrate <Name>
netmx db migrate AddProductSku

# Apply migrations
netmx db update
netmx db update --verbose

# Rollback last migration
netmx db rollback
netmx db rollback --steps 2

# Reset database (drop + recreate + migrate)
netmx db reset
netmx db reset --seed

# Seed data
netmx db seed
netmx db seed --class ProductSeeder

# Show migration status
netmx db status

# Add DbSet for model
netmx db add <Entity>
netmx db add Product

# Remove DbSet and migration
netmx db remove <Entity>
netmx db remove Product --keep-migration
```

### Utility Commands

```bash
# List all models
netmx list models

# Show model details
netmx show model Product

# Detect changes
netmx db changes

# Validate models (check for DbSet, migration)
netmx validate models
```

---

## 🚀 Flags & Options

### Generate Flags

```bash
--migrate, -m          # Auto-create and apply migration
--seed, -s             # Generate seeder with sample data
--api                  # Generate API controller (no views)
--views                # Generate views (default for MVC)
--nav                  # Add navigation link
--no-validation        # Skip validation attributes
--readonly             # Generate read-only CRUD (no create/update/delete)
```

### Examples

```bash
# API-only (no views)
netmx g scaffold Product name:string --migrate --api

# With seeding
netmx g scaffold Product name:string --migrate --seed

# Read-only
netmx g crud Product --readonly  # Only Index and Details
```

---

## 🎨 Code Generation Templates

### Entity Template

```csharp
using System.ComponentModel.DataAnnotations;
using NetMX.Ddd.Domain.Entities;

namespace {{Namespace}}.Models;

public class {{EntityName}} : Entity<Guid>
{
    {{#each Properties}}
    {{#if Required}}[Required]{{/if}}
    {{#if MaxLength}}[MaxLength({{MaxLength}})]{{/if}}
    public {{Type}} {{Name}} { get; set; }{{#if DefaultValue}} = {{DefaultValue}};{{/if}}
    
    {{/each}}
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; private set; }
    
    // EF Core constructor
    private {{EntityName}}() { }
    
    public {{EntityName}}(Guid id{{#each RequiredProperties}}, {{Type}} {{NameLower}}{{/each}})
    {
        Id = id;
        {{#each RequiredProperties}}
        {{Name}} = {{NameLower}};
        {{/each}}
        CreatedAt = DateTime.UtcNow;
    }
    
    public void Update({{#each EditableProperties}}{{Type}} {{NameLower}}{{#unless @last}}, {{/unless}}{{/each}})
    {
        {{#each EditableProperties}}
        {{Name}} = {{NameLower}};
        {{/each}}
        UpdatedAt = DateTime.UtcNow;
    }
}
```

---

## 🔄 Migration Strategy

### Auto-Detect Changes

```csharp
public class SmartMigrationGenerator
{
    public async Task<MigrationPlan> GeneratePlanAsync(string projectPath)
    {
        // 1. Scan all entities
        var models = _modelScanner.ScanForModels(projectPath);
        
        // 2. Check which models have DbSets
        var dbContextPath = _projectAnalyzer.FindDbContext(projectPath);
        var modelsWithDbSets = models.Where(m => 
            _dbContextAnalyzer.HasDbSet(dbContextPath, m.Name)).ToList();
        
        // 3. Detect pending EF Core changes
        var pendingChanges = await _migrationDetector.DetectChangesAsync(projectPath);
        
        // 4. Create plan
        return new MigrationPlan
        {
            ModelsToAddDbSet = models.Where(m => 
                !modelsWithDbSets.Contains(m)).ToList(),
            ModelsWithChanges = pendingChanges,
            RecommendedMigrationName = GenerateMigrationName(pendingChanges)
        };
    }
}
```

---

## 📊 Success Metrics

### Before CLI Enhancements

**Manual Development**:
```
1. Create model ........................ 5 min
2. Add DbSet ........................... 1 min
3. Create migration .................... 2 min
4. Apply migration ..................... 1 min
5. Create DTOs ......................... 10 min
6. Create service ...................... 15 min
7. Create controller ................... 10 min
8. Create views ........................ 30 min
-------------------------------------------
Total: ~74 minutes (1.2 hours)
```

### After CLI Enhancements

**One Command**:
```bash
netmx generate scaffold Product name:string price:decimal --migrate --seed

Total: ~5 seconds ✨
```

**Time Saved**: 1.2 hours → 5 seconds (99.9% reduction!)

---

## 🎯 Implementation Priority

### Week 2 (Oct 22-28): Core Features
- ✅ `netmx generate scaffold` with property syntax
- ✅ `netmx generate model` 
- ✅ `netmx generate crud` for existing models
- ✅ Model scanner (detect existing entities)
- ✅ DbSet auto-injection
- ✅ Auto-migration on `--migrate` flag

### Week 3 (Oct 29 - Nov 4): Database Commands
- ✅ `netmx db migrate` (auto-detect changes)
- ✅ `netmx db update`
- ✅ `netmx db rollback`
- ✅ `netmx db reset`
- ✅ `netmx db seed`
- ✅ `netmx db status`

### Week 4 (Nov 5-11): Advanced Features
- ✅ Property modifiers (required, unique, index)
- ✅ Foreign key generation
- ✅ Validation generation
- ✅ Seeder generation with `--seed`

---

**Next**: Start implementing Week 2 features! 🚀

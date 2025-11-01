# Relationship Generation - Design Document

**Date:** October 30, 2025  
**Version:** 0.2.0  
**Status:** Design Phase

---

## Overview

This document defines the architecture, CLI syntax, and implementation approach for relationship generation in Swap CLI v0.2.0.

---

## Command Syntax

### Primary Command

```bash
swap generate relationship <SourceEntity> <TargetEntity> [options]
swap g rel <SourceEntity> <TargetEntity> [options]  # Alias
```

### Relationship Types

#### One-to-Many (Most Common)

```bash
# Order belongs to Customer (Customer has many Orders)
swap g rel Order Customer --type many-to-one

# Equivalent reverse syntax:
swap g rel Customer Order --type one-to-many

# With options:
swap g rel Order Customer --type many-to-one --required --cascade-delete
swap g rel Order Customer --type many-to-one --nullable --on-delete restrict
swap g rel Order Customer --type many-to-one --display-field Email
```

#### Many-to-Many

```bash
# Post has many Tags, Tag has many Posts
swap g rel Post Tag --type many-to-many

# With custom junction table name:
swap g rel Post Tag --type many-to-many --junction PostTags

# With additional junction properties:
swap g rel Post Tag --type many-to-many --junction-props "CreatedAt:datetime CreatedBy:string"
```

#### One-to-One

```bash
# User has one Profile
swap g rel User Profile --type one-to-one --required

# Optional Profile:
swap g rel User Profile --type one-to-one --nullable
```

---

## Command Options

### Common Options

| Option | Alias | Type | Description | Default |
|--------|-------|------|-------------|---------|
| `--type` | `-t` | enum | Relationship type: `one-to-many`, `many-to-one`, `many-to-many`, `one-to-one` | `many-to-one` |
| `--required` | | flag | Foreign key is required (NOT NULL) | false |
| `--nullable` | | flag | Foreign key is nullable (NULL allowed) | true |
| `--display-field` | `-d` | string | Field to display in dropdowns (Name, Title, Email, etc.) | Auto-detect |
| `--cascade-delete` | | flag | Delete related entities when parent is deleted | false |
| `--on-delete` | | enum | Delete behavior: `cascade`, `restrict`, `set-null` | `restrict` |
| `--no-navigation` | | flag | Don't generate navigation properties | false |
| `--no-ui` | | flag | Skip UI generation (forms, views) | false |
| `--no-migrations` | | flag | Skip automatic migration creation | false |
| `--force` | `-f` | flag | Overwrite existing relationships without prompting | false |
| `--project` | `-p` | string | Path to project directory | Current directory |

### Many-to-Many Specific

| Option | Alias | Type | Description | Default |
|--------|-------|------|-------------|---------|
| `--junction` | `-j` | string | Junction table name | Auto-generate (alphabetical) |
| `--junction-props` | | string | Additional properties for junction table | None |

### One-to-One Specific

| Option | Alias | Type | Description | Default |
|--------|-------|------|-------------|---------|
| `--principal` | | string | Principal entity (owns the relationship) | Auto-detect |
| `--dependent` | | string | Dependent entity (has FK) | Auto-detect |

---

## Relationship Management Commands

### List Relationships

```bash
swap relationship list [options]
swap rel list  # Alias

# Options:
--project <path>    # Path to project
--format <type>     # Output format: table, json, yaml (default: table)
--entity <name>     # Filter by entity name
```

**Output Example:**
```
╭───────────────┬─────────────┬──────────────┬──────────┬──────────────╮
│ Source Entity │ Target      │ Type         │ Required │ Cascade      │
├───────────────┼─────────────┼──────────────┼──────────┼──────────────┤
│ Order         │ Customer    │ Many-to-One  │ Yes      │ Restrict     │
│ OrderItem     │ Order       │ Many-to-One  │ Yes      │ Cascade      │
│ OrderItem     │ Product     │ Many-to-One  │ Yes      │ Restrict     │
│ Post          │ Tag         │ Many-to-Many │ N/A      │ N/A          │
│ User          │ Profile     │ One-to-One   │ No       │ Cascade      │
╰───────────────┴─────────────┴──────────────┴──────────┴──────────────╯
```

### Remove Relationship

```bash
swap relationship remove <SourceEntity> <TargetEntity> [options]
swap rel rm <SourceEntity> <TargetEntity>  # Alias

# Options:
--drop-columns      # Also drop FK columns in migration
--no-migrations     # Skip migration creation
--force             # Don't prompt for confirmation
```

### Detect Existing Relationships

```bash
swap relationship detect [EntityName] [options]
swap rel detect  # Alias

# Detect all relationships:
swap rel detect

# Detect for specific entity:
swap rel detect Order
```

**Output Example:**
```
Detected relationships in Order model:
✓ CustomerId (int) → Customer.Id (FK constraint exists)
✓ ShippingAddressId (int?) → Address.Id (nullable FK)
⚠ ProductId (int) → No FK constraint (create with: swap g rel Order Product)
```

### Update Relationship

```bash
swap relationship update <SourceEntity> <TargetEntity> [options]
swap rel update  # Alias

# Change cascade behavior:
swap rel update Order Customer --cascade-delete

# Make FK required:
swap rel update Order Customer --required

# Change display field:
swap rel update Order Customer --display-field Email
```

---

## Architecture

### Command Structure

```
tools/Swap.CLI/Commands/
├── GenerateRelationshipCommand.cs           # Main command entry point
└── Relationships/
    ├── RelationshipOrchestrator.cs          # Coordinates all steps
    ├── RelationshipDetector.cs              # Detects existing relationships
    ├── RelationshipValidator.cs             # Validates relationship rules
    ├── Models/
    │   ├── RelationshipDefinition.cs        # Data model for relationship
    │   ├── RelationshipType.cs              # Enum: OneToMany, ManyToMany, etc.
    │   └── DeleteBehavior.cs                # Enum: Cascade, Restrict, SetNull
    ├── Generators/
    │   ├── OneToManyGenerator.cs            # Generates one-to-many relationships
    │   ├── ManyToManyGenerator.cs           # Generates many-to-many relationships
    │   ├── OneToOneGenerator.cs             # Generates one-to-one relationships
    │   ├── ModelPropertyGenerator.cs        # Adds FK and navigation properties
    │   ├── DbContextConfigurator.cs         # Updates DbContext configuration
    │   └── MigrationGenerator.cs            # Creates EF migrations
    └── UI/
        ├── FormGenerator.cs                 # Updates create/edit forms
        ├── ViewGenerator.cs                 # Updates list/details views
        ├── DropdownGenerator.cs             # Generates FK dropdown UI
        ├── CheckboxGenerator.cs             # Generates many-to-many checkbox UI
        └── ValidationGenerator.cs           # Adds client/server validation
```

### Workflow

```
1. Parse Command Arguments
   └─> RelationshipDefinition created

2. Validate Relationship
   ├─> Check entities exist
   ├─> Check for circular dependencies
   ├─> Detect existing FK properties
   └─> Warn about breaking changes

3. Generate Model Changes
   ├─> Add foreign key property (if needed)
   ├─> Add navigation properties (both sides)
   └─> Update model classes

4. Generate DbContext Configuration
   ├─> Add HasOne/HasMany configuration
   ├─> Configure cascade delete
   ├─> Add junction table DbSet (many-to-many)
   └─> Configure unique constraints (one-to-one)

5. Generate UI
   ├─> Update create/edit forms (dropdowns, checkboxes)
   ├─> Update list views (display related entity)
   ├─> Update details views (show related data)
   └─> Add validation (required FK, etc.)

6. Generate Migration
   ├─> Build project (safety check)
   ├─> Create migration with FK constraints
   └─> Show migration preview

7. Update Configuration Tracking
   └─> Record relationship in swap-config.json
```

---

## Data Structures

### RelationshipDefinition.cs

```csharp
public class RelationshipDefinition
{
    public string SourceEntity { get; set; }          // e.g., "Order"
    public string TargetEntity { get; set; }          // e.g., "Customer"
    public RelationshipType Type { get; set; }        // OneToMany, ManyToMany, etc.
    public bool IsRequired { get; set; }              // FK is NOT NULL
    public DeleteBehavior OnDelete { get; set; }      // Cascade, Restrict, SetNull
    public string? DisplayField { get; set; }         // Field for dropdown display
    public string? ForeignKeyName { get; set; }       // e.g., "CustomerId"
    public string? NavigationProperty { get; set; }   // e.g., "Customer"
    public string? InverseNavigation { get; set; }    // e.g., "Orders"
    public string? JunctionTable { get; set; }        // For many-to-many
    public Dictionary<string, string>? JunctionProps { get; set; } // Additional junction properties
}
```

### RelationshipType.cs

```csharp
public enum RelationshipType
{
    OneToMany,      // Parent → Children (Customer → Orders)
    ManyToOne,      // Child → Parent (Order → Customer) [same as OneToMany, different direction]
    ManyToMany,     // Posts ↔ Tags
    OneToOne        // User → Profile
}
```

### DeleteBehavior.cs

```csharp
public enum DeleteBehavior
{
    Cascade,   // Delete related entities
    Restrict,  // Prevent delete if related entities exist
    SetNull    // Set FK to NULL when parent deleted
}
```

---

## Smart Features

### 1. Display Field Auto-Detection

**Priority Order:**
1. `Name` property (string)
2. `Title` property (string)
3. `Email` property (string)
4. First string property
5. `Id` (fallback)

**Logic:**
```csharp
string DetectDisplayField(ClassDeclarationSyntax entityClass)
{
    var properties = GetProperties(entityClass);
    
    if (properties.Any(p => p.Name == "Name" && p.Type == "string"))
        return "Name";
    if (properties.Any(p => p.Name == "Title" && p.Type == "string"))
        return "Title";
    if (properties.Any(p => p.Name == "Email" && p.Type == "string"))
        return "Email";
    
    var firstString = properties.FirstOrDefault(p => p.Type == "string");
    return firstString?.Name ?? "Id";
}
```

### 2. Foreign Key Property Detection

**Check if FK already exists:**
```csharp
bool HasExistingForeignKey(ClassDeclarationSyntax entityClass, string targetEntity)
{
    var properties = GetProperties(entityClass);
    
    // Check for common patterns:
    // - {TargetEntity}Id (e.g., CustomerId)
    // - {TargetEntity}{PrimaryKey} (e.g., CustomerIdentifier)
    var fkPattern = $"{targetEntity}Id";
    
    return properties.Any(p => 
        p.Name.Equals(fkPattern, StringComparison.OrdinalIgnoreCase) &&
        (p.Type == "int" || p.Type == "long" || p.Type == "Guid"));
}
```

### 3. Circular Dependency Detection

```csharp
bool HasCircularDependency(string sourceEntity, string targetEntity)
{
    // Load existing relationships from swap-config.json
    var existingRelationships = LoadRelationships();
    
    // Check if adding this relationship creates a cycle
    // Order → Customer → Address → Order = CIRCULAR
    return WouldCreateCycle(sourceEntity, targetEntity, existingRelationships);
}
```

### 4. Breaking Change Detection

```csharp
async Task<bool> CheckBreakingChanges(RelationshipDefinition rel)
{
    // Check if table has existing data
    var hasData = await TableHasData(rel.SourceEntity);
    
    if (hasData && rel.IsRequired)
    {
        AnsiConsole.MarkupLine($"[yellow]⚠ {rel.SourceEntity} table has existing data.[/]");
        AnsiConsole.MarkupLine($"[yellow]  Adding required FK will fail migration.[/]");
        AnsiConsole.MarkupLine($"[yellow]  Options:[/]");
        AnsiConsole.MarkupLine($"[yellow]    1. Make FK nullable (--nullable)[/]");
        AnsiConsole.MarkupLine($"[yellow]    2. Provide default value (--default-value)[/]");
        AnsiConsole.MarkupLine($"[yellow]    3. Clear table data (--clear-table)[/]");
        
        return false; // Block operation
    }
    
    return true;
}
```

### 5. "Did You Mean?" Suggestions

```csharp
string? SuggestEntity(string typo, List<string> availableEntities)
{
    // Use Levenshtein distance to find closest match
    var suggestions = availableEntities
        .Select(e => new { Entity = e, Distance = LevenshteinDistance(typo, e) })
        .Where(x => x.Distance <= 2)
        .OrderBy(x => x.Distance)
        .ToList();
    
    return suggestions.FirstOrDefault()?.Entity;
}
```

---

## Configuration Tracking

### swap-config.json Structure

```json
{
  "version": "0.2.0",
  "entities": {
    "Order": {
      "relationships": [
        {
          "target": "Customer",
          "type": "ManyToOne",
          "foreignKey": "CustomerId",
          "navigation": "Customer",
          "inverseNavigation": "Orders",
          "required": true,
          "onDelete": "Restrict",
          "displayField": "Name"
        }
      ]
    },
    "Post": {
      "relationships": [
        {
          "target": "Tag",
          "type": "ManyToMany",
          "junctionTable": "PostTag",
          "navigation": "Tags",
          "inverseNavigation": "Posts"
        }
      ]
    }
  }
}
```

---

## Implementation Order

### Phase 1: Foundation (Days 1-2)
- ✅ Command structure and argument parsing
- ✅ RelationshipDefinition data models
- ✅ RelationshipValidator basic validation
- ✅ Configuration tracking (swap-config.json)

### Phase 2: One-to-Many (Days 3-5)
- ✅ Foreign key property generation
- ✅ Navigation property generation (both sides)
- ✅ DbContext configuration
- ✅ Dropdown UI in forms
- ✅ Display in list/details views
- ✅ Migration generation
- ✅ Tests (20+ scenarios)

### Phase 3: Many-to-Many (Days 6-8)
- ✅ Junction table entity generation
- ✅ Navigation collections (both sides)
- ✅ DbContext configuration
- ✅ Checkbox UI
- ✅ Badge display
- ✅ Migration generation
- ✅ Tests (15+ scenarios)

### Phase 4: One-to-One (Days 9-10)
- ✅ FK with unique constraint
- ✅ Navigation properties (both sides)
- ✅ DbContext configuration
- ✅ Inline editing UI
- ✅ Migration generation
- ✅ Tests (10+ scenarios)

### Phase 5: Management Commands (Days 11-12)
- ✅ `swap relationship list`
- ✅ `swap relationship remove`
- ✅ `swap relationship detect`
- ✅ `swap relationship update`

### Phase 6: Smart Features (Days 13-14)
- ✅ Display field auto-detection
- ✅ FK auto-detection
- ✅ Circular dependency detection
- ✅ Breaking change detection
- ✅ "Did you mean?" suggestions

---

## Success Criteria

### Functional Requirements
- ✅ Generate one-to-many relationships end-to-end
- ✅ Generate many-to-many relationships end-to-end
- ✅ Generate one-to-one relationships end-to-end
- ✅ UI works (dropdowns, checkboxes, validation)
- ✅ Migrations succeed without manual intervention
- ✅ List, remove, detect commands work

### Quality Requirements
- ✅ 50+ tests covering all relationship types
- ✅ Zero breaking changes to existing 0.1.0 features
- ✅ Comprehensive documentation
- ✅ Helpful error messages

### Demo Scenario
Build a complete blog application:

```bash
# Create project
swap new BlogApp
cd BlogApp

# Generate entities
swap g m Author --fields "Name:string Email:string Bio:string?"
swap g m Category --fields "Name:string Slug:string"
swap g m Tag --fields "Name:string"
swap g m Post --fields "Title:string Content:string PublishedAt:datetime?"
swap g m Comment --fields "Content:string AuthorName:string CreatedAt:datetime"

# Generate relationships
swap g rel Post Author --type many-to-one --required --display-field Name
swap g rel Post Category --type many-to-one --nullable
swap g rel Post Tag --type many-to-many
swap g rel Comment Post --type many-to-one --required --cascade-delete

# Generate controllers
swap g c Author
swap g c Category
swap g c Tag
swap g c Post
swap g c Comment

# Done! Full blog with relationships in 10 minutes.
```

---

## Next Steps

1. Review and approve this design
2. Create GenerateRelationshipCommand.cs skeleton
3. Implement Phase 1 (Foundation)
4. Begin Phase 2 (One-to-Many)

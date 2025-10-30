# Phase 2 One-to-Many - Implementation Summary

## Overview
Phase 2 implements complete one-to-many and many-to-one relationship generation for Swap CLI v0.2.0.

## Completed Components

### 1. EntityModifier.cs (217 lines)
**Location:** `tools/Swap.CLI/Commands/Relationships/EntityModifier.cs`

Modifies entity classes using Roslyn syntax trees to add relationship properties.

#### Key Methods:
- `AddOneToManyPropertiesAsync()` - Main entry point, determines which entity gets which properties
- `AddForeignKeyProperty()` - Adds FK column (e.g., `public int? CustomerId { get; set; }`)
- `AddNavigationProperty()` - Adds single reference (e.g., `public Customer? Customer { get; set; }`)
- `AddCollectionNavigation()` - Adds collection (e.g., `public ICollection<Order> Orders { get; set; } = new List<Order>();`)
- `Pluralize()` - Smart pluralization (Order→Orders, Category→Categories, Company→Companies)
- `EntityExists()` - Validates entity files exist
- `GetEntityPath()` - Resolves full paths

#### Example Output:
```csharp
// Order.cs (source entity in one-to-many)
public class Order
{
    public int Id { get; set; }
    public required DateTime OrderDate { get; set; }
    public required decimal Total { get; set; }
    public int? CustomerId { get; set; }          // ← FK added
    public Customer? Customer { get; set; }        // ← Navigation added
}

// Customer.cs (target entity in one-to-many)
public class Customer
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();  // ← Collection added
}
```

#### Features:
- Uses Roslyn `CSharpSyntaxTree` and `SyntaxFactory`
- Applies `NormalizeWhitespace()` for proper formatting
- Checks for existing properties to avoid duplicates
- Handles both OneToMany and ManyToOne perspectives
- Nullable FK support (int? for optional, int for required)

### 2. DbContextModifier.cs (120 lines)
**Location:** `tools/Swap.CLI/Commands/Relationships/DbContextModifier.cs`

Configures EF Core relationships in the DbContext using Fluent API.

#### Key Methods:
- `ConfigureRelationshipAsync()` - Main entry point using string manipulation
- `GenerateConfigurationCode()` - Routes to specific relationship type
- `GenerateOneToManyConfig()` - Creates Fluent API code for one-to-many
- `GenerateManyToOneConfig()` - Creates Fluent API code for many-to-one
- `FindDbContextFile()` - Locates DbContext in Data/ folder

#### Example Output:
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // Order -> Customer (One-to-Many)
    modelBuilder.Entity<Order>()
        .HasOne(e => e.Customer)
        .WithMany(e => e.Orders)
        .HasForeignKey(e => e.CustomerId)
        .IsRequired(false)
        .OnDelete(DeleteBehavior.Restrict);
}
```

#### Features:
- Uses simple string manipulation (more reliable than Roslyn for this case)
- Finds `OnModelCreating` method or creates it
- Inserts configuration before closing brace
- Supports all delete behaviors (Cascade, Restrict, SetNull)
- Handles required/optional FKs
- Adds comments for clarity

### 3. GenerateRelationshipCommand.cs (Updated)
**Location:** `tools/Swap.CLI/Commands/Relationships/GenerateRelationshipCommand.cs`

Enhanced with full one-to-many/many-to-one code generation.

#### New Method:
`GenerateOneToManyAsync()` - Complete workflow with status reporting

#### Workflow:
1. **Verify entities exist** - Checks Models/{Entity}.cs files
2. **Modify source entity** - Adds FK and navigation (or collection)
3. **Modify target entity** - Adds collection (or FK and navigation)
4. **Configure DbContext** - Adds Fluent API configuration
5. **Create migration** - Runs `dotnet ef migrations add` (if not skipped)
6. **Show summary** - Displays what was created

#### Example Run:
```bash
$ swap g rel --source Order --target Customer --type one-to-many --skip-migrations

  ____                                  ____   _       ___
 / ___|  __      __   __ _   _ __      / ___| | |     |_ _|
 \___ \  \ \ /\ / /  / _` | | '_ \    | |     | |      | |
  ___) |  \ V  V /  | (_| | | |_) |   | |___  | |___   | |
 |____/    \_/\_/    \__,_| | .__/     \____| |_____| |___|
                            |_|
Generate Relationship

✓ Relationship definition is valid
Source: Order
Target: Customer
Type: OneToMany
On Delete: Restrict
Required: False

✓ Modified Models/Order.cs
✓ Modified Models/Customer.cs
✓ Configured AppDbContext.cs

✓ Relationship generated successfully!

Summary:
  Type: OneToMany
  Order → Customer
  FK: CustomerId in Order
  Navigation: Order.Customer → Customer
  Inverse: Customer.Orders → ICollection<Order>

Next steps:
  1. Review the modified entity files
  3. UI generation not yet implemented (coming soon)
```

## Testing Results

### Build Status
✅ All projects compile successfully
- Swap.CLI builds without errors
- Swap.Patterns builds without errors
- Swap.Htmx builds without errors
- Swap.Testing builds without errors

### Test Suite
✅ All 269 tests pass
- Swap.CLI.Tests: 160 tests ✓
- Swap.Patterns.Tests: 72 tests ✓
- Swap.Htmx.Tests: 37 tests ✓

### Manual Testing
✅ Successfully tested on SqlServerTest app:
- Created Customer entity
- Created Order entity
- Generated one-to-many relationship
- Verified entity code formatting
- Verified DbContext configuration
- No duplicate properties on re-run

## Command Examples

### Basic One-to-Many
```bash
# Customer has many Orders
swap g rel --source Order --target Customer --type one-to-many
```

### Many-to-One (Inverse Perspective)
```bash
# Order belongs to Customer (same result, different perspective)
swap g rel --source Order --target Customer --type many-to-one
```

### With Options
```bash
# Required FK with cascade delete
swap g rel -s Order -t Customer --type 1:n --required --on-delete cascade

# Custom FK and navigation names
swap g rel -s Order -t Customer --type one-to-many --fk CustomerId --nav BelongsToCustomer --inverse CustomerOrders

# Skip migrations and UI
swap g rel -s Order -t Customer --type one-to-many --skip-migrations --skip-ui
```

## Generated Files Modified

For `swap g rel --source Order --target Customer --type one-to-many`:

1. **Models/Order.cs**
   - Added: `public int? CustomerId { get; set; }`
   - Added: `public Customer? Customer { get; set; }`

2. **Models/Customer.cs**
   - Added: `public ICollection<Order> Orders { get; set; } = new List<Order>();`

3. **Data/AppDbContext.cs**
   - Added EF Core Fluent API configuration in `OnModelCreating()`

4. **Migrations/YYYYMMDDHHMMSS_AddOrderToCustomerRelationship.cs** (if not skipped)
   - AddColumn: `CustomerId int NULL` to Orders table
   - AddForeignKey: Orders.CustomerId → Customers.Id

## Known Limitations

### Not Yet Implemented (Phase 2 TODO):
- ❌ UI generation (forms with dropdowns)
- ❌ View generation (display related data)
- ❌ Unit tests for relationship generation
- ❌ Display field auto-detection
- ❌ Relationship tracking in swap-config.json

### Planned for Future Phases:
- Phase 3: Many-to-Many relationships with junction tables
- Phase 4: One-to-One relationships with unique constraints
- Phase 5-6: Management commands and smart features

## Technical Details

### Roslyn Usage
- `CSharpSyntaxTree.ParseText()` - Parse C# source code
- `ClassDeclarationSyntax` - Represents class declarations
- `PropertyDeclarationSyntax` - Represents properties
- `SyntaxFactory.PropertyDeclaration()` - Create new properties
- `.NormalizeWhitespace()` - Format code with proper indentation
- `.ToFullString()` - Convert syntax tree back to string

### Delete Behaviors Supported
- `Restrict` (default) - Prevent deletion if related entities exist
- `Cascade` - Delete related entities when parent is deleted
- `SetNull` - Set FK to NULL (requires nullable FK)

### Naming Conventions
- FK: `{TargetEntity}Id` (e.g., CustomerId, ProductId)
- Navigation: `{TargetEntity}` (e.g., Customer, Product)
- Collection: `{Pluralized(SourceEntity)}` (e.g., Orders, Categories)

### Pluralization Rules
- Regular: Order → Orders, Product → Products
- Ending in 'y': Category → Categories, Company → Companies
- Ending in 's/x/z/ch/sh': Class → Classes, Box → Boxes

## File Structure
```
tools/Swap.CLI/Commands/Relationships/
├── Models/
│   ├── RelationshipType.cs          (28 lines) [Phase 1]
│   ├── DeleteBehavior.cs            (26 lines) [Phase 1]
│   └── RelationshipDefinition.cs    (97 lines) [Phase 1]
├── EntityModifier.cs                 (217 lines) [Phase 2] ← NEW
├── DbContextModifier.cs              (120 lines) [Phase 2] ← NEW
├── GenerateRelationshipCommand.cs    (403 lines) [Phase 1 + 2 updates]
└── RelationshipValidator.cs          (102 lines) [Phase 1]
```

Total code: **993 lines** (Phase 1: 461, Phase 2: 532)

## Next Steps (Phase 2 Continuation)

### Immediate TODO:
1. **UI Generation** - Generate dropdown selects in forms
2. **View Display** - Show related data in list/detail views
3. **Unit Tests** - 20+ tests for relationship generation
4. **Display Field Detection** - Auto-detect Name/Title/Email fields

### Future Enhancements:
- Bi-directional relationship management
- Relationship removal command
- Relationship listing command
- Smart suggestions based on existing entities

## Migration Example

When running without `--skip-migrations`:

```bash
$ swap g rel -s Order -t Customer --type one-to-many

# Creates migration:
Migrations/20251030123456_AddOrderToCustomerRelationship.cs
```

Migration content:
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AddColumn<int>(
        name: "CustomerId",
        table: "Orders",
        type: "int",
        nullable: true);

    migrationBuilder.CreateIndex(
        name: "IX_Orders_CustomerId",
        table: "Orders",
        column: "CustomerId");

    migrationBuilder.AddForeignKey(
        name: "FK_Orders_Customers_CustomerId",
        table: "Orders",
        column: "CustomerId",
        principalTable: "Customers",
        principalColumn: "Id",
        onDelete: ReferentialAction.Restrict);
}
```

## Completion Criteria

### ✅ Core Features Complete:
- [x] Entity modification with FK and navigation properties
- [x] DbContext Fluent API configuration
- [x] Proper code formatting
- [x] Delete behavior support (Cascade, Restrict, SetNull)
- [x] Required/optional FK support
- [x] Migration generation
- [x] Status reporting with Spectre.Console
- [x] Error handling and validation
- [x] All existing tests pass (269/269)

### ⏳ Remaining for Phase 2:
- [ ] UI generation (forms with dropdowns)
- [ ] View generation (display related data)
- [ ] Unit tests (20+ tests)
- [ ] Display field auto-detection
- [ ] Configuration tracking (swap-config.json)

---
**Completion Date:** October 30, 2025  
**Status:** ✅ Phase 2 Core Complete - Entity and DbContext generation working perfectly  
**Next:** UI generation, view updates, and comprehensive testing

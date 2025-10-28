---
sidebar_position: 3
---

# swap generate model

Generate entity models with custom fields, automatic DbContext registration, and support for all C# data types.

## Synopsis

```bash
swap generate model <name> [options]
swap g m <name> [options]  # Short alias
```

## Description

The `generate model` command creates entity classes (models) for your application. It:

- Generates a C# class with properties based on field specifications
- Always includes an `Id` primary key property
- Supports 11 C# data types plus aliases
- Handles nullable and required fields
- Automatically registers the entity in `AppDbContext`
- Uses project-specific namespaces
- Applies proper naming conventions (PascalCase)

## Arguments

### `<name>`

**Required.** The name of the entity to generate.

- Must be PascalCase (e.g., `Product`, `CustomerOrder`)
- Cannot contain spaces
- Will be used for the class name and DbSet property

**Examples:**
```bash
swap g m Product
swap g m Customer
swap g m BlogPost
swap g m TodoItem
```

## Options

### `--fields <specification>`

Define custom fields for the entity.

**Format:**
```
FieldName:Type[:flags] [FieldName:Type[:flags]...]
or
FieldName:Type[:flags],FieldName:Type[:flags],...
```

**Examples:**
```bash
# Space-separated
swap g m Product --fields "Name:string Price:decimal Stock:int"
# Comma-separated
swap g m Product --fields Name:string,Price:decimal,Stock:int
# With flags
swap g m Product --fields "Name:string:s,f Price:decimal:s Stock:int:ns"
swap g m Product --fields Name:string:s,f,Price:decimal:s,Stock:int:ns
```

**Default:** If omitted, generates a model with `Id`, `Title`, and `IsComplete` fields (Todo pattern).

## Supported Data Types

### Primitive Types

| Type | C# Type | Example | Notes |
|------|---------|---------|-------|
| `int` | `int` | `Age:int` | 32-bit integer |
| `long` | `long` | `FileSize:long` | 64-bit integer |
| `short` | `short` | `Code:short` | 16-bit integer |
| `byte` | `byte` | `Level:byte` | 8-bit unsigned |
| `bool` | `bool` | `IsActive:bool` | True/False |
| `decimal` | `decimal` | `Price:decimal` | High-precision decimals |
| `double` | `double` | `Rating:double` | 64-bit floating point |
| `float` | `float` | `Score:float` | 32-bit floating point |

### Reference Types

| Type | C# Type | Example | Notes |
|------|---------|---------|-------|
| `string` | `string` | `Name:string` | Text, always required unless nullable |
| `datetime` | `DateTime` | `CreatedAt:datetime` | Date and time |
| `guid` | `Guid` | `UniqueId:guid` | Globally unique identifier |

### Type Aliases

For convenience, these aliases are supported:

| Alias | Maps To | Example |
|-------|---------|---------|
| `str` | `string` | `Name:str` |
| `dec` | `decimal` | `Price:dec` |
| `date` | `DateTime` | `CreatedAt:date` |

## Nullable Fields

Add `?` after the type to make a field nullable:

```bash
swap g m Order --fields Notes:string?,CompletedAt:datetime?,Rating:int?
```

Generates:

```csharp
public string? Notes { get; set; }
public DateTime? CompletedAt { get; set; }
public int? Rating { get; set; }
```

### Required vs Optional Strings

- `Name:string` → `public required string Name { get; set; }` (non-nullable)
- `Name:string?` → `public string? Name { get; set; }` (nullable)

This follows C# nullable reference types conventions.

## Examples

### Basic Model with Custom Fields

```bash
swap g m Product --fields Name:string,Price:decimal,Stock:int
```

**Generated `Models/Product.cs`:**

```csharp
namespace MyApp.Models;

public class Product
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
}
```

**Updated `Data/AppDbContext.cs`:**

```csharp
public DbSet<MyApp.Models.Product> Products { get; set; }
```

### Model with Nullable Fields

```bash
swap g m BlogPost --fields Title:string,Content:string,PublishedAt:datetime?,Tags:string?
```

**Generated:**

```csharp
namespace MyApp.Models;

public class BlogPost
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? Tags { get; set; }
}
```

### Default Fields (No --fields Option)

```bash
swap g m Task
```

**Generated:**

```csharp
namespace MyApp.Models;

public class Task
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public bool IsComplete { get; set; }
}
```

### E-Commerce Models

```bash
# Customer
swap g m Customer --fields Name:string,Email:string,Phone:string?,DateJoined:datetime

# Order
swap g m Order --fields CustomerId:int,OrderDate:datetime,Total:decimal,Status:string,ShippedAt:datetime?

# Product
swap g m Product --fields Name:string,Description:string?,Price:decimal,Stock:int,SKU:string

# OrderItem
swap g m OrderItem --fields OrderId:int,ProductId:int,Quantity:int,UnitPrice:decimal
```

### Blog System Models

```bash
# Post
swap g m Post --fields Title:string,Content:string,AuthorId:int,PublishedAt:datetime?,ViewCount:int

# Comment
swap g m Comment --fields PostId:int,AuthorName:string,Content:string,CreatedAt:datetime,IsApproved:bool

# Category
swap g m Category --fields Name:string,Description:string?,PostCount:int
```

### CRM Models

```bash
# Contact
swap g m Contact --fields FirstName:string,LastName:string,Email:string,Company:string?,Phone:string?

# Lead
swap g m Lead --fields ContactId:int,Source:string,Status:string,Score:int,AssignedTo:string?

# Activity
swap g m Activity --fields LeadId:int,Type:string,Description:string?,Date:datetime,Duration:int?
```

## Generated Code Patterns

### Always Includes Id

Every model automatically gets a primary key:

```csharp
public int Id { get; set; }
```

This is added even if not specified in `--fields`.

### Required String Properties

Non-nullable strings use the `required` modifier:

```csharp
public required string Name { get; set; }
```

This enforces initialization at object creation.

### Namespace Convention

Models use the project namespace:

```csharp
namespace MyApp.Models;  // Where MyApp is your project name
```

### DbSet Registration

The entity is registered in your DbContext with a pluralized name:

```csharp
public DbSet<MyApp.Models.Product> Products { get; set; }
```

Currently uses simple pluralization (adds 's'). Advanced pluralization coming soon.

## Workflow

### 1. Generate Model

```bash
swap g m Product --fields Name:string,Price:decimal
```

### 2. Review Generated Code

Open `Models/Product.cs` and customize if needed:

```csharp
namespace MyApp.Models;

public class Product
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public decimal Price { get; set; }
    
    // Add custom properties, methods, or attributes
    public string? ImageUrl { get; set; }
    
    public string DisplayName => $"{Name} - ${Price:F2}";
}
```

### 3. Create Migration

```bash
dotnet ef migrations add AddProduct
```

### 4. Apply Migration

```bash
dotnet ef database update
```

### 5. Use in Controllers

Generate a CRUD controller:

```bash
swap g c Product
```

## Customizing Generated Models

### Add Data Annotations

```csharp
using System.ComponentModel.DataAnnotations;

public class Product
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public required string Name { get; set; }
    
    [Range(0.01, 10000)]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }
}
```

### Add Navigation Properties

```csharp
public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public decimal Total { get; set; }
    
    // Navigation property
    public Customer? Customer { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}
```

### Add Computed Properties

```csharp
public class BlogPost
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Computed property
    public int ReadingTimeMinutes => Content.Length / 250;
    public string Excerpt => Content.Length > 200 
        ? Content.Substring(0, 200) + "..." 
        : Content;
}
```

### Implement Interfaces

```csharp
public class Product : IAuditable
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public decimal Price { get; set; }
    
    // IAuditable
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
}
```

## Advanced Scenarios

### Regenerating Models

If you need to regenerate a model with different fields:

```bash
# First generation
swap g m Product --fields Name:string,Price:decimal

# Later, add more fields by regenerating
swap g m Product --fields Name:string,Price:decimal,Stock:int,SKU:string
```

:::warning
This overwrites the existing file. Commit your changes first or manually merge.
:::

### Multiple Related Models

Generate related models in sequence:

```bash
swap g m Author --fields Name:string,Bio:string?
swap g m Book --fields Title:string,AuthorId:int,PublishedDate:datetime,ISBN:string
swap g m Review --fields BookId:int,Rating:int,Comment:string?,ReviewDate:datetime
```

Then manually add navigation properties:

```csharp title="Models/Author.cs"
public class Author
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Bio { get; set; }
    
    // Add navigation property
    public List<Book> Books { get; set; } = new();
}
```

### Value Objects (Coming Soon)

Future enhancement for DDD value objects:

```bash
swap g m Address --fields Street:string,City:string,ZipCode:string --value-object
```

## Troubleshooting

### "No .csproj file found"

Run the command from your project root:

```bash
cd MyApp
swap g m Product --fields Name:string
```

### "DbSet already exists"

The model was already added. The CLI warns but doesn't fail:

```
Warning: DbSet<Product> already exists in DbContext
```

You can safely regenerate the model file.

### Invalid Field Type

```
Error: Unsupported field type 'unknown' for field 'MyField'
Supported types: string, int, bool, decimal, double, float, long, short, byte, datetime, guid
```

Check the [Supported Data Types](#supported-data-types) section.

### Entity Name Contains Spaces

```bash
swap g m "Product Item"  # ❌ Error
```

Use PascalCase instead:

```bash
swap g m ProductItem  # ✅ Correct
```

## Best Practices

### 1. Use Descriptive Names

```bash
# Good
swap g m CustomerOrder
swap g m BlogPost
swap g m PaymentTransaction

# Avoid
swap g m Order  # Too generic
swap g m Data   # Vague
```

### 2. Plan Your Fields

List out all fields before generating:

```bash
# Good: Complete field list
swap g m Product --fields Name:string,Description:string?,Price:decimal,Stock:int,SKU:string,CategoryId:int

# Avoid: Generating multiple times with different fields
```

### 3. Use Nullable Appropriately

```bash
# Required information
Name:string
Email:string
CreatedAt:datetime

# Optional information
MiddleName:string?
Phone:string?
LastLoginAt:datetime?
```

### 4. Follow Naming Conventions

- Use PascalCase: `BlogPost`, not `blogPost` or `blog_post`
- Use singular names: `Product`, not `Products`
- Be specific: `CustomerAddress` over `Address`

### 5. Commit Before Generating

```bash
git add .
git commit -m "Before adding Product model"
swap g m Product --fields Name:string,Price:decimal
```

## Next Steps

- [Generate Controllers](./generate-controller) - Create CRUD controllers for your models
- [Generate Resources](./generate-resource) - Generate model + controller together
- [CLI Overview](./overview) - All CLI commands and options

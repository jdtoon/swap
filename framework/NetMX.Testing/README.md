# NetMX.Testing

Testing utilities for NetMX applications. Simplifies unit, integration, and E2E testing.

## Features

- **TestProjectFactory** - Create temporary test projects with SQLite databases
- **FeatureTestRunner** - Run CLI commands and verify results in isolation
- **TestDataBuilder** - Generate realistic test data using Bogus
- **In-Memory SQLite** - Fast, isolated database testing
- **WebApplicationFactory Integration** - HTTP integration testing support

## Installation

```bash
dotnet add package NetMX.Testing
```

## Quick Start

### Testing a CLI-Generated Feature

```csharp
using NetMX.Testing;
using Xunit;

public class FeatureGenerationTests
{
    [Fact]
    public async Task GenerateFeature_WithMigrate_ShouldCreateAllFiles()
    {
        // Arrange: Create test project with SQLite
        using var runner = new FeatureTestRunner();
        
        // Act: Run CLI command
        var result = await runner.RunCliCommandAsync("generate feature Product --migrate");
        
        // Assert: Verify success
        Assert.True(result.Success, $"Command failed: {result.Error}");
        
        // Verify entity file created
        Assert.True(runner.FileExists("Models/Product.cs"));
        
        // Verify DbSet added
        var dbContextFile = runner.ReadFile("Data/TestProjectDbContext.cs");
        Assert.Contains("DbSet<Product>", dbContextFile);
        
        // Verify migration created
        Assert.True(runner.GetMigrationCount() > 0);
        
        // Verify database created
        Assert.True(runner.DatabaseExists());
    }
}
```

### In-Memory SQLite Testing

```csharp
using Microsoft.EntityFrameworkCore;
using NetMX.Testing;
using Xunit;

public class ProductServiceTests
{
    [Fact]
    public async Task CreateProduct_ShouldSaveToDatabase()
    {
        // Arrange: Create in-memory SQLite database
        using var connection = TestProjectFactory.CreateInMemoryConnection();
        var options = TestProjectFactory.CreateSqliteOptions<MyDbContext>(connection);
        
        using var context = new MyDbContext(options);
        await context.Database.EnsureCreatedAsync();
        
        var service = new ProductService(context);
        
        // Act
        var product = new Product { Name = "Test Product", Price = 99.99m };
        await service.CreateAsync(product);
        
        // Assert
        var saved = await context.Products.FirstOrDefaultAsync();
        Assert.NotNull(saved);
        Assert.Equal("Test Product", saved.Name);
    }
}
```

### Test Data Generation

```csharp
using NetMX.Testing;
using Xunit;

public class DataGenerationTests
{
    [Fact]
    public void GenerateTestData_ShouldCreateRealisticData()
    {
        // Generate fake products
        var productFaker = TestDataBuilder.Create<Product>()
            .RuleFor(p => p.Name, f => f.Commerce.ProductName())
            .RuleFor(p => p.Price, f => f.Random.Decimal(1, 1000))
            .RuleFor(p => p.Description, f => f.Commerce.ProductDescription());
        
        var products = productFaker.Generate(100);
        
        Assert.Equal(100, products.Count);
        Assert.All(products, p => Assert.False(string.IsNullOrEmpty(p.Name)));
    }
    
    [Fact]
    public void QuickHelpers_ShouldGenerateData()
    {
        var email = TestDataBuilder.RandomEmail();
        var phone = TestDataBuilder.RandomPhone();
        var companyName = TestDataBuilder.RandomCompanyName();
        
        Assert.NotEmpty(email);
        Assert.NotEmpty(phone);
        Assert.NotEmpty(companyName);
    }
}
```

### HTTP Integration Testing

```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using NetMX.Testing;
using Xunit;

public class ProductApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    
    public ProductApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }
    
    [Fact]
    public async Task GetProducts_ShouldReturnOk()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        // Act
        var response = await client.GetAsync("/api/products");
        
        // Assert
        response.EnsureSuccessStatusCode();
    }
}
```

## Test Project Structure

When you create a test project with `TestProjectFactory`, you get:

```
TempTestProject/
├── Models/                          (Empty, ready for entities)
├── Data/
│   └── TestProjectDbContext.cs     (Minimal DbContext)
├── Migrations/                      (Empty, ready for migrations)
├── appsettings.json                 (SQLite connection string)
└── TestProject.csproj               (Minimal dependencies)
```

## API Reference

### TestProjectFactory

- `CreateTestProject(string? projectName)` - Create temporary project
- `CreateInMemoryConnection()` - Create SQLite in-memory connection
- `CreateSqliteOptions<TContext>(connection)` - Create DbContext options
- `CleanupTestProject(string projectPath)` - Delete test project

### FeatureTestRunner

- `RunCliCommandAsync(string command, int timeout)` - Execute CLI command
- `FileExists(string relativePath)` - Check if file exists
- `ReadFile(string relativePath)` - Read file content
- `FileContains(string relativePath, string text)` - Check file content
- `GetFiles(string relativePath, string pattern)` - List files
- `DatabaseExists()` - Check if database was created
- `GetMigrationCount()` - Count migration files

### TestDataBuilder

- `Create<T>()` - Create Bogus Faker for type T
- `RandomEntityName(string? prefix)` - Generate entity name
- `RandomEmail()` - Generate email address
- `RandomPhone()` - Generate phone number
- `RandomCompanyName()` - Generate company name
- `RandomPersonName()` - Generate person name
- `RandomAddress()` - Generate full address
- `RandomNumber(int min, int max)` - Generate random number
- `RandomDecimal(decimal min, decimal max)` - Generate random decimal
- `RandomBool()` - Generate random boolean
- `RandomDate(DateTime? start, DateTime? end)` - Generate random date

## Best Practices

### 1. Always Dispose Test Runners

```csharp
// Use using statement
using var runner = new FeatureTestRunner();

// Or try-finally
var runner = new FeatureTestRunner();
try
{
    // Test code
}
finally
{
    runner.Dispose();
}
```

### 2. Keep In-Memory Connections Open

```csharp
// Connection must stay open for in-memory SQLite
using var connection = TestProjectFactory.CreateInMemoryConnection();
// ... use connection ...
// Disposed at end of using block
```

### 3. Use Realistic Test Data

```csharp
// Better than hardcoded values
var product = new Product
{
    Name = TestDataBuilder.RandomEntityName("Product"),
    Price = TestDataBuilder.RandomDecimal(1, 1000)
};
```

### 4. Test Cleanup

```csharp
// Auto-cleanup (default)
using var runner = new FeatureTestRunner();

// Manual cleanup (for debugging)
using var runner = new FeatureTestRunner(cleanupOnDispose: false);
// Project stays in: %TEMP%\netmx-tests\[ProjectName]
```

## Examples

See `tests/NetMX.Testing.Tests/` for comprehensive examples:
- CLI command testing
- Database migration testing
- Feature generation testing
- HTMX integration testing (Playwright)

## Dependencies

- **xUnit** - Testing framework
- **Bogus** - Test data generation
- **Microsoft.EntityFrameworkCore.Sqlite** - In-memory database testing
- **Microsoft.AspNetCore.Mvc.Testing** - HTTP integration testing

## Integration with CI/CD

NetMX.Testing works great in CI/CD pipelines:

```yaml
# .github/workflows/test.yml
- name: Run Tests
  run: dotnet test --no-build --verbosity normal
  
# SQLite tests run without PostgreSQL server!
# Faster, no external dependencies
```

## Performance

- **Fast** - SQLite in-memory tests run in milliseconds
- **Isolated** - Each test gets its own database
- **Parallel** - Tests can run in parallel (xUnit default)
- **No Setup** - No database server required

## Future Features (Phase 2D+)

- 🔄 Playwright integration for HTMX E2E tests
- 🔄 CLI test commands (`netmx test feature Product`)
- 🔄 Module testing support
- 🔄 Snapshot testing
- 🔄 Performance benchmarking

## Support

- **Issues**: [GitHub Issues](https://github.com/netmx-framework/netmx/issues)
- **Discussions**: [GitHub Discussions](https://github.com/netmx-framework/netmx/discussions)
- **Documentation**: [NetMX Docs](https://github.com/netmx-framework/netmx/tree/main/docs)

---

**NetMX.Testing** - Making testing NetMX apps dead simple! 🧪✨

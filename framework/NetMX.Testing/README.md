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
// Auto-cleanup (default) - RECOMMENDED
using var runner = new FeatureTestRunner();
// Project and SQLite database automatically deleted

// Manual cleanup (for debugging only)
using var runner = new FeatureTestRunner(cleanupOnDispose: false);
// Project stays in: %TEMP%\netmx-tests\[ProjectName]
// WARNING: Manual cleanup required to prevent disk space issues!
```

**Important**: Always use `using` statements or call `Dispose()` explicitly to prevent disk space accumulation.

### 5. Memory Management

```csharp
// In-memory SQLite: Connection must stay open
using var connection = TestProjectFactory.CreateInMemoryConnection();
// ... use connection ...
// Automatically closed and memory freed at end of using block

// File-based SQLite: Explicit cleanup
using var runner = new FeatureTestRunner();
// Database files deleted on dispose (retries if locked)
```

**Best Practice**: NetMX.Testing automatically:
- Deletes temp project directories on dispose
- Forces garbage collection before cleanup
- Retries cleanup if files are locked (up to 3 attempts)
- Explicitly deletes .db files before directory removal

## 6. Playwright E2E Testing

NetMX.Testing includes **PlaywrightTestBase** for browser-based E2E testing with HTMX-specific helpers.

### Installation

```bash
# Install Playwright browsers
dotnet playwright install
```

### Quick Start

```csharp
using NetMX.Testing;
using Xunit;

public class ProductE2ETests : PlaywrightTestBase, IAsyncLifetime
{
    private const string BaseUrl = "http://localhost:5000";

    public async Task InitializeAsync()
    {
        // Initialize Playwright (chromium/firefox/webkit)
        await base.InitializeAsync("chromium", headless: true);
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
    }

    [Fact]
    public async Task CreateProduct_ViaHTMX_AddsRowToTable()
    {
        // Navigate to page
        await Page.GotoAsync($"{BaseUrl}/Product");

        // Click button with hx-get attribute
        await ClickAndWaitForHxSwapAsync(
            "button[hx-get='/Product/Create']", 
            "#product-form");

        // Fill and submit form via HTMX
        await FillAndSubmitHxFormAsync("#product-form", new Dictionary<string, string>
        {
            ["Name"] = "Test Product",
            ["Price"] = "99.99"
        });

        // Wait for HTMX event
        await WaitForHxEventAsync("product-created");

        // Verify row added
        await AssertTextContainsAsync("table tbody", "Test Product");
    }
}
```

### HTMX-Specific Helpers

PlaywrightTestBase provides 10 HTMX-specific assertion methods:

#### 1. **WaitForHxRequestAsync** - Wait for HTMX requests

```csharp
await WaitForHxRequestAsync("/Product/Search", timeout: 5000);
```

#### 2. **WaitForHxEventAsync** - Wait for HTMX events

```csharp
await WaitForHxEventAsync("product-created", timeout: 5000);
```

#### 3. **AssertHxTriggerAsync** - Verify HX-Trigger header

```csharp
await AssertHxTriggerAsync("product-created");
```

#### 4. **ClickAndWaitForHxSwapAsync** - Click and wait for swap

```csharp
await ClickAndWaitForHxSwapAsync(
    "button[hx-get='/Edit']", 
    "#form-container");
```

#### 5. **AssertHxSwapAsync** - Verify hx-swap attribute

```csharp
await AssertHxSwapAsync("button[hx-delete]", "outerHTML");
```

#### 6. **FillAndSubmitHxFormAsync** - Submit HTMX forms

```csharp
await FillAndSubmitHxFormAsync("#my-form", new Dictionary<string, string>
{
    ["Name"] = "John Doe",
    ["Email"] = "john@example.com"
});
```

#### 7. **AssertHxLoadingAsync** - Check loading state

```csharp
await AssertHxLoadingAsync("button[hx-get]"); // Has htmx-request class
```

#### 8. **WaitForHxBoostAsync** - Wait for boosted navigation

```csharp
await Page.ClickAsync("a[hx-boost='true']");
await WaitForHxBoostAsync();
Assert.Contains("/products", GetCurrentUrl());
```

#### 9. **AssertHxConfirmAsync** - Verify hx-confirm attribute

```csharp
await AssertHxConfirmAsync("button[hx-delete]");
```

#### 10. **GetCurrentUrl** - Get URL after boost navigation

```csharp
string url = GetCurrentUrl(); // http://localhost:5000/products
```

### Additional Helper Methods

- **TakeScreenshotAsync(path)** - Save screenshot for debugging
- **GetTextAsync(selector)** - Get element text
- **AssertTextContainsAsync(selector, text)** - Verify text content
- **AssertVisibleAsync(selector)** - Check visibility
- **AssertHiddenAsync(selector)** - Check hidden state

### Complete Example

See `Examples/ProductFeatureE2EExample.cs` for 7 complete HTMX testing patterns:

1. **Create via HTMX** - Form submission and table updates
2. **Inline Edit** - Click-to-edit pattern
3. **Delete with Confirmation** - hx-confirm dialog handling
4. **Debounced Search** - Search-as-you-type pattern
5. **Infinite Scroll** - hx-trigger="revealed" pattern
6. **Boost Navigation** - hx-boost without page reload
7. **Form Validation** - Inline error display

### Browser Options

```csharp
// Chromium (default)
await base.InitializeAsync("chromium", headless: true);

// Firefox
await base.InitializeAsync("firefox", headless: true);

// WebKit (Safari)
await base.InitializeAsync("webkit", headless: true);

// Headed mode (see browser)
await base.InitializeAsync("chromium", headless: false);
```

### Why Playwright for HTMX?

HTMX testing requires **browser automation** because:
- ✅ HTMX events fire in the browser (htmx:afterSwap, etc.)
- ✅ DOM updates happen client-side
- ✅ hx-confirm triggers browser dialogs
- ✅ hx-boost changes history without page reload
- ✅ Unit tests can't verify visual behavior

PlaywrightTestBase makes HTMX E2E testing **dead simple**!

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
- **Microsoft.Playwright** - Browser automation for E2E testing

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

- ✅ Playwright integration for HTMX E2E tests (COMPLETE!)
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

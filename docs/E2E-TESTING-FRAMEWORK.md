# NetMX E2E Testing Framework

**Status**: Planned for Phase 2D (Week 8-9)  
**Priority**: HIGH - Critical for product quality  
**Integration**: CLI + Suite + Studio

---

## Vision

**NetMX.Testing**: A first-class E2E testing framework specifically designed for NetMX applications, with:
- **CLI integration** (`netmx test feature Product`, `netmx test module Identity`)
- **Playwright-based** (industry standard, cross-browser)
- **HTMX-aware** (understands hx-* attributes, events, swaps)
- **SQLite by default** (fast, isolated, no Docker)
- **Type-safe assertions** (leverages NetMX.Events for event testing)
- **Future: Visual testing** in NetMX Studio

---

## Architecture

### 1. NetMX.Testing Package

```
framework/NetMX.Testing/
├── NetMX.Testing.csproj
├── README.md
├── Core/
│   ├── NetMXTestBase.cs              # Base class for all tests
│   ├── NetMXFeatureTest.cs           # Feature-specific test base
│   ├── NetMXModuleTest.cs            # Module-specific test base
│   └── TestProjectFactory.cs         # Creates temp test projects
├── Database/
│   ├── SQLiteTestDbContext.cs        # In-memory SQLite helper
│   ├── TestDatabaseFixture.cs        # Database setup/teardown
│   └── SeedDataBuilder.cs            # Fluent API for test data
├── Playwright/
│   ├── NetMXPlaywrightTest.cs        # Playwright integration
│   ├── HtmxHelpers.cs                # HTMX-specific assertions
│   ├── EventHelpers.cs               # Event testing (NetMX.Events)
│   └── ScreenshotHelper.cs           # Visual regression testing
├── Assertions/
│   ├── HtmxAssertions.cs             # hx-trigger, hx-swap, etc.
│   ├── EventAssertions.cs            # Events.Product.Created fired?
│   └── ResponseAssertions.cs         # HTTP response helpers
└── CLI/
    ├── TestRunner.cs                 # CLI test execution
    └── TestReporter.cs               # Test result formatting
```

### 2. CLI Commands

```bash
# Test entire module
netmx test module Identity
# Result: Runs all Identity module tests (unit + integration + E2E)

# Test specific feature in isolation
netmx test feature Product
# Result: Creates temp project, adds Product feature, runs E2E tests, cleans up

# Test HTMX interactions
netmx test htmx Product --browser chromium
# Result: Playwright tests for hx-get, hx-post, hx-delete, events

# Visual regression testing
netmx test visual Product --baseline
# Result: Takes screenshots of Product pages, stores as baseline

# Run all tests in solution
netmx test all
# Result: Runs unit + integration + E2E for entire solution
```

### 3. Test File Structure

```
tests/
├── NetMX.Identity.Tests/             # Unit tests
├── NetMX.Identity.Integration.Tests/ # Integration tests (with TestHost)
└── NetMX.Identity.E2E.Tests/         # End-to-end tests (with Playwright)
    ├── Features/
    │   ├── RegistrationTests.cs
    │   ├── LoginTests.cs
    │   └── ProfileTests.cs
    ├── Htmx/
    │   ├── HtmxFormSubmissionTests.cs
    │   ├── HtmxEventsTests.cs
    │   └── HtmxSwapTests.cs
    └── Visual/
        ├── RegistrationPageTests.cs
        └── Snapshots/
            ├── registration-light.png
            └── registration-dark.png
```

---

## Example Usage

### Feature Test (Automated by CLI)

```csharp
using NetMX.Testing;
using NetMX.Testing.Playwright;
using Xunit;

namespace MyApp.Tests.E2E.Features;

public class ProductTests : NetMXFeatureTest
{
    // CLI auto-generates this test when you run: netmx generate feature Product
    [Fact]
    public async Task Product_CRUD_WorksEndToEnd()
    {
        // Arrange: Use fluent API for test data
        await SeedDatabase(db => db
            .AddProduct("Test Product", price: 99.99m)
            .AddCategory("Electronics"));
        
        // Act & Assert: Full CRUD flow
        await RunFeatureTest<Product>(async page =>
        {
            // 1. List page loads
            await page.GotoAsync("/Product");
            await page.AssertHtmxList("product-list", itemCount: 1);
            
            // 2. Create new product
            await page.ClickAsync("[hx-get='/Product/Create']");
            await page.FillAsync("#Name", "New Product");
            await page.FillAsync("#Price", "149.99");
            await page.ClickAsync("button[type='submit']");
            
            // Assert: HTMX event fired
            await page.AssertEventTriggered(Events.Product.Created);
            
            // Assert: List updated (no full reload)
            await page.AssertHtmxList("product-list", itemCount: 2);
            
            // 3. Edit product
            await page.ClickAsync("[hx-get='/Product/Edit/1']");
            await page.FillAsync("#Name", "Updated Product");
            await page.ClickAsync("button[type='submit']");
            
            // Assert: Event + swap
            await page.AssertEventTriggered(Events.Product.Updated);
            await page.AssertText("Updated Product");
            
            // 4. Delete product
            await page.ClickAsync("[hx-delete='/Product/Delete/1']");
            await page.ConfirmDialog(); // Handles hx-confirm
            
            // Assert: Row removed
            await page.AssertEventTriggered(Events.Product.Deleted);
            await page.AssertHtmxSwap(HtmxSwap.Delete);
            await page.AssertHtmxList("product-list", itemCount: 1);
        });
    }
}
```

### HTMX-Specific Assertions

```csharp
public static class HtmxAssertions
{
    // Assert HTMX request headers
    public static async Task AssertIsHtmxRequest(this IPage page)
    {
        var headers = await page.GetRequestHeaders();
        Assert.Contains("HX-Request", headers.Keys);
    }
    
    // Assert HTMX response headers
    public static async Task AssertHtmxTrigger(this IPage page, string eventName)
    {
        var response = await page.WaitForResponseAsync("**");
        var headers = response.Headers;
        Assert.Contains("HX-Trigger", headers.Keys);
        Assert.Contains(eventName, headers["HX-Trigger"]);
    }
    
    // Assert HTMX swap behavior
    public static async Task AssertHtmxSwap(this IPage page, HtmxSwap swap)
    {
        var response = await page.WaitForResponseAsync("**");
        var swapHeader = response.Headers.GetValueOrDefault("HX-Reswap");
        Assert.Equal(swap.ToString().ToLower(), swapHeader);
    }
    
    // Assert element was updated (not replaced)
    public static async Task AssertElementUpdated(this IPage page, string selector)
    {
        var element = await page.QuerySelectorAsync(selector);
        Assert.NotNull(element);
        
        // Check if element has data-htmx-updated attribute
        var updated = await element.GetAttributeAsync("data-htmx-updated");
        Assert.NotNull(updated);
    }
}
```

### Event Testing (Type-Safe)

```csharp
public static class EventAssertions
{
    // Assert NetMX event was triggered
    public static async Task AssertEventTriggered(
        this IPage page, 
        string eventName,
        object? payload = null)
    {
        // Listen for HTMX trigger header
        var response = await page.WaitForResponseAsync(r => 
            r.Headers.ContainsKey("HX-Trigger") && 
            r.Headers["HX-Trigger"].Contains(eventName));
        
        Assert.NotNull(response);
        
        if (payload != null)
        {
            var triggerData = JsonSerializer.Deserialize<Dictionary<string, object>>(
                response.Headers["HX-Trigger"]);
            
            Assert.Contains(eventName, triggerData.Keys);
            // Validate payload structure
        }
    }
    
    // Assert event listener exists on page
    public static async Task AssertEventListener(
        this IPage page, 
        string eventName,
        string selector)
    {
        var element = await page.QuerySelectorAsync(selector);
        var hxTrigger = await element.GetAttributeAsync("hx-trigger");
        
        Assert.Contains(eventName, hxTrigger);
    }
}
```

### SQLite Test Database

```csharp
public class SQLiteTestDbContext : IDisposable
{
    private readonly DbContext _context;
    private readonly string _connectionString;
    
    public SQLiteTestDbContext(Type dbContextType)
    {
        // Create in-memory SQLite database
        _connectionString = "Data Source=:memory:";
        
        var options = new DbContextOptionsBuilder()
            .UseSqlite(_connectionString)
            .Options;
        
        _context = (DbContext)Activator.CreateInstance(dbContextType, options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();
    }
    
    public async Task SeedAsync(Action<DbContext> seedAction)
    {
        seedAction(_context);
        await _context.SaveChangesAsync();
    }
    
    public void Dispose()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }
}
```

---

## CLI Integration

### Test Command Implementation

```csharp
// tools/NetMX.CLI/Commands/TestCommand.cs
public class TestCommand : Command
{
    public TestCommand() : base("test", "Run NetMX tests")
    {
        AddCommand(new TestFeatureCommand());
        AddCommand(new TestModuleCommand());
        AddCommand(new TestHtmxCommand());
        AddCommand(new TestVisualCommand());
    }
}

public class TestFeatureCommand : Command
{
    public TestFeatureCommand() : base("feature", "Test a specific feature in isolation")
    {
        AddArgument(new Argument<string>("name", "Feature name"));
        AddOption(new Option<string>("--browser", "Browser to use (chromium, firefox, webkit)"));
        AddOption(new Option<bool>("--headed", "Run browser in headed mode"));
    }
    
    public override async Task<int> ExecuteAsync(InvocationContext context)
    {
        var featureName = context.ParseResult.GetValueForArgument<string>("name");
        
        ConsoleHelper.WriteHeader($"Testing Feature: {featureName}");
        
        // 1. Create temp test project
        var testProject = await TestProjectFactory.CreateAsync(featureName);
        
        // 2. Generate feature
        await GenerateFeatureAsync(testProject, featureName);
        
        // 3. Run E2E tests
        var testRunner = new PlaywrightTestRunner(testProject);
        var results = await testRunner.RunAsync();
        
        // 4. Report results
        TestReporter.PrintResults(results);
        
        // 5. Cleanup
        await testProject.CleanupAsync();
        
        return results.Passed ? 0 : 1;
    }
}
```

---

## NetMX Studio Integration (Future)

### Visual Test Runner

```
┌─────────────────────────────────────────────────────────────┐
│  NetMX Studio - Test Explorer                               │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  📁 NetMX.Identity.E2E.Tests                                │
│    ├─ 📁 Features                                           │
│    │   ├─ ✅ RegistrationTests.cs (3 passed)                │
│    │   ├─ ✅ LoginTests.cs (5 passed)                       │
│    │   └─ ❌ ProfileTests.cs (1 failed, 2 passed)           │
│    │                                                         │
│    ├─ 📁 Htmx                                               │
│    │   ├─ ✅ HtmxFormSubmissionTests.cs (8 passed)          │
│    │   └─ ✅ HtmxEventsTests.cs (12 passed)                 │
│    │                                                         │
│    └─ 📁 Visual                                             │
│        ├─ ✅ RegistrationPageTests.cs (2 passed)            │
│        └─ 📖 View Snapshots →                               │
│                                                             │
│  [Run All] [Run Failed] [Debug] [Coverage]                 │
└─────────────────────────────────────────────────────────────┘
```

### Failed Test Details

```
┌─────────────────────────────────────────────────────────────┐
│  Test: ProfileTests.Update_ShouldModifyUserData             │
├─────────────────────────────────────────────────────────────┤
│  Status: FAILED                                             │
│  Duration: 1.23s                                            │
│  Browser: Chromium                                          │
│                                                             │
│  Error:                                                     │
│    Expected event 'user.profile.updated' to be triggered   │
│    but received 'user.updated' instead.                     │
│                                                             │
│  Screenshot: 📸 View →                                      │
│  Video: 🎥 View →                                           │
│  Trace: 🔍 View →                                           │
│                                                             │
│  [Re-run] [Debug] [Update Baseline]                        │
└─────────────────────────────────────────────────────────────┘
```

---

## NetMX Suite Integration (Future)

### Cloud Test Execution

Users can run tests in the cloud via NetMX Suite:

```bash
# Deploy and test in cloud
netmx suite deploy --test

# Result: 
# 1. Deploys to Azure/AWS test environment
# 2. Runs full E2E suite (all browsers)
# 3. Returns results + screenshots + videos
# 4. Cleans up resources
```

**Benefits**:
- No local Playwright installation needed
- Test across all browsers (Chrome, Firefox, Safari, Edge)
- Parallel execution (10x faster)
- Video recordings of failures
- Test history tracking

---

## Implementation Roadmap

### Phase 2D: Foundation (Week 8-9) - **PRIORITY**
1. **Week 8**: NetMX.Testing package
   - NetMXTestBase, NetMXFeatureTest
   - SQLiteTestDbContext
   - Playwright integration
   - HTMX helpers
   
2. **Week 9**: CLI integration
   - `netmx test feature` command
   - `netmx test module` command
   - Test result reporting

### Phase 3: Advanced Features (Month 4-5)
3. Visual regression testing
4. Event assertions (NetMX.Events integration)
5. Performance testing
6. Accessibility testing (a11y)

### Phase 5: Studio/Suite Integration (Month 10-15)
7. Visual test runner in NetMX Studio
8. Cloud test execution in NetMX Suite
9. Test history & analytics
10. AI-powered test generation

---

## Why This Matters

### For Developers
- **Confidence**: Tests validate HTMX interactions actually work
- **Speed**: SQLite = instant test execution (no Docker setup)
- **DX**: Type-safe assertions, fluent API, clear error messages
- **CI/CD**: Easy integration (just `netmx test all`)

### For NetMX Product
- **Quality**: Every feature/module validated before release
- **Dogfooding**: We test our own CLI/framework constantly
- **Differentiation**: No other .NET framework has HTMX-native testing
- **Trust**: Users know modules work because we test them

### For Business
- **Reliability**: Fewer bugs = happier customers
- **Velocity**: Automated tests = faster releases
- **Support**: Self-testing = fewer support tickets
- **Premium Feature**: E2E testing could be Suite/Studio value-add

---

## Success Metrics

- **Coverage**: 80%+ code coverage for all modules
- **Speed**: Full E2E suite runs in <5 minutes
- **Reliability**: 99%+ test pass rate (no flaky tests)
- **Adoption**: 90%+ of generated features include E2E tests
- **DX**: Developers say "testing is easy" not "testing is hard"

---

## Competitive Advantage

**ABP Framework**: No built-in E2E testing, no HTMX support  
**NetMX**: First-class E2E testing with HTMX-native assertions

**Result**: NetMX applications are more reliable, tested, and maintainable!

---

**Next Steps**: Implement Phase 2D (Week 8-9) after completing CLI automation Phase 1-3.

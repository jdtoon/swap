# NetMX Developer Experience (DX)

**Last Updated**: October 22, 2025  
**Purpose**: Define principles and guidelines for maintaining excellent developer experience in NetMX

---

## 🎯 What Is Developer Experience?

**Developer Experience (DX)** is how developers feel when using our framework.

Good DX means:
- ✅ **Fast**: From idea to running code in minutes, not hours
- ✅ **Clear**: Errors tell you what's wrong AND how to fix it
- ✅ **Predictable**: Conventions make sense, no surprises
- ✅ **Discoverable**: Features are easy to find (IntelliSense, docs, examples)
- ✅ **Powerful**: Can handle complex scenarios when needed
- ✅ **Joyful**: Building with NetMX is fun, not frustrating

---

## 🌟 Core DX Principles

### 1. **Make It Work Out of the Box**

**Principle**: Zero configuration to start, full customization when needed

**Good Example**:
```bash
# One command, project is ready
netmx new modular MyApp
cd MyApp
dotnet run  # Works immediately
```

**Bad Example**:
```bash
# Requires manual setup
dotnet new web
# Now manually:
# - Add 10 NuGet packages
# - Create DbContext
# - Configure Program.cs
# - Set up migrations
# - Configure HTMX
# - ...30 more steps
```

**Application**:
- Templates include everything configured
- Sensible defaults for all options
- Common use cases work without configuration
- Advanced scenarios have clear documentation

---

### 2. **Type Safety Everywhere**

**Principle**: Catch errors at compile-time, not runtime

**Good Example**:
```csharp
// ✅ Type-safe (compile-time error if event doesn't exist)
this.HxTrigger(Events.Product.Created, new { productId = product.Id });

// In view:
@Events.Product.Created  // IntelliSense shows all events
```

**Bad Example**:
```csharp
// ❌ Magic strings (typos = runtime errors)
this.HxTrigger("product-created", new { productId = product.Id });

// In view:
"product-created"  // No IntelliSense, easy to typo
```

**Application**:
- No magic strings (use constants, enums, or generated code)
- Strong typing for all public APIs
- Generics where appropriate
- IntelliSense support everywhere

---

### 3. **Fast Feedback Loops**

**Principle**: Developers should see results immediately

**Metrics**:
- **CLI generation**: <5 seconds for full feature
- **Build time**: <10 seconds for incremental build
- **Hot reload**: Changes visible without restart
- **Test execution**: <1 second for unit tests

**Application**:
- CLI is fast (async operations, minimal I/O)
- Build optimizations (incremental compilation)
- Hot reload support (edit code, see changes instantly)
- Fast tests (in-memory database, mock external services)

---

### 4. **Excellent Error Messages**

**Principle**: Errors should explain what's wrong AND how to fix it

**Good Example**:
```
❌ Error: Could not find web project

💡 Explanation:
   The CLI searches for projects ending in .Web.csproj
   
💡 Solution:
   1. Rename your project: MyApp.csproj → MyApp.Web.csproj
   2. Or run from a directory with a .Web.csproj file
   
📚 Learn more: https://docs.netmx.dev/cli/project-structure
```

**Bad Example**:
```
Error: Project not found
```

**Application**:
- Clear, actionable error messages
- Include context (what we were trying to do)
- Suggest solutions (how to fix it)
- Link to documentation for complex issues

---

### 5. **Documentation as Code**

**Principle**: Documentation lives where developers are

**Application**:
```csharp
/// <summary>
/// Triggers an HTMX event that can be caught by other components.
/// </summary>
/// <param name="eventName">
/// Event name from Events.* registry (e.g., Events.Product.Created).
/// Use IntelliSense to discover available events.
/// </param>
/// <param name="payload">
/// Event data passed to listeners. Will be serialized to JSON.
/// </param>
/// <example>
/// <code>
/// this.HxTrigger(Events.Product.Created, new { productId = product.Id });
/// </code>
/// </example>
public void HxTrigger(string eventName, object? payload = null)
```

**Benefits**:
- IntelliSense shows documentation
- Examples right in code
- Documentation stays up-to-date with code
- No context switching to browser

---

### 6. **Convention Over Configuration**

**Principle**: Sensible defaults, explicit configuration only when needed

**Examples**:

| Convention | Default | Override |
|------------|---------|----------|
| **Entity Name** | `Product` | N/A |
| **DbSet Property** | `Products` (pluralized) | Custom via DbContext |
| **Controller Route** | `/Product` | `[Route("/custom")]` |
| **View Location** | `Views/Product/` | Custom view resolution |
| **Primary Key** | `Id` (int/Guid) | Custom property name |

**Application**:
- Follow established patterns (ASP.NET MVC conventions)
- Don't require configuration for common scenarios
- Allow overrides for advanced scenarios
- Document conventions clearly

---

## 🛠️ DX in Practice

### CLI Design

**Principles**:
1. **Discoverable**: `netmx --help` shows all commands
2. **Consistent**: Similar commands have similar syntax
3. **Fast**: Operations complete in seconds
4. **Clear Output**: Progress indicators, colored messages
5. **Safe**: Confirm destructive operations

**Example**:
```bash
# Good: Clear, consistent, discoverable
netmx generate feature Product
netmx generate feature Category --search
netmx generate feature Order --search --export

# Bad: Cryptic, inconsistent
netmx gen-prod
netmx create-category-searchable
netmx order-with-export
```

---

### Generated Code Quality

**Principles**:
1. **Production-Ready**: Not scaffolding, actual working code
2. **Best Practices**: DDD patterns, separation of concerns
3. **Well-Formatted**: Proper indentation, spacing
4. **Commented**: XML docs for public APIs
5. **Tested**: Generated code should be testable

**Example**:
```csharp
// ✅ Generated code is production-quality
public class Product : AggregateRoot<Guid>
{
    /// <summary>
    /// Gets the product name.
    /// </summary>
    public string Name { get; private set; }
    
    /// <summary>
    /// Gets the product price.
    /// </summary>
    public decimal Price { get; private set; }
    
    private Product() { } // For EF Core
    
    /// <summary>
    /// Creates a new product.
    /// </summary>
    /// <param name="id">Product identifier.</param>
    /// <param name="name">Product name (required).</param>
    /// <param name="price">Product price (must be positive).</param>
    public Product(Guid id, string name, decimal price) : base(id)
    {
        Name = Guard.NotNullOrEmpty(name, nameof(name));
        Price = Guard.GreaterThanOrEqualTo(price, 0, nameof(price));
    }
}
```

---

### API Design

**Principles**:
1. **Intuitive**: Methods do what you expect
2. **Consistent**: Similar APIs work similarly
3. **Fluent**: Chainable when appropriate
4. **Async**: All I/O operations are async
5. **Generic**: Use generics to reduce code duplication

**Example**:
```csharp
// ✅ Good API design
var products = await _repository
    .AsQueryable()
    .Where(p => p.IsActive)
    .OrderBy(p => p.Name)
    .ToListAsync();

// ❌ Bad API design
var products = _repository.GetAllActiveProductsSortedByName(); // Too specific
```

---

### Testing Experience

**Principles**:
1. **Fast**: Tests run in <1 second
2. **Isolated**: Tests don't depend on each other
3. **Clear Names**: Test name explains what's being tested
4. **Arrange-Act-Assert**: Clear test structure
5. **Helpful Failures**: Failed assertion shows what went wrong

**Example**:
```csharp
[Fact]
public void UpdatePrice_WithNegativePrice_ThrowsArgumentException()
{
    // Arrange
    var product = new Product(Guid.NewGuid(), "Test", 100m);
    
    // Act & Assert
    var exception = Assert.Throws<ArgumentException>(() => 
        product.UpdatePrice(-10m));
    
    Assert.Contains("Price must be positive", exception.Message);
}
```

---

## 📊 Measuring DX

### Quantitative Metrics

| Metric | Target | Current |
|--------|--------|---------|
| **Time to First Feature** | <30 min | ~30 min ✅ |
| **CLI Feature Generation** | <5 sec | ~3 sec ✅ |
| **Build Time (Clean)** | <30 sec | ~25 sec ✅ |
| **Build Time (Incremental)** | <10 sec | ~5 sec ✅ |
| **Test Execution** | <1 sec/test | ~0.5 sec/test ✅ |
| **Documentation Search** | <2 min to answer | TBD 🔄 |

### Qualitative Feedback

**Questions to Ask Users**:
1. How easy was it to get started? (1-10)
2. Did you find what you needed in docs? (Yes/No)
3. Were error messages helpful? (1-10)
4. Would you recommend NetMX? (NPS score)
5. What was most frustrating?

**Target NPS Score**: >50 (World-class)

---

## 🎯 DX Checklist for New Features

Before releasing any feature, verify:

### ✅ Functionality
- [ ] Feature works as expected
- [ ] Edge cases handled
- [ ] Error conditions tested
- [ ] Performance is acceptable

### ✅ Developer Experience
- [ ] Zero configuration to start
- [ ] Type-safe APIs (no magic strings)
- [ ] IntelliSense support
- [ ] Clear error messages
- [ ] Fast execution (<5 sec for CLI)

### ✅ Documentation
- [ ] XML docs for public APIs
- [ ] Code examples provided
- [ ] README updated
- [ ] QUICK-START.md updated (if needed)

### ✅ Testing
- [ ] Unit tests (80%+ coverage)
- [ ] Integration tests
- [ ] Dogfooding (built real feature with it)
- [ ] Manual testing (tried it fresh)

### ✅ Quality
- [ ] Zero warnings
- [ ] Code formatted (proper indentation)
- [ ] Follows naming conventions
- [ ] No TODO comments left

---

## 🚀 DX Improvements Roadmap

### Phase 2 (Current)
- ✅ `netmx new` command (project templates)
- 🔄 Better error messages in CLI
- ⏳ QUICK-START.md improvements
- ⏳ Video tutorials (5-10 minutes each)

### Phase 3
- IntelliSense for HTMX attributes (VS Code extension)
- Live HTMX preview (see changes without refresh)
- Snippet library (common patterns)
- CLI autocomplete (shell integration)

### Phase 4
- Visual Studio integration
- Database schema viewer
- Entity relationship diagram generator
- Performance profiler

### Phase 5
- NetMX Studio (visual tools)
- NetMX Suite (low-code builder)
- AI-powered code suggestions
- Interactive tutorials

---

## 💡 DX Best Practices

### For CLI Commands
1. Use verb-noun pattern (`generate feature`, not `feature-gen`)
2. Provide `--help` for every command
3. Show progress for long operations
4. Use colors (green=success, red=error, blue=info)
5. Confirm destructive operations

### For APIs
1. Async by default (all I/O operations)
2. Fluent where it makes sense
3. Return meaningful types (not just `bool`)
4. Throw specific exceptions (not generic `Exception`)
5. Validate early (fail fast)

### For Documentation
1. Start with examples (show, don't tell)
2. Explain *why*, not just *what*
3. Link to related topics
4. Keep it up-to-date (docs as code)
5. Provide runnable samples

### For Testing
1. Name tests clearly (`WhenX_ThenY`)
2. One assertion per test (mostly)
3. Use test helpers (reduce boilerplate)
4. Mock external dependencies
5. Keep tests fast (<1 second)

---

## 🎯 Summary

**Good DX Means**:
1. **Fast**: Minutes from idea to running code
2. **Clear**: Errors tell you what to do
3. **Type-Safe**: Compile-time errors, not runtime
4. **Discoverable**: IntelliSense, docs, examples
5. **Joyful**: Fun to build with

**We Measure Success By**:
- Time to first feature (<30 minutes)
- CLI speed (<5 seconds per feature)
- Error message quality (actionable + helpful)
- Documentation findability (<2 minutes to answer)
- Developer satisfaction (NPS >50)

**Every Feature Must Have**:
- Zero configuration to start
- Type-safe APIs
- Clear error messages
- Complete documentation
- 80%+ test coverage

---

**Remember**: We're building a framework to make developers' lives easier. Every decision should optimize for developer happiness and productivity.

---

## 📚 Related Documents

- [THE-PRODUCT.md](THE-PRODUCT.md) - What we're building
- [INSPIRATION.md](INSPIRATION.md) - Why we're building it this way
- [ROADMAP.md](ROADMAP.md) - Where we're headed
- [QUICK-START.md](QUICK-START.md) - Get started in 5 minutes
- [TERMINOLOGY.md](TERMINOLOGY.md) - Key concepts and definitions

# Strategic Architecture Decisions - Static Events, Versioning, Repository Structure

**Date**: 2025-01-19  
**Critical Decisions**: Type safety, versioning, mono-repo vs multi-repo  
**Status**: 🎯 Architecture Refinement

## 🎯 Three Critical Issues

### Issue 1: Magic Strings for Events ❌

**Current Problem**:
```csharp
// ❌ BAD: Magic strings everywhere
this.HxTrigger("product-created");
this.HxTrigger("product-updated");
this.HxTrigger("order-placed");

// What if you typo?
this.HxTrigger("prodcut-created");  // ❌ Runtime error, hard to debug

// What events exist? No IntelliSense, no discoverability
```

**Why This Is Bad**:
- ❌ No compile-time safety
- ❌ Easy to typo
- ❌ Hard to refactor (find all usages)
- ❌ No IntelliSense / discoverability
- ❌ Debugging nightmare (which string did I use?)

### Issue 2: CLI Versioning Issues 🔧

**Current Problem**:
```powershell
# Update doesn't work, need to uninstall/reinstall
dotnet tool update --global NetMX.CLI  # ❌ Doesn't pick up changes

# Have to do this every time:
dotnet tool uninstall --global NetMX.CLI
dotnet tool install --global --add-source ./nupkg NetMX.CLI

# Which version am I running?
netmx --version  # Not showing correct version!
```

**Why This Is Bad**:
- ❌ Wastes development time
- ❌ Risk of using wrong version
- ❌ No clear version visibility

### Issue 3: Repository Structure Confusion 🏗️

**Current Reality**: Single monorepo  
**Expected**: Multi-repo with NuGet packages?

**Question**: "Isn't everything we are doing using NuGet package manager?"

**Confusion**: 
- We have `framework/NetMX.sln` with 9 packages
- These CAN be published to NuGet
- But currently, we're developing in a monorepo
- Should we split into separate repos per package?

## ✅ Proposed Solutions

### Solution 1: Static Event Names (Type-Safe Events)

#### Approach A: Static Constants Class (Simplest)

**Create framework package: `NetMX.Events`**

```csharp
// NetMX.Events/DomainEvents.cs
namespace NetMX.Events;

/// <summary>
/// Centralized domain event names for HTMX triggers.
/// Use these constants instead of magic strings.
/// </summary>
public static class DomainEvents
{
    /// <summary>Product-related events</summary>
    public static class Product
    {
        public const string Created = "product.created";
        public const string Updated = "product.updated";
        public const string Deleted = "product.deleted";
        public const string StockChanged = "product.stock-changed";
    }
    
    /// <summary>Order-related events</summary>
    public static class Order
    {
        public const string Created = "order.created";
        public const string Updated = "order.updated";
        public const string Placed = "order.placed";
        public const string Shipped = "order.shipped";
        public const string Cancelled = "order.cancelled";
    }
    
    /// <summary>User/Identity events</summary>
    public static class Identity
    {
        public const string LoggedIn = "identity.logged-in";
        public const string LoggedOut = "identity.logged-out";
        public const string Registered = "identity.registered";
        public const string ProfileUpdated = "identity.profile-updated";
        public const string PasswordChanged = "identity.password-changed";
    }
    
    /// <summary>Audit events</summary>
    public static class Audit
    {
        public const string LogCreated = "audit.log-created";
        public const string EntryCreated = "audit.entry-created";
    }
}
```

**Usage** (Type-Safe):
```csharp
// ✅ GOOD: IntelliSense, compile-time checking, refactor-safe
using NetMX.Events;

[HttpPost]
public async Task<IActionResult> Create(CreateProductDto dto)
{
    await _service.CreateAsync(dto);
    
    // ✅ Type-safe, discoverable, refactor-safe
    this.HxTrigger(DomainEvents.Product.Created);
    
    return await List();
}
```

**Benefits**:
- ✅ IntelliSense shows all available events
- ✅ Compile-time safety (typos caught immediately)
- ✅ Easy to refactor (F2 rename works)
- ✅ Discoverable (Ctrl+Space shows options)
- ✅ Simple implementation (just constants)

#### Approach B: Strongly-Typed Event Objects (More Advanced)

```csharp
// NetMX.Events/IHtmxEvent.cs
namespace NetMX.Events;

public interface IHtmxEvent
{
    string Name { get; }
    object? Data { get; }
}

public abstract class HtmxEvent<TData> : IHtmxEvent
{
    protected HtmxEvent(TData? data = default)
    {
        Data = data;
    }
    
    public abstract string Name { get; }
    public object? Data { get; }
}
```

```csharp
// Product/ProductEvents.cs
public class ProductCreatedEvent : HtmxEvent<ProductCreatedData>
{
    public override string Name => "product.created";
    
    public ProductCreatedEvent(Guid productId, string productName) 
        : base(new ProductCreatedData(productId, productName))
    {
    }
}

public record ProductCreatedData(Guid ProductId, string ProductName);
```

**Usage**:
```csharp
[HttpPost]
public async Task<IActionResult> Create(CreateProductDto dto)
{
    var product = await _service.CreateAsync(dto);
    
    // ✅ Strongly typed with data
    this.HxTrigger(new ProductCreatedEvent(product.Id, product.Name));
    
    return await List();
}
```

**Extension Method**:
```csharp
public static class HtmxEventExtensions
{
    public static void HxTrigger(this Controller controller, IHtmxEvent evt)
    {
        HtmxResponse.Trigger(controller, evt.Name, evt.Data);
    }
}
```

**Benefits**:
- ✅ Everything from Approach A
- ✅ Strongly-typed event data
- ✅ Can add validation/logic to events
- ✅ Better for complex scenarios

**Drawbacks**:
- ⚠️ More complex
- ⚠️ More files to maintain
- ⚠️ May be overkill for simple cases

#### Recommendation: **Approach A (Static Constants)**

**Rationale**:
- Simple and effective
- No overengineering
- Easy to understand
- Covers 95% of use cases
- Can evolve to Approach B later if needed

### Solution 2: CLI Versioning Strategy

#### Problem Analysis

**Why `dotnet tool update` doesn't work**:
```xml
<!-- NetMX.CLI.csproj -->
<PropertyGroup>
    <PackageVersion>1.0.0</PackageVersion>  <!-- ❌ Never changes! -->
</PropertyGroup>
```

**The issue**: Version is hardcoded, so .NET thinks there's no update.

#### Solution: Semantic Versioning + Git Tags

**Step 1: Implement Proper Versioning**

```xml
<!-- NetMX.CLI.csproj -->
<PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    
    <!-- ✅ Semantic Versioning -->
    <Version>0.1.0</Version>
    <PackageVersion>$(Version)</PackageVersion>
    
    <!-- ✅ Build metadata -->
    <InformationalVersion>$(Version)+$(Configuration)</InformationalVersion>
    
    <!-- Tool configuration -->
    <PackAsTool>true</PackAsTool>
    <ToolCommandName>netmx</ToolCommandName>
    <PackageOutputPath>./nupkg</PackageOutputPath>
    
    <!-- ✅ Package metadata -->
    <PackageId>NetMX.CLI</PackageId>
    <Authors>NetMX Framework</Authors>
    <Description>CLI tooling for NetMX framework - HTMX-first .NET development</Description>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageProjectUrl>https://github.com/toonjd/netmx</PackageProjectUrl>
    <RepositoryUrl>https://github.com/toonjd/netmx</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
    <PackageTags>netmx;htmx;cli;dotnet;code-generation</PackageTags>
</PropertyGroup>
```

**Step 2: Show Version in CLI**

```csharp
// Program.cs
private static void DisplayBanner()
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine(@"
 _   _      _   __  ____  __
| \ | | ___| |_|  \/  \ \/ /
|  \| |/ _ \ __| |\/| |\  / 
| |\  |  __/ |_| |  | |/  \ 
|_| \_|\___|\__|_|  |_/_/\_\
                            
");
    Console.ResetColor();
    
    var version = typeof(Program).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
        ?.InformationalVersion ?? "Unknown";
    
    ConsoleHelper.WriteInfo($"The best CLI for .NET + HTMX developers");
    ConsoleHelper.WriteInfo($"Version {version}");  // ✅ Show version
    Console.WriteLine();
}
```

**Step 3: Versioning Workflow**

```bash
# Development versions (local)
# Version format: 0.1.0-dev.{timestamp}

# Update version before each test
# In NetMX.CLI.csproj: <Version>0.1.0-dev.20250119001</Version>

# Build and install
dotnet pack -c Release
dotnet tool uninstall --global NetMX.CLI
dotnet tool install --global --add-source ./nupkg NetMX.CLI

# Verify version
netmx --version
```

**Step 4: Release Versions**

```bash
# When ready for release:
# 1. Update version in .csproj: <Version>0.2.0</Version>
# 2. Commit and tag
git commit -am "Release v0.2.0"
git tag v0.2.0
git push origin v0.2.0

# 3. Pack and publish to NuGet
dotnet pack -c Release
dotnet nuget push ./nupkg/NetMX.CLI.0.2.0.nupkg --source nuget.org
```

**Version Scheme**:
```
0.1.0-dev.20250119001  # Development (local)
0.1.0-alpha.1          # Alpha release
0.1.0-beta.1           # Beta release
0.1.0                  # Stable release
0.2.0                  # Minor update (new features)
1.0.0                  # Major release
```

#### Recommendation: Timestamp-Based Dev Versions

**For local development**:
```xml
<Version>0.1.$(date:yyyyMMddHHmm)</Version>  <!-- Auto-increments -->
```

Or simpler:
```xml
<Version>0.1.0-dev</Version>  <!-- Same version, force reinstall -->
```

**Install command**:
```bash
# Always uninstall first in development
dotnet tool uninstall -g NetMX.CLI
dotnet tool install -g --add-source ./nupkg NetMX.CLI

# Or create a script: reinstall-cli.ps1
```

### Solution 3: Repository Structure Clarification

#### Current State: Monorepo ✅

**What we have**:
```
netmx/                           # Single repo
├── framework/                   # 9 NuGet packages
│   ├── NetMX.Core/
│   ├── NetMX.Ddd.Domain/
│   ├── NetMX.Htmx/
│   └── ... (6 more)
├── modules/                     # Modules (also can be NuGet)
│   └── Identity/
├── templates/                   # Project templates
│   └── modular/
├── tools/                       # CLI tool (NuGet)
│   └── NetMX.CLI/
└── docs/
```

**This is CORRECT for now!**

#### Why Monorepo? ✅

**Benefits**:
1. ✅ **Easier development** - All code in one place
2. ✅ **Atomic commits** - Change framework + modules together
3. ✅ **Easier to refactor** - Can change APIs across packages
4. ✅ **Better discoverability** - See how everything fits
5. ✅ **Faster CI/CD** - Build everything at once
6. ✅ **Perfect for early stage** - We're still iterating rapidly

**Examples of successful monorepos**:
- Google (billions of lines of code)
- Microsoft (Windows, Office, Azure)
- Facebook (React, React Native, etc.)
- ABP Framework (our inspiration!)

#### When to Split? (Future)

**Consider multi-repo when**:
- Different release cadences needed
- Different teams own different packages
- Packages are truly independent
- Repo size becomes problematic (10GB+)
- CI/CD becomes too slow (>30min builds)

**We're nowhere near this yet!**

#### NuGet Publishing Strategy

**Current**: Develop in monorepo  
**Publish**: Individual packages to NuGet.org  
**Use**: Developers reference NuGet packages

**Workflow**:
```bash
# Develop in monorepo
cd framework/NetMX.Core
# Make changes...

# Pack individual packages
dotnet pack -c Release

# Publish to NuGet
dotnet nuget push bin/Release/NetMX.Core.1.0.0.nupkg --source nuget.org

# Developers use it
dotnet add package NetMX.Core
```

**This is standard practice!**

#### Recommendation: **Keep Monorepo** ✅

**Rationale**:
- We're in rapid development phase
- Easier to maintain consistency
- Can refactor across packages easily
- Matches ABP Framework structure
- Can split later if needed (but probably won't need to)

## 🎯 Implementation Plan

### Phase 1: Static Event Names (Priority: HIGH)

**Tasks**:
1. ✅ Create `NetMX.Events` package in framework
2. ✅ Create `DomainEvents` static class
3. ✅ Update CLI to generate with static events
4. ✅ Update existing code (Identity, Product, Demo)
5. ✅ Update documentation

**Time**: 2-3 hours

### Phase 2: CLI Versioning (Priority: HIGH)

**Tasks**:
1. ✅ Update NetMX.CLI.csproj with proper versioning
2. ✅ Add version display in banner
3. ✅ Create reinstall-cli.ps1 script
4. ✅ Document versioning workflow
5. ✅ Test version updates

**Time**: 1-2 hours

### Phase 3: Documentation Updates (Priority: MEDIUM)

**Tasks**:
1. ✅ Update INTEGRATION-PATTERNS.md with static events
2. ✅ Create ARCHITECTURE-DECISIONS.md (this document)
3. ✅ Update CLI-IMPLEMENTATION.md with versioning
4. ✅ Clarify repository structure in README

**Time**: 1 hour

## 📊 Comparison: Before & After

### Event Names

**Before (Magic Strings)**:
```csharp
this.HxTrigger("product-created");  // ❌ No IntelliSense
this.HxTrigger("prodcut-created");  // ❌ Typo = runtime error
```

**After (Static Constants)**:
```csharp
this.HxTrigger(DomainEvents.Product.Created);  // ✅ IntelliSense
this.HxTrigger(DomainEvents.Product.Craeted);  // ✅ Compile error!
```

### CLI Version

**Before**:
```bash
netmx --version  # Version 1.0.0 (always the same)
```

**After**:
```bash
netmx --version  # Version 0.1.20250119-dev
netmx --help     # Shows version in banner
```

### Repository Understanding

**Before**: "Are we multi-repo?"  
**After**: "We're monorepo, publish to NuGet individually" ✅

## 🎓 Key Principles

### 1. Type Safety Over Magic Strings

**Principle**: If it can be typed, type it.

```csharp
// ❌ Magic strings
this.HxTrigger("some-event");
controller.RedirectToAction("Index");
var value = config["ConnectionString"];

// ✅ Constants or strong types
this.HxTrigger(DomainEvents.Product.Created);
controller.RedirectToAction(nameof(Index));
var value = config.GetConnectionString("Default");
```

### 2. Versioning Everything

**Principle**: Every artifact has a version.

- Packages: SemVer (1.2.3)
- CLI: SemVer with build metadata (0.1.0-dev.20250119)
- Database: Migrations (AddProduct_20250119)
- APIs: Route versions (/api/v1/products)

### 3. Monorepo Until It Hurts

**Principle**: Keep things together until there's a compelling reason to split.

**Don't split because**:
- "It feels like we should"
- "Other people do it"
- "It's more 'microservices-y'"

**DO split when**:
- Different release schedules needed
- Different teams with clear boundaries
- Performance issues (repo size, CI time)
- Legal/licensing requirements

## ✅ Conclusion

### Issue 1: Magic Strings → Static Events ✅
**Solution**: Create `NetMX.Events` with `DomainEvents` static class  
**Benefit**: Type safety, IntelliSense, refactor-safe  
**Effort**: 2-3 hours

### Issue 2: CLI Versioning → SemVer ✅
**Solution**: Proper versioning in .csproj, display in banner  
**Benefit**: Clear version tracking, easier debugging  
**Effort**: 1-2 hours

### Issue 3: Repo Structure → Monorepo is Correct ✅
**Decision**: Keep monorepo, publish packages individually  
**Benefit**: Easier development, matches industry best practices  
**Effort**: None (already correct!)

## 🚀 Next Steps

**Immediate (Today)**:
1. Create `NetMX.Events` package
2. Implement `DomainEvents` static class
3. Update CLI to generate with static events
4. Fix CLI versioning

**Tomorrow**:
1. Update existing code to use static events
2. Test CLI version updates
3. Document new patterns

**This Week**:
1. Build Audit module with new patterns
2. Validate everything works
3. Refine based on experience

---

**Status**: Clear architecture decisions, ready to implement! 🎯

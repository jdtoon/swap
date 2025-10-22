# NetMX Dependency Audit

**Date**: October 21, 2025  
**Purpose**: Review all external dependencies - Can we remove/replace them?

---

## 📊 **Dependency Summary**

### **Critical Microsoft Dependencies** (Keep)

| Package | Used In | Purpose | Replace? |
|---------|---------|---------|----------|
| `Microsoft.EntityFrameworkCore` | Framework, Modules | Database ORM | ❌ NO - Core infrastructure |
| `Microsoft.AspNetCore.*` | Framework | Web framework | ❌ NO - Core infrastructure |
| `Microsoft.Extensions.*` | Framework | DI, Logging, Config | ❌ NO - Core infrastructure |
| `Microsoft.CodeAnalysis.CSharp` | CLI | Code generation | ❌ NO - Roslyn needed |

**Decision**: These are **fundamental .NET infrastructure**. Replacing would mean rebuilding .NET itself. **KEEP**.

---

### **External Dependencies** (Review)

#### **CLI Tools**

| Package | Purpose | Can Replace? | Alternative |
|---------|---------|--------------|-------------|
| `System.CommandLine` | CLI framework | ⚠️ MAYBE | Build our own argument parser |
| `Spectre.Console` | Rich terminal UI | ⚠️ MAYBE | Use Console.WriteLine |
| `LibGit2Sharp` | Git operations | ✅ YES | Use `dotnet` CLI commands |
| `Microsoft.CodeAnalysis.CSharp.Workspaces` | Code modification | ❌ NO | Need Roslyn |

**Recommendations**:
- ✅ **Remove LibGit2Sharp** - Not needed yet
- ⚠️ **Keep Spectre.Console** - Great DX (progress bars, colors)
- ⚠️ **Keep System.CommandLine** - Beta but stable, saves time
- ❌ **Keep CodeAnalysis** - Essential for code generation

---

#### **Testing Libraries**

| Package | Purpose | Can Replace? | Alternative |
|---------|---------|--------------|-------------|
| `xUnit` | Test framework | ⚠️ MAYBE | NUnit/MSTest |
| `Moq` | Mocking | ⚠️ MAYBE | NSubstitute |
| `FluentAssertions` | Assertions | ✅ YES | Write our own |
| `Microsoft.AspNetCore.Mvc.Testing` | Integration tests | ❌ NO | Need for E2E |
| `Microsoft.Playwright` | E2E tests | ⚠️ MAYBE | Selenium |
| `Bogus` | Test data generation | ✅ YES | Write our own |

**Recommendations**:
- ⚠️ **Keep xUnit** - Industry standard, well-supported
- ⚠️ **Keep Moq** - Industry standard
- ✅ **Remove FluentAssertions** - Nice-to-have, not essential
- ✅ **Remove Bogus** - Nice-to-have, not essential
- ⚠️ **Keep Playwright** - Best HTMX testing tool
- ❌ **Keep Mvc.Testing** - Essential for integration tests

---

#### **Identity Module Dependencies**

| Package | Purpose | Can Replace? |
|---------|---------|--------------|
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | Identity + EF Core | ❌ NO |
| `Microsoft.Extensions.Identity.*` | Identity abstractions | ❌ NO |

**Decision**: Identity is **Microsoft's system**. We'd have to rebuild authentication/authorization from scratch. **KEEP**.

---

### **Database Providers** (User Choice)

| Package | Purpose | Keep? |
|---------|---------|-------|
| `Npgsql.EntityFrameworkCore.PostgreSQL` | PostgreSQL provider | ✅ YES - Template default |
| `Microsoft.EntityFrameworkCore.SqlServer` | SQL Server | ⏸️ User adds if needed |
| `Microsoft.EntityFrameworkCore.Sqlite` | SQLite | ✅ YES - Testing only |
| `Microsoft.EntityFrameworkCore.InMemory` | In-memory DB | ✅ YES - Testing only |

---

## 🎯 **Actionable Recommendations**

### **Phase 1: Remove Unnecessary** (30 min)

1. ✅ **Remove LibGit2Sharp from CLI**
   - Not used yet
   - Can use `dotnet` CLI commands instead

2. ✅ **Remove FluentAssertions**
   - Nice-to-have, not essential
   - Use xUnit assertions: `Assert.Equal()`, `Assert.True()`

3. ✅ **Remove Bogus**
   - Only in NetMX.Testing
   - Write simple test data builders instead

**Impact**: Reduce dependency count from ~15 to ~12

---

### **Phase 2: Validate Essential Dependencies** (15 min)

**Keep These (Justified)**:

1. ✅ **Spectre.Console** - Amazing DX for CLI
   - Progress bars, spinners, colors
   - Makes NetMX CLI feel professional
   - **Keep!**

2. ✅ **System.CommandLine** - Argument parsing
   - Handles complex CLI arguments
   - In beta but stable enough
   - Alternative = 500+ lines of manual parsing
   - **Keep!**

3. ✅ **Microsoft.CodeAnalysis.CSharp** - Code generation
   - Roslyn API for code modification
   - Essential for DbSet injection, using statements
   - No alternative
   - **Keep!**

4. ✅ **Microsoft.Playwright** - HTMX E2E testing
   - Best tool for browser automation
   - Perfect for testing HTMX interactions
   - **Keep!**

5. ✅ **xUnit + Moq** - Testing
   - Industry standard
   - Well-supported, stable
   - **Keep!**

---

## 🔗 **NuGet Package Transitive Dependencies**

### **Question**: Do our modules reference each other correctly?

**YES! ✅** Let me show you:

#### **Authorization Module** → References Framework
```xml
<!-- Authorization.Core.csproj -->
<PackageReference Include="NetMX.Ddd.Domain" Version="0.1.0-*" />

<!-- Authorization.Application.csproj -->
<PackageReference Include="NetMX.Ddd.Application" Version="0.1.0-*" />
<PackageReference Include="NetMX.EntityFrameworkCore" Version="0.1.0-*" />

<!-- Authorization.Web.csproj -->
<PackageReference Include="NetMX.AspNetCore.Mvc" Version="0.1.0-*" />
<PackageReference Include="NetMX.Events" Version="0.1.0-*" />
```

**When a user installs Authorization**:
```bash
dotnet add package Authorization.Web
# NuGet automatically installs:
# - Authorization.Web
# - Authorization.Application (dependency)
# - Authorization.Core (dependency)
# - NetMX.AspNetCore.Mvc (dependency)
# - NetMX.Events (dependency)
# - NetMX.Ddd.Application (transitive)
# - NetMX.Ddd.Domain (transitive)
# - NetMX.EntityFrameworkCore (transitive)
# - NetMX.Core (transitive)
```

**All transitive dependencies install automatically! ✅**

---

#### **Audit Module** → References Framework
```xml
<!-- Audit.Core.csproj -->
<PackageReference Include="NetMX.Ddd.Domain" Version="0.1.0-*" />

<!-- Audit.Application.csproj -->
<PackageReference Include="NetMX.Ddd.Application" Version="0.1.0-*" />
<PackageReference Include="NetMX.EntityFrameworkCore" Version="0.1.0-*" />

<!-- Audit.Web.csproj -->
<PackageReference Include="NetMX.AspNetCore.Mvc" Version="0.1.0-*" />
<PackageReference Include="NetMX.Events" Version="0.1.0-*" />
```

**Same pattern - all dependencies auto-install! ✅**

---

#### **Identity Module** → Uses Microsoft Identity + Framework
```xml
<!-- NetMX.Identity.Core.csproj -->
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.Identity.Stores" Version="9.0.0" />

<!-- NetMX.Identity.Application.csproj -->
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.Identity.Core" Version="9.0.0" />
```

**⚠️ ISSUE**: Identity module doesn't reference NetMX framework packages!

**Should be**:
```xml
<!-- NetMX.Identity.Core.csproj -->
<PackageReference Include="NetMX.Ddd.Domain" Version="0.1.0-*" />
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="9.0.0" />

<!-- NetMX.Identity.Application.csproj -->
<PackageReference Include="NetMX.Ddd.Application" Version="0.1.0-*" />
<PackageReference Include="NetMX.EntityFrameworkCore" Version="0.1.0-*" />
```

---

## 🚨 **Issues Found**

### **1. Identity Module Doesn't Use NetMX Framework**

**Current**: Identity uses raw Microsoft.EntityFrameworkCore  
**Should**: Identity uses NetMX.EntityFrameworkCore (our wrapper)

**Impact**: 
- Identity doesn't get NetMX features (soft delete, multi-tenancy, etc.)
- Inconsistent with other modules
- Can't share DbContext

**Fix**: Refactor Identity to use NetMX framework packages

---

### **2. Module Naming Inconsistency**

**Authorization Module**: `Authorization.Core`, `Authorization.Web`  
**Audit Module**: `Audit.Core`, `Audit.Web`  
**Identity Module**: `NetMX.Identity.Core`, `NetMX.Identity.Web` ❌

**Should all be**: `ModuleName.Core`, `ModuleName.Web`

**Fix**: Rename Identity module projects

---

### **3. Modules Not Published to NuGet**

**Current**: Modules only exist as source code  
**Should**: Modules published as NuGet packages

**Impact**: Users can't install via `dotnet add package`

**Fix**: 
1. Pack modules: `dotnet pack`
2. Publish to NuGet or local feed
3. For now: Use local NuGet source

---

## 📋 **Action Plan**

### **Immediate (This Session)**

#### **1. Clean Up Dependencies** (15 min)
- [ ] Remove LibGit2Sharp from CLI
- [ ] Remove FluentAssertions from tests
- [ ] Remove Bogus from NetMX.Testing
- [ ] Update all test projects to use xUnit assertions

#### **2. Fix Identity Module** (30 min)
- [ ] Add NetMX framework package references
- [ ] Rename projects (remove NetMX. prefix)
- [ ] Ensure consistency with other modules

#### **3. Document Essential Dependencies** (10 min)
- [ ] List "why we keep X" for each dependency
- [ ] Add to copilot-instructions.md

---

### **Next Session**

#### **4. Package Modules as NuGet** (1 hour)
- [ ] Configure NuGet metadata in .csproj
- [ ] Create local NuGet feed
- [ ] Pack all modules
- [ ] Test installation

#### **5. Implement `netmx add module`** (2 hours)
- [ ] Auto-add package reference
- [ ] Auto-wire in Program.cs
- [ ] Auto-apply migrations
- [ ] Test end-to-end

---

## 💭 **My Thoughts**

### **Dependencies Philosophy**

**Your instinct is RIGHT**: Fewer dependencies = better

**But consider**:
1. **Microsoft packages** - Core .NET, unavoidable
2. **Roslyn (CodeAnalysis)** - Essential for code generation
3. **Spectre.Console** - Makes CLI feel premium (worth it!)
4. **System.CommandLine** - Saves 500+ lines (worth it!)
5. **xUnit/Moq** - Industry standard (worth it!)

**My recommendation**:
- ✅ Remove: LibGit2Sharp, FluentAssertions, Bogus
- ✅ Keep: Everything else (justified value)

---

### **NuGet Transitive Dependencies**

**YES! ✅** It works exactly like you described:

```bash
dotnet add package Authorization.Web
# Auto-installs:
# - Authorization.Web
# - Authorization.Application
# - Authorization.Core
# - NetMX.AspNetCore.Mvc
# - NetMX.Events
# - NetMX.Ddd.Application
# - NetMX.Ddd.Domain
# - NetMX.EntityFrameworkCore
# - NetMX.Core
# - Microsoft.EntityFrameworkCore (transitive)
# - etc.
```

**One command, entire module + dependencies installed! ✅**

---

## 🎯 **What Should We Do Next?**

**Option A: Clean Dependencies First** (30 min)
- Remove LibGit2Sharp, FluentAssertions, Bogus
- Validate builds still work
- Document "why we keep X"

**Option B: Fix Identity Module** (30 min)
- Add NetMX framework references
- Rename projects for consistency
- Test it works

**Option C: Package Modules** (1 hour)
- Set up local NuGet feed
- Pack all modules
- Test installation

**Option D: Implement `netmx add module`** (2 hours)
- Build the command
- Auto-wire everything
- Test end-to-end

---

**My recommendation**: **Option A + B** (1 hour total)

Clean dependencies + fix Identity, then we have:
- ✅ Minimal external dependencies
- ✅ Consistent module structure
- ✅ Ready to package and test

**Your call!** 🎯

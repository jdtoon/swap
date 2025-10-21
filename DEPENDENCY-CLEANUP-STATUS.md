# Dependency Cleanup - In Progress

**Date**: October 21, 2025  
**Status**: ⚠️ PARTIALLY COMPLETE (compilation errors remain)  
**Goal**: Minimize external dependencies, standardize Microsoft package versions

---

## ✅ Completed

### 1. Removed Unnecessary Packages

| Package | Removed From | Reason |
|---------|-------------|--------|
| **LibGit2Sharp** | NetMX.CLI | Not needed yet (can use dotnet CLI commands) |
| **FluentAssertions** | 3 test projects | Nice-to-have, use xUnit assertions instead |
| **Bogus** | NetMX.Testing | Nice-to-have, write simple test data builders |

### 2. Fixed Identity Module

**Problem**: Identity module used ProjectReferences to framework packages  
**Solution**: Changed to NuGet PackageReferences (consistent with other modules)

**Files Updated**:
- `NetMX.Identity.Core.csproj`
- `NetMX.Identity.Application.csproj`
- `NetMX.Identity.Web.csproj`

**Changes**:
```xml
<!-- Before -->
<ProjectReference Include="..\..\..\framework\NetMX.Ddd.Domain\NetMX.Ddd.Domain.csproj" />

<!-- After -->
<PackageReference Include="NetMX.Ddd.Domain" Version="0.1.0-*" />
```

### 3. Standardized Microsoft Package Versions

**Target**: 9.0.10 (latest stable)

**Updated**:
- Microsoft.EntityFrameworkCore: 9.0.0 → 9.0.10
- Microsoft.Extensions.*: 9.0.0 → 9.0.10
- Microsoft.AspNetCore.*: 9.0.0 → 9.0.10
- System.Text.Json: 9.0.0 → 9.0.10

### 4. Documented Dependencies in CLI

Added comments explaining why we keep each dependency:

```xml
<!-- CLI Framework - handles argument parsing, help generation -->
<PackageReference Include="System.CommandLine" Version="2.0.0-rc.2.25502.107" />

<!-- Rich Terminal UI - progress bars, spinners, colors (KEEP: Amazing DX) -->
<PackageReference Include="Spectre.Console" Version="0.49.1" />

<!-- Code Generation - Roslyn API for C# code modification (KEEP: Essential) -->
<PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.14.0" />
```

---

## ❌ Remaining Issues

### Compilation Errors

**Test files still reference removed packages**:

1. **FluentAssertions** (6 files):
   - `NetMX.Core.Tests/Events/EventBusTests.cs`
   - `NetMX.Core.Tests/Events/EventContextTests.cs`
   - `NetMX.AspNetCore.Core.Tests/Events/EventBusHttpContextExtensionsTests.cs`
   - `NetMX.AspNetCore.Core.Tests/Events/EventBusMiddlewareTests.cs`
   - Plus CLI test files

2. **Bogus** (1 file):
   - `NetMX.Testing/TestDataBuilder.cs`

**Fix Required**: Replace with xUnit assertions and simple test builders

---

## 📋 Next Steps

### Immediate (30 min)

1. **Fix FluentAssertions** in test files:
   ```csharp
   // Before
   result.Should().BeTrue();
   result.Should().NotBeNull();
   list.Should().HaveCount(5);
   
   // After (xUnit)
   Assert.True(result);
   Assert.NotNull(result);
   Assert.Equal(5, list.Count);
   ```

2. **Fix Bogus** in TestDataBuilder:
   ```csharp
   // Before
   private static readonly Randomizer _random = new();
   
   // After
   private static readonly Random _random = new();
   
   // Replace Faker<T> with simple builders
   public static string GenerateName() => $"Test_{_random.Next(1000, 9999)}";
   ```

3. **Build and verify**: `dotnet build NetMX.sln`

4. **Commit changes**: Document dependency cleanup

---

## 📊 Dependency Summary

### Kept (Justified)

| Package | Reason | Alternative Cost |
|---------|--------|------------------|
| **Microsoft.***| Core .NET infrastructure | Rebuild .NET ❌ |
| **Microsoft.CodeAnalysis**| Code generation (Roslyn) | 5000+ lines ❌ |
| **Spectre.Console**| Amazing CLI DX | Basic console output 😞 |
| **System.CommandLine**| Argument parsing | 500+ lines ❌ |
| **xUnit + Moq**| Industry standard testing | Learning curve 😞 |
| **Playwright**| Best HTMX E2E testing | Selenium (worse) 😞 |

### Removed (Unnecessary)

| Package | Why Removed | Impact |
|---------|-------------|--------|
| **LibGit2Sharp**| Not used yet | None (can add later if needed) |
| **FluentAssertions**| Nice-to-have, not essential | Slightly less readable tests |
| **Bogus**| Nice-to-have, not essential | Write our own test data |

**Total Dependencies**: ~12 (from ~15)  
**External (non-Microsoft)**: 4 (Spectre.Console, System.CommandLine, Moq, Playwright)  
**All Justified**: YES ✅

---

## 💡 Key Decisions

### 1. Keep Spectre.Console

**Why**: Makes NetMX CLI feel premium  
**Value**: Progress bars, colors, tables, spinners  
**Cost**: 1 small package dependency  
**Decision**: **KEEP** - Worth it for DX

### 2. Keep System.CommandLine

**Why**: Handles complex CLI arguments  
**Alternative**: 500+ lines of manual parsing  
**Status**: Beta but stable (Microsoft package)  
**Decision**: **KEEP** - Saves significant time

### 3. Keep Playwright

**Why**: Best tool for testing HTMX interactions  
**Alternative**: Selenium (worse DX, slower)  
**Use Case**: E2E testing (essential for HTMX framework)  
**Decision**: **KEEP** - Essential for our use case

### 4. Remove FluentAssertions

**Why**: Nice-to-have, not essential  
**Alternative**: xUnit assertions (built-in)  
**Impact**: Slightly less readable tests  
**Decision**: **REMOVE** - Not worth dependency

### 5. Remove Bogus

**Why**: Nice-to-have, not essential  
**Alternative**: Simple test data builders (20-30 lines)  
**Impact**: Write our own random data generation  
**Decision**: **REMOVE** - Easy to replace

---

## 🎯 Philosophy

**Our Approach**:
1. Keep Microsoft packages (core infrastructure)
2. Keep essential tools (Roslyn, Playwright)
3. Keep DX enhancements that are worth it (Spectre.Console)
4. Remove nice-to-haves we can easily replace

**Result**: ~12 justified dependencies (down from ~15)

---

**Status**: Ready to fix test files and complete cleanup  
**Next**: Fix FluentAssertions/Bogus usage, build, commit

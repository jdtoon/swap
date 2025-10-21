# Roslyn Auto-Migration Implementation Plan

**Date**: October 21, 2025  
**Goal**: Auto-add DbSet to DbContext and auto-apply migrations  
**Estimated Time**: 8-10 hours  
**Impact**: 99.9% time reduction (74 min → 5 sec)

---

## 🎯 Objective

Make `netmx generate feature Product --migrate` fully automated:
1. Generate feature files (DONE ✅)
2. **Auto-add DbSet to DbContext** (NEW)
3. **Auto-create migration** (NEW)
4. **Auto-apply migration** (NEW)

---

## 📦 Implementation Steps

### Step 1: Add Roslyn Packages
```bash
dotnet add tools/NetMX.CLI/NetMX.CLI.csproj package Microsoft.CodeAnalysis.CSharp
dotnet add tools/NetMX.CLI/NetMX.CLI.csproj package Microsoft.CodeAnalysis.CSharp.Workspaces
```

### Step 2: Create CodeModificationHelper
**Location**: `tools/NetMX.CLI/Services/CodeModificationHelper.cs`

**Responsibilities**:
- Parse C# files with Roslyn
- Find DbContext class
- Add DbSet property
- Format and save changes

**Key Methods**:
```csharp
public async Task<bool> AddDbSetToContextAsync(string dbContextPath, string entityName)
public async Task<string?> FindDbContextAsync(string projectPath)
```

### Step 3: Update GenerateFeatureCommand
**Location**: `tools/NetMX.CLI/Commands/GenerateFeatureCommand.cs`

**Changes**:
1. After generating entity → Find DbContext
2. Add DbSet property
3. If --migrate flag → Create migration
4. If --migrate flag → Apply migration

### Step 4: Testing
- Unit tests for CodeModificationHelper
- Integration tests for full workflow
- Manual testing with demo project

---

## 🔧 Technical Details

### Finding DbContext

**Strategy**: Search for class inheriting from DbContext

```csharp
var tree = CSharpSyntaxTree.ParseText(code);
var root = await tree.GetRootAsync();
var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

foreach (var cls in classes)
{
    if (cls.BaseList?.Types.Any(t => t.ToString().Contains("DbContext")) == true)
    {
        // Found it!
    }
}
```

### Adding DbSet Property

**Strategy**: Insert property after last DbSet

```csharp
// Find last DbSet property
var lastDbSet = properties
    .Where(p => p.Type.ToString().StartsWith("DbSet<"))
    .LastOrDefault();

// Create new property
var newProperty = SyntaxFactory.PropertyDeclaration(
    SyntaxFactory.GenericName("DbSet")
        .WithTypeArgumentList(
            SyntaxFactory.TypeArgumentList(
                SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                    SyntaxFactory.IdentifierName(entityName)))),
    entityName + "s")
    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
    .AddAccessorListAccessors(
        SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
        SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));

// Insert after last DbSet
var newRoot = root.InsertNodesAfter(lastDbSet, new[] { newProperty });
```

### Running Migrations

**Strategy**: Use existing MigrationRunner

```csharp
await _migrationRunner.CreateMigrationAsync($"Add{entityName}", projectPath);
await _migrationRunner.UpdateDatabaseAsync(projectPath);
```

---

## 📋 Implementation Checklist

### Phase 1: Setup (30 min)
- [ ] Add Roslyn NuGet packages
- [ ] Create CodeModificationHelper.cs skeleton
- [ ] Add unit test project (if not exists)

### Phase 2: CodeModificationHelper (3-4 hours)
- [ ] Implement FindDbContextAsync
- [ ] Implement AddDbSetToContextAsync
- [ ] Handle edge cases (no DbContext, multiple DbContexts)
- [ ] Add XML documentation
- [ ] Write unit tests (10+ tests)

### Phase 3: Integration (2-3 hours)
- [ ] Update GenerateFeatureCommand
- [ ] Add --migrate flag handling
- [ ] Call CodeModificationHelper
- [ ] Call MigrationRunner
- [ ] Add progress indicators
- [ ] Handle errors gracefully

### Phase 4: Testing (1-2 hours)
- [ ] Unit tests (CodeModificationHelper)
- [ ] Integration tests (full workflow)
- [ ] Manual testing with demo project
- [ ] Test edge cases

### Phase 5: Documentation (1 hour)
- [ ] Update CLI-USAGE-GUIDE.md
- [ ] Add examples
- [ ] Update QUICK-START.md
- [ ] Create troubleshooting guide

---

## 🧪 Test Cases

### CodeModificationHelper Tests
1. ✅ FindDbContextAsync - Finds DbContext in project
2. ✅ FindDbContextAsync - Returns null if no DbContext
3. ✅ FindDbContextAsync - Handles multiple DbContexts
4. ✅ AddDbSetToContextAsync - Adds DbSet property
5. ✅ AddDbSetToContextAsync - Adds after last DbSet
6. ✅ AddDbSetToContextAsync - Handles no existing DbSets
7. ✅ AddDbSetToContextAsync - Handles duplicate entity name
8. ✅ AddDbSetToContextAsync - Preserves formatting
9. ✅ AddDbSetToContextAsync - Handles using statements
10. ✅ AddDbSetToContextAsync - Handles namespaces

### Integration Tests
1. ✅ Generate feature with --migrate flag
2. ✅ DbSet added to DbContext
3. ✅ Migration created
4. ✅ Migration applied
5. ✅ Database table exists

---

## 🎯 Success Criteria

1. ✅ `netmx generate feature Product --migrate` completes successfully
2. ✅ DbSet added to DbContext automatically
3. ✅ Migration created with correct name
4. ✅ Migration applied to database
5. ✅ Table exists in database
6. ✅ All tests passing
7. ✅ Documentation complete

---

## 🚀 Expected Outcome

**Before** (5-10 minutes manual work):
```bash
netmx generate feature Product
# ... manually add DbSet to DbContext
# ... manually run: dotnet ef migrations add AddProduct
# ... manually run: dotnet ef database update
```

**After** (5 seconds):
```bash
netmx generate feature Product --migrate
✨ Generated Product feature
✅ Added DbSet<Product> to AppDbContext
✅ Created migration: AddProduct
✅ Applied migration to database
🎉 Done in 5 seconds!
```

---

## ⚠️ Edge Cases to Handle

1. **No DbContext found** → Warn user, skip migration
2. **Multiple DbContexts** → Ask user which one to use
3. **Entity already exists** → Warn user, skip addition
4. **Migration already exists** → Skip creation, apply existing
5. **Database connection fails** → Fail gracefully with helpful message
6. **Roslyn parsing fails** → Fall back to manual instructions

---

## 📝 Notes

- Use Roslyn for code manipulation (not regex!)
- Preserve formatting and comments
- Handle both tabs and spaces
- Support both `DbSet<T>` and `DbSet<T> => Set<T>()`
- Support C# 8-12 syntax features
- Add comprehensive logging
- Add progress indicators (Spectre.Console)

---

**Ready to start implementation!** 🚀

# Domain Events Architecture - Refactoring Plan

**Date**: October 22, 2025  
**Issue**: CS0433 - Type ambiguity with partial class `DomainEvents`  
**Status**: Design Phase - Ready for Implementation

---

## 🔍 Problem Analysis

### Current Architecture

**NetMX.Events Package** (`framework/NetMX.Events/DomainEvents.cs`):
```csharp
namespace NetMX.Events;

public static partial class DomainEvents
{
    public static class User { ... }
    public static class Session { ... }
    // Core events
}
```

**Module Extensions** (e.g., `Authorization.Web/Events/DomainEvents.Authorization.cs`):
```csharp
namespace NetMX.Events;

public static partial class DomainEvents  // ← Extends base class
{
    public static class Permission { ... }
    public static class Role { ... }
}
```

### The Problem

When a test project references `Authorization.Web`:
1. Gets `Authorization.Web.dll` (contains compiled `DomainEvents` with Permission/Role)
2. Gets `NetMX.Events.dll` (contains compiled `DomainEvents` with User/Session)
3. **Both assemblies export** `NetMX.Events.DomainEvents` type
4. Compiler error: CS0433 - Type exists in both assemblies

### Why It Happens

Partial classes are **compile-time only**. At runtime:
- Each assembly contains **its own compiled version** of the partial class
- `NetMX.Events.dll` has: `DomainEvents { User, Session, ... }`
- `Authorization.Web.dll` has: `DomainEvents { User, Session, Permission, Role, ... }`
- Both assemblies export the **same type name**: `NetMX.Events.DomainEvents`
- CLR sees two different types with the same fully-qualified name → Ambiguity

### Current Workarounds

**In Production Code**: ✅ Works fine
- Modules only reference `NetMX.Events` package
- Use their own partial extension
- Get merged view at compile time
- No ambiguity (one assembly output)

**In Test Code**: ❌ Fails
- Reference module assembly (Authorization.Web.dll)
- Module assembly includes merged DomainEvents
- Also references NetMX.Events.dll (base)
- Two assemblies, same type name → CS0433

---

## 💡 Solution Options

### Option 1: Module-Specific Event Classes (RECOMMENDED)

**Move away from partial classes** to module-specific event classes.

#### Implementation

**Before** (Current - Partial Class):
```csharp
// NetMX.Events/DomainEvents.cs
namespace NetMX.Events;
public static partial class DomainEvents
{
    public static class User { ... }
}

// Authorization.Web/Events/DomainEvents.Authorization.cs
namespace NetMX.Events;
public static partial class DomainEvents
{
    public static class Permission { ... }
}
```

**After** (Module-Specific Classes):
```csharp
// NetMX.Events/DomainEvents.cs (UNCHANGED - Core events)
namespace NetMX.Events;
public static class DomainEvents
{
    public static class User { ... }
    public static class Session { ... }
}

// Authorization.Web/Events/AuthorizationEvents.cs (NEW)
namespace NetMX.Authorization.Events;  // Module namespace!
public static class AuthorizationEvents
{
    public static class Permission
    {
        [EventDirection(EventDirection.Upstream)]
        public const string Created = "permission.created";
        // ...
    }
    
    public static class Role
    {
        [EventDirection(EventDirection.Upstream)]
        public const string Created = "role.created";
        // ...
    }
}
```

#### Usage Changes

**In Controllers**:
```csharp
// Before
this.HxTrigger(DomainEvents.Permission.Created, new { id });

// After
this.HxTrigger(AuthorizationEvents.Permission.Created, new { id });
```

**In Views**:
```cshtml
@* Before *@
@using NetMX.Events
<div hx-trigger="@DomainEvents.Permission.Created from:body">

@* After *@
@using NetMX.Authorization.Events
<div hx-trigger="@AuthorizationEvents.Permission.Created from:body">
```

#### Pros & Cons

**Pros**:
- ✅ **No ambiguity** - Each module has unique class name
- ✅ **Better discoverability** - `AuthorizationEvents` clearly module-specific
- ✅ **Easier testing** - No compiler issues
- ✅ **Module isolation** - Each module owns its events
- ✅ **Namespace clarity** - `NetMX.Authorization.Events` vs `NetMX.Events`

**Cons**:
- ⚠️ **Breaking change** - All existing code needs updates
- ⚠️ **More verbose** - `AuthorizationEvents.Permission.Created` vs `DomainEvents.Permission.Created`
- ⚠️ **Different pattern** - Core events vs module events use different classes

#### Impact Analysis

**Files to Change**:
1. **Authorization Module** (6 files):
   - `Authorization.Web/Events/DomainEvents.Authorization.cs` → `AuthorizationEvents.cs`
   - `Authorization.Web/Controllers/PermissionsController.cs`
   - `Authorization.Web/Controllers/RolesController.cs`
   - `Authorization.Web/Views/Permissions/*.cshtml` (3 files)
   - `Authorization.Web/Views/Roles/*.cshtml` (3 files)

2. **Identity Module** (17 files):
   - `Identity.Web/Events/DomainEvents.Identity.cs` → `IdentityEvents.cs`
   - Controllers: Login, Registration, Profile, Account, Session, UserRole
   - Views: Multiple CSHTML files

3. **Audit Module** (15 files):
   - `Audit.Web/Events/DomainEvents.Audit.cs` → `AuditEvents.cs`
   - Controllers: AuditLog, AuditEntry, EntityChange, Compliance
   - Views: Multiple CSHTML files

4. **CLI Generator**:
   - `tools/NetMX.CLI/Commands/GenerateFeatureCommand.cs`
   - Update template to generate `{ModuleName}Events` instead of `DomainEvents` partial

**Estimated Effort**: 2-3 hours

---

### Option 2: Keep Partial, Use Type Forwarding

**Use `TypeForwardedTo` attribute** to avoid duplication.

#### Implementation

```csharp
// NetMX.Events/DomainEvents.cs
[assembly: TypeForwardedTo(typeof(NetMX.Events.DomainEvents))]

namespace NetMX.Events;
public static partial class DomainEvents { ... }
```

**Problem**: Type forwarding doesn't work for partial classes (compile-time construct).

**Verdict**: ❌ **Not viable** - Type forwarding is for moving types between assemblies, not for partial classes.

---

### Option 3: Extern Alias (Test-Only Workaround)

**Use extern alias in test projects** to disambiguate.

#### Implementation

**Test Project (.csproj)**:
```xml
<ItemGroup>
  <ProjectReference Include="..\Authorization.Web\Authorization.Web.csproj">
    <Aliases>AuthModule</Aliases>
  </ProjectReference>
</ItemGroup>
```

**Test Code**:
```csharp
extern alias AuthModule;

using NetMX.Events;  // Base DomainEvents from NetMX.Events package
using ModuleEvents = AuthModule::NetMX.Events.DomainEvents;  // Module's version

[Fact]
public void Test()
{
    // Use base events
    var userEvent = DomainEvents.User.Created;
    
    // Use module events
    var permEvent = ModuleEvents.Permission.Created;
}
```

#### Pros & Cons

**Pros**:
- ✅ **No production code changes** - Only test projects affected
- ✅ **Quick fix** - Can implement in minutes
- ✅ **Tests work** - Resolves ambiguity

**Cons**:
- ⚠️ **Test-only solution** - Doesn't fix underlying architecture
- ⚠️ **Confusing syntax** - `extern alias` is advanced C# feature
- ⚠️ **Future issues** - Ambiguity could surface elsewhere (analyzers, tools, etc.)
- ⚠️ **Maintenance burden** - Every test project needs this config

**Verdict**: ⚠️ **Temporary workaround only** - Not a long-term solution

---

### Option 4: Hybrid Approach (Best of Both Worlds)

**Combine Options 1 and 3** for incremental migration.

#### Phase 1: Fix Tests (Immediate)
Use extern alias to unblock testing:
```xml
<ProjectReference Include="..\Authorization.Web\Authorization.Web.csproj">
  <Aliases>AuthModule</Aliases>
</ProjectReference>
```

#### Phase 2: Refactor (Next Sprint)
Migrate to module-specific event classes:
- Authorization → `AuthorizationEvents`
- Identity → `IdentityEvents`
- Audit → `AuditEvents`

#### Phase 3: Update Core (Future)
Keep `DomainEvents` for core events only:
- User, Session, System events remain in `DomainEvents`
- Module events use `{Module}Events` classes

#### Pros & Cons

**Pros**:
- ✅ **Unblocks testing now** - Extern alias fixes immediate issue
- ✅ **Better long-term architecture** - Module-specific classes
- ✅ **Incremental migration** - No big bang refactor
- ✅ **Backward compatible** - Can support both patterns temporarily

**Cons**:
- ⚠️ **Two phases of work** - More total effort
- ⚠️ **Temporary complexity** - Both patterns exist during migration

---

## 🎯 Recommended Solution

### **Option 1: Module-Specific Event Classes**

**Why**:
1. **Cleaner architecture** - No partial class complexity
2. **Better namespacing** - `NetMX.Authorization.Events.AuthorizationEvents`
3. **Easier to understand** - Each module owns its events
4. **No ambiguity** - Ever (tests, analyzers, tools)
5. **Aligns with DDD** - Module boundaries are explicit

**Migration Plan**:
1. ✅ **Phase 1**: Refactor Authorization module (1 hour)
2. ✅ **Phase 2**: Refactor Identity module (1 hour)
3. ✅ **Phase 3**: Refactor Audit module (30 min)
4. ✅ **Phase 4**: Update CLI generator (30 min)
5. ✅ **Phase 5**: Update documentation (30 min)
6. ✅ **Phase 6**: Create domain events tests (30 min)

**Total Effort**: 4 hours

---

## 📋 Implementation Checklist

### Phase 1: Authorization Module (1 hour)

- [ ] Create `Authorization.Web/Events/AuthorizationEvents.cs`:
  ```csharp
  namespace NetMX.Authorization.Events;
  
  public static class AuthorizationEvents
  {
      public static class Permission { ... }
      public static class Role { ... }
  }
  ```

- [ ] Update `PermissionsController.cs`:
  - Replace `DomainEvents.Permission.*` with `AuthorizationEvents.Permission.*`
  - Add `using NetMX.Authorization.Events;`

- [ ] Update `RolesController.cs`:
  - Replace `DomainEvents.Role.*` with `AuthorizationEvents.Role.*`
  - Add `using NetMX.Authorization.Events;`

- [ ] Update views (if events used):
  - `Views/Permissions/*.cshtml`
  - `Views/Roles/*.cshtml`

- [ ] Delete old file:
  - `Authorization.Web/Events/DomainEvents.Authorization.cs`

- [ ] Build and test:
  ```bash
  dotnet build modules/Authorization/Authorization.sln
  dotnet test modules/Authorization/Authorization.Web.Tests
  ```

### Phase 2: Identity Module (1 hour)

- [ ] Create `Identity.Web/Events/IdentityEvents.cs`:
  ```csharp
  namespace NetMX.Identity.Events;
  
  public static class IdentityEvents
  {
      public static class Login { ... }
      public static class Registration { ... }
      public static class Profile { ... }
      public static class Account { ... }
      public static class Session { ... }
      public static class UserRole { ... }
  }
  ```

- [ ] Update controllers (6 files):
  - `LoginController.cs`
  - `RegistrationController.cs`
  - `ProfileController.cs`
  - `AccountController.cs`
  - `SessionController.cs`
  - `UserRolesController.cs`

- [ ] Update views (if events used)

- [ ] Delete old file:
  - `Identity.Web/Events/DomainEvents.Identity.cs`

- [ ] Build and test

### Phase 3: Audit Module (30 min)

- [ ] Create `Audit.Web/Events/AuditEvents.cs`
- [ ] Update controllers (4 files)
- [ ] Delete old file: `Audit.Web/Events/DomainEvents.Audit.cs`
- [ ] Build and test

### Phase 4: CLI Generator (30 min)

- [ ] Update `GenerateFeatureCommand.cs`:
  - Generate `{ModuleName}Events` instead of partial `DomainEvents`
  - Use module namespace: `NetMX.{ModuleName}.Events`
  - Update controller template to use new event class

- [ ] Test CLI generation:
  ```bash
  netmx generate feature TestEntity -m Authorization
  # Should generate AuthorizationEvents.TestEntity class
  ```

### Phase 5: Documentation (30 min)

- [ ] Update `docs/DOMAIN-EVENTS-ARCHITECTURE.md`
- [ ] Update `framework/NetMX.Events/README.md`
- [ ] Update `docs/QUICK-START.md`
- [ ] Update `.github/copilot-instructions.md`

### Phase 6: Create Domain Events Tests (30 min)

- [ ] Create `Authorization.Web.Tests/Events/AuthorizationEventsTests.cs`:
  ```csharp
  [Fact]
  public void AllPermissionEvents_HaveCorrectNames()
  {
      Assert.Equal("permission.created", AuthorizationEvents.Permission.Created);
      Assert.Equal("permission.updated", AuthorizationEvents.Permission.Updated);
      Assert.Equal("permission.deleted", AuthorizationEvents.Permission.Deleted);
  }
  
  [Fact]
  public void AllEvents_HaveEventDirectionAttribute()
  {
      var fields = typeof(AuthorizationEvents.Permission).GetFields();
      foreach (var field in fields)
      {
          var attr = field.GetCustomAttribute<EventDirectionAttribute>();
          Assert.NotNull(attr);
      }
  }
  ```

- [ ] Create similar tests for Identity and Audit
- [ ] Run all tests:
  ```bash
  dotnet test
  ```

---

## 🔄 Backward Compatibility

### For Core Events

`DomainEvents` class remains in `NetMX.Events` for core events:
```csharp
// Still works!
DomainEvents.User.Created
DomainEvents.Session.Expired
```

### For Module Events

Use new module-specific classes:
```csharp
// New pattern
AuthorizationEvents.Permission.Created
IdentityEvents.Login.Success
AuditEvents.AuditLog.Created
```

### Migration Guide for Users

If users extended `DomainEvents` in their apps:

**Before**:
```csharp
namespace NetMX.Events;
public static partial class DomainEvents
{
    public static class Product
    {
        public const string Created = "product.created";
    }
}
```

**After** (Recommended):
```csharp
namespace MyApp.Events;
public static class MyAppEvents
{
    public static class Product
    {
        public const string Created = "product.created";
    }
}
```

---

## 📊 Risk Assessment

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| Breaking changes | High | 100% | Clear migration guide, version bump |
| Missed event references | Medium | Low | Compiler errors catch all issues |
| Test failures | Low | Low | Comprehensive testing before commit |
| User confusion | Medium | Medium | Update docs, provide examples |

---

## 🚀 Rollout Plan

### Week 1 (This Week)
- **Day 1**: Implement Authorization module refactor
- **Day 2**: Implement Identity module refactor
- **Day 3**: Implement Audit module refactor
- **Day 4**: Update CLI generator
- **Day 5**: Update documentation, create tests

### Success Criteria
- ✅ All modules build without warnings
- ✅ All tests pass (EventBus + domain events)
- ✅ No CS0433 errors
- ✅ CLI generates new pattern correctly

---

## � Cross-Module Event Access

### The Question
**"How does Authorization module access Identity events, or vice versa?"**

### The Answer: Project References

**Key Insight**: Modules that need to **listen** to other modules' events simply add a project reference.

#### Example 1: Audit Listens to Authorization

**Scenario**: Audit module wants to capture when permissions are created/deleted.

**Solution**:
```xml
<!-- Audit.Web/Audit.Web.csproj -->
<ItemGroup>
  <ProjectReference Include="..\..\Authorization\Authorization.Web\Authorization.Web.csproj" />
</ItemGroup>
```

**Usage in View**:
```html
@using NetMX.Authorization.Events

<div id="audit-log" 
     hx-get="/AuditLog/List" 
     hx-trigger="@AuthorizationEvents.Permission.Created from:body">
    <!-- Auto-refreshes when permissions created -->
</div>
```

**Usage in Controller**:
```csharp
using NetMX.Authorization.Events;

// Listen to Authorization events
this.HxTrigger(AuthorizationEvents.Permission.Created, new { id });
```

#### Example 2: Authorization Listens to Identity

**Scenario**: Authorization module wants to refresh role list when user logs in.

**Solution**:
```xml
<!-- Authorization.Web/Authorization.Web.csproj -->
<ItemGroup>
  <ProjectReference Include="..\..\Identity\NetMX.Identity.Web\NetMX.Identity.Web.csproj" />
</ItemGroup>
```

**Usage**:
```csharp
using NetMX.Identity.Events;

// In Authorization controller/view
this.HxTrigger(IdentityEvents.Login.Success, new { userId });
```

### Architecture Pattern: Module Dependencies

```
┌──────────────────────────────────────────────────────────────┐
│                        Your Application                       │
│                                                               │
│  References ALL modules:                                      │
│  - NetMX.Identity.Web         → IdentityEvents               │
│  - Authorization.Web          → AuthorizationEvents           │
│  - Audit.Web                  → AuditEvents                   │
└──────────────────────────────────────────────────────────────┘
         │                    │                    │
         │                    │                    │
         ▼                    ▼                    ▼
┌──────────────┐    ┌──────────────┐    ┌──────────────┐
│   Identity   │    │Authorization │    │    Audit     │
│              │    │              │    │              │
│ Publishes:   │    │ Publishes:   │    │ Publishes:   │
│ - Login.*    │    │ - Permission.*│   │ - AuditLog.* │
│ - Register.* │    │ - Role.*     │    │ - Entry.*    │
└──────────────┘    └──────────────┘    └──────────────┘
                             │                    │
                             │                    │
                             │      Listens to:   │
                             │      ┌─────────────┘
                             │      │
                             ▼      ▼
                    ┌─────────────────────────┐
                    │   Audit Module          │
                    │                         │
                    │  References:            │
                    │  - Authorization.Web    │──► Access AuthorizationEvents
                    │  - Identity.Web         │──► Access IdentityEvents
                    │                         │
                    │  Views can listen to:   │
                    │  - Permission.Created   │
                    │  - Role.Updated         │
                    │  - Login.Success        │
                    └─────────────────────────┘
```

### Real-World Example: Audit Module

**Audit.Web.csproj** needs to reference other modules:
```xml
<ItemGroup>
  <!-- Audit needs to listen to Authorization events -->
  <ProjectReference Include="..\..\Authorization\Authorization.Web\Authorization.Web.csproj" />
  
  <!-- Audit needs to listen to Identity events -->
  <ProjectReference Include="..\..\Identity\NetMX.Identity.Web\NetMX.Identity.Web.csproj" />
</ItemGroup>
```

**Audit View** can now listen to events from both modules:
```html
@using NetMX.Authorization.Events
@using NetMX.Identity.Events

<div id="recent-events" 
     hx-get="/Audit/RecentEvents" 
     hx-trigger="load,
                 @AuthorizationEvents.Permission.Created from:body,
                 @AuthorizationEvents.Role.Updated from:body,
                 @IdentityEvents.Login.Success from:body,
                 @IdentityEvents.Registration.Success from:body">
    <!-- Refreshes when ANY monitored event fires -->
</div>
```

### Key Point: **Consumers** Reference **Publishers**

```
Publisher (Authorization)          Consumer (Audit)
    │                                  │
    │  Defines:                        │  References:
    │  AuthorizationEvents.cs          │  Authorization.Web project
    │                                  │
    │  Publishes:                      │  Listens:
    │  Permission.Created ────────────►│  in HTMX views
    │                                  │  in Controllers
```

**No circular dependency** because:
- Authorization **doesn't know** Audit exists
- Audit **references** Authorization to listen to its events
- One-way dependency: Audit → Authorization (safe!)

### Benefits

1. **✅ Explicit Dependencies**: Project references make dependencies clear
2. **✅ Type Safety**: IntelliSense works across modules
3. **✅ Compile-Time Checks**: Broken event references = compile errors
4. **✅ No Magic Strings**: `AuthorizationEvents.Permission.Created` not `"permission.created"`
5. **✅ Discoverability**: IDE shows available events from referenced modules

### Tradeoffs

**Pros**:
- Clear dependencies in .csproj files
- Strong typing across modules
- Better developer experience

**Cons**:
- Tighter coupling (but only for event **listeners**, not publishers)
- Need to manage project references

### Alternative: Keep Partial Classes?

If we kept partial `DomainEvents` class, cross-module access would be "automatic":

```csharp
// Any module can reference DomainEvents without knowing which module defines Permission
DomainEvents.Permission.Created  // ← Where does Permission come from? Unclear!
```

**Problem**: This "magic" discoverability comes at the cost of:
- ❌ Type ambiguity (CS0433 errors)
- ❌ Hidden dependencies (can't tell which modules you depend on)
- ❌ Testing issues (compiler confusion)

### Recommendation: Embrace Explicit Dependencies

**Module-specific event classes with project references** provides:
1. Clear dependencies (visible in .csproj)
2. Better IDE support (IntelliSense knows what you have access to)
3. No ambiguity (no CS0433 errors)
4. Testability (no compiler confusion)

**Example Dependency Graph**:
```
NetMX.Events (Core)
    ↓
Authorization.Events (Authorization-specific)
    ↓
Audit.Web (References Authorization.Web to listen to its events)
```

---

### Complete Code Example: Cross-Module Communication

#### Step 1: Authorization Publishes Events

**Authorization.Web/Events/AuthorizationEvents.cs**:
```csharp
namespace NetMX.Authorization.Events;

public static class AuthorizationEvents
{
    public static class Permission
    {
        [EventDirection(EventDirection.Upstream)]
        public const string Created = "permission.created";
        
        [EventDirection(EventDirection.Upstream)]
        public const string Updated = "permission.updated";
        
        [EventDirection(EventDirection.Upstream)]
        public const string Deleted = "permission.deleted";
    }
}
```

**Authorization.Web/Controllers/PermissionsController.cs**:
```csharp
using NetMX.Authorization.Events;

public class PermissionsController : Controller
{
    [HttpPost]
    public async Task<IActionResult> Create(CreatePermissionDto dto)
    {
        var permission = await _service.CreateAsync(dto);
        
        // Publish event
        this.HxTrigger(AuthorizationEvents.Permission.Created, new { id = permission.Id });
        
        return Ok();
    }
}
```

#### Step 2: Audit Listens to Authorization Events

**Audit.Web/Audit.Web.csproj** (Add reference):
```xml
<ItemGroup>
  <ProjectReference Include="..\..\Authorization\Authorization.Web\Authorization.Web.csproj" />
</ItemGroup>
```

**Audit.Web/Views/AuditLog/Index.cshtml**:
```html
@using NetMX.Authorization.Events  @* ← Import Authorization events *@

<div class="container">
    <h1>Audit Log</h1>
    
    <!-- This view auto-refreshes when Authorization events fire -->
    <div id="audit-entries" 
         hx-get="/AuditLog/GetEntries" 
         hx-trigger="load,
                     @AuthorizationEvents.Permission.Created from:body,
                     @AuthorizationEvents.Permission.Updated from:body,
                     @AuthorizationEvents.Permission.Deleted from:body">
        <!-- Audit entries will load here -->
    </div>
</div>
```

**Audit.Web/Controllers/AuditLogController.cs**:
```csharp
using NetMX.Authorization.Events;  // ← Import Authorization events

public class AuditLogController : Controller
{
    [HttpGet]
    public async Task<IActionResult> GetEntries()
    {
        var entries = await _service.GetRecentAsync();
        return PartialView("_Entries", entries);
    }
    
    // Can also PUBLISH Authorization events if needed
    [HttpPost]
    public async Task<IActionResult> RestorePermission(Guid id)
    {
        await _service.RestorePermissionAsync(id);
        
        // Trigger Authorization event from Audit module!
        this.HxTrigger(AuthorizationEvents.Permission.Updated, new { id });
        
        return Ok();
    }
}
```

#### Step 3: IntelliSense Shows Available Events

When you type in Audit module:
```csharp
this.HxTrigger(AuthorizationEvents.  // ← IntelliSense shows:
    ├─ Permission
    │   ├─ Created
    │   ├─ Updated
    │   └─ Deleted
    └─ Role
        ├─ Created
        ├─ Updated
        └─ Deleted
```

### Summary: How Cross-Module Events Work

1. **Publisher module** (Authorization) defines `AuthorizationEvents` class
2. **Consumer module** (Audit) adds `<ProjectReference>` to Authorization.Web
3. **Consumer module** imports `using NetMX.Authorization.Events;`
4. **Consumer module** can now:
   - Listen to Authorization events in HTMX views
   - Publish Authorization events from controllers (if needed)
   - Get IntelliSense support for all Authorization events

**Result**: Type-safe, discoverable, explicit cross-module communication! 🎉

---

## �💭 Discussion Questions

1. **Naming Convention**: 
   - `AuthorizationEvents` vs `AuthEvents` vs `AuthorizationDomainEvents`?
   - Recommendation: `AuthorizationEvents` (clear, not too verbose)

2. **Namespace Strategy**:
   - `NetMX.Authorization.Events` vs `NetMX.Events.Authorization`?
   - Recommendation: `NetMX.Authorization.Events` (groups by module)

3. **Core Events**:
   - Keep `DomainEvents` for core events or rename to `CoreEvents`?
   - Recommendation: Keep `DomainEvents` for backward compatibility

4. **CLI Template**:
   - Always generate `{Module}Events` or detect module context?
   - Recommendation: Always generate module-specific classes

5. **Cross-Module References** (NEW):
   - Is it acceptable for Audit to reference Authorization.Web to access events?
   - Recommendation: Yes - explicit dependencies are better than magic partial classes

---

## 📝 Next Steps

**Immediate**:
1. Get approval on Option 1 (Module-Specific Event Classes)
2. Start with Authorization module refactor
3. Validate approach works before proceeding to other modules

**After Approval**:
1. Implement Phase 1-6 (4 hours total)
2. Run full test suite
3. Update copilot-instructions.md
4. Commit with detailed message
5. Update CHANGELOG.md with breaking changes

---

**Decision Needed**: Approve Option 1 and begin implementation?

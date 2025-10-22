# Comprehensive Test Plan - Phase 3 Complete

**Date**: October 22, 2025  
**Status**: Ready to Execute  
**Goal**: Validate all work done in Phase 1-3 before continuing

---

## 📊 Current Test Coverage

### Framework Tests (124 total)
- ✅ NetMX.Core.Tests: 13 tests passing
- ✅ NetMX.AspNetCore.Core.Tests: 13 tests passing  
- ✅ NetMX.AspNetCore.Mvc.Tests: 44 tests passing
- ✅ NetMX.Events.Tests: 47 tests passing
- ✅ NetMX.EntityFrameworkCore.Tests: 7 tests passing

### Module Tests (66 total)
- ✅ Authorization.Tests: 38 tests passing
- ✅ Identity.Core.Tests: 28 tests passing

**Total Current: 190 tests, all passing ✅**

---

## 🎯 Testing Strategy

### Phase 1: Run All Existing Tests
**Goal**: Ensure nothing broke during Phase 3 Event Registry changes

**Actions**:
```bash
# 1. Build everything
dotnet build framework/NetMX.sln
dotnet build modules/Authorization/Authorization.sln
dotnet build modules/Identity/Identity.sln
dotnet build modules/Audit/Audit.sln

# 2. Run all framework tests
dotnet test framework/NetMX.sln --no-build

# 3. Run all module tests  
dotnet test modules/Authorization/Authorization.sln --no-build
dotnet test modules/Identity/Identity.sln --no-build
dotnet test modules/Audit/Audit.sln --no-build

# 4. Generate coverage report
dotnet test framework/NetMX.sln --collect:"XPlat Code Coverage"
```

**Expected Result**: All 190+ tests pass

---

### Phase 2: Create Event Registry Integration Tests
**Goal**: Validate the new Event Registry pattern we built in Phase 3

**Test Scenarios**:

#### A. Events Static Class Tests
```csharp
[Fact]
public void Events_Should_Have_Authorization_Events()
{
    // Verify Events.Permission.* exists
    Assert.Equal("permission.created", Events.Permission.Created);
    Assert.Equal("permission.updated", Events.Permission.Updated);
    Assert.Equal("permission.deleted", Events.Permission.Deleted);
    
    // Verify Events.Role.* exists
    Assert.Equal("role.created", Events.Role.Created);
}

[Fact]
public void Events_Should_Have_Identity_Events()
{
    // Verify Events.User.*, Events.Login.*, etc.
    Assert.NotNull(Events.User.Created);
    Assert.NotNull(Events.Login.Success);
}
```

#### B. EventDefinitions Registration Tests
```csharp
[Fact]
public void EventDefinitions_Should_Register_All_Events()
{
    // Arrange
    var registry = new EventRegistry();
    
    // Act
    PermissionEventDefinitions.Register(registry);
    RoleEventDefinitions.Register(registry);
    
    // Assert
    var permissionEvents = registry.GetEventsByCategory("Permission");
    Assert.Equal(3, permissionEvents.Count());
    
    var roleEvents = registry.GetEventsByCategory("Role");
    Assert.Equal(3, roleEvents.Count());
}
```

#### C. CLI Generated Code Tests
```csharp
[Fact]
public async Task CLI_Generate_Feature_Should_Use_Event_Registry()
{
    // Arrange
    using var runner = new FeatureTestRunner();
    
    // Act
    var result = await runner.RunCliCommandAsync("generate feature Product");
    
    // Assert
    Assert.True(result.Success);
    
    // Verify Events.Product.cs generated in NetMX.Events
    var eventsFile = runner.ReadFile("../../../NetMX.Events/Events.Product.cs");
    Assert.Contains("public static class Product", eventsFile);
    Assert.Contains("public const string Created", eventsFile);
    
    // Verify ProductEventDefinitions.cs generated
    var definitionsFile = runner.ReadFile("Events/ProductEventDefinitions.cs");
    Assert.Contains("public static void Register", definitionsFile);
    
    // Verify controller uses Events.Product.*
    var controllerFile = runner.ReadFile("Controllers/ProductController.cs");
    Assert.Contains("Events.Product.Created", controllerFile);
    Assert.Contains("using NetMX.Events;", controllerFile);
    
    // Verify view uses @Events.Product.*
    var viewFile = runner.ReadFile("Views/Product/Index.cshtml");
    Assert.Contains("@Events.Product.Created", viewFile);
    Assert.Contains("@using NetMX.Events", viewFile);
}
```

**Location**: Create `framework/NetMX.Events.Tests/EventRegistryIntegrationTests.cs`

---

### Phase 3: Module Integration Tests
**Goal**: Test modules work together with Event Registry

#### A. Authorization Module Tests
```csharp
[Fact]
public async Task Permission_CRUD_Should_Trigger_Events()
{
    // Test that creating/updating/deleting permissions triggers Events.Permission.*
}

[Fact]
public async Task Role_CRUD_Should_Trigger_Events()
{
    // Test that role operations trigger Events.Role.*
}
```

#### B. Cross-Module Event Tests
```csharp
[Fact]
public async Task Identity_Login_Should_Trigger_Event_For_Audit()
{
    // Verify Events.Login.Success can be listened to by Audit module
}
```

**Location**: Create tests in each module's test project

---

### Phase 4: CLI End-to-End Tests
**Goal**: Validate full CLI workflow with Event Registry

**Test Scenarios**:
```bash
# 1. Generate feature in app
netmx generate feature Product

# Verify:
# - Events.Product.cs exists in NetMX.Events
# - ProductEventDefinitions.cs exists
# - ProductEventExtensions.cs exists
# - Controller uses Events.Product.*
# - View uses @Events.Product.*

# 2. Generate feature in module
netmx generate feature AuditLog -m Audit

# Verify same pattern in module context

# 3. Generate multiple features
netmx generate feature Category
netmx generate feature Order

# Verify:
# - No event name collisions
# - All events accessible via Events.*
# - IntelliSense works for all events
```

**Location**: `tools/NetMX.CLI.Tests/EventRegistryGenerationTests.cs`

---

### Phase 5: Dogfooding - Build Sample E-Commerce App
**Goal**: Real-world validation of entire framework

**Plan**:
```bash
# 1. Create new project
cd sampleApps
mkdir ecommerce-test
cd ecommerce-test

# 2. Initialize with template
dotnet new web -n ECommerceApp
cd ECommerceApp

# 3. Add NetMX packages
dotnet add package NetMX.Core --version 0.2.0-local
dotnet add package NetMX.Events --version 0.2.0-local
dotnet add package NetMX.AspNetCore.Mvc --version 0.2.0-local
dotnet add package NetMX.EntityFrameworkCore --version 0.2.0-local

# 4. Generate features using CLI
netmx generate feature Product
netmx generate feature Category  
netmx generate feature Order
netmx generate feature Customer

# 5. Add modules
netmx add module Authorization
netmx add module Identity

# 6. Test in browser
dotnet run
# Open http://localhost:5000
# Test CRUD operations
# Verify HTMX works
# Verify events trigger properly
```

**Validation Checklist**:
- [ ] All features generate without errors
- [ ] DbContext compiles with all DbSets
- [ ] Migrations create successfully
- [ ] Database updates successfully  
- [ ] App runs without errors
- [ ] CRUD operations work in browser
- [ ] HTMX interactions work (no page reloads)
- [ ] Events trigger properly (check browser console)
- [ ] IntelliSense works for Events.* in VS Code
- [ ] No CS0436 errors
- [ ] Authorization works
- [ ] Identity login/logout works

**Document Pain Points**:
Create `sampleApps/ecommerce-test/ISSUES.md` to track any problems found

---

## 🚀 Execution Plan

### Today (Oct 22) - 4 hours

**Hour 1: Run Existing Tests**
- Build all solutions
- Run all tests
- Fix any failures
- Commit: "test: Verify all existing tests pass after Phase 3"

**Hour 2: Create Event Registry Integration Tests**
- Create `EventRegistryIntegrationTests.cs`
- Write 10-15 tests covering:
  * Events static class
  * EventDefinitions registration
  * Cross-module event access
- Run tests, ensure all pass
- Commit: "test: Add Event Registry integration tests"

**Hour 3: Create CLI E2E Tests**
- Create `EventRegistryGenerationTests.cs`
- Write tests for CLI generation
- Test both app and module contexts
- Verify generated code quality
- Commit: "test: Add CLI Event Registry generation tests"

**Hour 4: Start Dogfooding**
- Create sample e-commerce app
- Generate 4 features
- Add 2 modules
- Test basic workflows
- Document issues found
- Commit: "sample: Add e-commerce dogfooding app"

### Tomorrow (Oct 23) - 2 hours

**Hour 1: Fix Dogfooding Issues**
- Address issues found during dogfooding
- Update CLI if needed
- Update documentation
- Re-test in sample app

**Hour 2: Documentation Update**
- Update COMPLETE-DEVELOPMENT-ROADMAP.md (mark Phase 3 complete)
- Update PROGRESS-REPORT.md
- Create PHASE-3-COMPLETE.md summary
- Update README.md with testing status

---

## 📈 Success Metrics

### Code Quality
- ✅ All existing tests pass (190+)
- ✅ New integration tests pass (15+)
- ✅ CLI E2E tests pass (10+)
- ✅ Total: 215+ tests passing

### Developer Experience
- ✅ Sample app generates without errors
- ✅ IntelliSense works for Events.*
- ✅ No CS0436 errors
- ✅ HTMX patterns work correctly
- ✅ CLI commands work as expected

### Documentation
- ✅ All pain points documented
- ✅ README updated
- ✅ Roadmap updated
- ✅ Phase 3 summary created

---

## 🎯 Current Status: READY TO EXECUTE

**All Prerequisites Met**:
- ✅ Phase 3 (CLI Event Registry) complete
- ✅ All code committed
- ✅ NetMX.Testing package exists
- ✅ Test infrastructure ready

**Next Command**:
```bash
# Start with running all existing tests
dotnet build framework/NetMX.sln
dotnet test framework/NetMX.sln
```

**Time Estimate**: 6 hours total (4 today, 2 tomorrow)  
**Risk Level**: Low (mostly validation, not new features)  
**Blocking Issues**: None

Let's execute! 🚀

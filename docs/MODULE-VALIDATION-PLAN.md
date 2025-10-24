# Module Validation Plan (October 24, 2025)

**Goal**: Validate that NetMX modules work correctly in applications

## 🎯 Tasks

### Task 1: Test Existing Modules ✅ PRIORITY
**Test Identity, Authorization, Audit modules in a real app**

#### 1.1 Create Test App
- [x] Create `dogfood/ModuleValidation` from modular template
- [ ] Build successfully
- [ ] Run successfully

#### 1.2 Add Identity Module
- [ ] Copy Identity module files to app
- [ ] Add project references
- [ ] Configure in Program.cs
- [ ] Run migrations
- [ ] Test endpoints (/Account/Login, /Account/Register, etc.)
- [ ] Validate: User registration works
- [ ] Validate: Login works
- [ ] Validate: Role assignment works

#### 1.3 Add Authorization Module  
- [ ] Copy Authorization module files
- [ ] Add project references
- [ ] Configure in Program.cs
- [ ] Run migrations
- [ ] Test [RequirePermission] attribute
- [ ] Test permission seeding
- [ ] Test role-based access

#### 1.4 Add Audit Module
- [ ] Copy Audit module files
- [ ] Add project references
- [ ] Configure in Program.cs
- [ ] Run migrations
- [ ] Test audit logging
- [ ] Test entity change tracking

### Task 2: Create Monolith Template ✅ PRIORITY
**We only have "modular" template - need pure monolith**

#### 2.1 Analyze Differences
- [ ] Modular: Modules as separate projects/assemblies
- [ ] Monolith: Everything in one project
- [ ] Decision: What's the difference?

**Proposed Structure**:
```
templates/
├── monolith/          # Single-project app (NEW)
│   └── src/
│       └── MyApp.Web/
│           ├── Features/      # Feature folders
│           ├── Data/
│           ├── Models/
│           └── Program.cs
└── modular/           # Multi-project app (EXISTING)
    └── src/
        ├── MyApp.Web/
        └── MyApp.Modules/     # Separate projects per module
```

#### 2.2 Create Monolith Template
- [ ] Copy modular template as base
- [ ] Remove module-specific structure
- [ ] Add Features/ folder
- [ ] Update README
- [ ] Test with CLI

### Task 3: Validate Modular Template Architecture 🏗️
**Ensure modular template follows true modular design**

#### 3.1 Review Current Structure
- [ ] Check module isolation
- [ ] Check dependencies
- [ ] Check how modules are loaded

#### 3.2 Validate Design Principles
- [ ] Modules are independently deployable?
- [ ] Modules have clear boundaries?
- [ ] Modules communicate via contracts?
- [ ] Modules can be added/removed easily?

#### 3.3 Document Architecture
- [ ] Create MODULAR-ARCHITECTURE.md
- [ ] Explain module structure
- [ ] Explain module loading
- [ ] Show examples

### Task 4: Test CLI Module Creation 🛠️
**Validate `netmx create module` works correctly**

#### 4.1 Create Test Module
- [ ] Run: `netmx create module TestModule`
- [ ] Verify 4-layer structure created
- [ ] Verify solution file created
- [ ] Verify module.json created

#### 4.2 Generate Feature in Module
- [ ] Run: `netmx generate feature TestEntity -m TestModule`
- [ ] Verify entity created in Core
- [ ] Verify DTOs created in Contracts
- [ ] Verify service created in Application
- [ ] Verify controller/views created in Web

#### 4.3 Use Module in App
- [ ] Add module to test app
- [ ] Build successfully
- [ ] Run successfully
- [ ] Test feature works

### Task 5: Fix Issues Found 🐛
**Document and fix any issues discovered**

- [ ] List all issues
- [ ] Prioritize fixes
- [ ] Implement fixes
- [ ] Validate fixes

## 📋 Success Criteria

### For Modules
- ✅ Identity module works in app
- ✅ Authorization module works in app
- ✅ Audit module works in app
- ✅ Modules are independently usable
- ✅ Module dependencies are clear

### For Templates
- ✅ Monolith template exists
- ✅ Monolith template works with CLI
- ✅ Modular template architecture is sound
- ✅ Clear documentation exists

### For CLI
- ✅ `netmx create module` works
- ✅ Generated modules have correct structure
- ✅ Generated modules work in apps
- ✅ Features can be generated in modules

## 🚀 Execution Order

**Phase 1: Quick Wins** (1-2 hours)
1. Test `netmx create module` (Task 4.1)
2. Create monolith template (Task 2.2)
3. Document findings

**Phase 2: Module Validation** (2-3 hours)
1. Add Identity to test app (Task 1.2)
2. Add Authorization to test app (Task 1.3)
3. Test integration
4. Document issues

**Phase 3: Architecture Review** (1-2 hours)
1. Review modular template (Task 3)
2. Document architecture
3. Fix issues

**Phase 4: Complete Validation** (1 hour)
1. Add Audit module (Task 1.4)
2. Test full stack
3. Document results

## 📝 Notes

**Key Questions**:
1. What's the real difference between monolith and modular templates?
2. Should modular template load modules dynamically or at compile-time?
3. How should modules reference each other?
4. Should modules have their own migrations or shared?

**Decisions Needed**:
- [ ] Monolith vs Modular - what's the actual distinction?
- [ ] Module loading strategy (compile-time vs runtime)
- [ ] Module dependency management
- [ ] Migration strategy for modules

---

**Status**: Ready to execute Phase 1 (Quick Wins)

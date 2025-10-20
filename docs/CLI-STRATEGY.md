# NetMX CLI & Developer Experience - Strategic Plan

**Date**: October 20, 2025  
**Status**: Planning Phase

## Vision

Create a **world-class developer experience** where developers can:
1. **Scaffold new projects** in seconds
2. **Add modules** with a single command
3. **Generate CRUD** for entities instantly
4. **Test in isolation** without affecting the main app
5. **Manage everything** through CLI (and future NetMX Studio)

## 🎯 CLI Command Structure (Future)

### Project Management
```bash
# Create new project
netmx new <template> <name> [--output <path>]
netmx new modular MyApp
netmx new api MyApiApp
netmx new microservices MyDistributedApp

# Add module to existing project
netmx add module <module-name> [--source <local|nuget>]
netmx add module Identity
netmx add module Audit
netmx add module FileStorage

# Remove module
netmx remove module <module-name>
netmx remove module Identity
```

### Code Generation
```bash
# Generate CRUD for entity
netmx generate crud <entity> --module <module> [--features <list>]
netmx generate crud Product --module Catalog --features crud,search,export
netmx generate crud Order --module Sales --features crud,workflow

# Generate entity
netmx generate entity <name> --module <module>
netmx generate entity Customer --module Sales

# Generate service
netmx generate service <name> --module <module>
netmx generate service EmailService --module Notifications

# Generate API controller
netmx generate api <entity> --module <module>
netmx generate api Product --module Catalog
```

### Testing & Isolation
```bash
# Create isolated test module
netmx test create <name> --based-on <module>
netmx test create MyFeature --based-on Identity

# Run test module
netmx test run <name> [--watch]
netmx test run MyFeature --watch

# List test modules
netmx test list

# Clean up test module
netmx test remove <name>
netmx test remove MyFeature
```

### Database Management
```bash
# Add migration
netmx db migration add <name> [--module <module>]
netmx db migration add AddProductTable --module Catalog

# Update database
netmx db update [--module <module>]
netmx db update --module Identity

# Seed data
netmx db seed [--module <module>]
netmx db seed --module Identity
```

### Module Management
```bash
# List available modules
netmx module list [--available|--installed]
netmx module list --available

# Search for modules
netmx module search <keyword>
netmx module search authentication

# Update module
netmx module update <module-name>
netmx module update Identity

# Module info
netmx module info <module-name>
netmx module info Identity
```

### Development Workflow
```bash
# Start development server
netmx dev [--watch] [--module <module>]
netmx dev --watch --module Identity

# Build project
netmx build [--module <module>]
netmx build --module Identity

# Run tests
netmx test [--module <module>]
netmx test --module Identity

# Clean
netmx clean
```

## 🏗️ Module Registration System

### Current Implementation (Manual)

Developers need to:
1. Add project reference to module
2. Register module in `Program.cs`
3. Configure DbContext
4. Run migrations

### Proposed Implementation (Automatic)

**CLI Command**:
```bash
netmx add module Identity
```

**What CLI Does**:
1. **Add Project References**:
   ```xml
   <ProjectReference Include="..\..\modules\NetMX.Identity.Core\..." />
   <ProjectReference Include="..\..\modules\NetMX.Identity.Application\..." />
   <ProjectReference Include="..\..\modules\NetMX.Identity.Web\..." />
   ```

2. **Update Program.cs**:
   ```csharp
   // Auto-generated module registration
   builder.Services.AddNetMXModule<NetMXIdentityWebModule>();
   builder.Services.AddNetMXModule<NetMXIdentityApplicationModule>();
   
   // Auto-generated DbContext configuration
   builder.Services.AddDbContext<IdentityDbContext>(options =>
       options.UseNpgsql(connectionString));
   ```

3. **Update appsettings.json**:
   ```json
   {
     "Modules": {
       "Identity": {
         "Enabled": true,
         "Options": {
           "RequireEmailConfirmation": false,
           "PasswordRequirements": {
             "MinLength": 8
           }
         }
       }
     }
   }
   ```

4. **Generate Migration**:
   ```bash
   # CLI automatically runs
   dotnet ef migrations add AddIdentityModule
   ```

5. **Ask User**:
   ```
   ✓ Added Identity module
   ✓ Updated Program.cs
   ✓ Generated migration
   
   Do you want to:
   1. Apply migration now? [Y/n]
   2. Seed sample data? [Y/n]
   3. Add authentication to existing controllers? [y/N]
   ```

### Module Descriptor File

Each module should have a `module.json`:

```json
{
  "name": "NetMX.Identity",
  "version": "1.0.0",
  "description": "Authentication and authorization module",
  "author": "NetMX Team",
  "dependencies": [
    "NetMX.Core",
    "NetMX.EntityFrameworkCore"
  ],
  "projects": [
    {
      "name": "NetMX.Identity.Core",
      "path": "NetMX.Identity.Core/NetMX.Identity.Core.csproj",
      "type": "domain"
    },
    {
      "name": "NetMX.Identity.Application",
      "path": "NetMX.Identity.Application/NetMX.Identity.Application.csproj",
      "type": "application"
    },
    {
      "name": "NetMX.Identity.Web",
      "path": "NetMX.Identity.Web/NetMX.Identity.Web.csproj",
      "type": "web"
    }
  ],
  "services": [
    {
      "type": "module",
      "class": "NetMX.Identity.Web.NetMXIdentityWebModule"
    },
    {
      "type": "dbcontext",
      "class": "NetMX.Identity.Core.Data.IdentityDbContext",
      "connectionStringName": "DefaultConnection"
    }
  ],
  "migrations": {
    "enabled": true,
    "autoApply": false
  },
  "configuration": {
    "section": "Modules:Identity",
    "required": false
  },
  "features": [
    "authentication",
    "authorization",
    "user-management",
    "role-management",
    "2fa",
    "external-auth"
  ]
}
```

## 🧪 Test Isolation System

### Problem
Developers want to test new features without:
- Breaking existing functionality
- Polluting main database
- Affecting other developers
- Complex setup/teardown

### Solution: Isolated Test Modules

**CLI Command**:
```bash
netmx test create MyFeatureTest --based-on Identity
```

**What CLI Does**:

1. **Create Isolated Project Structure**:
   ```
   tests/
     MyFeatureTest/
       MyFeatureTest.csproj
       Program.cs              # Isolated startup
       appsettings.json        # Separate config
       wwwroot/
       Controllers/
         TestController.cs     # Your test code
       Views/
       Data/
         TestDbContext.cs      # Isolated database
   ```

2. **Configure Isolated Database**:
   ```csharp
   // Automatically uses in-memory or separate test DB
   builder.Services.AddDbContext<TestDbContext>(options =>
       options.UseInMemoryDatabase("MyFeatureTest"));
   ```

3. **Copy Module References**:
   ```xml
   <!-- Copies all references from base module -->
   <ProjectReference Include="..\..\modules\NetMX.Identity.Core\..." />
   <ProjectReference Include="..\..\modules\NetMX.Identity.Application\..." />
   ```

4. **Add Test Utilities**:
   ```csharp
   // Auto-generated test helpers
   public class TestDataSeeder
   {
       public static void SeedTestData(TestDbContext context)
       {
           // Sample data for testing
       }
   }
   ```

5. **Create Launch Profile**:
   ```json
   {
     "profiles": {
       "MyFeatureTest": {
         "commandName": "Project",
         "launchBrowser": true,
         "environmentVariables": {
           "ASPNETCORE_ENVIRONMENT": "Test"
         },
         "applicationUrl": "https://localhost:5001;http://localhost:5000"
       }
     }
   }
   ```

**Usage**:
```bash
# Create test module
netmx test create MyFeatureTest --based-on Identity

# Run test module (isolated)
netmx test run MyFeatureTest --watch

# View in browser
# Opens at https://localhost:5001 with isolated database

# When done testing
netmx test remove MyFeatureTest
```

**Benefits**:
- ✅ **Isolated**: Separate database, configuration, ports
- ✅ **Fast**: In-memory database by default
- ✅ **Clean**: Easy to create/destroy
- ✅ **Safe**: Can't break main application
- ✅ **Realistic**: Uses real modules, not mocks

### Test Module Templates

**Feature Test** (default):
```bash
netmx test create MyFeature --based-on Identity --template feature
# Creates isolated feature test with UI
```

**Integration Test**:
```bash
netmx test create MyIntegrationTest --based-on Identity --template integration
# Creates xUnit test project with WebApplicationFactory
```

**Performance Test**:
```bash
netmx test create MyPerfTest --based-on Identity --template performance
# Creates BenchmarkDotNet test project
```

## 🎨 NetMX Studio (Future)

Visual tool for managing NetMX projects:

### Features
- **Project Dashboard**: See all modules, status, health
- **Module Marketplace**: Browse, install, configure modules
- **Visual Designer**: Design entities, relationships visually
- **Code Generator**: Generate code with GUI
- **Test Manager**: Create/manage test modules visually
- **Database Viewer**: See database schema, data
- **API Explorer**: Test APIs interactively
- **Performance Monitor**: Real-time metrics

### Architecture
```
NetMX Studio (Electron or Blazor)
    ↓
NetMX CLI (Backend)
    ↓
NetMX Framework
```

## 📋 Implementation Plan

### Phase 1: CLI Foundation (Day 19) ✅ Planned
- [x] Basic project creation
- [x] Module scaffolding
- [ ] Enhanced module management
- [ ] Auto-registration system
- [ ] Module descriptor format

### Phase 2: Test Isolation (Week 4)
- [ ] Test module creation
- [ ] Isolated database support
- [ ] Test data seeders
- [ ] Test runner integration
- [ ] Cleanup utilities

### Phase 3: Code Generation (Week 5)
- [ ] CRUD scaffolding
- [ ] Entity generation
- [ ] Service generation
- [ ] API generation
- [ ] Custom templates

### Phase 4: Advanced CLI (Week 6)
- [ ] Database management
- [ ] Migration helpers
- [ ] Performance profiling
- [ ] Deployment helpers
- [ ] Plugin system

### Phase 5: NetMX Studio (Future)
- [ ] Desktop application
- [ ] Visual module manager
- [ ] Code generator UI
- [ ] Test manager UI
- [ ] Marketplace

## 🚀 Immediate Next Steps

### For Current Session (Day 11.5 → 12)
1. **Update Template** - Add Identity to modular template
2. **Create Module Descriptor** - Add `module.json` to Identity
3. **Enhance CLI** - Add `netmx add module` command skeleton
4. **Document Pattern** - Show how other modules should be structured

### For Day 19 (CLI Enhancement Day)
1. **Implement Auto-Registration** - Read module.json, update Program.cs
2. **Add Test Isolation** - Create isolated test modules
3. **Generate CRUD** - Basic CRUD scaffolding
4. **Migration Management** - Simplified database operations

## 💡 Design Principles

1. **Convention over Configuration**: Smart defaults, minimal config
2. **Discoverability**: CLI should guide users with helpful messages
3. **Safety**: Always confirm destructive operations
4. **Flexibility**: Power users can override automation
5. **Speed**: Commands should be fast and responsive
6. **Clarity**: Clear error messages and suggestions

## 📝 Example Workflow

**Developer adds Identity to new project**:
```bash
# Create new project
netmx new modular MyApp
cd MyApp

# Add Identity module (fully automated)
netmx add module Identity
# ✓ Added project references
# ✓ Registered in Program.cs
# ✓ Generated migration
# ? Apply migration now? Y
# ✓ Migration applied
# ? Seed sample admin user? Y
# ✓ Seeded: admin@example.com / Admin123!
# ✓ Identity module ready!

# Test new feature in isolation
netmx test create MyAuthFeature --based-on Identity
netmx test run MyAuthFeature --watch
# Opens isolated test app at https://localhost:5001
# Make changes, test, verify
# Ctrl+C when done

netmx test remove MyAuthFeature
# ✓ Cleaned up test module

# Generate CRUD for new entity
netmx generate crud Product --module Catalog
# ✓ Created ProductAppService
# ✓ Created ProductController
# ✓ Created Product views (List, Create, Edit, Delete)
# ✓ Generated migration
# ? Apply migration now? Y
# ✓ Ready! Visit /products

# Run project
netmx dev --watch
```

**Total time**: ~2 minutes from empty folder to working app with authentication and CRUD! 🚀

---

**This is the vision. Let's start building it step by step!**

Next: Update the template with Identity, then enhance CLI on Day 19.

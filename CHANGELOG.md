# Swap CLI Changelog

All notable changes to the Swap CLI will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### Added - Demo Applications
- **ProjectHub** — Modular monolith demo showcasing Swap.Modularity patterns
  - 3 independent modules (Workspaces, Projects, Tasks) with Contracts/Module/Web RCL structure
  - Server-Sent Events (SSE) live dashboard with real-time metrics streaming
  - Per-module EF Core migrations (Postgres + SQLite support)
  - Docker Compose stack (Postgres + RabbitMQ)
  - Cross-module communication via contracts and distributed events
  - SwapRedirectToAction helper for internal action invocation
  - Fluent OOB API (`.WithOobSwap()`) for out-of-band updates
  - Integration tests covering HTMX flows and module interactions
- **TaskFlow** — HTMX patterns demo (existing, now documented)
  - Core SwapController patterns and automatic partial detection
  - Declarative event chains (domain → UI)
  - Toast notifications and form validation
  - HTMX integration testing examples
- Added `/demo/README.md` with feature comparison and architecture overview
- Updated main README with "Live Demos" section

### Planned / In Progress
- WebSocket Chat Demo completion: front-end OOB swap rendering & E2E Playwright tests still in progress
- Additional HTMX real-time patterns (presence, notifications) pending stabilization of WebSocket infrastructure

---

## [0.4.3] - 2025-11-11

### Added - Server-Sent Events (SSE)
- Introduced SSE utilities with `MapSwapServerSentEvents` patterns for streaming UI updates.
- Razor partial rendering helpers used to broadcast OOB fragments (`hx-swap-oob`) for live list updates.
- Demo endpoints & partials: notifications stream, incremental list append, completion markers.
- Documentation: `SERVER-SENT-EVENTS.md` detailing setup, rendering pipeline, error handling & reconnection strategies.

### Added - WebSockets (Incomplete)
- Initial WebSocket handler framework: `SwapWebSocketHandler`, `WebSocketConnection` lifecycle (connect, message, disconnect).
- Dynamic Razor partial rendering over sockets via `RenderPartialToStringAsync`.
- Chat demo scaffold (`/ws/chat`) broadcasting HTML fragments.
- HTMX WebSocket extension integrated (connection + form `ws-send`).
- OOB swap partial `_ChatMessage` prepared with `hx-swap-oob="beforeend:#chat-messages"`.
- Known Gaps (feature incomplete):
  - Messages currently not rendering in browser (OOB swap target investigation ongoing).
  - Missing automated E2E Playwright coverage (infra conflict w/ running app).
  - Needs connection state indicator & retry UX.
  - Additional samples (presence count, system notifications) deferred.

### Documentation
- `WEBSOCKETS.md` draft published: architecture, handler lifecycle, broadcasting patterns, troubleshooting.
- Real-time section expanded to compare SSE vs WebSockets trade-offs and recommended usage scenarios.

### Internal
- Rendering pipeline reused between SSE & WebSockets for consistent partial composition.
- Extension event hooks (`htmx:ws*`) evaluated for future diagnostics instrumentation.

### Status
- SSE: Complete & validated.
- WebSockets: Partially implemented (handler + transport ok) — UI integration & tests pending.

---

---

## [0.4.2] - 2025-11-11

### Changed - Bulma Migration
- **Zero NPM Dependencies**: Migrated all templates from Tailwind/DaisyUI/NPM to Bulma CSS via LibMan
  - **swap-monolith**: Removed package.json, tailwind.config.js, npm scripts; added Bulma 1.0.4 via LibMan
  - **swap-layered**: Removed all NPM infrastructure; added Bulma 1.0.4 via LibMan
  - **swap-modular-monolith**: Removed NPM/Tailwind from host and all modules; added Bulma 1.0.4 via LibMan
  - **swap-minimal**: Already using Bulma, no changes needed
- **Simplified Development**: No build step required for CSS
  - Removed `npm install`, `npm run build:css`, `npm run watch:css` from all workflows
  - LibMan restore is the only frontend dependency management step
  - Dockerfile updates: Removed Node.js stages, kept LibMan restoration only
- **View Modernization**: Converted all Razor views to Bulma class syntax
  - Tailwind utility classes → Bulma semantic classes (e.g., `btn-primary` → `button is-primary`)
  - DaisyUI components → Bulma components (cards, forms, navbar, notifications)
  - Maintained responsive design and accessibility features
- **Documentation Updates**: Refreshed all READMEs to reflect Bulma architecture
  - Removed Tailwind/DaisyUI references and theming instructions
  - Updated quickstart guides to remove npm commands
  - Simplified production deployment (no CSS build required)
  - New main README with clear template comparison and feature highlights

### Fixed - swap-modular-monolith
- **Routing Issues**: Fixed controller endpoint mismatches in Demo module
  - Fixed `/Demo/BulkList` → `/Demo/BulkTodos` in `_BulkListPanel.cshtml`
  - Fixed Details component auto-loading: added `load` trigger to `hx-trigger` in `_DetailsPanel.cshtml`
  - Fixed Home controller routes: `/Home/AddTodo` → `/todos/ui/add`, `/Home/TodoList` → `/todos/ui/list`
- **Module Namespacing**: Corrected namespace patterns for module contracts
  - Demo.Contracts: `{{ProjectName}}.Modules.Demo.Contracts`
  - Todos.Contracts: `{{ProjectName}}.Modules.Todos.Contracts`
- **EventKey Migration**: Wrapped all string event constants in `new EventKey()` for type safety

### Documentation
- **Main README**: Complete redesign with modern, engaging presentation
  - Clear template comparison (minimal → monolith → layered → modular-monolith)
  - Feature highlights with code examples
  - Quick start guide for all 4 templates
  - Technology stack summary
- **Template READMEs**: Modernized all template documentation
  - Updated swap-monolith README (removed Tailwind/DaisyUI sections, added Bulma)
  - Updated swap-layered README (clean architecture focus, zero NPM)
  - swap-modular-monolith and swap-minimal READMEs already current

---

## [0.4.1] - 2025-11-07

### Changed - Framework Focus
- **Removed Code Generation**: Eliminated all CLI code generation commands and infrastructure
  - Removed `swap generate controller`, `swap generate seeder`, `swap generate test`, `swap generate factory`
  - Removed `swap generate pattern`, `swap generate relationship`, `swap generate auth`
  - Removed all code generation classes: EntityGenerator, DtoGenerator, ServiceGenerator, ControllerGenerator, ViewGenerator, SeederGenerator
  - Removed PropertyParser, FieldDefinition, and related parsing infrastructure
  - Removed template system and .template files
- **Framework-Only CLI**: Simplified CLI to focus on Swap framework capabilities
  - Retained `swap new` for project scaffolding from templates
  - Retained `swap events` commands for event system introspection
  - Retained `swap db` commands for database operations
- **Philosophy Shift**: Swap is now purely a framework, not a code generator
  - Templates provide complete, working applications
  - Developers modify generated code directly
  - No ongoing code generation workflow
  - Focus on HTMX patterns, event chains, and testing utilities

### Removed
- All code generation commands and infrastructure (~8,000 lines)
- Template processing system for controller/view/service generation
- PropertyParser and field definition models
- SeederGenerator, EntityGenerator, DtoGenerator, ServiceGenerator, ControllerGenerator, ViewGenerator
- Test projects for removed generators

### Validated
- `swap new` creates complete working applications
- `swap events` commands work with existing event system
- `swap db` commands work with EF Core migrations
- All three templates (monolith, layered, modular-monolith) generate successfully

---
## [0.4.0] - 2025-11-06

### Added - Template Documentation Suite
- **Comprehensive Documentation**: Created epic developer experience documentation for all templates
  - **swap-monolith**: 5 documentation files (README, ARCHITECTURE, DEVELOPMENT, DEPLOYMENT, EVENTS)
  - **swap-layered**: 5 documentation files (README, ARCHITECTURE, DEVELOPMENT, DEPLOYMENT, EVENTS)
  - Each README includes quickstart, stack overview, project structure, HTMX event system examples, testing guide, Docker setup, CLI generators, DaisyUI styling
  - ARCHITECTURE.md covers high-level design, folder responsibilities, event system flow, database strategy, security, performance, testing, deployment
  - DEVELOPMENT.md includes initial setup, hot reload, CSS development, database migrations, seeding, debugging, code generation, common tasks, environment configuration
  - DEPLOYMENT.md covers Docker, cloud platforms (Azure/AWS/GCP/DigitalOcean), database migrations in production, HTTPS, monitoring, scaling, CI/CD pipelines
  - EVENTS.md provides deep dive on Swap event system with naming conventions, event chains, real-world examples, debugging, performance, testing, best practices

### Added - Template File Completeness
- **Docker Support**: Fixed missing .dockerignore.template files
  - Added .dockerignore.template to swap-monolith root
  - Added .dockerignore.template to swap-layered root
  - Added .dockerignore.template to swap-modular-monolith root
  - Templates now properly exclude build artifacts, node_modules, etc.
- **Git Support**: Fixed missing .gitignore.template files
  - Added .gitignore.template to all three templates
  - Proper exclusion of bin/, obj/, node_modules/, database files, etc.
- **Template Whitelist Fix**: Updated NewCommand.cs to copy .dockerignore and .gitignore
  - Added to root file whitelist in three locations
  - Files now properly copied during template generation

### Changed - NewCommand.cs Refactoring
- **Code Organization**: Refactored 900+ line NewCommand.cs for better maintainability
  - Extracted methods: ValidateInputs, DisplayProjectInfo, CheckPrerequisitesAsync, RunSetupCommandsAsync, GenerateProjectAsync
  - Added regions: Validation, Display Messages, Template Helpers, Prerequisites, Setup Commands, Project Generation, Template Processing
  - Template helper methods: IsLayeredTemplate, IsModularMonolithTemplate, ResolveTemplateFolder
  - Improved readability and easier modification for future enhancements

### Changed - Template Alias Support
- **Modular Monolith Alias**: Added "modular-monolith" as alias for "swap-modular-monolith"
  - Updated template resolution logic in 5 locations
  - Consistent alias support across all template detection points
  - Users can now use either "swap-modular-monolith" or "modular-monolith"

### Fixed - Docker Configuration
- **Build Context Issues**: Fixed Docker build failures for layered and modular-monolith templates
  - Changed build context from "src/Web" to "src" in docker-compose.yml.template
  - Updated Dockerfile.template COPY paths to match new build context
  - Allows Docker to access all project layers (Web, Application, Infrastructure, Domain)
  - Fixed "COPY failed: file not found" errors for Application/Infrastructure/Domain

### Fixed - Migration Error Handling
- **Database Migration Errors**: Added try-catch around db.Database.Migrate() in templates
  - swap-monolith/src/Program.cs.template
  - swap-layered/src/Web/Program.cs.template
  - Helpful error message suggests "docker compose down -v" to clear volumes
  - Prevents confusing SQLite "table already exists" errors in Docker

### Validated
- **Template Generation**: All three templates generate successfully
  - swap-monolith builds and runs with Docker
  - swap-layered builds and runs with Docker
  - swap-modular-monolith builds and runs with Docker
- **Docker Support**: Multi-stage builds work correctly with new build context
- **Documentation Quality**: Comprehensive guides for developers at all levels
- **Developer Experience**: Epic README files with emojis, clear examples, troubleshooting tips

---

## [0.3.2] - 2025-11-06

### Added - Template: Modular Monolith (swap-modular-monolith)
- New production-lean template for modular monoliths: single deployable host with clearly bounded modules.
- Per-module ownership: each module ships with `Contracts`, `Module` (services/endpoints), and a `Web` Razor Class Library for UI.
- Provider-specific EF Core migrations as module-owned projects (SqlServer/Postgres) with design-time factories.
- Uses NuGet packages (`Swap.Modularity`, `Swap.Htmx`, `Swap.Testing`) instead of project references.
- Docker assets included (Postgres, optional RabbitMQ) with sensible defaults.
- Example event chains, tests, and docs included to guide module authoring.

### Added - CLI Support
- `swap new MyApp --template swap-modular-monolith` generates the full modular monolith solution.
- Host pre-wired with `AddSwapModules(...)`, `MapSwapModuleEndpoints()`, and optional event-chain configuration hook.
- Server events transport selectable via configuration (in-memory or RabbitMQ) using `AddSwapServerEventChainsFromConfiguration`.

### Added - Documentation
- Template-level README and docs: architecture, module authoring, database migrations, server events, and host wiring.
- Quickstarts added to template docs; links integrated into the main docs site.

### Validated
- Generated modular monolith builds and runs locally; example integration tests pass using `Swap.Testing`.

---

## [0.3.1] - 2025-11-03

### Fixed - CLI Setup Hang
- Resolved intermittent hang during "Building project before migration..." by streaming stdout/stderr in the command runner to avoid process output buffer deadlocks.

### Changed - Templates & Tests
- Integration tests for both templates standardized on Swap.Testing; removed direct Microsoft.AspNetCore.Mvc.Testing references to prevent NU1605 downgrades.
- HTMX smoke tests adjusted to use the fluent API correctly (no async chaining on Task instances).
- Layered Docker/readme instructions aligned with src/test layout and correct EF commands (`-p src/Infrastructure -s src/Web`).

### Added - Documentation
- Heavily expanded template READMEs (monolith and layered):
  - What you get, architecture/layout, run/migrate instructions, HTMX + event chains, Swap.Testing usage and example, Docker notes, and links to the wiki.
- Wiki updates:
  - New Templates overview page with comparison and quick-starts.
  - Intro highlights Templates prominently; CLI `new` page links to Templates.
  - Layered getting-started updated to src/test paths.

### Version Bumps
- Swap.CLI: 0.3.0 → 0.3.1
- Wiki site (docs): 0.0.1 → 0.0.2

### Validated
- Generated monolith and layered apps build/run; integration tests pass using Swap.Testing.

---

## [0.3.0] - 2025-11-03

### Added - HTMX-Native Event System (Foundation)
- Server-side event bus with chain resolution and client filtering via X-Swap-Events
- Resolution modes with safe default: `ChainResolutionMode`
  - `OneHop` (default), `Bidirectional`, `Transitive` (with `MaxTransitiveDepth`)
- Development endpoints (Development only):
  - `/_swap/dev/events` (HTML dashboard with chains table and Mermaid graph)
  - `/_swap/dev/events.json` (chains JSON)
  - `/_swap/dev/events.meta.json` (mode/depth meta)
  - `/_swap/dev/explain.json?event=...` (server-side resolution under current mode)
- CLI event commands:
  - `swap events list` (source scan with constants resolution)
  - `swap events from-server` (read live chains from dev endpoint)
  - `swap events validate` (naming + cycle checks with exit codes)
  - `swap events graph` (Mermaid/DOT output)

### Fixed - Dev Dashboard
- Mermaid graph now renders properly (no HTML encoding of graph text)
- Distinct edge counting in summary and table; de-duplicated edges in graph output
- Explain section includes clear legend about server-side resolution vs client filtering

### Changed - Templates
- `swap-monolith` and classic `monolith` templates reference the event system by default
- Both templates updated to reference Swap packages at `0.3.0`

### Changed - Package Versions
- Swap.CLI: 0.2.0-dev → 0.3.0 (Assembly/FileVersion: 0.3.0.0)
- Swap.Htmx: 0.2.0-dev → 0.3.0
- Swap.Patterns: 0.2.0-dev → 0.3.0
- Swap.Testing: 0.2.0-dev → 0.3.0

### Validated
- 55 new/updated tests cover event resolution modes, filtering, and edge cases
- Manual validation of dev dashboard endpoints and CLI event commands

---

## [0.2.0-dev] - 2025-11-01

### 🎉 Relationship Auto-Wiring Complete

This release completes the relationship story with **automatic UI generation** for all four relationship types. Controllers generated with `--with-relationships` now create fully functional forms with dropdowns and checkboxes, no manual wiring required.

### Added - One-to-One Relationship Generation
- **One-to-One Support**: Full CLI support for generating one-to-one relationships
  - Generates FK with unique constraint on dependent entity
  - Adds single navigation properties to both entities
  - Configures EF Core Fluent API using `HasOne`/`WithOne` with unique index
  - Supports principal/dependent specification via `--principal` and `--dependent` flags
  - Automatically determines principal/dependent if not specified (source is dependent by default)
  - Supports required and optional relationships
  - Full unit test coverage (13 tests)
- **CLI Examples**:
  ```bash
  # Basic one-to-one (Profile is dependent by default)
  swap g rel -s Profile -t User --type one-to-one --required
  
  # Explicitly specify principal
  swap g rel -s Profile -t User --type one-to-one --principal User
  
  # Optional relationship
  swap g rel -s Profile -t User --type one-to-one
  ```
- **Automatic UI**: Generates dropdown in forms on dependent side with automatic display field detection
  - ⚠️ **Known Limitation**: Principal side navigation property generates as text input instead of dropdown
  - Workaround: Manage one-to-one relationships from the dependent side (entity with FK)
  - Example: Edit UserProfile to select User, rather than editing User to select UserProfile

### Added - Many-to-Many Relationship Generation
- **Many-to-Many Support**: Full CLI support for generating many-to-many relationships
  - Generates junction entity with composite key (alphabetical naming convention, e.g., `CourseStudent`)
  - Adds collection navigation properties to both entities (`ICollection<T>`)
  - Configures EF Core Fluent API using `UsingEntity<TJunction>` with proper composite key
  - Supports custom junction table names via `--junction` flag
  - Supports additional junction properties via `--junction-props` flag (e.g., `CreatedAt:datetime,CreatedBy:string`)
  - Automatic DbSet creation with Models namespace detection
  - Full unit test coverage (21 tests)
- **CLI Examples**:
  ```bash
  # Basic many-to-many
  swap g rel -s Student -t Course --type many-to-many
  
  # Custom junction with extra properties
  swap g rel -s Post -t Tag --type many-to-many --junction PostTag --junction-props "CreatedAt:datetime"
  ```
- **Automatic UI**: Generates checkbox list UI in forms, ViewBag population, and controller action handlers for Selected{Entity}Ids

### Added - Automatic Display Field Detection
- **Smart Display Field Selection**: Controllers now intelligently choose display fields for dropdowns
  - Priority order: `Name` → `Title` → `Email` → `Username` → `Description` → fallback to `Id`
  - Works for all foreign key relationships (one-to-many, many-to-one, one-to-one dependent)
  - No manual configuration required
  - Generates clean UI code: `@item.Email` or `@item.Name` instead of `@item.ToString()`

### Added - Select-All Checkbox Fix
- **Bulk Selection**: Fixed ID casing mismatch in select-all checkbox
  - Previously used all-lowercase entity name (e.g., `#userprofile-list`)
  - Now correctly uses camelCase (e.g., `#userProfile-list`)
  - Select-all checkbox now properly targets the list container

### Added - OneToOne Edit FK Protection
- **Unique Constraint Protection**: Added conditional readonly FK field for one-to-one edit forms
  - FK dropdown shown on create (Model.Id == 0)
  - FK shown as readonly on edit with helper text explaining unique constraint
  - Prevents SQLite UNIQUE constraint errors when trying to reassign one-to-one relationships
  - Note: Detection logic for one-to-one vs many-to-one currently based on FK inference, not metadata

### Added - Comprehensive Documentation
- **README.md Enhancements**:
  - Added "🔗 Relationship Generation" to "Why Swap?" section highlighting all relationship types
  - New "Add Relationships" quick start example showing Product-Category dropdown
  - Comprehensive `swap generate relationship` command documentation with all options
  - New "Building a Blog" tutorial demonstrating all relationship types in ~2 minutes
    - Creates Post, Author, Category, Tag, Comment entities
    - Shows one-to-many (Post-Author), many-to-many (Post-Tag), one-to-one (Author-Profile)
    - Complete working blog with automatic UI in 15 commands
- **CLI Help Text**: All relationship features properly documented in `--help` output

### Added - Authentication Scaffolding
- **Authentication System**: Complete ASP.NET Core Identity integration with `swap generate auth`
  - Generates ApplicationUser model extending IdentityUser with DisplayName, CreatedAt, LastLoginAt
  - Creates 4 ViewModels: LoginViewModel, RegisterViewModel, ForgotPasswordViewModel, ResetPasswordViewModel
  - Generates AuthController with full auth flow: register, login, logout, password reset, access denied
  - Creates 7 views: Login, Register, ForgotPassword, ForgotPasswordConfirmation, ResetPassword, ResetPasswordConfirmation, AccessDenied
  - Includes _LoginPartial.cshtml for layout integration
  - All views styled with Tailwind CSS and HTMX-compatible markup
  - Automatically adds Microsoft.AspNetCore.Identity.EntityFrameworkCore package reference
  - Comprehensive setup instructions with code snippets for Program.cs and DbContext configuration
- **CLI Options**: `--dry-run`, `--force`, `--project` support

### Changed - Package Versions
- **Swap.CLI**: 0.1.0 → 0.2.0-dev
- **Swap.Htmx**: 0.1.0 → 0.2.0-dev
- **Swap.Patterns**: 0.1.0 → 0.2.0-dev
- **Swap.Testing**: 0.1.0 → 0.2.0-dev

### Known Issues
- **One-to-One Principal Side**: Navigation property on principal side generates as text input in forms
  - Affects scenarios where you want to select dependent entity from principal entity form
  - Detection logic successfully identifies one-to-one relationships but form generation needs enhancement
  - Workaround: Always manage one-to-one relationships from dependent side (entity with FK)

### Validated
- **All Relationship Types Production-Ready**:
  - ✅ One-to-Many: CLI, migration, UI generation, display field detection
  - ✅ Many-to-One: CLI, migration, UI generation, display field detection
  - ✅ Many-to-Many: CLI, junction tables, checkbox UI, ViewBag population
  - ✅ One-to-One: CLI, unique constraint, dropdown UI, display field detection
- **Test Coverage**: 319 tests passing (212 CLI + 35 Htmx + 72 Patterns)
- **Documentation**: Comprehensive README with relationship examples and blog tutorial
- **End-to-End Verification**: Tested with User-Profile one-to-one and Post-Tag many-to-many applications

---

## [0.1.0] - 2025-01-29

### 🎉 First Production Release - OSS Ready

This release marks Swap's readiness for open-source production use with comprehensive pattern auto-wiring, removal capabilities, and cross-platform support.

### Added - Pattern Auto-Wiring
- **Automatic DbContext Configuration**: Patterns now self-configure with zero boilerplate
  - `ISoftDeletable`: Automatically adds global query filter to DbContext
  - `IAuditable`: Auto-registers IHttpContextAccessor and SaveChanges interceptor
  - `ITimestampable`: Auto-registers SaveChanges interceptor  
  - `ISluggable`: Automatically creates unique index on Slug column
- **Pattern Tracking**: New `swap-config.json` tracks applied patterns per entity
  - Enables safe pattern removal with intelligent cleanup
  - Prevents accidental removal when shared infrastructure is in use
  - JSON format: `{ "patterns": { "EntityName": ["pattern1", "pattern2"] } }`

### Added - Pattern Removal
- **Remove Pattern Command**: `swap generate pattern remove <pattern> <entity>`
  - Supported patterns: `softdelete`, `auditable`, `timestampable`, `sluggable`
  - Removes interface implementation from model
  - Removes pattern properties from model
  - Intelligently removes DbContext configuration only when safe
  - Updates or removes `swap-config.json` tracking
  - CLI aliases: `rm`, `delete`, `del`
- **Smart Cleanup Logic**:
  - Checks if other entities still use shared infrastructure before removal
  - Preserves IHttpContextAccessor if any entity uses IAuditable
  - Preserves interceptor if any entity uses IAuditable or ITimestampable
  - Removes global query filter only for the specific entity
- **Documentation**: Comprehensive removal guides added to wiki
  - `docs/cli/generate-pattern.md`: Full command reference with examples
  - `docs/features/patterns.md`: Removal workflows and safety notes

### Added - Pattern Compatibility Validation
- **Conflict Detection**: Prevents incompatible pattern combinations
  - Blocks applying `IAuditable` when `ITimestampable` exists (property overlap: CreatedAt, UpdatedAt)
  - Blocks applying `ITimestampable` when `IAuditable` exists (same conflict)
  - Clear error messages guide users to choose one or the other
- **CheckPatternCompatibilityAsync**: Pre-flight validation before pattern application

### Added - Roslyn-Based Code Modifications
- **Robust Code Generation**: Replaced regex-based edits with Roslyn SyntaxFactory
  - Uses `SyntaxFactory` with `NormalizeWhitespace()` for proper C# formatting
  - Handles complex DbContext modifications safely
  - Pattern removal with fallback cleanup for edge cases
  - Eliminates formatting issues and malformed code generation

### Added - Non-Blocking Migrations
- **Migration Flag**: New `--no-migrations` option for all generation commands
  - Allows entity/pattern generation without running migrations
  - Useful for batch operations or CI/CD pipelines
  - Clear error messages if migration fails
  - Continues command execution even if migration step fails

### Added - Cross-Platform Scripts
- **Bash Scripts**: Linux/Mac developer support
  - `scripts/pack-local.sh`: Build all framework packages locally
  - `scripts/reinstall-cli.sh`: Reinstall CLI from local feed
  - Mirrors existing PowerShell scripts for Windows users

### Changed - Templates
- **Controller Template Fix**: `EntityController.cs.template`
  - `ToggleSelectAll` now calls correct method: `Get{{EntityName}}List` instead of `Index`
  - Fixes CS1501 method overload mismatch error
- **Monolith Template Fix**: `Index.cshtml.template`
  - Removed duplicate sections causing Razor compilation errors
  - Removed inline partial rendering with null model (ArgumentNullException)
- **Todo Partial Template Fix**: `_TodoList.cshtml.template`
  - Added null safety check: `Model == null || !Model.Any()`
  - Prevents runtime exceptions on empty collections
- **Model Generator Fix**: `GenerateModelFromFields` in `GenerateControllerCommand.cs`
  - Fixed invalid semicolon generation for value type properties
  - Proper C# syntax for nullable and non-nullable properties

### Changed - Documentation
- **README.md**: Updated main documentation
  - Version badge: v0.0.14-prerelease → v0.1.0
  - Expanded Swap.Patterns section with auto-wiring details
  - Added pattern removal command documentation
  - Documented `swap-config.json` tracking system
- **Swap.Patterns README**: Enhanced package documentation
  - Quick Start section emphasizes CLI auto-wiring benefits
  - Removal command examples and safety notes
- **Wiki Documentation**: Comprehensive updates
  - Pattern removal workflows with step-by-step guides
  - Safety validation and common scenarios
  - Database column handling notes

### Changed - Package Versions
- **Swap.CLI**: 0.0.14 → 0.1.0
- **Swap.Htmx**: 0.0.1 → 0.1.0
- **Swap.Patterns**: 0.0.1 → 0.1.0
- **Swap.Testing**: 0.0.1 → 0.1.0

### Validated
- **Database Provider Support**:
  - ✅ SQL Server: Template validation completed
  - ✅ PostgreSQL: Template validation completed with proper connection strings
  - ✅ SQLite: Default provider, validated with Docker
- **Docker Support**: Multi-stage build structure validated
- **Test Coverage**: 267 tests passing (195 CLI/Htmx + 72 Patterns)
- **Build Quality**: Full solution builds successfully in Release configuration

## [0.0.14] - 2025-10-28

### Added - Seeder Enhancements
- **Pattern Integration**: `swap generate seed` now auto-excludes pattern properties managed by interceptors
  - Skips: `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy` (IAuditable)
  - Skips: `IsDeleted`, `DeletedAt`, `DeletedBy` (ISoftDeletable)
  - Skips: `Version` (IVersionable)
  - Skips: `IsVisible`, `VisibleFrom`, `VisibleUntil` (IVisibility)
  - Skips: `Position` (IOrderable)
- **Improved Slug Generation**: Unique slugs with random suffix for collision avoidance
- **Enhanced Documentation**: Seeder template now includes inline comments about pattern property handling
- **Better Relationship Handling**: Foreign key preloading with null safety guards

### Changed
- Seeder template adds XML doc comments and improved code comments
- README updated with pattern integration details and field intelligence improvements

## [0.0.13] - 2025-10-28

### Added - Visibility Pattern
- **Visibility Pattern**: Controllable visibility with optional time-based scheduling
  - `IVisibility` interface with `IsVisible`, `VisibleFrom`, `VisibleUntil`
  - Extensions: `Show()`, `Hide()`, `ShowNow()`, `ScheduleVisibility()`, `ScheduleVisibilityWindow()`, `IsCurrentlyVisible()`
  - Query helpers: `Visible()`, `Hidden()`, `Scheduled()`, `Expired()`
  - CLI command: `swap generate pattern visibility <entity>` with aliases
- **New Tests**: Added 6 tests covering manual toggle, scheduling, time window logic, and query filters
- **Docs**:
  - CLI README: added Visibility section with full examples
  - Wiki `features/patterns.md`: added Visibility guide with when-to-use and quick start

### Changed
- Pattern command help now mentions `visibility`

## [0.0.12] - 2025-10-28

### Added - Versionable Pattern
- **Versionable Pattern**: Automatic integer versioning on save
  - `IVersionable` interface with `Version`
  - `VersionInterceptor` initializes and increments version
  - Query helpers: `WithMinVersion()`, `WithVersion()`, `OrderByVersion()`
  - CLI command: `swap generate pattern versionable <entity>` with aliases
- **New Tests**: Added 3 tests covering initialization, increment, and query helpers
- **Docs**:
  - CLI README: added Versionable section
  - Wiki `features/patterns.md`: added Versionable guide

## [0.0.11] - 2025-10-28

### Added - Publishable Pattern
- **Publishable Pattern**: Draft/Published workflow
  - `IPublishable` interface with `IsPublished`, `PublishedAt`
  - Extensions: `Publish()`, `Unpublish()`, `Published()`, `Drafts()`, `PublishedAfter()`, `PublishedBefore()`
  - CLI command: `swap generate pattern publishable <entity>`
- **New Tests**: Added 3 tests for Publishable behavior (publish/unpublish and query helpers)
- **Docs**:
  - CLI README: added Publishable section
  - Wiki `features/patterns.md`: added Publishable guide

### Changed
- Pattern command help now mentions `publishable`

## [0.0.10] - 2025-10-28

### Added - Timestampable & Orderable Patterns
- **Timestampable Pattern**: Lightweight automatic timestamps
  - `ITimestampable` interface with `CreatedAt`, `UpdatedAt`
  - `TimestampInterceptor` sets timestamps on insert/update
  - CLI command: `swap generate pattern timestampable <entity>`
  - No `IHttpContextAccessor` required
- **Orderable Pattern**: Stable manual ordering support
  - `IOrderable` interface with `Position`
  - Extensions: `OrderByPosition()`, `OrderByPositionDescending()`, `GetNextPositionAsync()`, `ReorderAsync()`, `NormalizePositionsAsync()`
  - CLI command: `swap generate pattern orderable <entity>`
- **New Tests**: `Swap.Patterns.Tests` project with 5 passing tests
  - Verifies timestamp behavior and ordering helpers
  - In-memory database isolation to avoid cross-test contamination
- **Docs**:
  - CLI README: added Timestampable and Orderable sections with setup and examples
  - Wiki `features/patterns.md`: Timestampable and Orderable guides; updated Combining Patterns (don’t mix Auditable and Timestampable)

### Changed
- CLI pattern help updated to include `timestampable` and `orderable`
- Improved code-generation formatting via NormalizeWhitespace (earlier change validated)

## [0.0.9] - 2025-10-28

### Added - Auditable & Sluggable Patterns
- **Auditable Pattern**: Track entity creation and modification
  - `IAuditable` interface with CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
  - `AuditInterceptor` for automatic timestamp/user tracking
  - CLI command: `swap generate pattern auditable <entity>`
  - Integration with HttpContextAccessor for user tracking
  - Comprehensive documentation and setup guidance
- **Sluggable Pattern**: Generate SEO-friendly URL slugs
  - `ISluggable` interface with Slug property
  - `SlugGenerator` utility with collision detection
  - CLI command: `swap generate pattern sluggable <entity>`
  - Unicode normalization and automatic slug generation
  - Unique index configuration helpers
- **Pattern Composition**: All three patterns (soft delete, auditable, sluggable) can be combined on the same entity
- **Enhanced Documentation**:
  - Updated CLI README with auditable and sluggable sections
  - Updated wiki patterns.md with complete documentation
  - Added "Combining Patterns" sections with examples
  - Configuration guides for Program.cs, DbContext, interceptors

### Changed
- Expanded Swap.Patterns library from 1 to 3 production-ready patterns
- CLI pattern command now supports: `softdelete`, `auditable`, `sluggable`

## [0.0.8] - 2025-10-28

### Added - Entity Patterns
- **Swap.Patterns Library**: Battle-tested entity patterns for common scenarios
  - Soft Delete pattern with `ISoftDeletable` interface
  - Extension methods: `SoftDelete()`, `Restore()`, `IncludeDeleted()`, `OnlyDeleted()`
  - EF Core query filter configuration helper
  - Automatic exclusion of deleted records from queries
- **CLI Command**: `swap generate pattern softdelete <entity>`
  - Applies soft delete pattern to existing entities
  - Adds interface, properties, and using statements via Roslyn
  - Ensures Swap.Patterns package reference
  - Provides DbContext configuration guidance
- **Integration Tests**: 5 comprehensive tests for soft delete behavior
  - Query filter exclusion, property setting, restore functionality
  - `IncludeDeleted()` and `OnlyDeleted()` query extensions
- **Documentation**: Complete pattern library documentation
  - CLI wiki page for `generate pattern` command
  - Features wiki page with soft delete deep dive
  - Updated getting-started guide with pattern workflow
  - API reference and best practices

## [0.0.7] - 2025-10-28

### Added - HTMX Testing Framework
- **Swap.Testing Library**: Fluent testing framework for ASP.NET Core + HTMX applications
  - HTMX-aware client with automatic HX-Request headers
  - Rich HTML and HTMX attribute assertions
  - Snapshot testing with normalization and scrubbers
  - Validation helpers and form submission utilities
  - Typed HX header parsing (HX-Location JSON, HX-Trigger events, HX-Push-Url)
- **CLI Commands**: `swap generate test` and `swap generate factory`
  - `swap g test <controller>` generates integration test scaffolds with common HTMX scenarios
  - `swap g factory <entity>` generates Bogus-based test data factories with intelligent property mappings
- **Demo App**: HtmxTestingDemo showcasing end-to-end testing patterns
  - 5 passing integration tests covering partial forms, validation, triggers, and HX-Location
- **Documentation**: Complete testing framework guide with examples and API reference
  - Framework README with setup tips and best practices
  - Wiki documentation for testing features
  - CLI docs for test and factory generators

### Changed
- Enhanced CLI README with prominent testing framework section and examples
- Updated wiki CLI overview with testing command quick references

## [0.0.4] - 2025-10-27

### Added - Docker Support
- Docker-ready templates for generated apps with multi-stage Dockerfile
  - Build: .NET SDK 9.0 + Node 20 + libman CLI; automatic libman restore and Tailwind build
  - Runtime: ASP.NET minimal runtime; HTTP-only for Development
  - Preserves wwwroot assets across stages
- docker-compose templates with health checks and proper startup ordering
  - SQL Server: sqlcmd-based health check; service_healthy depends_on
  - PostgreSQL: pg_isready health check; service_healthy depends_on
  - SQLite: named volume for DB; shared named volume for DataProtection keys
- Data Protection keys persisted in containers (Program.cs template persists to /app/keys when running in Docker)
- .dockerignore template includes Migrations to enable auto-migration

### Fixed - HTMX Todo List Bug
- Views template: ensured the HTMX target element remains stable across swaps
  - Moved `<div id="todo-list">` wrapper into `_TodoList.cshtml.template`
  - Removed duplicate wrapper from `Index.cshtml.template`

### Changed
- New command no longer applies `dotnet ef database update` during scaffolding; migrations are created and applied at runtime in Development

### Docs
- README and wiki updated to reflect Docker-ready status, health checks, auto-migrations, and libman usage

### CI
- Added PR workflow to build the Docusaurus wiki on changes under `wiki/**`

### Fixed - HTMX Todo List Bug (2025-10-27)
- **_TodoList.cshtml.template**: Fixed HTMX target element disappearing
  - Moved `<div id="todo-list">` wrapper into partial view
  - Ensures target element persists after HTMX swaps
  - Resolves issue where add/delete operations would stop working after first use
- **Index.cshtml.template**: Removed duplicate wrapper div
  - Prevents nested target elements
  - Cleaner markup structure

### Added - Docker Support (2025-10-27)
- **Dockerfile Template**: Multi-stage build with libman and Node.js
  - Build stage: .NET SDK 9.0 + Node.js 20.x + libman CLI
  - Automatic `libman restore` for HTMX/DaisyUI libraries
  - Tailwind CSS compilation with `npm run build:css`
  - wwwroot preservation across build stages
  - Runtime stage: Minimal ASP.NET runtime image
  - HTTP-only configuration for Development environment
  - Optimized layer caching for faster rebuilds
- **docker-compose.yml Template**: Complete environment with health checks
  - SQLite: Named volumes for database and data protection keys
  - SQL Server: Health check with 30s start period, sqlcmd validation
  - PostgreSQL: Health check with 10s start period, pg_isready validation
  - App waits for database health before starting
  - Persistent volumes for all database providers
  - Network isolation for security
- **.dockerignore Template**: Optimized build context
  - Excludes bin/, obj/, node_modules/, etc.
  - Includes Migrations folder (required for auto-migration)
- **Auto-Migration**: Migrations apply automatically on container startup
  - Added to Program.cs template for Development environment
  - Eliminates manual `dotnet ef database update` steps
  - Works with all three database providers
- **Data Protection Keys**: Configured for Docker environments
  - Detects Docker via DOTNET_RUNNING_IN_CONTAINER environment variable
  - Persists keys to /app/keys volume
  - Prevents session/cookie issues across container restarts
- **README.md Template**: Docker usage instructions
  - Quick start commands for each database provider
  - Port mappings and connection information
  - Security warnings for default passwords
- **Wiki Documentation**: Comprehensive Docker deployment guide
  - Updated with health check configurations
  - Auto-migration documentation
  - libman restore instructions
  - Database-specific setup guides
  - Troubleshooting section
  - Production deployment examples
- **CLI new Command**: Database provider option
  - `--database` or `--db` flag for sqlite/sqlserver/postgres
  - Default: SQLite for simplicity

### Changed - Repository Rebrand & Documentation Update (2025-10-27)
- **Repository Rename**: netmx → swap across all files and URLs
- **Git Remote**: Updated to https://github.com/jdtoon/swap.git
- **Solution File**: Renamed netmx.sln → swap.sln
- **Product Vision**: Removed all framework comparisons (Rails, Laravel, ABP)
  - Shifted messaging from "Rails of .NET" to unique HTMX + DaisyUI value
  - Added "Why HTMX?" section comparing SPA vs HTMX architecture
  - Added "How Swap Works" 5-step meta-development process
- **Documentation Restructure**: 
  - Renamed HTMX-PATTERNS-LEARNED.md → PATTERNS-LIBRARY.md
  - Removed redundant philosophy content, kept pattern catalog
  - Enhanced THE-PRODUCT.md with new sections
- **Wiki Documentation**: Comprehensive Bootstrap → DaisyUI migration
  - Updated 9 documentation files with accurate DaisyUI examples
  - Fixed all repository URLs in Docusaurus config
  - Updated homepage features with new messaging
  - Fixed MDX compilation errors (JSX syntax in markdown)
  - Replaced placeholder SVG images with emoji icons (⚡🎯🎨)
- **README.md**: Complete rewrite to match actual CLI project
  - Removed incorrect framework/modular architecture content
  - Added accurate CLI commands and examples
  - Updated tech stack (ASP.NET Core MVC + HTMX + DaisyUI)
  - Fixed quick start guide and installation instructions
- **Test Suite**: All 136 tests passing after changes
- **Breaking**: None - purely documentation and branding changes

### Added - Week 1 Day 2: Server-Driven Bulk Operations Architecture (2025-10-27)
- **Session-Based State**: Selections stored in server session, not client-side
- **Session Middleware**: Added AddSession() + UseSession() to Program.cs template
- **SessionExtensions Helper**: JSON serialization helpers for session storage (SetObject/GetObject)
- **ToggleSelection Action**: Individual checkbox POST handler with session toggle
- **BulkActionsBar Action**: GET endpoint to render fresh bulk actions UI
- **Event-Based Coordination**: HX-Trigger: selectionChanged for automatic UI sync
- **Zero Client JavaScript**: Removed all bulk operation JavaScript (getSelectedIds, updateBulkActions, etc.)
- **Checkbox Partial View**: _EntityCheckboxCell.cshtml.template for OOB swaps
- **Bulk Actions Partial**: _BulkActionsBar.cshtml.template with event listener
- **HTMX Event Listeners**: hx-trigger="selectionChanged from:body" for reactive updates
- **Session Persistence**: Selections survive page refresh, navigation, and back button
- **BulkDelete Update**: Reads selected IDs from session instead of request body
- **FieldHelper Updates**: GenerateBulkActionsBar() now includes event attributes
- **Template Controller Updates**: EntityController.cs.template with new actions
- **GenerateControllerCommand Update**: Generates _BulkActionsBar.cshtml file
- **Documentation**: Comprehensive server-driven architecture section in bulk-operations.md
- **Architecture Benefits**: 
  - Session persistence (selections survive navigation)
  - No client-side state management needed
  - Automatic UI synchronization via HTMX events
  - Server-side validation and authorization
  - Simplified testing (no browser automation)
  - Declarative behavior via HTML attributes
- **Migration Path**: Documented upgrade guide for existing projects
- **Network Pattern**: Individual POST per checkbox + GET for bulk bar (event-driven)
- **CLI Rebuild**: Version 0.0.1 updated and reinstalled globally

### Added - Phase 4: Advanced List Management (Sorting, Filtering & Bulk Operations) ✅

#### Toast Notification System (2025-10-26)
- **Toastify.js Integration**: Replaced custom toast with battle-tested Toastify library (1.12.0)
- **LibMan Configuration**: Added toastify-js to libman.json for CDN delivery
- **Professional Styling**: Clean slide-in animations with close button
- **Server-Driven**: Server sends HX-Trigger, HTMX fires event, Toastify renders
- **Color-Coded Types**: Success (green), Error (red), Warning (orange), Info (blue)
- **Multiple Event Patterns**: showToast, showToastSuccess, showToastError, showToastWarning, showToastInfo
- **Auto-Dismiss**: 3.5 second duration with manual close option
- **Top-Right Position**: Non-intrusive placement with gravity/position config
- **Event Listener Fix**: Removed DOMContentLoaded wrapper for HTMX compatibility
- **Global Scope**: Toast handler in _Layout.cshtml available to all pages
- **CRUD Integration**: All Create/Update/Delete operations trigger toast notifications
- **Bulk Operations**: Bulk delete shows count in toast message
- **Consistent UX**: Matches CareStream implementation pattern

#### Phase 4.4: Bulk Operations (2025-10-26)
- **Selection Checkboxes**: Checkbox column added to all generated tables
- **Select All Functionality**: Header checkbox to select/deselect all items on page
- **Bulk Action Bar**: Alert bar appears when items selected, showing count
- **Bulk Delete**: Delete multiple items at once with confirmation dialog
- **Transaction Support**: BulkDelete uses database transactions for atomicity
- **Component-Level Updates**: Each checkbox targets itself for independent updates
- **JavaScript Functions**: toggleSelectAll(), updateBulkActions(), getSelectedIds(), clearSelection()
- **Confirmation Dialog**: Native confirm() with item count before deletion
- **Toast Notifications**: Success/error feedback after bulk operations
- **List Refresh**: Automatic table refresh after successful bulk delete
- **Clear Selection**: Button to deselect all items and hide action bar
- **Fetch API Integration**: POST to /BulkDelete with JSON array of IDs
- **Error Handling**: Try-catch with rollback, returns 500 on failure
- **HTMX Trigger**: Custom event to refresh list after deletion
- **DaisyUI Styling**: Consistent checkbox and alert styling
- **Template Updates**: _EntityList.cshtml.template with bulk operations UI
- **Controller Updates**: EntityController.cs.template with BulkDelete action
- **FieldHelper Methods**: 5 new generation methods for bulk operations
  - GenerateBulkSelectHeader(): Checkbox column header with select-all
  - GenerateBulkSelectCell(): Row checkbox with entity-specific class
  - GenerateBulkSelectionScript(): JavaScript for selection management
  - GenerateBulkActionsBar(): Alert bar with delete/clear buttons
  - GenerateBulkDeleteScript(): Bulk delete fetch logic with notifications
- **GenerateControllerCommand Updates**: Variables for bulk operation tokens
- **Example Usage**: Select items → "Delete Selected" → Confirm → Toast → Refresh
- **Note**: Refactored to server-driven architecture on 2025-10-27 (see above)

#### Phase 4.3: Field-Level Flags (2025-10-26)
- **Field Control Flags**: Developer control over sortable/filterable behavior per field
- **Sortable Flags**: `:sortable`/`:s` (enable), `:nosort`/`:ns` (disable)
- **Filterable Flag**: `:filterable`/`:f` (enable filtering for bool fields)
- **Smart Defaults**: Fields sortable by default, not filterable by default
- **Flag Syntax**: `FieldName:Type:Flags` (e.g., `SKU:string:ns`, `InStock:bool:f`)
- **Multiple Flags**: Support for comma-separated flags (e.g., `:ns,f`)
- **Conditional Generation**: Only sortable fields get clickable headers
- **Clean UI**: Non-sortable fields render as plain text headers
- **Filter Control**: Only filterable bool fields get dropdown filters
- **FieldDefinition Properties**: IsSortable (default true), IsFilterable (default false)
- **ParseFields Enhancement**: Flag parsing with validation and error messages
- **GenerateSortCases Update**: Filters fields by IsSortable before generating
- **GenerateTableHeader Update**: Conditional rendering based on IsSortable
- **Filter Methods Update**: All 6 filter helpers respect IsFilterable flag
- **Short Form Support**: `:s` = `:sortable`, `:ns` = `:nosort`, `:f` = `:filterable`
- **Example Usage**: `--fields "Name:string Price:decimal:ns InStock:bool:f CreatedDate:DateTime"`
- **Benefits**: Cleaner UIs, better UX, developer control, future-proof architecture

#### Phase 4.2: Boolean Filtering (2025-10-26)
- **Filter Parameters**: Controller Index action accepts bool? parameters for filtering
- **Filter Logic**: ApplyFilters method applies bool field filters to queries
- **Filter UI**: DaisyUI dropdowns with All/Yes/No options
- **HTMX Integration**: Filter changes update table without page reload
- **State Preservation**: Filter state tracked in view model and HTMX requests
- **Automatic Generation**: Filters generated for all bool fields automatically
- **FieldHelper Methods**: GenerateFilterParameters, GenerateFilterCases, GenerateFilterControls, GenerateFilterSection
- **View Model Updates**: Added Filters dictionary property
- **Index View Updates**: Filter section with responsive grid layout

#### Phase 4.1: Column Sorting (2025-10-26)
- **Sortable Headers**: All table columns now clickable for sorting
- **Sort Indicators**: Visual arrows (↑ ascending, ↓ descending)
- **Toggle Behavior**: Click same column to reverse sort order
- **HTMX Integration**: Sorting updates table without page reload
- **State Preservation**: Sort persists during search, filter, and pagination
- **All Field Types**: Support for string, int, decimal, bool, DateTime sorting
- **ApplySorting Method**: Dynamic sorting with switch expression
- **FieldHelper.GenerateSortCases**: Generates sort switch cases
- **FieldHelper.GenerateTableHeader**: Creates sortable headers with HTMX

### Added - Phase 3: HTMX-Powered CRUD with Pagination & Search ✅

#### Phase 3.5: Complete CRUD with Pagination (2025-10-25)
- **Pagination System**: Configurable page sizes (10, 25, 50, 100)
- **Page Navigation**: Previous, Next, First, Last buttons
- **Page Info Display**: "Showing X-Y of Z items"
- **PaginationDto**: Comprehensive pagination model with HTMX support
- **State Preservation**: Maintains pagination across operations
- **Search Integration**: Real-time search with 500ms debounce
- **Multiple Field Search**: Searches across all string fields
- **HTMX Partial Updates**: Zero page reloads for all operations

#### Phase 3.4: HTMX Modal CRUD (2025-10-25)
- **Modal-Based Create**: HTMX-powered create modal
- **Modal-Based Edit**: HTMX-powered edit modal with prefill
- **Modal-Based Details**: Read-only details view in modal
- **Delete Confirmation**: In-list delete with HTMX confirmation
- **Toast Notifications**: Success/error feedback system
- **Validation**: Client and server-side with inline errors
- **Partial Views**: Modular view structure (_Create, _Edit, _Details, _Form)

#### Phase 3.3: Field Type Support (2025-10-25)
- **11 Field Types**: string, int, long, short, byte, bool, float, double, decimal, DateTime, Guid
- **Nullable Fields**: Support for nullable reference and value types
- **DateTime Defaults**: Auto-populated with DateTime.Now
- **Decimal Precision**: step="any" for decimal inputs
- **Boolean Badges**: Visual Yes/No badges in lists
- **Formatted Display**: Proper formatting for dates and decimals
- **FieldHelper Class**: Centralized field generation logic

#### Phase 3.2: Template System (2025-10-25)
- **Template-Based Generation**: Moved from inline strings to .template files
- **Modular Templates**: Separate templates for controller, views, models
- **Token Replacement**: {{EntityName}}, {{Fields}}, {{TableHeaders}}, etc.
- **Views Subfolder**: Organized template structure
- **PreserveNewest**: Templates copied to output directory
- **Reusable Partials**: _PaginationControls, _Form shared components

#### Phase 3.1: CLI Field Parser (2025-10-25)
- **FieldDefinition Model**: Name, Type, IsNullable properties
- **Field Parsing**: Parse "Name:Type?" format from command line
- **GenerateControllerCommand**: New command with --fields option
- **Automatic DbSet Update**: Adds entity to DbContext if not exists
- **Duplicate Detection**: Warns if DbSet already exists
- **Complete Generation**: Controller, Model, Views, ViewModels in one command

### Fixed

#### Phase 4.4 Bug Fixes (2025-10-26)
- **Monolith Template HTMX**: Fixed component-level targeting for todo items
  - Created _TodoItem.cshtml.template for single-item rendering
  - Each item has unique ID (todo-item-{id})
  - Checkbox targets itself with hx-target="#todo-item-{id}"
  - Controller has ToggleItem action returning single item
  - Prevents HTMX target errors on subsequent clicks
- **CLI Error Handling**: Improved setup command error reporting
  - Added try-catch around npm install, libman restore, npm build:css
  - Shows warnings instead of silent failures
  - Provides helpful tips for manual setup
  - Updated "Next steps" with manual commands
- **Missing HTMX Library**: Documented libman restore requirement
  - Monolith template requires libman restore for HTMX
  - Added to setup instructions in NewCommand
  - Users warned if setup commands fail

#### Earlier Bug Fixes
- **Duplicate DbSet Bug**: Now checks fully qualified names (DbSet<Project.Models.Entity>)
- **Template Paths**: Fixed {{entityNameLower}} token in hx-target attributes
- **Razor Syntax**: Removed inline C# from HTML attributes to fix compilation errors

### Changed
- **UI Framework**: Migrated from Bootstrap to DaisyUI/Tailwind CSS
- **Architecture**: HTMX-first approach for all interactions
- **View Structure**: Partial views for better modularity
- **Controller Pattern**: Async/await with HTMX headers
- **Response Pattern**: HTMX triggers instead of redirects

### Documentation Needed
- **CLI Documentation**: Update generate-controller.md with new features
- **Field Types Guide**: Document all 11 supported field types
- **HTMX Patterns**: Document HTMX integration patterns
- **Pagination Guide**: Document pagination implementation
- **Sorting Guide**: Document column sorting usage
- **Filtering Guide**: Document boolean filtering usage

### Planned - Phase 4: Remaining Features
- **Phase 4.5**: Export (CSV/Excel) - Generate downloadable data exports
- **Phase 4.6**: Advanced Search - Multi-field search with operators
- **Phase 4.7**: Audit Trail - Track all entity changes with timestamps
- **Phase 4.5**: Advanced Search (multi-field with operators)
- **Phase 4.6**: Audit Trail (CreatedBy, ModifiedBy, timestamps)
- **Field Flags**: --sortable and --filterable flags for field-level control

---

## [0.1.0-dev] - 2025-10-21

### Added - Phase 2D: Seeder Generation Complete ✅

#### Seeder Generation (Commit TBD)
- **SeederGenerator** class for database seeder generation
- **SeederGenerationOptions** model for configuration
- **GenerateSeederCommand** for CLI integration
- Repository injection pattern (IQueryableRepository<TEntity, TKey>)
- Automatic duplicate check (GetCountAsync > 0)
- Sample data template with 3 items
- Module context support (modules/{Module}.Application/Seeding/)
- App context support (src/{App}.Web/Seeding/)
- Smart seeder name handling (removes duplicate "Seeder" suffix)
- Next steps guidance after generation
- **Tests**: 14 passing (11 generator tests + 3 command tests)
- **Files**: 
  - SeederGenerator.cs (87 lines)
  - SeederGenerationOptions.cs (35 lines)
  - GenerateSeederCommand.cs (96 lines)
  - SeederGeneratorTests.cs (181 lines)
  - GenerateSeederCommandTests.cs (31 lines)

**Usage**:
```bash
# Generate seeder in app context
Swap generate seeder ProductSeeder

# Generate seeder in module context
Swap generate seeder PermissionSeeder -m Authorization
```

**Generated Code Features**:
- ISeeder interface implementation
- Repository dependency injection
- XML documentation comments
- Async SeedAsync method
- Duplicate check guard clause
- Sample data array template
- InsertAsync loop for batch insertion

**Statistics**:
- Total tests: 134 (120 from Phase 2C + 14 new)
- Code added: ~430 lines (production + tests)
- Time investment: ~2 hours
- Status: ✅ Production-ready

### Added - Phase 2C: Code Generation Complete ✅

#### Part 1: EntityGenerator (Commit 5f23a3b)
- **EntityGenerator** class for DDD entity generation
- Private setters for encapsulation
- Private constructor for EF Core
- Public constructor with validation
- SetMethods for updates (UpdateDetails, Activate, Deactivate)
- Audit fields support (CreatedAt, UpdatedAt)
- Soft delete support (IsDeleted, DeletedAt)
- Module vs app context handling
- **Tests**: 14 passing
- **Files**: EntityGenerator.cs (350 lines), EntityGeneratorTests.cs (320 lines)

#### Part 2: DtoGenerator (Commit 7e6d9fa)
- **DtoGenerator** class for DTO generation
- ReadDto generation (for queries)
- CreateDto generation (for creation with validation)
- UpdateDto generation (for updates with validation)
- FilterDto generation (for search/filter with range filters)
- PagedResultDto generation (for pagination with TotalPages)
- Validation attributes (Required, MaxLength, Range)
- Range filter properties (PriceMin/PriceMax, StartDate/EndDate)
- **Tests**: 12 passing
- **Files**: DtoGenerator.cs (500 lines), DtoGeneratorTests.cs (380 lines)

#### Part 3: ServiceGenerator (Commit 3ab7445)
- **ServiceGenerator** class for service layer generation
- Service interface generation (IEntityService)
- Service implementation generation (EntityService)
- Basic CRUD operations (GetAll, GetById, Create, Update, Delete)
- Pagination logic (Skip/Take)
- Search with OR logic (Contains on multiple properties)
- Exact match filters (property == value)
- Range filters (PriceMin/Max, StartDate/EndDate)
- Sorting with switch expression (OrderBy/OrderByDescending)
- Module context uses repository pattern
- App context uses DbContext directly
- **Tests**: 17 passing
- **Files**: ServiceGenerator.cs (550 lines), ServiceGeneratorTests.cs (440 lines)

#### Part 4: ControllerGenerator (Commit d10da27)
- **ControllerGenerator** class for MVC controller generation
- Index action (full page view)
- List action (partial view for HTMX with pagination/search/filter/sort)
- Create actions (GET for form, POST for submit)
- Edit actions (GET to load entity, POST to update)
- Delete action (DELETE with HxReswap)
- HTMX integration (HxTrigger for events, HxReswap for delete)
- Type-safe events via DomainEvents constants
- Pagination/search/filter/sort parameters
- **Tests**: 15 passing
- **Files**: ControllerGenerator.cs (300 lines), ControllerGeneratorTests.cs (360 lines)

#### Part 5: ViewGenerator (Commit 0ac38de)
- **ViewGenerator** class for Razor view generation
- Index.cshtml generation (main page with search, filters, list container, modal container)
- _List.cshtml generation (table partial with sortable headers, pagination, actions)
- _Form.cshtml generation (modal form with property-specific inputs, validation)
- HTMX attributes (hx-get, hx-post, hx-delete, hx-trigger, hx-target, hx-include, hx-confirm)
- Bulma CSS styling (container, section, title, box, field, control, input, button, table, modal, pagination)
- Font Awesome icons (fa-box, fa-search, fa-plus, fa-edit, fa-trash, fa-check, fa-times, fa-sort)
- Debounced search (keyup changed delay:500ms)
- Sortable table headers with icons
- Pagination controls (Previous/Next)
- Property-specific formatting (decimal → currency, DateTime → "g", bool → icons)
- Modal auto-close on success
- **Tests**: 20 passing
- **Files**: ViewGenerator.cs (600 lines), ViewGeneratorTests.cs (470 lines)

#### Part 6: Integration (Commit 2ac2d6d)
- **GenerateFeatureCommand** rewrite to orchestrate all generators
- Removed 961 lines of inline code generation
- Added 319 lines of orchestration logic
- EntityGenerationOptions for clean configuration
- File writing logic (directory creation, file writing)
- Module vs app context handling
- CLI flag mapping (--search, --export, --migrate)
- Auto-migration support (--migrate flag)
- Success messaging with file list
- Next steps guidance
- Advanced features display (pagination, search, filter, sort, export)
- **Tests**: 120/120 passing (100%)
- **Files**: GenerateFeatureCommand.cs (319 lines), Program.cs (updated)

### Added - Phase 2B: Property Parsing (October 2025)
- **PropertyParser** class for parsing CLI property definitions
- **PropertyDefinition** model with comprehensive metadata
- Type mappings (string, int, decimal, datetime, bool, guid, enum, etc.)
- String constraints (required, minlength, maxlength)
- Numeric constraints (min, max, range)
- Decimal precision/scale support
- Default values
- Enum support (with values)
- Foreign key support (with navigation)
- Collection support (one-to-many, many-to-many)
- **Tests**: 21 passing
- **Files**: PropertyParser.cs (314 lines), PropertyDefinition.cs (150 lines)

### Added - Phase 2A: DB Commands (October 2025)
- `Swap db migrate <name>` - Create database migration
- `Swap db update` - Apply pending migrations
- `Swap db rollback` - Undo last migration
- `Swap db reset` - Drop and recreate database
- `Swap db seed` - Run database seeders (placeholder)
- `Swap db status` - Show migration status
- **DbContextInjector** for automatic DbSet injection
- **MigrationRunner** for EF Core migration automation
- **Tests**: 8 passing
- **Files**: DbCommands.cs, DbContextInjector.cs (200 lines), MigrationRunner.cs (180 lines)

### Added - Phase 1: Foundation (September-October 2025)
- Basic CLI structure with System.CommandLine
- `Swap create module <name>` command
- Module scaffolding (4-layer architecture)
- `Swap generate feature <name>` command (basic)
- `Swap generate crud <name>` alias (deprecated)
- ConsoleHelper for formatted output
- Zero-warning builds
- NuGet packaging support

### Changed
- **Program.cs**: Updated CLI flag mapping for new generator system
  - `--search` flag now enables pagination (20 items), search (Name, Description), and sorting (Name, CreatedAt)
  - `--export` flag now adds CSV export format
  - `--migrate` flag triggers auto-migration workflow
  - Future: `--props` flag for custom property definitions

- **GenerateFeatureCommand**: Complete rewrite
  - From: 1,151 lines of inline generation code
  - To: 319 lines of orchestration logic
  - Improvement: 832 lines removed, cleaner separation of concerns

### Fixed
- Property parser capitalization for range filters (PriceMin/Max not priceMin/Max)
- Module context now correctly uses repository pattern
- App context now correctly uses DbContext directly
- HTMX event names now use kebab-case (product-created not ProductCreated)

### Performance
- Feature generation: ~100ms for 13 files
- All generators are pure functions (no I/O, no dependencies)
- Test suite runs in ~4 seconds (120 tests)

---

## Statistics (Phase 2C)

### Code Metrics
- **Production Code**: ~3,000 lines (all generators)
- **Test Code**: ~2,500 lines (comprehensive coverage)
- **Command Code**: 319 lines (orchestration)
- **Total**: ~5,800 lines

### Test Coverage
- EntityGeneratorTests: 14 tests
- DtoGeneratorTests: 12 tests
- ServiceGeneratorTests: 17 tests
- ControllerGeneratorTests: 15 tests
- ViewGeneratorTests: 20 tests
- PropertyParserTests: 21 tests
- DbCommandsTests: 8 tests
- Other tests: 13 tests
- **Total**: 120 tests (100% passing)

### Commits (Phase 2C)
1. `5f23a3b` - EntityGenerator (Part 1) - Oct 21, 2025
2. `7e6d9fa` - DtoGenerator (Part 2) - Oct 21, 2025
3. `3ab7445` - ServiceGenerator (Part 3) - Oct 21, 2025
4. `d10da27` - ControllerGenerator (Part 4) - Oct 21, 2025
5. `0ac38de` - ViewGenerator (Part 5) - Oct 21, 2025
6. `2ac2d6d` - Integration (Part 6) - Oct 21, 2025

### Time Investment
- Development: ~8 hours (all 6 parts)
- Testing: ~4 hours (120 comprehensive tests)
- Documentation: ~2 hours (README, ARCHITECTURE, CHANGELOG)
- **Total**: ~14 hours

### Time Savings
- **Per Feature Generated**: 4-6 hours saved (vs manual creation)
- **ROI**: After 3-4 feature generations, time savings exceed development time

---

## [0.0.1-dev] - 2025-09-15

### Added
- Initial CLI project structure
- Basic `Swap` command
- Development setup documentation

---

## Versioning Strategy

### Pre-Release Versions (develop branch)
Format: `0.1.0-dev.YYYYMMDD.sha`
- Example: `0.1.0-dev.20251021.2ac2d6d`
- Published to NuGet.org as pre-release
- Installed with: `dotnet tool install --global Swap.CLI --version "0.1.0-dev*"`

### Stable Versions (main branch)
Format: `0.1.0`
- Example: `0.1.0` (first stable release)
- Published to NuGet.org as stable
- Installed with: `dotnet tool install --global Swap.CLI`

### Semantic Versioning Rules
- **Major** (x.0.0): Breaking changes
- **Minor** (0.x.0): New features (backwards compatible)
- **Patch** (0.0.x): Bug fixes

---

## Upgrade Guide

### From 0.0.x to 0.1.0

**New Commands**:
```bash
# Old (still works, deprecated)
Swap generate crud Product

# New (recommended)
Swap generate feature Product
```

**New Flags**:
```bash
# Enable pagination, search, sorting
Swap generate feature Product --search

# Enable CSV export
Swap generate feature Product --export

# Auto-migration (app context only)
Swap generate feature Product --migrate
```

**Breaking Changes**:
- None (fully backwards compatible)

**Deprecations**:
- `Swap generate crud` is deprecated, use `Swap generate feature` instead

---

## Future Roadmap

### Phase 2D: Seeders (Week 4)
- `Swap generate seeder <name>` command
- Seeder class scaffolding
- Seeder execution in `Swap db seed`
- Seeder ordering (1_, 2_, 3_)
- Idempotent seeding

### Phase 3: Custom Properties (Weeks 5-6)
- `--props` flag for custom property definitions
- Property parsing from CLI
- Support for all property types (enums, FKs, collections)
- Navigation property generation
- Foreign key constraint generation

### Phase 4: Templates (Weeks 7-8)
- Customizable code templates (Liquid)
- Template inheritance
- User templates directory (~/.Swap/templates/)
- Template variables (EntityName, Properties, Options)

### Phase 5: AI-Assisted Generation (Week 9+)
- Natural language feature generation
- AI-powered property inference
- Confirmation before generation
- Learning from user patterns

---

## Contributing

See [CONTRIBUTING.md](../CONTRIBUTING.md) for guidelines on:
- Reporting bugs
- Suggesting features
- Submitting pull requests
- Running tests locally
- Extending generators

---

## License

MIT License - See [LICENSE](../LICENSE) for details.

---

**Maintained By**: Swap Team  
**Repository**: https://github.com/jdtoon/Swap  
**Documentation**: https://Swap.dev/docs/cli (planned)  
**Issues**: https://github.com/jdtoon/Swap/issues

---

**Last Updated**: October 21, 2025  
**Current Version**: 0.1.0-dev  
**Status**: Phase 2C Complete ✅


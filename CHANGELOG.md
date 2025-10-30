# Swap CLI Changelog

All notable changes to the Swap CLI will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

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
- **New Tests**: Added 4 tests for auth command structure (name, aliases, options)
- **CLI Options**: `--dry-run`, `--force`, `--project` support
- **Documentation**: Updated CLI README with complete auth scaffolding guide and usage examples

### Added - GitHub Release Automation
- **Automated Release on PR Merge**: When PR is merged to main, workflow automatically:
  1. Extracts version from `Swap.CLI.csproj`
  2. Builds and tests all packages
  3. Publishes all 4 packages to NuGet.org
  4. Creates git tag (`v0.1.0`, etc.)
  5. Extracts release notes from CHANGELOG.md
  6. Creates GitHub release with packages attached
  7. Marks pre-release versions (e.g., `v0.1.0-alpha`) automatically
- **Idempotent**: Checks if tag/version already exists and skips if so
- **Zero manual steps**: No manual tagging or release creation needed

### Added - Local NuGet Development Support  
- **--local-nuget Flag**: New option for `swap new` command (framework development only)
  - `swap new MyApp --local-nuget` creates project using local NuGet feed
  - Automatically generates nuget.config with relative path to `.nuget/local` directory
  - Verifies local feed exists before creating project
  - Intended exclusively for testing Swap framework changes in testApps/
  - Enables rapid iteration without publishing packages to NuGet.org

### Changed - Template Cleanup
- **Removed Default nuget.config**: Removed nuget.config.template from monolith template
  - Now only created when explicitly using `--local-nuget` flag
  - Prevents confusion for normal users (they don't need local feed configuration)
  - Cleaner project generation for production use cases

### Fixed - Documentation
- **Wiki Build Errors**: Fixed broken anchor links in patterns.md
  - Updated `#auditable-pattern` → `#auditable`
  - Updated `#sluggable-pattern` → `#sluggable`
  - Wiki now builds without warnings (except minor broken anchor notices)

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


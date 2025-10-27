# Swap CLI Changelog

All notable changes to the Swap CLI will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

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


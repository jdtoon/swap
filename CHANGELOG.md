# NetMX CLI Changelog

All notable changes to the NetMX CLI will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### Added
- Phase 2D: Seeder generation (planned)
- Phase 3: Custom property definitions via --props flag (planned)
- Phase 4: Template system for code customization (planned)
- Phase 5: AI-assisted feature generation (planned)

---

## [0.1.0-dev] - 2025-10-21

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
- `netmx db migrate <name>` - Create database migration
- `netmx db update` - Apply pending migrations
- `netmx db rollback` - Undo last migration
- `netmx db reset` - Drop and recreate database
- `netmx db seed` - Run database seeders (placeholder)
- `netmx db status` - Show migration status
- **DbContextInjector** for automatic DbSet injection
- **MigrationRunner** for EF Core migration automation
- **Tests**: 8 passing
- **Files**: DbCommands.cs, DbContextInjector.cs (200 lines), MigrationRunner.cs (180 lines)

### Added - Phase 1: Foundation (September-October 2025)
- Basic CLI structure with System.CommandLine
- `netmx create module <name>` command
- Module scaffolding (4-layer architecture)
- `netmx generate feature <name>` command (basic)
- `netmx generate crud <name>` alias (deprecated)
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
- Basic `netmx` command
- Development setup documentation

---

## Versioning Strategy

### Pre-Release Versions (develop branch)
Format: `0.1.0-dev.YYYYMMDD.sha`
- Example: `0.1.0-dev.20251021.2ac2d6d`
- Published to NuGet.org as pre-release
- Installed with: `dotnet tool install --global NetMX.CLI --version "0.1.0-dev*"`

### Stable Versions (main branch)
Format: `0.1.0`
- Example: `0.1.0` (first stable release)
- Published to NuGet.org as stable
- Installed with: `dotnet tool install --global NetMX.CLI`

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
netmx generate crud Product

# New (recommended)
netmx generate feature Product
```

**New Flags**:
```bash
# Enable pagination, search, sorting
netmx generate feature Product --search

# Enable CSV export
netmx generate feature Product --export

# Auto-migration (app context only)
netmx generate feature Product --migrate
```

**Breaking Changes**:
- None (fully backwards compatible)

**Deprecations**:
- `netmx generate crud` is deprecated, use `netmx generate feature` instead

---

## Future Roadmap

### Phase 2D: Seeders (Week 4)
- `netmx generate seeder <name>` command
- Seeder class scaffolding
- Seeder execution in `netmx db seed`
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
- User templates directory (~/.netmx/templates/)
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

**Maintained By**: NetMX Team  
**Repository**: https://github.com/toonjd/netmx  
**Documentation**: https://netmx.dev/docs/cli (planned)  
**Issues**: https://github.com/toonjd/netmx/issues

---

**Last Updated**: October 21, 2025  
**Current Version**: 0.1.0-dev  
**Status**: Phase 2C Complete ✅
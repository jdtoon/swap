# Documentation Backlog - October 26, 2025

## Priority: HIGH - CLI Documentation Updates Needed

The following documentation needs to be created/updated to reflect Phase 3 and Phase 4 implementations:

### 1. CLI Command Documentation

**File**: `wiki/docs/cli/generate-controller.md`
**Status**: Needs complete rewrite
**Current State**: Outdated (references Bootstrap, traditional forms, no HTMX)
**Required Updates**:
- Update synopsis to show `--fields` requirement
- Document all 11 supported field types
- Add HTMX modal CRUD examples
- Document pagination features
- Document search features
- Document column sorting
- Document boolean filtering
- Update all code examples to match current implementation
- Add screenshots of generated UI
- Document field flags (once implemented)

### 2. Feature Guides (New Documents Needed)

#### a. Pagination Guide
**File**: `wiki/docs/features/pagination.md` (create)
**Content**:
- How pagination works
- Page size options
- Navigation controls
- HTMX integration
- State preservation
- Customization options

#### b. Search Guide
**File**: `wiki/docs/features/search.md` (create)
**Content**:
- Real-time search with debouncing
- Multi-field searching
- Search logic customization
- Performance considerations
- HTMX integration

#### c. Sorting Guide
**File**: `wiki/docs/features/sorting.md` (create)
**Content**:
- Clickable column headers
- Sort direction toggle
- Visual indicators
- Custom sort logic
- Multi-column sorting (future)

#### d. Filtering Guide
**File**: `wiki/docs/features/filtering.md` (create)
**Content**:
- Boolean field filtering
- Dropdown UI
- Filter combinations
- Custom filters
- Enum filtering (future)

### 3. HTMX Integration Guide

**File**: `wiki/docs/concepts/htmx-integration.md` (create)
**Content**:
- Why HTMX was chosen
- HTMX patterns used
- Modal implementation
- Partial view updates
- HTMX headers and triggers
- Toast notifications
- Best practices

### 4. Field Types Reference

**File**: `wiki/docs/reference/field-types.md` (create)
**Content**:
- Complete list of 11 types
- Display formats (form, list, details)
- Validation rules
- Nullable behavior
- Default values
- Sortability
- Filterability

### 5. Template System Documentation

**File**: `wiki/docs/concepts/template-system.md` (create)
**Content**:
- Template file structure
- Token replacement
- Custom templates
- FieldHelper methods
- Template organization
- Extending templates

### 6. Migration Guide

**File**: `wiki/docs/guides/migration-from-phase2.md` (create)
**Content**:
- Differences from Phase 2
- Breaking changes
- Migration steps
- UI framework change (Bootstrap → DaisyUI)
- HTMX requirements

### 7. Update README.md

**File**: `README.md`
**Updates Needed**:
- Update feature list
- Add Phase 3/4 highlights
- Update screenshots
- Update quick start example

### 8. Update Getting Started

**File**: `wiki/docs/getting-started/first-project.md`
**Updates Needed**:
- Update example to use `--fields`
- Show new UI features
- Update workflow
- Add pagination/search/sort examples

## Implementation Strategy

1. **Week 1**: Update CHANGELOG.md ✅ (COMPLETE)
2. **Week 1**: Rewrite generate-controller.md (1-2 hours)
3. **Week 2**: Create feature guides (pagination, search, sorting, filtering) (2-3 hours)
4. **Week 2**: Create HTMX integration guide (1-2 hours)
5. **Week 3**: Create field types reference (1 hour)
6. **Week 3**: Create template system docs (1 hour)
7. **Week 4**: Create migration guide (1 hour)
8. **Week 4**: Update README and getting started (1 hour)

## Notes

- All documentation should include code examples
- Add screenshots/GIFs where helpful
- Keep examples practical and testable
- Link between related docs
- Update sidebar navigation as new docs are added
- Consider video tutorials for complex features

## Automated Documentation

Consider using tools to:
- Generate API reference from code comments
- Auto-generate field type matrix
- Extract examples from tests
- Keep docs in sync with code

## Review Schedule

- Review docs after each phase completion
- Monthly documentation audit
- Update docs as part of feature PRs (going forward)

## Owner

Assigned to: Development Team
Due Date: Rolling updates
Priority: HIGH - Blocking user adoption

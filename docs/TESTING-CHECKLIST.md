# Testing Checklist

**Last Updated**: October 29, 2025  
**Purpose**: Comprehensive validation of Swap framework before release

---

## 🎯 Testing Strategy

This checklist ensures all components of Swap work correctly together:
- ✅ CLI commands
- ✅ Framework packages (Swap.Htmx, Swap.Patterns, Swap.Testing)
- ✅ Generated code
- ✅ Templates
- ✅ Docker support
- ✅ Database providers
- ✅ Pattern implementations

---

## 📋 Pre-Testing Setup

### Local Environment Preparation

- [ ] Clean build of entire solution
  ```bash
  dotnet clean
  dotnet build
  ```

- [ ] Pack framework packages to local NuGet feed
  ```bash
  .\scripts\pack-local.ps1
  ```

- [ ] Reinstall CLI from local feed
  ```bash
  .\scripts\reinstall-cli.ps1
  ```

- [ ] Verify CLI version
  ```bash
  swap --version  # Should show latest version
  ```

---

## 🧪 Unit Tests

### Framework Tests

- [ ] **Swap.Htmx Tests** (35 tests total)
  ```bash
  cd framework/Swap.Htmx.Tests
  dotnet test
  ```
  - [ ] SwapController tests (7 tests)
  - [ ] Extension methods tests (18 tests)
  - [ ] Middleware tests (10 tests)

- [ ] **Swap.Patterns Tests**
  ```bash
  cd framework/Swap.Patterns.Tests
  dotnet test
  ```
  - [ ] Auditable pattern
  - [ ] Orderable pattern
  - [ ] Publishable pattern
  - [ ] Sluggable pattern
  - [ ] SoftDelete pattern
  - [ ] Timestampable pattern
  - [ ] Versionable pattern
  - [ ] Visibility pattern

- [ ] **Swap.CLI Tests**
  ```bash
  cd tools/Swap.CLI.Tests
  dotnet test
  ```

---

## 🚀 CLI Commands Testing

### Project Creation

#### SQLite (Default)
- [ ] Create new project with SQLite
  ```bash
  swap new TestSqlite --database sqlite
  cd TestSqlite
  ```
- [ ] Verify project structure
- [ ] Verify nuget.config points to local feed
- [ ] Run migrations
  ```bash
  dotnet ef database update
  ```
- [ ] Run application
  ```bash
  dotnet run
  ```
- [ ] Visit http://localhost:5000
- [ ] Test todo list functionality:
  - [ ] Add todo item
  - [ ] Toggle completion
  - [ ] Delete todo item
  - [ ] Input clears after adding

#### PostgreSQL
- [ ] Create new project with PostgreSQL
  ```bash
  swap new TestPostgres --database postgres
  cd TestPostgres
  ```
- [ ] Update connection string in appsettings.json
- [ ] Run migrations
- [ ] Run application
- [ ] Test basic functionality

#### SQL Server
- [ ] Create new project with SQL Server
  ```bash
  swap new TestSqlServer --database sqlserver
  cd TestSqlServer
  ```
- [ ] Update connection string
- [ ] Run migrations
- [ ] Run application
- [ ] Test basic functionality

---

### Controller Generation

#### Basic Controller
- [ ] Generate simple controller
  ```bash
  swap generate controller Product --fields "Name:string Price:decimal Stock:int"
  ```
- [ ] Verify generated files:
  - [ ] Controllers/ProductController.cs
  - [ ] Models/Product.cs
  - [ ] ViewModels/ProductListViewModel.cs
  - [ ] Views/Product/Index.cshtml
  - [ ] Views/Product/_ProductList.cshtml
  - [ ] Views/Product/_ProductCreateModal.cshtml
  - [ ] Views/Product/_ProductEditModal.cshtml
  - [ ] Views/Product/_ProductDetailsModal.cshtml
  - [ ] Views/Product/_ProductDeleteModal.cshtml

- [ ] Verify migration created automatically
- [ ] Apply migration
  ```bash
  dotnet ef database update
  ```
- [ ] Run application
- [ ] Test CRUD operations:
  - [ ] Navigate to /Product
  - [ ] Verify hero section loads
  - [ ] Verify search bar present
  - [ ] Verify product list loads (empty state)
  - [ ] Click "Create Product"
  - [ ] Fill form and submit
  - [ ] Verify product appears in list
  - [ ] Verify input clears after create
  - [ ] Click "Edit" on product
  - [ ] Modify and save
  - [ ] Verify changes appear in list
  - [ ] Click "Details" on product
  - [ ] Verify all fields displayed
  - [ ] Click "Delete" on product
  - [ ] Confirm deletion
  - [ ] Verify product removed from list

#### Controller with Navigation
- [ ] Generate controller with nav link
  ```bash
  swap generate controller Customer --fields "Name:string Email:string Phone:string?" --add-nav
  ```
- [ ] Verify navigation link added to _Layout.cshtml
- [ ] Run application
- [ ] Click navigation link
- [ ] Verify page loads without full refresh (HTMX)
- [ ] Verify browser URL updates
- [ ] Verify browser back button works

#### Controller with Complex Fields
- [ ] Generate controller with various field types
  ```bash
  swap generate controller Order --fields "OrderNumber:string Total:decimal OrderDate:DateTime Status:string IsShipped:bool Notes:string?"
  ```
- [ ] Verify all field types rendered correctly in:
  - [ ] Create modal
  - [ ] Edit modal
  - [ ] Details modal
  - [ ] List view
- [ ] Test sorting on each field
- [ ] Test searching
- [ ] Test filtering (if applicable)

---

### Pattern Generation

#### Auditable Pattern
- [ ] Add to existing model
  ```bash
  swap generate pattern auditable Product
  ```
- [ ] Verify IAuditable interface added
- [ ] Verify properties added (CreatedBy, ModifiedBy, CreatedAt, ModifiedAt)
- [ ] Create/edit product
- [ ] Verify audit fields populated

#### Publishable Pattern
- [ ] Add to model
  ```bash
  swap generate pattern publishable BlogPost --fields "Title:string Content:string"
  ```
- [ ] Verify publish/unpublish functionality
- [ ] Test publish status filtering

#### SoftDelete Pattern
- [ ] Add to model
  ```bash
  swap generate pattern softdelete Customer
  ```
- [ ] Delete a record
- [ ] Verify record marked as deleted (not removed)
- [ ] Verify deleted records hidden from list
- [ ] Test restore functionality

#### Sluggable Pattern
- [ ] Add to model
  ```bash
  swap generate pattern sluggable Article --fields "Title:string"
  ```
- [ ] Create article with title
- [ ] Verify slug generated automatically
- [ ] Access via slug URL

---

### Authentication Generation

- [ ] Generate auth scaffolding
  ```bash
  swap generate auth
  ```
- [ ] Verify Identity files generated
- [ ] Verify registration page works
- [ ] Verify login page works
- [ ] Verify logout works
- [ ] Test protected routes
- [ ] Test role-based access

---

## 🎨 Container Architecture Testing

### Layer 1: Shell
- [ ] Navigate between pages
- [ ] Verify shell (nav, footer) doesn't reload
- [ ] Verify menu links use HTMX
- [ ] Verify `hx-push-url="true"` updates browser URL
- [ ] Verify browser back/forward buttons work

### Layer 2: Page Container
- [ ] Navigate to entity page
- [ ] Verify static elements load immediately:
  - [ ] Hero section
  - [ ] Search bar
  - [ ] Create button
  - [ ] Loading spinner for list
- [ ] Verify page loads without full refresh
- [ ] Verify no database queries in Index action

### Layer 3: Dynamic Component
- [ ] Verify list component loads asynchronously
- [ ] Verify loading spinner shows briefly
- [ ] Verify list replaces spinner
- [ ] Create new item
- [ ] Verify list refreshes via event trigger
- [ ] Edit item
- [ ] Verify list updates
- [ ] Delete item
- [ ] Verify item removed from list
- [ ] Search
- [ ] Verify only list updates (hero stays)
- [ ] Sort
- [ ] Verify only list updates
- [ ] Change page
- [ ] Verify only list updates

---

## 🔄 Event System Testing

### Custom Events
- [ ] Create triggers `refreshProductList` event
- [ ] Edit triggers `refreshProductList` event
- [ ] Delete triggers `refreshProductList` event
- [ ] List component listens for `refreshProductList`
- [ ] Modal closes automatically after success
- [ ] List updates show new/modified data

### Server-Driven Selection
- [ ] Select individual items
- [ ] Verify checkboxes persist across pages
- [ ] Click "Select All"
- [ ] Verify all items on current page selected
- [ ] Click "Select All" again
- [ ] Verify all items deselected
- [ ] Select multiple items
- [ ] Click "Clear Selection"
- [ ] Verify all selections cleared
- [ ] Navigate to different page
- [ ] Select items
- [ ] Return to first page
- [ ] Verify selections preserved

---

## 📦 Bulk Operations Testing

- [ ] Select multiple items
- [ ] Verify bulk actions bar appears
- [ ] Shows correct count
- [ ] Click "Delete Selected"
- [ ] Confirm deletion
- [ ] Verify selected items deleted
- [ ] Verify list refreshes
- [ ] Verify selections cleared

---

## 🔍 Search, Sort, Filter Testing

### Search
- [ ] Enter search term
- [ ] Verify debounce (500ms)
- [ ] Verify results filtered
- [ ] Clear search
- [ ] Verify all results shown

### Sorting
- [ ] Click column header
- [ ] Verify ascending sort
- [ ] Click again
- [ ] Verify descending sort
- [ ] Verify sort indicator (arrows)
- [ ] Search with sort active
- [ ] Verify sort preserved

### Pagination
- [ ] Navigate to page 2
- [ ] Verify correct items shown
- [ ] Verify page number highlighted
- [ ] Go to last page
- [ ] Go to first page
- [ ] Change page size
- [ ] Verify correct number of items
- [ ] Search with pagination
- [ ] Verify page resets to 1

---

## 🐳 Docker Testing

### Build Image
- [ ] Create new project with Docker support
  ```bash
  swap new DockerTest --docker
  cd DockerTest
  ```
- [ ] Build Docker image
  ```bash
  docker build -t dockertest .
  ```
- [ ] Verify build succeeds
- [ ] Verify multi-stage build used
- [ ] Verify image size reasonable

### Run Container
- [ ] Run container
  ```bash
  docker run -p 8080:8080 dockertest
  ```
- [ ] Visit http://localhost:8080
- [ ] Verify app works in container
- [ ] Test CRUD operations

### Docker Compose
- [ ] Use docker-compose
  ```bash
  docker-compose up
  ```
- [ ] Verify app and database both start
- [ ] Verify connectivity
- [ ] Test full workflow

---

## 🗄️ Database Provider Testing

### SQLite
- [ ] Create project
- [ ] Run migrations
- [ ] Generate controller
- [ ] Test all CRUD operations
- [ ] Verify database file created
- [ ] Test with DB Browser for SQLite

### PostgreSQL
- [ ] Create project
- [ ] Start PostgreSQL (Docker recommended)
- [ ] Update connection string
- [ ] Run migrations
- [ ] Generate controller
- [ ] Test all CRUD operations
- [ ] Verify tables created in pgAdmin

### SQL Server
- [ ] Create project
- [ ] Start SQL Server
- [ ] Update connection string
- [ ] Run migrations
- [ ] Generate controller
- [ ] Test all CRUD operations
- [ ] Verify tables in SSMS

---

## 🧩 Swap.Patterns Testing

For each pattern:

### In Isolation
- [ ] Generate new project
- [ ] Generate model with pattern
- [ ] Verify interface implemented
- [ ] Verify properties added
- [ ] Test pattern-specific functionality

### Combined Patterns
- [ ] Generate model with multiple patterns
  ```bash
  swap generate pattern auditable+publishable+softdelete BlogPost
  ```
- [ ] Verify all interfaces implemented
- [ ] Test interactions between patterns
- [ ] Create/edit/delete with all patterns active

---

## 🎭 Swap.Testing Framework

### HTMX Test Client
- [ ] Create test project
- [ ] Reference Swap.Testing
- [ ] Write tests using HtmxTestClient
- [ ] Test HTMX requests vs normal requests
- [ ] Test custom headers
- [ ] Test event triggers
- [ ] Verify assertions work

### Snapshot Testing
- [ ] Create snapshot tests for views
- [ ] Verify snapshots created
- [ ] Modify view
- [ ] Verify test fails
- [ ] Update snapshot
- [ ] Verify test passes

---

## 📱 Responsive Design Testing

- [ ] Test on desktop (1920x1080)
- [ ] Test on tablet (768x1024)
- [ ] Test on mobile (375x667)
- [ ] Verify navigation menu works on mobile
- [ ] Verify tables scroll horizontally
- [ ] Verify modals responsive
- [ ] Verify forms usable on mobile

---

## ♿ Accessibility Testing

- [ ] Navigate with keyboard only
- [ ] Verify tab order logical
- [ ] Verify focus indicators visible
- [ ] Test with screen reader
- [ ] Verify form labels present
- [ ] Verify error messages announced
- [ ] Test color contrast
- [ ] Verify ARIA attributes

---

## 🚀 Performance Testing

### Load Time
- [ ] Measure initial page load
- [ ] Verify < 200ms for Index (static)
- [ ] Measure list component load
- [ ] Verify reasonable for data size

### Navigation
- [ ] Measure HTMX navigation
- [ ] Verify < 100ms page swap
- [ ] Verify smooth transitions
- [ ] No visible flickering

### Large Datasets
- [ ] Insert 1000+ records
- [ ] Test pagination
- [ ] Test sorting
- [ ] Test searching
- [ ] Verify performance acceptable

---

## 🔐 Security Testing

- [ ] Test SQL injection in search
- [ ] Test XSS in input fields
- [ ] Verify CSRF protection
- [ ] Test with auth enabled
- [ ] Verify protected routes
- [ ] Test role-based access
- [ ] Verify password requirements

---

## 📝 Documentation Testing

- [ ] Read CONTAINER-ARCHITECTURE.md
- [ ] Follow examples
- [ ] Verify code samples work
- [ ] Read DEVELOPER-EXPERIENCE.md
- [ ] Follow quick start guide
- [ ] Verify all commands work
- [ ] Read PATTERNS-LIBRARY.md
- [ ] Test pattern examples

---

## ✅ Sign-Off

After completing all tests above:

- [ ] All unit tests passing
- [ ] All CLI commands working
- [ ] All database providers tested
- [ ] All patterns working
- [ ] Docker builds successful
- [ ] Documentation accurate
- [ ] No critical bugs

**Tested By**: _________________  
**Date**: _________________  
**Version**: _________________  

---

## 🐛 Issues Found

Document any issues discovered during testing:

### Critical Issues
_(Must fix before release)_

### Minor Issues
_(Can fix in patch release)_

### Enhancement Ideas
_(Future improvements)_

---

## 📊 Test Results Summary

```
Total Tests: ___
Passed: ___
Failed: ___
Skipped: ___

Pass Rate: ___%
```

**Overall Status**: ☐ Ready for Release  ☐ Needs Work

# Release Checklist - v0.2.0

## Pre-Release Verification

### ✅ Code Quality
- [x] All tests pass (319 total: 212 CLI + 35 Htmx + 72 Patterns)
- [x] No compilation errors or warnings
- [x] Solution builds in Release configuration
- [x] All relationship types tested end-to-end

### ✅ Documentation
- [x] CHANGELOG.md updated with v0.2.0 release notes
- [x] README.md includes comprehensive relationship documentation
- [x] README.md includes "Building a Blog" tutorial
- [x] CLI help text accurate (`swap --help`, `swap g rel --help`, `swap g c --help`)
- [x] VERSION-0.2.0-PLAN.md reflects completion status

### ✅ Package Versions
- [x] Swap.CLI: 0.2.0
- [x] Swap.Htmx: 0.2.0
- [x] Swap.Patterns: 0.2.0
- [x] Swap.Testing: 0.2.0

### ✅ Feature Completeness
- [x] **One-to-Many**: CLI ✓, Migrations ✓, UI Generation ✓, Display Fields ✓
- [x] **Many-to-One**: CLI ✓, Migrations ✓, UI Generation ✓, Display Fields ✓
- [x] **Many-to-Many**: CLI ✓, Junction Tables ✓, Checkbox UI ✓, ViewBag ✓
- [x] **One-to-One**: CLI ✓, Unique Constraint ✓, Dropdown UI ✓, Display Fields ✓

### ✅ End-to-End Validation
- [x] User-Profile one-to-one relationship verified
- [x] Post-Tag many-to-many relationship verified (from previous session)
- [x] All generated migrations apply successfully
- [x] All generated controllers compile and run
- [x] Dropdown and checkbox UI render correctly

## Release Process

### Step 1: Local Verification
```bash
# Build all packages locally
.\scripts\pack-local.ps1

# Verify packages created
ls .\nuget\local\*.nupkg

# Test CLI installation
.\scripts\reinstall-cli.ps1
swap --version  # Should show 0.2.0
```

### Step 2: Git Preparation
```bash
# Review all changes
git status
git diff

# Commit version bump and CHANGELOG
git add tools/Swap.CLI/Swap.CLI.csproj
git add framework/Swap.Htmx/Swap.Htmx.csproj
git add framework/Swap.Patterns/Swap.Patterns.csproj
git add framework/Swap.Testing/Swap.Testing.csproj
git add CHANGELOG.md
git commit -m "chore: bump version to 0.2.0 - relationship auto-wiring complete"

# Push to branch
git push origin HEAD
```

### Step 3: GitHub Release (Automated via CI/CD)
The GitHub Actions workflow will automatically:
1. ✅ Extract version from Swap.CLI.csproj (0.2.0)
2. ✅ Build and test all packages
3. ✅ Publish to NuGet.org:
   - Swap.CLI 0.2.0
   - Swap.Htmx 0.2.0
   - Swap.Patterns 0.2.0
   - Swap.Testing 0.2.0
4. ✅ Create git tag `v0.2.0`
5. ✅ Extract release notes from CHANGELOG.md
6. ✅ Create GitHub release with packages attached

### Step 4: Post-Release Verification
```bash
# Wait for NuGet.org to index (5-10 minutes)
# Then test installation from public feed
dotnet tool uninstall -g swap-cli
dotnet tool install -g swap-cli --version 0.2.0

# Verify version
swap --version

# Quick smoke test
mkdir test-0.2.0 && cd test-0.2.0
swap new TestApp
cd TestApp
swap g m Product --fields "Name:string,Price:decimal"
swap g m Category --fields "Name:string"
swap g rel -s Product -t Category --type many-to-one
swap g c Product --with-relationships
dotnet build
```

## Release Highlights (for GitHub Release Notes)

### 🎉 Relationship Auto-Wiring Complete

Version 0.2.0 completes the relationship story with **automatic UI generation** for all four relationship types. Controllers generated with `--with-relationships` now create fully functional forms with dropdowns and checkboxes—no manual wiring required.

**Key Features:**
- ✨ One-to-One relationships with unique constraint and automatic dropdown UI
- ✨ Many-to-Many relationships with automatic checkbox list UI
- ✨ Automatic display field detection (Name, Title, Email, Username, Description)
- ✨ Comprehensive documentation with "Building a Blog" tutorial
- ✨ 319 tests passing (100% relationship coverage)

**What's New:**
- One-to-One: CLI support, FK with unique constraint, dropdown UI with display fields
- Many-to-Many: Automatic checkbox UI, ViewBag population, junction entity generation
- Display Fields: Smart detection for clean dropdown labels (`@item.Name` vs `@item.Id`)
- Documentation: Complete relationship guide, blog tutorial, command reference

**Getting Started:**
```bash
# Install or update
dotnet tool install -g swap-cli --version 0.2.0

# Create a blog in ~2 minutes
swap new MyBlog
cd MyBlog
swap g m Post --fields "Title:string,Content:string,PublishedAt:datetime?"
swap g m Author --fields "Name:string,Email:string"
swap g m Tag --fields "Name:string"
swap g rel -s Post -t Author --type many-to-one
swap g rel -s Post -t Tag --type many-to-many
swap g c Post --with-relationships
swap g c Author --with-relationships
dotnet run
```

See the [CHANGELOG](CHANGELOG.md) for full details.

## Post-Release Tasks

- [ ] Update VERSION-0.2.0-PLAN.md to mark as "SHIPPED"
- [ ] Create VERSION-0.3.0-PLAN.md for next iteration
- [ ] Announce release on social media / community channels
- [ ] Monitor GitHub issues for bug reports
- [ ] Update wiki documentation if needed

---

**Release prepared by**: GitHub Copilot  
**Release date**: 2025-01-XX (TBD)  
**Version**: 0.2.0  
**Status**: ✅ READY FOR RELEASE

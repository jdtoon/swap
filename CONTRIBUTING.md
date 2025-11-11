# Contributing to Swap

Thank you for considering contributing to Swap! This document provides guidelines and instructions for contributing code, documentation, and improvements.

---

## 🎯 Ways to Contribute

- **🐛 Report Bugs** — Found an issue? Open an issue with reproduction steps
- **💡 Suggest Features** — Have an idea? Open an issue with the feature request
- **📖 Improve Documentation** — Fix typos, clarify explanations, add examples
- **💻 Write Code** — Fix bugs, implement features, improve templates
- **🧪 Write Tests** — Increase test coverage, add edge case tests
- **🎨 Improve Templates** — Refine generated code quality and DX

---

## 🐛 Reporting Bugs

When reporting bugs, please include:

1. **Description** — Clear description of the issue
2. **Steps to Reproduce** — Exact commands and steps to reproduce
3. **Expected Behavior** — What should happen
4. **Actual Behavior** — What actually happened
5. **Environment** — OS, .NET version, Swap CLI version
6. **Screenshots/Code** — If applicable, include generated code

**Example:**
```markdown
### Bug: Template generation fails with error

**Steps:**
1. `swap new TestApp`
2. `cd TestApp`
3. Run: `dotnet build`

**Expected:** Clean build ✅
**Actual:** Build fails with missing reference error ❌

**Environment:**
- OS: Windows 11
- .NET: 9.0.0
- Swap CLI: 0.4.1
```

---

## 💡 Suggesting Features

When suggesting features, describe:

1. **Use Case** — What problem does this solve?
2. **Proposed Solution** — How should it work?
3. **Alternatives** — Other solutions you considered
4. **Examples** — CLI commands, code samples, mockups

---

## 🔧 Development Setup

### Prerequisites

- **.NET 9.0 SDK** or later
- **Git**
- Your favorite editor (VS Code, Visual Studio, Rider)

### Quick Start

```bash
# Fork and clone the repository
git clone https://github.com/YOUR_USERNAME/swap.git
cd swap

# Option 1: Use the convenience script (recommended)
.\scripts\reinstall-cli.ps1   # Windows
./scripts/reinstall-cli.sh    # Linux/Mac

# Option 2: Manual setup
cd tools/Swap.CLI
dotnet build
dotnet tool uninstall --global Swap.CLI  # If installed
dotnet tool install --global --add-source ./bin/Debug Swap.CLI
cd ../..

# Verify installation
swap --version
```

---

## 📦 Working with Framework Packages

When modifying framework packages (Swap.Htmx, Swap.Modularity, Swap.Testing), you need to test them in a real project.

### Local NuGet Workflow (Recommended)

This workflow lets you test unreleased framework changes without publishing to NuGet.org.

#### Step 1: Pack Local Packages

```bash
# Build and pack all framework packages to .nuget/local/
.\scripts\pack-local.ps1       # Windows
./scripts/pack-local.sh        # Linux/Mac

# What it does:
# - Restores dependencies
# - Builds Swap.Htmx, Swap.Modularity, Swap.Testing, Swap.CLI
# - Packs each as .nupkg
# - Copies to .nuget/local/ directory
# - Clears NuGet caches

# Output:
# All packages packed successfully to .nuget/local/
# - Swap.CLI.0.3.2
# - Swap.Htmx.0.3.1
# - Swap.Modularity.0.1.0
# - Swap.Testing.0.3.0
```

#### Step 2: Create a Test Project with Local Packages

```bash
# Create a new project using local NuGet packages
swap new MyTestApp --local-nuget

# What this does:
# - Generates project from template (Monolith by default)
# - Creates nuget.config pointing to ../../.nuget/local
# - Runs npm install, libman restore, Tailwind build
# - Creates and applies EF Core migrations

cd MyTestApp
```

#### Step 3: Test Your Changes

```bash
# Restore with local packages
dotnet restore

# Build (uses Swap.Htmx 0.3.1, Swap.Modularity 0.1.0, etc. from local feed)
dotnet build

# Run the app
dotnet run

# Run tests
dotnet test
```

#### Step 4: Iterate

```bash
# Edit framework code
cd ../../framework/Swap.Htmx
# ... make changes ...

# Repack packages
cd ../../
.\scripts\pack-local.ps1

# In your test project, force fresh restore
cd testApps/MyTestApp
dotnet restore --force-evaluate
dotnet build
dotnet run
```

### Workflow Tips

**Faster iteration:**
```bash
# Pack only one package instead of all
cd framework/Swap.Htmx
dotnet pack -c Release -o ../../.nuget/local

# Then in test project
dotnet restore --no-cache
```

**Clear all caches if packages won't update:**
```bash
# PowerShell
Remove-Item -Path "$env:USERPROFILE\.nuget\packages\swap.*" -Recurse -Force

# Then repack and restore
.\scripts\pack-local.ps1
cd testApps/MyTestApp
dotnet restore --force-evaluate
```

**Test with different templates:**
```bash
swap new MyLayeredApp --template swap-layered --local-nuget
swap new MyModularApp --template swap-modular --local-nuget
```

---

## 🧪 Running Tests

### Test All Packages

```bash
cd tools/Swap.CLI.Tests
dotnet test

# With coverage
dotnet test /p:CollectCoverage=true
```

### Test Specific Packages

```bash
# CLI tests only
cd tools/Swap.CLI.Tests
dotnet test

# Framework tests
cd framework
dotnet test

# Specific framework package
cd framework/Swap.Htmx.Tests
dotnet test
```

### Run Specific Tests

```bash
# Test filtering
dotnet test --filter "FullyQualifiedName~ModelGeneration"
dotnet test --filter "FullyQualifiedName~ControllerGeneration"
```

### Expected Test Results

```
Framework Tests:
- Swap.Htmx.Tests: 35 tests
- Swap.Modularity.Tests: 24 tests
- Swap.Testing.Tests: (in progress)
Total: 59+ tests
```

---

## 📝 Code Style & Standards

### C# Guidelines

- **Naming:** PascalCase for classes/methods, camelCase for variables/properties
- **Indentation:** 4 spaces (no tabs)
- **Line Length:** Max 120 characters
- **Async:** Use `async`/`await` appropriately; name async methods with `Async` suffix
- **Comments:** Use XML docs for public APIs

Example:
```csharp
/// <summary>
/// Creates a new product with the specified details.
/// </summary>
/// <param name="dto">The product creation DTO</param>
/// <returns>The created product entity</returns>
public async Task<Product> CreateAsync(CreateProductDto dto)
{
    // Implementation...
}
```

### Template Standards

- **Syntax:** Clean C# and Razor, no cryptic abbreviations
- **Comments:** Explain HTMX patterns and non-obvious behavior
- **Formatting:** Consistent indentation, clear structure
- **Variables:** Use descriptive names in templates

---

## 🔀 Pull Request Process

### 1. Create Branch

From `develop` branch, use descriptive names:
```bash
git checkout -b feature/add-seeder-command
git checkout -b fix/nullable-field-validation
git checkout -b docs/improve-cli-examples
```

### 2. Make Changes

- Write code following style guidelines
- Add/update tests for your changes
- Update documentation if behavior changes

### 3. Test Locally

```bash
# CLI changes
dotnet test -c Release

# Framework changes (with local NuGet)
.\scripts\pack-local.ps1
swap new TestApp --local-nuget
# Test thoroughly
```

### 4. Commit with Conventional Format

Use [Conventional Commits](https://www.conventionalcommits.org/):

```bash
git commit -m "feat(generate): add seeder command for database seeding"
git commit -m "fix(controller): handle nullable decimal fields correctly"
git commit -m "docs(readme): update installation instructions"
git commit -m "test(field-helper): add tests for sortable flag parsing"
git commit -m "refactor(template): extract modal generation to helper"
```

**Types:**
- `feat:` — New feature
- `fix:` — Bug fix
- `docs:` — Documentation only
- `style:` — Code formatting (no logic changes)
- `refactor:` — Code refactoring
- `test:` — Add/update tests
- `chore:` — Maintenance tasks

### 5. Push to Fork

```bash
git push origin feature/add-seeder-command
```

### 6. Open Pull Request

- Clear title describing the change
- Description explaining what and why
- Link related issues (`Fixes #123`)
- Mark as draft if work in progress

### 7. Address Code Review

- Respond to feedback
- Make requested changes
- Push new commits (no force-push during review)

### 8. Merge

Maintainers will merge when approved. Ensure:
- ✅ All tests pass
- ✅ Code follows style guidelines
- ✅ Documentation is updated
- ✅ No merge conflicts

---

## 🎨 Template Contribution Guidelines

When modifying templates in `templates/`:

### 1. Test Generation

```bash
# Generate code with your template changes
cd c:\temp
swap new TestApp
cd TestApp

# Verify it compiles
dotnet build

# Test runtime
dotnet run
```

### 2. Test Edge Cases

- Different template options
- Database providers (SQLite, SQL Server, PostgreSQL)
- Local NuGet package references
- Build and migration steps

### 3. Update Tests

Add integration tests for new template features:
```csharp
[Fact]
public async Task NewCommand_WithTemplate_GeneratesExpectedStructure()
{
    // Test that template generates correct project structure
}
```

### 4. Document Changes

Update relevant README files if template behavior changes.

---

## 📚 Documentation Updates

When updating documentation:

1. **Main README** — Update if CLI commands or core features change
2. **Tool/Framework READMEs** — Update library docs with examples
3. **Code Comments** — Update XML docs for public APIs
4. **CHANGELOG** — Add entry under `[Unreleased]` section

---

## 🚀 Adding New Features

### Checklist

- [ ] Feature has corresponding tests
- [ ] Code follows style guidelines
- [ ] Documentation is updated
- [ ] CHANGELOG entry added
- [ ] No breaking changes (or documented)
- [ ] Local NuGet tested (if framework change)

### Example: Adding a New Feature

Follow these general steps when adding new features to the framework:

1. **Implement in framework** — Add code to the appropriate package (Swap.Htmx, Swap.Modularity, etc.)
2. **Add tests** — Create comprehensive tests in the corresponding test project
3. **Update CLI** — If applicable, add CLI command or generation support
4. **Add templates** — Create necessary templates in `templates/generate/`
5. **Test end-to-end** — Test the complete workflow
6. **Update docs** — Document in README and wiki

---

## 🤝 Code of Conduct

- Be respectful and constructive in all interactions
- Welcome newcomers and help them contribute
- Focus on what's best for the community
- Accept feedback gracefully and constructively

---

## ❓ Questions?

- **Issues** — For bugs and feature requests
- **Discussions** — For questions and general help
- **Pull Requests** — For code contributions

---

## 📄 License

By contributing, you agree that your contributions will be licensed under the MIT License (see [LICENSE](LICENSE)).

---

## 🎯 Roadmap

See [CHANGELOG.md](CHANGELOG.md) for planned features and current version status.

---

**Thank you for contributing to Swap! 🙏**

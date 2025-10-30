# Contributing to Swap CLI

Thank you for considering contributing to Swap CLI! This document provides guidelines and instructions for contributing.

## 🎯 Ways to Contribute

- **Report Bugs** - Found a bug? Open an issue with reproduction steps
- **Suggest Features** - Have an idea? Open an issue with the feature request
- **Improve Documentation** - Fix typos, clarify explanations, add examples
- **Write Code** - Fix bugs, implement features, improve templates
- **Write Tests** - Increase test coverage, add edge case tests

## 🐛 Reporting Bugs

When reporting bugs, please include:

1. **Description** - Clear description of the issue
2. **Steps to Reproduce** - Exact commands to reproduce the bug
3. **Expected Behavior** - What you expected to happen
4. **Actual Behavior** - What actually happened
5. **Environment** - OS, .NET version, Swap CLI version
6. **Generated Code** - If applicable, share the generated code

**Example:**
```markdown
### Bug: Generated controller has syntax error

**Steps:**
1. `swap new TestApp`
2. `swap g c Product --fields "Name:string Price:decimal"`
3. Build project: `dotnet build`

**Expected:** Clean build
**Actual:** CS1002: ; expected on line 45

**Environment:** 
- Windows 11
- .NET 9.0.0
- Swap CLI 0.1.0-dev
```

## 💡 Suggesting Features

When suggesting features, please include:

1. **Use Case** - What problem does this solve?
2. **Proposed Solution** - How should it work?
3. **Alternatives** - Other solutions you considered
4. **Examples** - CLI commands, code samples, UI mockups

## 🔧 Development Setup

### Prerequisites
- .NET 9.0 SDK or later
- Git
- Your favorite editor (VS Code, Visual Studio, Rider)

### Getting Started

```bash
# Fork and clone the repository
git clone https://github.com/YOUR_USERNAME/swap.git
cd swap

# Build and install CLI locally (automatic method - recommended)
cd tools/Swap.CLI
dotnet build
dotnet tool uninstall --global Swap.CLI  # Remove existing if installed
dotnet tool install --global --add-source ./bin/Debug Swap.CLI

# Or use the convenience script
cd ../..
.\scripts\reinstall-cli.ps1   # Windows
./scripts/reinstall-cli.sh    # Linux/Mac

# Run tests
cd tools/Swap.CLI.Tests
dotnet test
```

### Testing Your Changes with --local-nuget

When working on framework changes (Swap.Htmx, Swap.Patterns, Swap.Testing), you need to test them in a real project without publishing to NuGet:

```bash
# 1. Build all local packages
.\scripts\pack-local.ps1   # Windows
./scripts/pack-local.sh    # Linux/Mac

# This creates packages in .nuget/local/

# 2. Create a test project using local packages
cd testApps
swap new MyTestApp --local-nuget --skip-setup

# The --local-nuget flag:
# - Automatically runs pack-local if packages don't exist
# - Creates nuget.config pointing to ../../.nuget/local
# - Lets you test unreleased framework changes immediately

# 3. Test your changes
cd MyTestApp
dotnet restore  # Uses local packages
dotnet build
dotnet run

# 4. Make framework changes and rebuild
cd ../../framework/Swap.Patterns
# ... make your changes ...
cd ../../
.\scripts\pack-local.ps1  # Rebuild packages

# 5. Update your test app
cd testApps/MyTestApp
dotnet restore --force-evaluate  # Force re-evaluation of packages
dotnet build
```

**Important:** The `--local-nuget` flag is ONLY for development within the Swap repository. Regular users should never use it.

### Project Structure

```
swap/
├── tools/
│   ├── Swap.CLI/              # CLI source code
│   │   ├── Commands/          # Command implementations
│   │   │   ├── NewCommand.cs              # swap new
│   │   │   ├── GenerateCommand.cs         # swap generate
│   │   │   ├── GenerateControllerCommand.cs
│   │   │   ├── GenerateModelCommand.cs
│   │   │   └── GenerateResourceCommand.cs
│   │   ├── Infrastructure/    # Core utilities
│   │   │   ├── TemplateEngine.cs  # Template processing
│   │   │   └── FieldHelper.cs     # Code generation helpers
│   │   └── Program.cs         # CLI entry point
│   └── Swap.CLI.Tests/        # Test suite (136 tests)
├── templates/                 # Code generation templates
│   ├── monolith/             # New project template
│   └── generate/             # CRUD templates
└── docs/                     # Documentation
```

## 🧪 Writing Tests

All new features must include tests. We use xUnit for testing.

```csharp
[Fact]
public void GenerateController_WithValidFields_ShouldCreateControllerFile()
{
    // Arrange
    var projectPath = CreateTestProject();
    var fields = "Name:string Price:decimal";
    
    // Act
    var result = GenerateController("Product", fields, projectPath);
    
    // Assert
    Assert.True(File.Exists(Path.Combine(projectPath, "Controllers/ProductController.cs")));
}
```

### Running Tests

```bash
cd tools/Swap.CLI.Tests
dotnet test

# Run specific test
dotnet test --filter "FullyQualifiedName~GenerateController"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## 📝 Code Style

- **C# Style** - Follow standard .NET conventions
- **Naming** - PascalCase for classes/methods, camelCase for variables
- **Comments** - XML docs for public APIs, inline comments for complex logic
- **Formatting** - 4-space indentation, no tabs
- **Line Length** - Max 120 characters

### Template Style

- **Razor Syntax** - Clean indentation, clear attribute placement
- **HTML** - Semantic markup, DaisyUI classes
- **HTMX** - Clear attribute naming, documented behavior
- **Comments** - Explain HTMX patterns and non-obvious behavior

## 🔀 Pull Request Process

1. **Create Branch** - From `develop` branch, use descriptive name
   - `feature/add-seeder-command`
   - `fix/nullable-field-validation`
   - `docs/improve-cli-examples`

2. **Make Changes** - Write code, add tests, update docs

3. **Test Locally** - Ensure all tests pass
   ```bash
   dotnet test
   ```

4. **Commit** - Use conventional commits
   ```bash
   git commit -m "feat: add database seeder command"
   git commit -m "fix: handle nullable fields in validation"
   git commit -m "docs: add examples for field flags"
   ```

5. **Push** - Push to your fork
   ```bash
   git push origin feature/add-seeder-command
   ```

6. **Open PR** - Create pull request on GitHub
   - Clear title describing the change
   - Description explaining what and why
   - Link related issues
   - Mark as draft if work in progress

7. **Code Review** - Address feedback, make changes

8. **Merge** - Maintainers will merge when approved

## 📋 Commit Message Format

We use [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

**Types:**
- `feat:` - New feature
- `fix:` - Bug fix
- `docs:` - Documentation only
- `style:` - Code style (formatting, no logic change)
- `refactor:` - Code refactoring
- `test:` - Add/update tests
- `chore:` - Maintenance tasks

**Examples:**
```bash
feat(generate): add seeder command for database seeding
fix(controller): handle nullable decimal fields correctly
docs(readme): update installation instructions
test(field-helper): add tests for sortable flag parsing
refactor(template): extract modal generation to helper method
```

## 🎨 Template Contribution Guidelines

When modifying templates in `templates/`:

1. **Test Generation** - Generate code and verify it compiles
2. **Test Runtime** - Run generated app and test features manually
3. **Test Edge Cases** - Nullable fields, special characters, etc.
4. **Update Tests** - Add/update integration tests
5. **Document Changes** - Update wiki if behavior changes

### Testing Template Changes

```bash
# Generate test project
cd c:\temp
swap new TestTemplates
cd TestTemplates

# Test model generation
swap g m Customer --fields "Name:string Email:string Age:int?"

# Test controller generation  
swap g c Product --fields "Name:string Price:decimal InStock:bool:f"

# Verify compilation
dotnet build

# Verify runtime
dotnet ef database update
dotnet run
# Test in browser
```

## 📚 Documentation Updates

When updating docs:

1. **Wiki** - Update `wiki/docs/` for user-facing documentation
2. **Code Comments** - Update XML docs for API changes
3. **README** - Update if CLI commands or features change
4. **CHANGELOG** - Add entry under `[Unreleased]` section

## 🤝 Code of Conduct

- Be respectful and constructive
- Welcome newcomers and help them contribute
- Focus on what's best for the community
- Accept constructive criticism gracefully

## ❓ Questions?

- Open an [issue](https://github.com/jdtoon/swap/issues) with the `question` label
- Check existing issues and discussions first

## 📄 License

By contributing, you agree that your contributions will be licensed under the MIT License.

---

Thank you for contributing to Swap CLI! 🙏


# Swap CLI Tools

This folder contains the Swap CLI tool and its test suite.

## Overview

The `tools/` directory houses the command-line interface (CLI) tool that powers code generation for Swap projects.

## Structure

```
tools/
├── Swap.CLI/              # CLI tool source code
│   ├── Commands/          # All CLI command implementations
│   ├── Infrastructure/    # Template engine, helpers, utilities
│   ├── Program.cs         # CLI entry point
│   ├── Swap.CLI.csproj    # Project file
│   └── README.md          # CLI documentation
└── Swap.CLI.Tests/        # Test suite
    ├── Commands/          # Command tests
    ├── Infrastructure/    # Infrastructure tests
    ├── Swap.CLI.Tests.csproj
    └── README.md          # Test documentation
```

## Projects

### Swap.CLI

**The code generation engine for ASP.NET Core + HTMX applications.**

**What it does:**
- Generate complete projects with `swap new`
- Generate CRUD controllers with `swap generate controller`
- Apply entity patterns with `swap generate pattern`
- Scaffold authentication with `swap generate auth`
- Manage database workflows with `swap db` commands
- Generate tests, seeders, and factories

**Installation:**
```bash
dotnet tool install --global Swap.CLI
```

**Documentation:** [Swap.CLI/README.md](./Swap.CLI/README.md)

### Swap.CLI.Tests

**Comprehensive test suite for the CLI tool.**

**Test Coverage:**
- 160+ tests for CLI commands
- Command structure validation
- Template processing tests
- Integration tests
- Error handling tests

**Running Tests:**
```bash
cd Swap.CLI.Tests
dotnet test
```

**Documentation:** [Swap.CLI.Tests/README.md](./Swap.CLI.Tests/README.md)

## Commands Overview

The CLI provides the following commands:

### Project Generation
- `swap new <name>` - Create new project

### Code Generation
- `swap generate controller <entity>` - CRUD controller with views
- `swap generate model <entity>` - Entity model
- `swap generate pattern <pattern> <entity>` - Apply entity pattern
- `swap generate auth` - Authentication scaffolding
- `swap generate seed <entity>` - Database seeder
- `swap generate factory <entity>` - Test data factory
- `swap generate test <controller>` - Integration test

### Database Workflows
- `swap db info` - Database configuration info
- `swap db migrate` - Apply migrations
- `swap db seed` - Run seeders
- `swap db reset` - Reset database

### Utilities
- `swap list` - List entities in project
- `swap doctor` - Diagnose issues

### Events (DX)
- `swap events list [-p <dir>]` – Source-scan your project for chains in `Events/SwapEventChains.cs`, resolving `EventNames.*` constants.
- `swap events from-server --url <http://host:port>` – Query a running app’s dev endpoint (`/_swap/dev/events.json`) and pretty-print Trigger → Chained.
 - `swap events validate [-p <dir>]` – Validate names and detect cycles from source; exits non‑zero on errors.
 - `swap events graph [-p <dir>] [--format mermaid|dot] [--output file]` – Output a graph of your chains.

Tip: Run the app in one terminal and query from another so the process isn’t interrupted.

**Full reference:** [Swap.CLI/README.md](./Swap.CLI/README.md)

## Development

### Building the CLI

```bash
cd Swap.CLI
dotnet build
```

### Running Tests

```bash
cd Swap.CLI.Tests
dotnet test
```

### Local Installation

For testing CLI changes locally:

```bash
# Build and pack
.\scripts\pack-local.ps1  # Windows
./scripts/pack-local.sh   # Linux/Mac

# Install locally
.\scripts\reinstall-cli.ps1  # Windows
./scripts/reinstall-cli.sh   # Linux/Mac
```

### Debugging

**Visual Studio / VS Code:**
1. Open `swap.sln`
2. Set `Swap.CLI` as startup project
3. Add command-line arguments in project properties
4. F5 to debug

**Command-line arguments example:**
```
new TestApp --database sqlite
```

## Architecture

### Command Structure

Commands use `System.CommandLine` for parsing:

```csharp
public static class NewCommand
{
    public static Command Create()
    {
        var command = new Command("new", "Create a new project");
        var nameArg = new Argument<string>("name");
        command.AddArgument(nameArg);
        
        command.SetHandler(async (InvocationContext context) =>
        {
            var name = context.ParseResult.GetValueForArgument(nameArg);
            await ExecuteAsync(name);
        });
        
        return command;
    }
}
```

### Template Processing

Templates use variable substitution and conditionals:

```csharp
var template = "namespace {{ProjectName}};";
var variables = new Dictionary<string, string> 
{
    ["ProjectName"] = "MyApp"
};
var processed = TemplateEngine.Process(template, variables);
// Result: "namespace MyApp;"
```

### File Structure

**Commands:** `tools/Swap.CLI/Commands/`
- `NewCommand.cs` - Project generation (542 LOC)
- `GenerateControllerCommand.cs` - CRUD generation (691 LOC)
- `GeneratePatternCommand.cs` - Pattern application (2,389 LOC) ⚠️ Refactor target
- `GenerateAuthCommand.cs` - Auth scaffolding (654 LOC)
- `DatabaseInfoCommand.cs` - DB information
- And more...

**Infrastructure:** `tools/Swap.CLI/Infrastructure/`
- `TemplateEngine.cs` - Template processing
- `FieldHelper.cs` - Field parsing
- `MigrationHelper.cs` - Migration creation
- `ProjectScanner.cs` - Project analysis

**Templates:** `templates/`
- All code generation templates
- See [templates/README.md](../templates/README.md)

## Code Generation Flow

1. **Parse command** - Extract arguments and options
2. **Validate input** - Check project structure, entity names
3. **Load templates** - Read template files from `templates/`
4. **Process variables** - Replace placeholders with actual values
5. **Generate files** - Write processed templates to disk
6. **Create migration** - Auto-generate EF Core migration
7. **Report success** - Display generated files

## Testing Strategy

### Unit Tests
Test individual methods and classes:
```csharp
[Fact]
public void FieldHelper_ParsesFieldsCorrectly()
{
    var fields = FieldHelper.ParseFields("Name:string Age:int");
    Assert.Equal(2, fields.Count);
}
```

### Integration Tests
Test complete command execution:
```csharp
[Fact]
public async Task NewCommand_CreatesProject()
{
    await NewCommand.ExecuteAsync("TestApp", "sqlite", null, false, false, false);
    Assert.True(Directory.Exists("TestApp"));
}
```

### Template Tests
Validate generated code compiles:
```csharp
[Fact]
public void GeneratedController_Compiles()
{
    var code = GenerateController("Product", fields);
    var compilation = CSharpCompilation.Create("Test")
        .AddSyntaxTrees(CSharpSyntaxTree.ParseText(code));
    Assert.Empty(compilation.GetDiagnostics());
}
```

## Refactoring Plan (v0.2.0)

Large command files will be refactored:

**Priority 1: GeneratePatternCommand.cs (2,389 LOC)**
- Split into: `PatternGenerator`, `PatternDetector`, `PatternMigrationBuilder`

**Priority 2: GenerateControllerCommand.cs (691 LOC)**
- Split into: `ControllerGenerator`, `ViewGenerator`, `ViewModelGenerator`

**Priority 3: NewCommand.cs (542 LOC)**
- Split into: `ProjectScaffolder`, `DependencyInstaller`, `MigrationRunner`

**Goals:**
- No class >300 LOC
- Single Responsibility Principle
- Improved testability
- Zero logic changes (tests must pass)

## Contributing

See [CONTRIBUTING.md](../CONTRIBUTING.md) for:
- Development environment setup
- Coding standards
- Pull request process
- Testing requirements

## CI/CD

The CLI is automatically built, tested, and published:

**Build:** `.github/workflows/ci-build.yml`
- Build Swap.CLI
- Run all tests (160+)
- Pack as NuGet package

**Publish:** `.github/workflows/nuget-publish.yml`
- Publish to NuGet.org
- Create GitHub release
- Tag repository

## Notes

- **Production-ready:** Swap.CLI v0.1.0 is stable
- **Well-tested:** 160+ tests covering core functionality
- **Actively maintained:** Regular updates and improvements
- **Documentation:** Comprehensive README and code comments

---

**Related Documentation:**
- [Swap.CLI/README.md](./Swap.CLI/README.md) - Detailed CLI reference
- [Swap.CLI.Tests/README.md](./Swap.CLI.Tests/README.md) - Test documentation
- [README.md](../README.md) - Main project README
- [templates/README.md](../templates/README.md) - Template documentation

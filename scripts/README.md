# Scripts

This folder contains development utility scripts for building, packing, and testing Swap CLI locally.

## Overview

These scripts are used for **local development and testing only**. They help framework developers iterate quickly by building NuGet packages locally and installing the CLI tool without publishing to NuGet.org.

## Available Scripts

### `pack-local.ps1` / `pack-local.sh`

**Purpose:** Build all Swap packages and pack them to the local NuGet feed.

**What it does:**
1. Cleans previous builds and packages
2. Restores dependencies for all projects
3. Builds all framework packages (Swap.Htmx, Swap.Patterns, Swap.Testing)
4. Builds Swap.CLI
5. Packs all projects as NuGet packages
6. Copies packages to `.nuget/local/` directory

**Usage:**
```powershell
# Windows
.\scripts\pack-local.ps1

# Linux/Mac
./scripts/pack-local.sh
```

**Output:**
- Creates `.nuget/local/` directory with all `.nupkg` files
- Packages are versioned according to `.csproj` files (currently v0.1.0)

### `reinstall-cli.ps1` / `reinstall-cli.sh`

**Purpose:** Uninstall and reinstall the Swap CLI tool from the local NuGet feed.

**What it does:**
1. Uninstalls the existing `Swap.CLI` global tool
2. Installs `Swap.CLI` from the local NuGet feed at `.nuget/local/`
3. Verifies installation with `swap --version`

**Usage:**
```powershell
# Windows
.\scripts\reinstall-cli.ps1

# Linux/Mac
./scripts/reinstall-cli.sh
```

**Prerequisites:**
- Run `pack-local` script first to build packages
- `.nuget/local/` directory must exist with packages

## Typical Development Workflow

1. **Make changes to framework code** (Swap.Htmx, Swap.Patterns, etc.) or CLI code
2. **Build and pack locally:**
   ```powershell
   .\scripts\pack-local.ps1
   ```
3. **Reinstall CLI tool:**
   ```powershell
   .\scripts\reinstall-cli.ps1
   ```
4. **Test changes in a test app:**
   ```powershell
   cd testApps
   swap new TestApp --local-nuget
   cd TestApp
   # Test your changes...
   ```

## Local NuGet Feed

The `.nuget/local/` directory acts as a local NuGet feed:
- Created automatically by `pack-local` scripts
- Referenced by root `nuget.config`
- Used when generating projects with `swap new MyApp --local-nuget`

**Location:** `c:\jd\swap\.nuget\local\`

**Contents:**
- `Swap.CLI.0.1.0.nupkg`
- `Swap.Htmx.0.1.0.nupkg`
- `Swap.Patterns.0.1.0.nupkg`
- `Swap.Testing.0.1.0.nupkg`

## Notes

- **NOT for end users:** These scripts are for Swap framework developers only
- **Version matching:** Ensure all `.csproj` files have matching versions
- **Clean state:** Scripts clean previous builds to avoid stale packages
- **CI/CD:** Production builds use `.github/workflows/` instead

## Troubleshooting

### "Tool 'swap.cli' is not installed"
Run `reinstall-cli.ps1` script to install from local feed.

### "Package not found"
Run `pack-local.ps1` first to build packages.

### "Version conflict"
Delete `.nuget/local/` directory and re-run `pack-local.ps1`.

---

**Related Documentation:**
- [CONTRIBUTING.md](../CONTRIBUTING.md) - Complete development workflow
- [testApps/README.md](../testApps/README.md) - Testing generated projects
- [.github/workflows/](../.github/workflows/) - CI/CD pipelines

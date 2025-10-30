# GitHub Workflows

CI/CD automation pipelines for Swap CLI.

## Overview

This directory contains GitHub Actions workflows that automate building, testing, and publishing Swap CLI packages.

## Workflows

### `ci-build.yml` - Continuous Integration

**Trigger:** Every push to any branch, all pull requests

**Purpose:** Validate code quality and functionality

**Jobs:**
1. **Build**
   - Restore dependencies
   - Build all projects (`swap.sln`)
   - Ensure zero compilation errors

2. **Test**
   - Run all unit tests
   - `Swap.CLI.Tests` (~160 tests)
   - `Swap.Patterns.Tests` (~72 tests)
   - `Swap.Htmx.Tests` (if any)
   - Fail on any test failures

3. **Pack**
   - Package all projects as NuGet packages
   - Store in `artifacts/packages/`
   - Upload as GitHub Actions artifacts
   - Validate package metadata

4. **Integration Test**
   - Install Swap CLI from packed artifact
   - Run `swap new TestApp --skip-setup`
   - Build generated project
   - Verify end-to-end workflow

**Exit Criteria:**
- ✅ All projects build successfully
- ✅ All tests pass
- ✅ Packages created successfully
- ✅ Generated project compiles

**Status Badge:** `[![CI Build](https://github.com/jdtoon/swap/workflows/ci-build/badge.svg)](https://github.com/jdtoon/swap/actions)`

### `nuget-publish.yml` - Automated Release

**Trigger:** Pull request merged to `main` branch

**Purpose:** Automatically publish new versions to NuGet.org

**Steps:**
1. **Extract Version**
   - Read version from `tools/Swap.CLI/Swap.CLI.csproj`
   - Parse `<Version>` tag (e.g., `0.1.0`)
   - Check if git tag exists (e.g., `v0.1.0`)

2. **Check for Existing Release**
   - Query GitHub API for tag
   - Skip if tag already exists (idempotent)

3. **Build & Test**
   - Full CI build (same as `ci-build.yml`)
   - Ensure quality before publishing

4. **Pack Packages**
   - Build NuGet packages for all projects:
     - `Swap.CLI`
     - `Swap.Htmx`
     - `Swap.Patterns`
     - `Swap.Testing`

5. **Publish to NuGet.org**
   - Push packages to NuGet.org
   - Use `NUGET_API_KEY` secret
   - Skip if package version already exists

6. **Create Git Tag**
   - Tag commit with version (e.g., `v0.1.0`)
   - Push tag to GitHub

7. **Extract Release Notes**
   - Parse `CHANGELOG.md`
   - Extract notes for current version
   - Format for GitHub release

8. **Create GitHub Release**
   - Create release on GitHub
   - Attach all `.nupkg` files
   - Include release notes from CHANGELOG
   - Mark pre-release if version contains `-alpha`, `-beta`, `-rc`

**Secrets Required:**
- `NUGET_API_KEY` - NuGet.org API key with push permissions

**Idempotent Behavior:**
- ✅ Re-running workflow won't duplicate releases
- ✅ Existing tags/packages are detected and skipped
- ✅ Safe to merge multiple PRs (only first triggers publish)

**Version Detection:**
```xml
<!-- Swap.CLI.csproj -->
<Version>0.1.0</Version>
```
→ Creates tag `v0.1.0` and publishes packages

### Workflow Dependencies

```
ci-build.yml     ← Runs on every push/PR
     ↓
nuget-publish.yml ← Runs on PR merge to main
     ↓
NuGet.org        ← Packages published
     ↓
GitHub Release   ← Release created with artifacts
```

## Configuration

### Secrets

**Repository Secrets** (Settings → Secrets → Actions):
- `NUGET_API_KEY` - API key from NuGet.org
  - Create at: https://www.nuget.org/account/apikeys
  - Permissions: Push packages for `Swap.*`
  - Expiration: Set appropriate expiry (1 year recommended)

### Triggers

```yaml
# ci-build.yml
on:
  push:
    branches: ['**']  # All branches
  pull_request:
    branches: ['**']  # All PRs

# nuget-publish.yml
on:
  pull_request:
    types: [closed]
    branches: [main]
```

### Environment Variables

```yaml
env:
  DOTNET_VERSION: '9.0.x'
  CONFIGURATION: Release
```

## Development Workflow

### Feature Development
1. Create feature branch: `git checkout -b feature/my-feature`
2. Make changes
3. Push branch: `git push origin feature/my-feature`
4. **CI build runs automatically** ← Validates changes
5. Open pull request to `main`
6. **CI build runs on PR** ← Final validation
7. Merge PR
8. **Publish workflow runs** ← Automatic release

### Version Bumping

To release a new version:

1. **Update version in `.csproj` files:**
   ```xml
   <!-- tools/Swap.CLI/Swap.CLI.csproj -->
   <Version>0.2.0</Version>
   
   <!-- framework/Swap.Htmx/Swap.Htmx.csproj -->
   <Version>0.2.0</Version>
   
   <!-- And all other packages... -->
   ```

2. **Update CHANGELOG.md:**
   ```markdown
   ## [0.2.0] - 2025-11-30
   
   ### Added
   - Relationship generation
   - etc.
   ```

3. **Commit and push:**
   ```bash
   git add .
   git commit -m "chore: bump version to 0.2.0"
   git push
   ```

4. **Merge PR to main** → Automatic publish! 🚀

### Pre-release Versions

For alpha/beta releases:
```xml
<Version>0.2.0-alpha.1</Version>
```

Publish workflow will:
- Mark GitHub release as "pre-release"
- Push to NuGet with pre-release flag
- Allow users to install with `--prerelease` flag

## Monitoring

### Build Status
- View workflow runs: https://github.com/jdtoon/swap/actions
- Check badge in README.md
- Email notifications on failure (GitHub settings)

### NuGet Package Status
- Swap.CLI: https://www.nuget.org/packages/Swap.CLI
- Swap.Htmx: https://www.nuget.org/packages/Swap.Htmx
- Swap.Patterns: https://www.nuget.org/packages/Swap.Patterns
- Swap.Testing: https://www.nuget.org/packages/Swap.Testing

### GitHub Releases
- All releases: https://github.com/jdtoon/swap/releases
- Latest release: https://github.com/jdtoon/swap/releases/latest

## Troubleshooting

### CI Build Failing
1. Check build logs in Actions tab
2. Reproduce locally: `dotnet build`
3. Fix errors, push changes
4. CI re-runs automatically

### Test Failures
1. Check test logs in Actions tab
2. Reproduce locally: `dotnet test`
3. Fix failing tests
4. Ensure all tests pass locally before pushing

### Publish Failing

**NuGet push error:**
- Check `NUGET_API_KEY` secret is valid
- Verify API key has push permissions
- Ensure package version doesn't already exist

**Git tag error:**
- Tag might already exist
- Delete tag: `git tag -d v0.1.0 && git push --delete origin v0.1.0`
- Re-run workflow

**GitHub release error:**
- Check repository permissions
- Verify `GITHUB_TOKEN` has write permissions
- Manually create release if needed

## Local Testing

Test workflows locally with `act`:

```bash
# Install act (https://github.com/nektos/act)
choco install act-cli  # Windows
brew install act       # macOS

# Test CI build
act push

# Test publish (dry-run)
act pull_request --secret NUGET_API_KEY=dummy
```

## Notes

- **Zero manual steps:** Entire release process is automated
- **Safe defaults:** Workflows are idempotent and skip duplicates
- **Version control:** All versions tracked via git tags
- **Rollback:** Previous versions always available on NuGet
- **Documentation:** Keep CHANGELOG.md updated for release notes

---

**Related Documentation:**
- [CONTRIBUTING.md](../../CONTRIBUTING.md) - Development workflow
- [CHANGELOG.md](../../CHANGELOG.md) - Version history
- [scripts/README.md](../../scripts/README.md) - Local development

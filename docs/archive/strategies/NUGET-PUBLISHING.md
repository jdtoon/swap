# NuGet Publishing Workflow

**Status**: ✅ Configured  
**Strategy**: Pre-release packages from `develop`, stable from `main`

## Overview

NetMX uses NuGet.org for all package distribution with version-based differentiation:

- **develop branch** → Pre-release packages (`0.1.0-dev.20251020.abc1234`)
- **main branch** → Stable packages (`0.1.0`)

## Workflow Summary

### Automatic Publishing (CI/CD)

**On Push to `develop`**:
```
Build → Test → Pack with -dev suffix → Publish to NuGet.org
```
Example version: `0.1.0-dev.20251020.abc1234`

**On Release from `main`**:
```
Create GitHub Release → Build → Test → Pack → Publish to NuGet.org
```
Example version: `0.1.0` (stable)

### Package Naming

All framework packages follow this pattern:
- NetMX.Core
- NetMX.Ddd.Domain
- NetMX.Ddd.Application
- NetMX.AspNetCore.Mvc
- NetMX.EntityFrameworkCore
- etc.

## Developer Usage

### Installing Pre-Release Packages

```bash
# Install latest dev version
dotnet add package NetMX.Core --version "0.1.0-dev*"

# Install specific dev version
dotnet add package NetMX.Core --version "0.1.0-dev.20251020.abc1234"
```

### Installing Stable Packages

```bash
# Install latest stable
dotnet add package NetMX.Core

# Install specific stable version
dotnet add package NetMX.Core --version "0.1.0"
```

### NuGet.config for Pre-Release

To see pre-release packages in Visual Studio/Rider:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
  <config>
    <add key="includePreRelease" value="true" />
  </config>
</configuration>
```

## CI/CD Configuration

### GitHub Secrets Required

Navigate to: `Settings → Secrets and variables → Actions`

Add secret:
- **Name**: `NUGET_API_KEY`
- **Value**: Your NuGet.org API key

### Getting a NuGet.org API Key

1. Go to https://www.nuget.org/account/apikeys
2. Click "Create"
3. **Key Name**: `NetMX-CI-CD`
4. **Expiration**: 365 days
5. **Scopes**: 
   - ✅ Push
   - ✅ Push new packages and package versions
6. **Glob Pattern**: `NetMX.*`
7. Click "Create"
8. Copy the key (shown only once!)
9. Add to GitHub Secrets

### Workflow Files

**`.github/workflows/ci-build.yml`** (Updated):
- Builds on every push to `develop` and `main`
- Runs all tests
- Packs NuGet packages
- **On `develop`**: Publishes pre-release to NuGet.org
- **On `main` (PR)**: Dry run only

**`.github/workflows/publish-nuget.yml`** (Existing):
- Triggered by GitHub Releases
- Pre-release → GitHub Packages (dev)
- Full release → NuGet.org (production)

## Version Strategy

### Pre-Release Versions

Format: `{major}.{minor}.{patch}-dev.{date}.{commit}`

Examples:
- `0.1.0-dev.20251020.abc1234` - October 20, 2025, commit abc1234
- `0.2.0-dev.20251105.def5678` - November 5, 2025, commit def5678

### Stable Versions

Format: `{major}.{minor}.{patch}`

Examples:
- `0.1.0` - First stable release
- `0.2.0` - Second stable release
- `1.0.0` - Production ready

### Semantic Versioning Rules

- **Major** (x.0.0): Breaking changes
- **Minor** (0.x.0): New features (backwards compatible)
- **Patch** (0.0.x): Bug fixes

## Testing Pre-Release Packages

### Before Merging to Main

1. Push to `develop` branch
2. CI builds and publishes `0.1.0-dev.YYYYMMDD.sha`
3. Test in a sample project:

```bash
# Create test project
dotnet new web -o TestNetMX
cd TestNetMX

# Install pre-release packages
dotnet add package NetMX.Core --version "0.1.0-dev*"
dotnet add package NetMX.AspNetCore.Mvc --version "0.1.0-dev*"

# Test functionality
dotnet run
```

4. If tests pass → merge to `main`
5. Create GitHub Release → stable packages published

## Troubleshooting

### Issue: "API key is invalid"

**Solution**: Regenerate API key on NuGet.org and update GitHub secret.

### Issue: "Package already exists"

**Solution**: 
- For stable: Increment version number
- For dev: Wait for next commit (auto-increments)

### Issue: Pre-release not showing in VS

**Solution**: Enable "Include prerelease" checkbox in NuGet Package Manager.

### Issue: Build fails but package published

**Solution**: GitHub Actions uses `--skip-duplicate` flag, so re-running is safe.

## Future Enhancements

### Phase 2: Symbol Packages

Enable debugging with source code:

```xml
<PropertyGroup>
  <IncludeSymbols>true</IncludeSymbols>
  <SymbolPackageFormat>snupkg</SymbolPackageFormat>
</PropertyGroup>
```

### Phase 3: Package Signing

Sign packages for authenticity:

```bash
dotnet nuget sign ./nupkg/*.nupkg \
  --certificate-path cert.pfx \
  --timestamper http://timestamp.digicert.com
```

### Phase 4: Azure Artifacts (Pro Packages)

For paid/private modules:

```yaml
- name: Push to Azure Artifacts
  run: |
    dotnet nuget push ./nupkg/*.nupkg \
      --api-key ${{ secrets.AZURE_ARTIFACTS_KEY }} \
      --source https://pkgs.dev.azure.com/netmx/_packaging/netmx-pro/nuget/v3/index.json
```

## Package Lifecycle

```
Developer Commits
    ↓
Push to develop
    ↓
CI Build (zero warnings ✅)
    ↓
Run Tests (87 tests ✅)
    ↓
Pack with -dev suffix
    ↓
Publish to NuGet.org
    ↓
Developers test pre-release
    ↓
Merge to main (if stable)
    ↓
Create GitHub Release
    ↓
Publish stable to NuGet.org
    ↓
Developers use stable version
```

## Best Practices

1. **Never skip tests** - Tests must pass before publish
2. **Use semantic versioning** - Clear breaking change communication
3. **Test pre-releases** - Always install and test dev packages
4. **Document breaking changes** - Update CHANGELOG.md
5. **Rotate API keys** - Regenerate annually for security

## Monitoring

### NuGet.org Package Stats

View at: `https://www.nuget.org/packages/NetMX.Core/`

Metrics:
- Total downloads
- Version distribution
- Download trends
- Dependency graph

### GitHub Actions

View at: `https://github.com/toonjd/netmx/actions`

Metrics:
- Build success rate
- Test pass rate
- Average build time
- Package publish rate

---

**Next Steps**:
1. ✅ Configure `NUGET_API_KEY` secret in GitHub
2. ✅ Push to `develop` to test workflow
3. ✅ Verify pre-release package appears on NuGet.org
4. ✅ Test installation in sample project
5. ✅ Document any issues or improvements

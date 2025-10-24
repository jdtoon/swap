# NetMX GitHub Repository Setup Guide

## Repository Structure

### Open Source Repository: `netmx`
- Framework packages (NetMX.Core, NetMX.Ddd.*, NetMX.Htmx, etc.)
- Community modules (Identity, Audit, etc.)
- Minimal template
- CLI tool
- Public documentation

### Private Repository: `netmx-pro` (Future)
- Pro modules (Multi-tenancy, Advanced CMS, etc.)
- SaaS template
- Enterprise features
- Private documentation

## Environments & NuGet Deployment

### Development/Testing (GitHub Packages)
- **Trigger**: Manual dispatch or pre-release tags
- **Version Format**: `1.0.0-beta`, `1.0.0-alpha.1`
- **Target**: GitHub Packages (private feed)
- **Purpose**: Internal testing, alpha/beta releases
- **Access**: Requires GitHub authentication

**To install from GitHub Packages:**
```bash
dotnet nuget add source \
  --name github \
  --username toonjd \
  --password $GITHUB_TOKEN \
  https://nuget.pkg.github.com/toonjd/index.json

dotnet add package NetMX.Core --version 1.0.0-beta
```

### Production (NuGet.org)
- **Trigger**: GitHub Release (non-prerelease) or manual dispatch
- **Version Format**: `1.0.0`, `1.1.0`, `2.0.0`
- **Target**: NuGet.org (public feed)
- **Purpose**: Stable releases for public consumption
- **Access**: Public, no authentication needed

**To install from NuGet.org:**
```bash
dotnet add package NetMX.Core --version 1.0.0
```

## Workflow Overview

### 1. CI Build (`ci-build.yml`)
- **Runs on**: Every push and PR to `master`/`develop`
- **Purpose**: Ensure code compiles and tests pass
- **Actions**:
  - Restore dependencies
  - Build framework and template
  - Run tests
  - Create NuGet packages (dry run, not published)
  - Upload artifacts for inspection

### 2. Publish to NuGet (`publish-nuget.yml`)
- **Runs on**: GitHub Release or manual dispatch
- **Environments**:
  - `nuget-dev`: GitHub Packages (beta versions)
  - `nuget-production`: NuGet.org (stable versions)

## Setup Instructions

### Step 1: Configure GitHub Repository Settings

1. **Enable GitHub Packages**:
   - Already enabled by default for repositories

2. **Create Environments**:
   ```bash
   # Go to: Settings → Environments → New environment
   # Create two environments:
   - nuget-dev (for development/testing)
   - nuget-production (for stable releases)
   ```

3. **Add Protection Rules** (for `nuget-production`):
   - Require manual approval before deployment
   - Restrict to specific branches (e.g., `master` only)

### Step 2: Configure Secrets

#### Repository Secrets (Settings → Secrets and variables → Actions)

1. **`GITHUB_TOKEN`**:
   - Automatically provided by GitHub
   - Used for GitHub Packages

2. **`NUGET_API_KEY`**:
   - Create at: https://www.nuget.org/account/apikeys
   - Scope: Push new packages and package versions
   - Glob pattern: `NetMX.*`
   - Add to repository secrets

### Step 3: Update Package Metadata

Each `.csproj` file in `framework/` needs package metadata:

```xml
<PropertyGroup>
  <TargetFramework>net9.0</TargetFramework>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
  
  <!-- NuGet Package Metadata -->
  <GeneratePackageOnBuild>false</GeneratePackageOnBuild>
  <PackageId>NetMX.Core</PackageId>
  <Version>0.1.0</Version>
  <Authors>jdtoon</Authors>
  <Company>NetMX</Company>
  <Product>NetMX Framework</Product>
  <Description>Core abstractions and utilities for NetMX framework</Description>
  <PackageProjectUrl>https://github.com/toonjd/netmx</PackageProjectUrl>
  <RepositoryUrl>https://github.com/toonjd/netmx</RepositoryUrl>
  <RepositoryType>git</RepositoryType>
  <PackageTags>netmx;framework;ddd;htmx;modular-monolith</PackageTags>
  <PackageLicenseExpression>MIT</PackageLicenseExpression>
  <PackageReadmeFile>README.md</PackageReadmeFile>
  <PackageIcon>icon.png</PackageIcon>
</PropertyGroup>

<ItemGroup>
  <None Include="README.md" Pack="true" PackagePath="\" />
  <None Include="..\..\docs\icon.png" Pack="true" PackagePath="\" />
</ItemGroup>
```

## Development Workflow

### For New Features (Development)

1. **Create feature branch**:
   ```bash
   git checkout -b feature/my-awesome-feature
   git push -u origin feature/my-awesome-feature
   ```

2. **Open PR to `develop`**:
   - CI build runs automatically
   - Requires tests to pass

3. **Merge to `develop`**:
   - CI build runs
   - No packages published yet

### For Beta/Testing Releases

1. **Create pre-release tag**:
   ```bash
   git tag v0.1.0-beta.1
   git push origin v0.1.0-beta.1
   ```

2. **Or use manual workflow**:
   - Go to: Actions → Publish to NuGet → Run workflow
   - Select: `nuget-dev` environment
   - Enter version: `0.1.0-beta.1`

3. **Packages published to GitHub Packages**:
   - Available at: `https://github.com/toonjd/netmx/packages`

### For Stable Production Releases

1. **Merge `develop` to `master`**:
   ```bash
   git checkout master
   git merge develop
   git push origin master
   ```

2. **Create GitHub Release**:
   ```bash
   # Via GitHub UI or CLI:
   gh release create v1.0.0 \
     --title "NetMX v1.0.0" \
     --notes "First stable release" \
     --target master
   ```

3. **Workflow runs automatically**:
   - Builds and tests
   - Publishes to NuGet.org
   - Requires approval in `nuget-production` environment

## Version Management

### Semantic Versioning

- **Major (X.0.0)**: Breaking changes
- **Minor (1.X.0)**: New features (backward compatible)
- **Patch (1.0.X)**: Bug fixes

### Pre-release Identifiers

- `1.0.0-alpha.1`: Early development
- `1.0.0-beta.1`: Feature complete, testing
- `1.0.0-rc.1`: Release candidate

### How to Update Versions

**Option 1: Manual (in .csproj)**
```xml
<Version>1.0.0</Version>
```

**Option 2: Via Build Parameter (CI/CD)**
```bash
dotnet pack /p:Version=1.0.0
```

**Option 3: Automated (from git tags)**
- Use tools like GitVersion or Nerdbank.GitVersioning

## Testing Packages Locally

Before publishing, test packages locally:

```bash
# Build packages
dotnet pack framework/NetMX.sln --configuration Release --output ./local-packages

# Add local source
dotnet nuget add source $(pwd)/local-packages --name local

# Install from local source
dotnet new console -n TestApp
cd TestApp
dotnet add package NetMX.Core --source local --version 0.1.0

# Remove local source when done
dotnet nuget remove source local
```

## Monitoring & Maintenance

### GitHub Actions
- Monitor workflow runs: `https://github.com/toonjd/netmx/actions`
- Review deployment history in Environments

### NuGet.org
- Package statistics: `https://www.nuget.org/packages/NetMX.Core`
- Download counts, dependencies, versions

### GitHub Packages
- Package registry: `https://github.com/toonjd/netmx/packages`
- Usage by other projects

## Troubleshooting

### Package Push Fails
- Check API key is valid and not expired
- Verify package version doesn't already exist
- Ensure package ID follows NuGet naming conventions

### GitHub Packages Authentication
```bash
# Create Personal Access Token with packages:read scope
dotnet nuget add source \
  --name github \
  --username YOUR_USERNAME \
  --password YOUR_PAT \
  https://nuget.pkg.github.com/toonjd/index.json
```

### Version Conflicts
- Always increment version for new releases
- Use pre-release versions for testing
- Never re-publish same version

## Next Steps

1. ✅ Configure GitHub environments
2. ✅ Add NuGet API key to secrets
3. ✅ Update all `.csproj` files with package metadata
4. ✅ Create initial GitHub release (v0.1.0-alpha)
5. ✅ Test GitHub Packages workflow
6. ✅ Publish first stable release to NuGet.org (v1.0.0)

# 🚀 Quick Start: Complete GitHub & NuGet Setup

This guide will walk you through the final manual steps to activate your CI/CD pipeline.

## ✅ What's Already Done

- ✅ All 9 framework packages have NuGet metadata
- ✅ GitHub Actions workflows created (CI + Publish)
- ✅ Package icon created
- ✅ Documentation written
- ✅ **Everything committed and pushed to GitHub!**

## 🎯 Remaining Manual Steps (10 minutes)

### Step 1: Verify CI Pipeline (2 minutes)

Your push just triggered the CI build! Check its status:

1. Visit: https://github.com/toonjd/netmx/actions
2. Look for the "CI Build" workflow
3. Click on it to see the build progress
4. Verify all jobs pass (should see green checkmarks)

**Expected outcome**: All framework projects build successfully, tests run (when added), NuGet packages created as artifacts.

### Step 2: Configure GitHub Environments (3 minutes)

1. Go to: https://github.com/toonjd/netmx/settings/environments
2. Click **"New environment"**
3. Create **"nuget-dev"**:
   - Name: `nuget-dev`
   - No protection rules needed (for development)
   - Click "Configure environment"
4. Create **"nuget-production"**:
   - Name: `nuget-production`
   - ✅ Enable "Required reviewers" (add yourself)
   - ⏱️ Optional: Add 5-minute wait timer
   - Click "Configure environment"

**Why**: Environments provide deployment gates. Dev releases are automatic, but production requires your approval.

### Step 3: Add NuGet.org API Key (2 minutes)

1. Get your NuGet.org API key:
   - Visit: https://www.nuget.org/account/apikeys
   - Create new key with "Push" permission for "netmx*" pattern
   - Copy the key (you'll only see it once!)

2. Add to GitHub Secrets:
   - Go to: https://github.com/toonjd/netmx/settings/secrets/actions
   - Click "New repository secret"
   - Name: `NUGET_API_KEY`
   - Value: [paste your API key]
   - Click "Add secret"

**Why**: This allows the production workflow to publish to NuGet.org.

### Step 4: Create First Alpha Release (3 minutes)

**Option A: Via GitHub Web UI**
1. Visit: https://github.com/toonjd/netmx/releases/new
2. Tag: `v0.1.0-alpha`
3. Title: "NetMX v0.1.0-alpha - First Alpha Release"
4. Description:
   ```markdown
   🎉 First alpha release of NetMX framework!
   
   ## What's New
   - 9 framework packages for building modular, HTMX-first applications
   - DDD building blocks (entities, aggregates, repositories)
   - EF Core integration with multi-tenancy support
   - ASP.NET Core extensions and middleware
   - Strongly-typed HTMX helpers
   
   ## Packages
   - NetMX.Core
   - NetMX.Ddd.Domain
   - NetMX.Ddd.Application.Contracts
   - NetMX.Ddd.Application
   - NetMX.Data
   - NetMX.EntityFrameworkCore
   - NetMX.AspNetCore.Core
   - NetMX.AspNetCore.Mvc
   - NetMX.Htmx
   
   ## Installation
   ```bash
   dotnet add package NetMX.Core --version 0.1.0-alpha --source https://nuget.pkg.github.com/toonjd/index.json
   ```
   
   **Note**: This is an alpha release for testing. Not ready for production use.
   ```
5. ✅ Check "This is a pre-release"
6. Click "Publish release"

**Option B: Via PowerShell (when GitHub CLI is working)**
```powershell
gh release create v0.1.0-alpha --prerelease --title "NetMX v0.1.0-alpha - First Alpha Release" --notes "First alpha release for testing framework packages"
```

**What happens**: This triggers the `publish-nuget.yml` workflow, which publishes all packages to GitHub Packages.

### Step 5: Verify Packages Published (2 minutes)

1. Wait for the publish workflow to complete
2. Visit: https://github.com/jdtoon?tab=packages
3. You should see all 9 NetMX packages listed!

## 📦 Testing Your Packages

### Installing from GitHub Packages

First, authenticate with GitHub Packages:

```powershell
# Create a Personal Access Token (PAT) at:
# https://github.com/settings/tokens/new
# Select scopes: read:packages

# Store your PAT
$env:GITHUB_TOKEN = "your_github_pat_here"

# Add GitHub Packages as a NuGet source
dotnet nuget add source https://nuget.pkg.github.com/toonjd/index.json `
  --name github `
  --username jdtoon `
  --password $env:GITHUB_TOKEN `
  --store-password-in-clear-text
```

Then install packages:

```powershell
dotnet new console -n TestNetMX
cd TestNetMX
dotnet add package NetMX.Core --version 0.1.0-alpha
```

## 🎯 Next Steps

### Immediate (This Week)

1. **Test the alpha release** - Install packages in a test project
2. **Complete NetMX.Htmx** - Implement remaining HTMX helper methods
3. **Write documentation** - Update README.md with getting started guide
4. **Add unit tests** - Start with NetMX.Core and NetMX.Htmx

### Short Term (Next 2 Weeks)

1. **Enhance CLI tool** - Implement `netmx new` and `netmx add module`
2. **Create samples** - Build example applications
3. **Beta release** - v0.1.0-beta after testing and polish
4. **Documentation site** - Consider GitHub Pages or Docusaurus

### Production Release (When Ready)

1. Ensure all tests pass
2. Complete documentation
3. Update version to `1.0.0` in all .csproj files
4. Create release (without prerelease flag)
5. Workflow automatically publishes to NuGet.org
6. Approve the production deployment
7. Announce on social media, Reddit, etc.

## 🔧 Troubleshooting

### CI Build Fails
- Check the Actions tab for error details
- Verify all .csproj files build locally: `dotnet build framework/NetMX.sln`
- Check for missing dependencies

### Can't Create Environments
- Ensure you have admin access to the repository
- Repository must be public or have GitHub Pro for private repos

### NuGet Package Not Found
- GitHub Packages require authentication (see above)
- Verify package was published: check Actions → Publish workflow
- Check package visibility settings

### Production Deployment Stuck
- Check if environment protection rules are blocking
- Go to Actions → Publish workflow → Review deployments
- Click "Review deployments" and approve

## 📚 Reference Links

- **Repository**: https://github.com/toonjd/netmx
- **Actions**: https://github.com/toonjd/netmx/actions
- **Packages**: https://github.com/jdtoon?tab=packages
- **Documentation**: See `/docs` folder
- **Roadmap**: `/docs/ROADMAP.md`
- **GitHub Setup Guide**: `/docs/GITHUB-SETUP.md`

---

**Need Help?** Check the troubleshooting section in `docs/GITHUB-SETUP.md` or review the workflow YAML files in `.github/workflows/`.

**Status Check**: Visit https://github.com/toonjd/netmx/actions to see your CI pipeline running right now! 🚀

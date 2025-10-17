# 🔧 Complete GitHub Setup - Step by Step

This guide provides **exact click-by-click instructions** for configuring your GitHub repository for NuGet publishing.

## ✅ Prerequisites Checklist

Before you begin, ensure you have:
- [x] GitHub repository created: https://github.com/jdtoon/netmx
- [x] All code pushed to master branch
- [x] Workflows committed to `.github/workflows/`
- [x] Admin access to the repository

---

## Part 1: Configure GitHub Environments (5 minutes)

### Step 1.1: Access Environments Settings

1. Navigate to: **https://github.com/jdtoon/netmx/settings/environments**
2. You should see "Environments" in the left sidebar under "Code and automation"

### Step 1.2: Create Development Environment

1. Click **"New environment"** button (top right)
2. Enter name: `nuget-dev`
3. Click **"Configure environment"**
4. **No protection rules needed** for dev environment
5. Leave all checkboxes unchecked
6. Scroll down and verify it says "Configured" ✅

**What it looks like:**
```
Environment: nuget-dev
├── Deployment branches: All branches
├── Environment protection rules: None
└── Environment secrets: (will add later if needed)
```

### Step 1.3: Create Production Environment

1. Click **"New environment"** button again
2. Enter name: `nuget-production`
3. Click **"Configure environment"**

**Now configure protection rules:**

#### ✅ Required Reviewers
1. Check the box: **"Required reviewers"**
2. Click in the search box that appears
3. Type your GitHub username: `jdtoon`
4. Select yourself from the dropdown
5. You should see your avatar appear

**Why**: Prevents accidental production deployments. You must manually approve each release to NuGet.org.

#### ⏱️ Wait Timer (Optional but Recommended)
1. Check the box: **"Wait timer"**
2. Enter: `5` minutes
3. This gives you a 5-minute window to cancel if needed

**Why**: Adds a delay before deployment starts, giving you time to catch mistakes.

#### 🌳 Deployment Branches (Leave Default)
- Leave "All branches" selected
- This allows deployments from any branch (workflow will restrict to releases anyway)

**Final Production Environment Configuration:**
```
Environment: nuget-production
├── Deployment branches: All branches
├── Environment protection rules:
│   ├── ✅ Required reviewers: jdtoon
│   └── ✅ Wait timer: 5 minutes
└── Environment secrets: (will add later if needed)
```

4. Click **"Save protection rules"** at the bottom

### Step 1.4: Verify Environments

You should now see both environments listed:
- `nuget-dev` - No protection rules
- `nuget-production` - Required reviewers (1)

✅ **Environments configured successfully!**

---

## Part 2: Add NuGet.org API Key (3 minutes)

### Step 2.1: Generate NuGet.org API Key

1. **Login to NuGet.org**: https://www.nuget.org
2. Click your username (top right) → **API Keys**
3. Click **"+ Create"** button

**Configure the API Key:**

| Field | Value |
|-------|-------|
| **Key Name** | `NetMX GitHub Actions` |
| **Expires In** | `365 days` (or your preference) |
| **Scopes** | Select **"Push"** (and "Push new packages and package versions") |
| **Glob Pattern** | `NetMX.*` |
| **Package Owners** | (leave empty or select your account) |

4. Click **"Create"**
5. **IMPORTANT**: Copy the API key immediately! You'll only see it once.
   - It looks like: `oy2abc...xyz123` (long alphanumeric string)

### Step 2.2: Add API Key to GitHub Secrets

1. Navigate to: **https://github.com/jdtoon/netmx/settings/secrets/actions**
2. Click **"New repository secret"** (green button, top right)

**Enter the secret:**

| Field | Value |
|-------|-------|
| **Name** | `NUGET_API_KEY` (exactly as shown, case-sensitive) |
| **Secret** | Paste your NuGet.org API key |

3. Click **"Add secret"**

### Step 2.3: Verify Secret Added

You should see:
```
Repository secrets
└── NUGET_API_KEY     Updated now
```

**🔒 Security Note**: The value is now hidden. You won't be able to see it again, only update or delete it.

✅ **NuGet API Key configured successfully!**

---

## Part 3: Fix and Push CI Workflow (2 minutes)

The workflow needs a small fix to restore template dependencies separately.

### Already Fixed!

I've already updated `.github/workflows/ci-build.yml` with the fix. Now commit and push:

```powershell
cd c:\jd\netmx
git add .github/workflows/ci-build.yml
git commit -m "fix(ci): Add separate restore step for template solution"
git push
```

### Step 3.1: Verify CI Build Passes

1. Go to: **https://github.com/jdtoon/netmx/actions**
2. Find the latest "CI Build & Test" workflow run
3. Click on it to see details
4. Wait for it to complete (should take ~2-3 minutes)
5. Verify all steps have green checkmarks ✅

**Expected outcome:**
```
✅ Setup .NET
✅ Restore Framework dependencies
✅ Build Framework
✅ Restore Template dependencies
✅ Build Template
✅ Run Tests
✅ Pack NuGet Packages (Dry Run)
✅ Upload Build Artifacts
```

---

## Part 4: Create Your First Alpha Release (3 minutes)

Once CI is green, you're ready to release!

### Step 4.1: Navigate to Releases

1. Go to: **https://github.com/jdtoon/netmx/releases**
2. Click **"Create a new release"** or **"Draft a new release"**

### Step 4.2: Configure the Release

Fill in the form:

#### 📌 Choose a tag
- Click the tag dropdown
- Type: `v0.1.0-alpha`
- Click **"+ Create new tag: v0.1.0-alpha on publish"**

#### 🌳 Target: master (should be selected by default)

#### 📝 Release title
```
NetMX v0.1.0-alpha - First Alpha Release
```

#### 📄 Describe this release

Paste this markdown:

```markdown
🎉 **First alpha release of the NetMX framework!**

This is an early preview release for testing the framework packages and CI/CD infrastructure.

## 📦 What's Included

### Framework Packages (9 total)
- **NetMX.Core** - Core abstractions and dependency injection
- **NetMX.Ddd.Domain** - DDD building blocks (entities, aggregates, repositories)
- **NetMX.Ddd.Application.Contracts** - DTOs and service interfaces
- **NetMX.Ddd.Application** - Application services and use cases
- **NetMX.Data** - Data access abstractions
- **NetMX.EntityFrameworkCore** - EF Core integration with DDD support
- **NetMX.AspNetCore.Core** - ASP.NET Core middleware and extensions
- **NetMX.AspNetCore.Mvc** - MVC extensions and controller base classes
- **NetMX.Htmx** - Strongly-typed HTMX helpers

### Modules
- **Identity Module** - User and role management with HTMX UI

### Templates
- **Modular Monolith** - Clean, minimal starter template

## 🚀 Installation

Packages are published to GitHub Packages. To install:

1. **Authenticate with GitHub Packages** (one-time setup):
   ```bash
   # Create a PAT at: https://github.com/settings/tokens/new
   # Select scope: read:packages
   
   dotnet nuget add source https://nuget.pkg.github.com/jdtoon/index.json \
     --name github \
     --username jdtoon \
     --password YOUR_GITHUB_PAT \
     --store-password-in-clear-text
   ```

2. **Install packages**:
   ```bash
   dotnet add package NetMX.Core --version 0.1.0-alpha
   dotnet add package NetMX.AspNetCore.Core --version 0.1.0-alpha
   dotnet add package NetMX.Htmx --version 0.1.0-alpha
   # ... etc
   ```

## ⚠️ Alpha Release Notice

This is an **alpha release** intended for:
- ✅ Testing and feedback
- ✅ Early adopters and contributors
- ✅ Exploring the framework architecture

**Not recommended for production use yet!**

## 🐛 Known Limitations

- Limited test coverage (tests being added)
- CLI tool partially implemented
- Documentation in progress
- Some XML documentation warnings

## 📚 Documentation

- **Getting Started**: [QUICK-START-SETUP.md](https://github.com/jdtoon/netmx/blob/master/docs/QUICK-START-SETUP.md)
- **Roadmap**: [ROADMAP.md](https://github.com/jdtoon/netmx/blob/master/docs/ROADMAP.md)
- **Architecture**: [copilot-instructions.md](https://github.com/jdtoon/netmx/blob/master/.github/copilot-instructions.md)

## 🤝 Contributing

We welcome contributions! See [CONTRIBUTING.md](https://github.com/jdtoon/netmx/blob/master/CONTRIBUTING.md)

## 📝 Changelog

### Added
- Complete framework SDK (9 packages)
- Identity module with HTMX UI
- Modular monolith template
- CI/CD pipeline with GitHub Actions
- Comprehensive documentation
- HTMX-first architecture

### Technical Details
- .NET 9.0 LTS
- EF Core 9.0.10
- PostgreSQL support
- HTMX 2.0.4
- Bulma 1.0.4

---

**Next Steps**: Beta release coming soon with enhanced testing, documentation, and CLI tool!

**Feedback**: Please open [issues](https://github.com/jdtoon/netmx/issues) or [discussions](https://github.com/jdtoon/netmx/discussions) with your thoughts!
```

#### ✅ Set as a pre-release
- **Check this box**: "Set as a pre-release"
- This marks it as alpha/beta, not production-ready

#### ❌ Set as the latest release (unchecked)
- Leave unchecked for alpha
- We'll check this for v1.0.0

### Step 4.3: Publish!

1. Click **"Publish release"** (green button at bottom)
2. You'll be redirected to the release page

---

## Part 5: Watch the Deployment (5 minutes)

### Step 5.1: Monitor the Workflow

1. Go to: **https://github.com/jdtoon/netmx/actions**
2. You should see **"Publish to NuGet"** workflow starting
3. Click on it to watch progress

### Step 5.2: Understand What Happens

The workflow has **two jobs**:

#### Job 1: `publish-dev` (Automatic)
- Runs immediately
- Builds all 9 packages
- Publishes to **GitHub Packages**
- Target: `https://nuget.pkg.github.com/jdtoon/index.json`
- Takes ~3-4 minutes

**You'll see:**
```
✅ Checkout code
✅ Setup .NET
✅ Restore dependencies
✅ Build
✅ Pack NuGet packages
✅ Publish to GitHub Packages (9 packages)
```

#### Job 2: `publish-production` (Requires Approval)
- **Waits for your approval** due to environment protection
- You'll see: "Waiting for approval" with a timer
- **DO NOT APPROVE for alpha releases!**
- This job is only for stable v1.0.0+ releases

### Step 5.3: Approve Dev Deployment (if needed)

The dev environment has no protection rules, so it should run automatically. If it asks for approval:

1. Click **"Review deployments"**
2. Check the box next to `nuget-dev`
3. Click **"Approve and deploy"**

### Step 5.4: Wait for Completion

Wait for the `publish-dev` job to complete (~3-4 minutes):
- ✅ Green checkmark = Success!
- ❌ Red X = Failed (check logs)

---

## Part 6: Verify Packages Published (2 minutes)

### Step 6.1: Check GitHub Packages

1. Go to: **https://github.com/jdtoon?tab=packages**
2. You should see **9 packages** listed:
   - NetMX.AspNetCore.Core
   - NetMX.AspNetCore.Mvc
   - NetMX.Core
   - NetMX.Data
   - NetMX.Ddd.Application
   - NetMX.Ddd.Application.Contracts
   - NetMX.Ddd.Domain
   - NetMX.EntityFrameworkCore
   - NetMX.Htmx

3. Click on any package to see details
4. You should see version `0.1.0-alpha`

### Step 6.2: Test Installation

In a new terminal:

```powershell
# Create test project
cd c:\temp
dotnet new console -n TestNetMX
cd TestNetMX

# Try to install (will fail without auth)
dotnet add package NetMX.Core --version 0.1.0-alpha

# You'll get: "Unable to find package NetMX.Core"
# This is expected! GitHub Packages require authentication
```

**To actually install, you need a GitHub Personal Access Token (PAT):**

1. Create PAT: https://github.com/settings/tokens/new
   - Select scope: `read:packages`
   - Expiration: 90 days (or your preference)
   - Click "Generate token"
   - **Copy the token!**

2. Add GitHub Packages source:
```powershell
dotnet nuget add source https://nuget.pkg.github.com/jdtoon/index.json `
  --name github `
  --username jdtoon `
  --password ghp_YourTokenHere `
  --store-password-in-clear-text
```

3. Now install works:
```powershell
dotnet add package NetMX.Core --version 0.1.0-alpha
# Success! ✅
```

---

## 📋 Summary: What You've Configured

### ✅ Environments
- **nuget-dev**: No protection, auto-deploys on pre-releases
- **nuget-production**: Requires your approval + 5 min wait timer

### ✅ Secrets
- **NUGET_API_KEY**: For publishing to NuGet.org (production releases)

### ✅ Workflows
- **CI Build**: Runs on every push, validates code compiles
- **Publish**: Runs on releases, publishes packages

### ✅ First Release
- **v0.1.0-alpha**: Published to GitHub Packages
- **9 packages**: All available for testing

---

## 🎯 Next Steps

### Immediate
1. ✅ Verify CI build passes (after the fix)
2. ✅ Create v0.1.0-alpha release
3. ✅ Verify packages appear in GitHub Packages
4. ✅ Test installation with PAT

### This Week
1. Add XML documentation to reduce warnings
2. Complete NetMX.Htmx implementation
3. Write unit tests for core packages
4. Test packages in a real project

### Future Production Release (v1.0.0)
1. Update versions to `1.0.0` in all .csproj files
2. Ensure all tests pass
3. Complete documentation
4. Create release **without** pre-release flag
5. **Approve production deployment** when workflow asks
6. Packages auto-publish to NuGet.org! 🎉

---

## 🆘 Troubleshooting

### Environment Protection Rules Not Working
- Verify you're an admin on the repository
- Check that environment names match exactly: `nuget-production`
- Refresh the Actions page

### Workflow Doesn't Trigger
- Verify tag starts with `v` (e.g., `v0.1.0-alpha`)
- Check workflow file syntax in `.github/workflows/publish-nuget.yml`
- Try creating release via web UI instead of CLI

### Packages Not Found After Publishing
- Check Actions tab for errors
- Verify job completed successfully
- GitHub Packages can take 1-2 minutes to index
- Ensure you're authenticated with correct PAT

### Can't Install Packages
- Verify PAT has `read:packages` scope
- Check username in nuget source matches repository owner
- Try removing and re-adding the source

---

## 📚 Quick Reference

| What | URL |
|------|-----|
| **Environments** | https://github.com/jdtoon/netmx/settings/environments |
| **Secrets** | https://github.com/jdtoon/netmx/settings/secrets/actions |
| **Actions** | https://github.com/jdtoon/netmx/actions |
| **Releases** | https://github.com/jdtoon/netmx/releases |
| **Packages** | https://github.com/jdtoon?tab=packages |
| **NuGet.org API Keys** | https://www.nuget.org/account/apikeys |
| **GitHub PAT** | https://github.com/settings/tokens/new |

---

**You're all set! Follow this guide step-by-step and you'll have a professional CI/CD pipeline running in ~20 minutes.** 🚀

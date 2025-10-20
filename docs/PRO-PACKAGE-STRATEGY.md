# Pro Package Strategy - Paid & Private Packages

**Date**: October 20, 2025  
**Topic**: How to manage paid/premium packages in NetMX  
**Status**: 🎯 Strategic Planning

## Quick Answer

**YES, keep everything in this monorepo!**

The repo can be:
- ✅ **Public** for open-source packages (framework, free modules)
- ✅ **Private** for pro packages (paid modules)
- ✅ **Mixed** - public repo with private submodules (advanced)

**Recommendation**: Start with **private monorepo**, selectively open-source later.

---

## Repository Strategy Options

### Option 1: Private Monorepo (Recommended - Simplest)

**Structure**:
```
netmx/ (PRIVATE repo)
├── framework/          # Core packages (can open-source later)
├── modules/
│   ├── Identity/       # Free
│   ├── Audit/          # Free
│   ├── CMS/            # Free
│   ├── Saas/           # 💰 Pro - Multi-tenancy
│   ├── Payment/        # 💰 Pro - Stripe, PayPal
│   └── Analytics/      # 💰 Pro - Advanced reporting
├── templates/
│   ├── modular/        # Free starter
│   └── pro/            # 💰 Pro - Full-featured SaaS template
└── tools/
    └── NetMX.CLI/      # Free (or Pro features gated)
```

**Publishing**:
```bash
# Free packages → NuGet.org (public)
dotnet nuget push NetMX.Core.1.0.0.nupkg --source nuget.org

# Pro packages → Private NuGet feed
dotnet nuget push NetMX.Saas.1.0.0.nupkg --source https://your-feed.com/v3/index.json
```

**Pros**:
- ✅ Simplest to manage
- ✅ All code in one place
- ✅ Easy to refactor across packages
- ✅ One CI/CD pipeline
- ✅ Can selectively open-source later

**Cons**:
- ⚠️ Can't show free packages publicly (GitHub stars)
- ⚠️ No community contributions (yet)

---

### Option 2: Public Monorepo + Private Pro Modules (Mixed)

**Structure**:
```
netmx/ (PUBLIC repo)
├── framework/          # Open source
├── modules/
│   ├── Identity/       # Open source
│   ├── Audit/          # Open source
│   ├── CMS/            # Open source
│   └── .gitignore      # Ignore pro/ folder
└── tools/
    └── NetMX.CLI/      # Open source

netmx-pro/ (PRIVATE repo or submodule)
├── modules/
│   ├── Saas/           # 💰 Pro
│   ├── Payment/        # 💰 Pro
│   └── Analytics/      # 💰 Pro
└── templates/
    └── pro/            # 💰 Pro
```

**How to Link**:
```bash
# In netmx/ (public repo)
git submodule add https://github.com/toonjd/netmx-pro.git modules/pro

# Clone for customers
git clone --recurse-submodules https://github.com/toonjd/netmx.git
# (Only works if they have access to netmx-pro)
```

**Pros**:
- ✅ Free packages are public (GitHub stars, community)
- ✅ Pro packages stay private
- ✅ Can accept community contributions to free packages
- ✅ Marketing: "NetMX is open source!"

**Cons**:
- ⚠️ More complex (submodules can be tricky)
- ⚠️ Two repos to manage
- ⚠️ Harder to refactor across free/pro boundary

---

### Option 3: Fully Public with Pro Features Gated

**Structure**:
```
netmx/ (PUBLIC repo)
├── framework/          # Open source
├── modules/
│   ├── Identity/       # Open source
│   ├── Saas/           # Open source code, license-gated
│   └── Payment/        # Open source code, license-gated
└── licensing/
    └── LicenseValidator.cs  # Check license at runtime
```

**License Check**:
```csharp
// In NetMX.Saas module
public class SaasModule : NetMXModule
{
    public override void ConfigureServices(IServiceCollection services)
    {
        // Check license key
        var licenseKey = Configuration["NetMX:LicenseKey"];
        if (!LicenseValidator.IsValid(licenseKey, "NetMX.Saas"))
        {
            throw new LicenseException("NetMX.Saas requires a valid license key.");
        }
        
        // Register services
        services.AddSaasFeatures();
    }
}
```

**Pros**:
- ✅ Fully transparent (can see all code)
- ✅ Community can report bugs in pro features
- ✅ Marketing: "All code is open source"
- ✅ Simplified development (one repo)

**Cons**:
- ⚠️ Pro code is visible (can be copied)
- ⚠️ Need licensing infrastructure
- ⚠️ Need license key management
- ⚠️ Easier to pirate

**Examples**: JetBrains IDEs, Tailwind UI, ABP Commercial

---

## Recommended Approach: Private Monorepo → Selective Open Source

### Phase 1: Private Development (Current)

**Goal**: Build everything privately, focus on quality

**Repo**: `github.com/toonjd/netmx` (PRIVATE)

**Structure**:
```
netmx/ (private)
├── framework/          # Core packages
├── modules/
│   ├── Identity/       # Free (but private for now)
│   ├── Audit/          # Free (but private for now)
│   ├── CMS/            # Free (but private for now)
│   ├── Saas/           # 💰 Pro
│   ├── Payment/        # 💰 Pro
│   └── Analytics/      # 💰 Pro
└── ...
```

**Publishing**:
- Free packages → NuGet.org (public feed)
- Pro packages → MyGet/Azure Artifacts (private feed)

**Benefits**:
- ✅ Keep IP protected while building
- ✅ No pressure to maintain public image
- ✅ Can refactor freely
- ✅ Easier to iterate

---

### Phase 2: Open Source Free Packages (Future)

**When**: After achieving product-market fit, stable API

**Goal**: Build community, get contributions, marketing

**Repo**: Make `github.com/toonjd/netmx` PUBLIC

**What to Open Source**:
```
netmx/ (public)
├── framework/          # ✅ Open source (MIT)
├── modules/
│   ├── Identity/       # ✅ Open source (MIT)
│   ├── Audit/          # ✅ Open source (MIT)
│   └── CMS/            # ✅ Open source (MIT)
└── tools/
    └── NetMX.CLI/      # ✅ Open source (MIT)
```

**What to Keep Private**:
```
netmx-pro/ (private repo or ignored folder)
├── modules/
│   ├── Saas/           # 💰 Pro (commercial license)
│   ├── Payment/        # 💰 Pro (commercial license)
│   └── Analytics/      # 💰 Pro (commercial license)
└── templates/
    └── pro/            # 💰 Pro SaaS template
```

**How to Split**:
```bash
# Option A: Move to separate repo
mkdir ../netmx-pro
mv modules/Saas ../netmx-pro/modules/
mv modules/Payment ../netmx-pro/modules/
mv modules/Analytics ../netmx-pro/modules/
git submodule add https://github.com/toonjd/netmx-pro.git modules/pro

# Option B: Just ignore in public repo
echo "modules/Saas/" >> .gitignore
echo "modules/Payment/" >> .gitignore
echo "modules/Analytics/" >> .gitignore
git rm -r --cached modules/Saas
git rm -r --cached modules/Payment
git rm -r --cached modules/Analytics
```

---

## Private NuGet Feed Options

### Option 1: Azure Artifacts (Microsoft)

**Best for**: Professional SaaS products

**Pricing**: 
- Free: 2GB storage, 5 users
- $4/user/month: Unlimited

**Setup**:
```bash
# Create feed
az artifacts universal publish --organization https://dev.azure.com/yourorg --feed netmx-pro

# Push package
dotnet nuget push NetMX.Saas.1.0.0.nupkg \
  --source https://pkgs.dev.azure.com/yourorg/_packaging/netmx-pro/nuget/v3/index.json \
  --api-key az
```

**Pros**:
- ✅ Microsoft-hosted (reliable)
- ✅ Integrated with Azure DevOps
- ✅ User management built-in
- ✅ Package retention policies

---

### Option 2: MyGet (Popular for .NET)

**Best for**: Small teams, indie developers

**Pricing**:
- Free: 500MB, public feeds
- $9/month: 2GB, private feeds
- $19/month: 10GB, private feeds

**Setup**:
```bash
# Push package
dotnet nuget push NetMX.Saas.1.0.0.nupkg \
  --source https://www.myget.org/F/netmx-pro/api/v3/index.json \
  --api-key your-api-key
```

**Pros**:
- ✅ Designed for NuGet
- ✅ Simple setup
- ✅ Good for small teams

---

### Option 3: GitHub Packages (Free with GitHub)

**Best for**: Already using GitHub

**Pricing**: Free with GitHub account

**Setup**:
```bash
# Authenticate
dotnet nuget add source https://nuget.pkg.github.com/toonjd/index.json \
  --name github \
  --username toonjd \
  --password $GITHUB_TOKEN

# Push package
dotnet nuget push NetMX.Saas.1.0.0.nupkg \
  --source github
```

**Pros**:
- ✅ Free
- ✅ Integrated with GitHub
- ✅ No separate account needed

**Cons**:
- ⚠️ Requires GitHub authentication for customers
- ⚠️ Tied to GitHub

---

### Option 4: Self-Hosted (NuGet.Server or BaGet)

**Best for**: Full control, large enterprises

**Setup** (BaGet - open source):
```bash
# Run BaGet server
docker run -d -p 5000:80 --name baget loicsharma/baget:latest

# Push package
dotnet nuget push NetMX.Saas.1.0.0.nupkg \
  --source http://localhost:5000/v3/index.json \
  --api-key BAGET-SERVER-API-KEY
```

**Pros**:
- ✅ Full control
- ✅ No monthly fees
- ✅ Can customize

**Cons**:
- ⚠️ Need to host/maintain
- ⚠️ Need to manage uptime

---

## Customer Experience

### For Free Users

**Installation**:
```bash
# Install from public NuGet.org
dotnet add package NetMX.Core
dotnet add package NetMX.Identity
dotnet add package NetMX.Audit
```

**No authentication needed** ✅

---

### For Pro Customers

**Installation** (with private feed):
```bash
# 1. Customer gets feed URL + API key (one-time setup)
dotnet nuget add source https://pkgs.dev.azure.com/yourorg/_packaging/netmx-pro/nuget/v3/index.json \
  --name netmx-pro \
  --username customer@example.com \
  --password <customer-api-key>

# 2. Install pro packages (just like free packages)
dotnet add package NetMX.Saas
dotnet add package NetMX.Payment
dotnet add package NetMX.Analytics
```

**Or with license key** (if using Option 3 - gated features):
```bash
# appsettings.json
{
  "NetMX": {
    "LicenseKey": "NETMX-PRO-XXXXX-XXXXX-XXXXX"
  }
}
```

---

## Licensing Models

### Model 1: Per-Developer License

**How it Works**:
- Customer buys licenses for N developers
- Each developer gets API key for private NuGet feed
- Can build unlimited apps

**Pricing Example**:
- 1-5 developers: $299/year per developer
- 6-20 developers: $249/year per developer
- 21+ developers: $199/year per developer

**Enforcement**: API key management in private feed

---

### Model 2: Per-Project License

**How it Works**:
- Customer buys license per deployed project
- License key in appsettings.json
- Validated at runtime

**Pricing Example**:
- Startup: $999/year (1 project)
- Business: $2,999/year (5 projects)
- Enterprise: $9,999/year (unlimited)

**Enforcement**: License validation code in package

---

### Model 3: Subscription (SaaS-style)

**How it Works**:
- Monthly/annual subscription
- Access to private feed + updates
- Cancel anytime, packages stop updating

**Pricing Example**:
- Solo: $29/month (1 developer)
- Team: $99/month (up to 10 developers)
- Business: $299/month (unlimited developers)

**Enforcement**: Expiring API keys

---

## Implementation Timeline

### Phase 1: Private Development (Current - 6 months)

**Focus**: Build core framework + pro modules

**Tasks**:
- ✅ Keep repo private
- ✅ Publish free packages to NuGet.org
- ✅ Build pro modules in same repo
- ✅ Test with private customers (beta)

**Result**: Stable framework, proven pro features

---

### Phase 2: Private Feed Setup (Month 7)

**Focus**: Set up paid package distribution

**Tasks**:
- Set up Azure Artifacts or MyGet
- Create customer onboarding process
- Build licensing dashboard (who has access)
- Test package publishing workflow

**Result**: Can sell pro packages to early customers

---

### Phase 3: Open Source Free Packages (Month 12+)

**Focus**: Build community, marketing

**Tasks**:
- Make repo public (or split repos)
- Move pro modules to private location
- Add CONTRIBUTING.md
- Set up GitHub Discussions
- Market: "NetMX is open source!"

**Result**: Community growth, contributions, GitHub stars

---

## Key Decisions for NOW

### Q: Should repo be private or public?

**A: Keep PRIVATE for now**

**Rationale**:
- We're still iterating rapidly
- No pressure to maintain public image
- Can refactor freely
- Easier to build pro features without scrutiny
- Can open-source later when ready

---

### Q: Where to host pro packages?

**A: Azure Artifacts** (recommended for professional product)

**Rationale**:
- Reliable (Microsoft-hosted)
- Free tier for testing
- Scales to enterprise
- Integrated with Azure DevOps (future CI/CD)

**Alternative**: MyGet if you want simplicity

---

### Q: What should be free vs pro?

**Free (Open Source - Future)**:
- ✅ NetMX.Core (DI, modules)
- ✅ NetMX.Ddd.* (domain-driven design)
- ✅ NetMX.EntityFrameworkCore (data access)
- ✅ NetMX.AspNetCore.* (web features)
- ✅ NetMX.CLI (code generation)
- ✅ Identity module (basic auth)
- ✅ Audit module (basic logging)
- ✅ CMS module (basic content)

**Pro (Paid)**:
- 💰 NetMX.Saas (multi-tenancy, tenant isolation)
- 💰 NetMX.Payment (Stripe, PayPal, subscriptions)
- 💰 NetMX.Analytics (advanced reporting, dashboards)
- 💰 NetMX.Notifications (email, SMS, push)
- 💰 NetMX.FileStorage (S3, Azure Blob, CDN)
- 💰 NetMX.BackgroundJobs (Hangfire integration)
- 💰 Pro templates (full SaaS starter)
- 💰 Pro CLI features (advanced scaffolding)

**Rationale**: Free packages provide value, pro packages save months of development

---

## Recommended: Azure Artifacts Setup

```bash
# 1. Create Azure DevOps organization (free)
https://dev.azure.com/yourorg

# 2. Create project: "NetMX"

# 3. Create feed: "netmx-pro" (private)
Azure DevOps → Artifacts → Create Feed → "netmx-pro" (private)

# 4. Get feed URL
https://pkgs.dev.azure.com/yourorg/_packaging/netmx-pro/nuget/v3/index.json

# 5. Create PAT (Personal Access Token)
User Settings → Personal Access Tokens → New Token
Scope: Packaging (Read & Write)

# 6. Push package
dotnet nuget push NetMX.Saas.1.0.0.nupkg \
  --source https://pkgs.dev.azure.com/yourorg/_packaging/netmx-pro/nuget/v3/index.json \
  --api-key az

# 7. Customer setup (one-time)
dotnet nuget add source https://pkgs.dev.azure.com/yourorg/_packaging/netmx-pro/nuget/v3/index.json \
  --name netmx-pro \
  --username customer@example.com \
  --password <customer-pat>

# 8. Customer installs (just like free packages)
dotnet add package NetMX.Saas
```

---

## Summary

### Current State
✅ **Private monorepo** - All code in `github.com/toonjd/netmx`  
✅ **Publish free to NuGet.org** - Anyone can use  
✅ **Build pro modules** - Keep in same repo for now

### Immediate Next Steps (This Week)
1. ✅ Keep repo private
2. ✅ Continue building framework + modules
3. ✅ Publish free packages to NuGet.org
4. ⏳ Don't worry about pro distribution yet (Month 7+)

### Future (Month 7+)
1. Set up Azure Artifacts for pro packages
2. Create customer onboarding process
3. Test with beta customers
4. Consider open-sourcing free packages (Month 12+)

### Key Principle
> **Build everything privately first, then selectively open-source what makes sense for marketing.**

**This is the ABP Framework approach, and it works!** 🚀

---

## References

- **ABP Framework**: Private pro modules, open-source core
- **Tailwind UI**: Open source CSS, paid components
- **JetBrains**: Open source IDE core, paid features
- **Stripe**: Open source SDKs, paid service

**All successful commercial open-source products follow this model!**

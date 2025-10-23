# NetMX Local Development Setup

**Quick setup for development on any workstation**

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Git](https://git-scm.com/downloads)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (optional, for running apps)

## One-Time Setup

```powershell
# 1. Clone repository
git clone https://github.com/toonjd/netmx.git
cd netmx

# 2. Run setup script (builds all packages + installs CLI)
.\scripts\setup-local-dev.ps1
```

**That's it!** You're ready to develop.

## What the Setup Script Does

1. ✅ Creates `.nuget/` folder for local packages
2. ✅ Builds all framework packages (NetMX.Core, NetMX.AspNetCore.Mvc, etc.)
3. ✅ Builds all module packages (Identity, Authorization, Audit)
4. ✅ Installs NetMX CLI globally (`netmx` command)

**Time**: ~15 seconds

## Rebuilding Packages

After making changes to framework or modules:

```powershell
# Rebuild all packages (skip CLI reinstall)
.\scripts\setup-local-dev.ps1 -SkipCLI

# Rebuild only framework packages
.\scripts\pack-framework.ps1

# Rebuild only module packages
.\scripts\pack-modules.ps1
```

## Creating Your First Project

```powershell
# Create new project
netmx new modular MyApp

# Navigate to project
cd MyApp

# Build and run
dotnet build
dotnet run --project src/MyApp.Web

# Or use Docker
docker-compose up
```

Open: http://localhost:5263 (or http://localhost:5263 if using Docker)

## Generating Features

```powershell
# Navigate to web project
cd src/MyApp.Web

# Generate CRUD feature
netmx generate feature Product

# Add DbSet to AppDbContext.cs (manual step for now)
# public DbSet<Product> Products => Set<Product>();

# Create and apply migration
dotnet ef migrations add AddProduct
dotnet ef database update

# Run app
dotnet run
```

Navigate to: `/Product`

## Troubleshooting

### "netmx: command not found"

Run setup script again:
```powershell
.\scripts\setup-local-dev.ps1
```

### "Package not found" errors

Clear NuGet cache and rebuild:
```powershell
dotnet nuget locals all --clear
.\scripts\setup-local-dev.ps1 -SkipCLI
```

### Docker build fails

Ensure Docker Desktop is running:
```powershell
docker --version  # Should show version
```

### Database connection errors

Check connection string in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=myapp_dev;Username=postgres;Password=postgres"
  }
}
```

Start PostgreSQL:
```powershell
docker-compose up -d db  # Just database
```

## Package Locations

- **Local packages**: `.nuget/` (git-ignored, rebuilt via setup script)
- **NuGet config**: `nuget.config` (repository-relative paths)
- **Framework source**: `framework/` (10 packages)
- **Module source**: `modules/` (Identity, Authorization, Audit)
- **CLI source**: `tools/NetMX.CLI/`

## Benefits of This Setup

✅ **Portable** - Works on any workstation  
✅ **Fast** - No NuGet.org downloads for local packages  
✅ **Consistent** - Everyone uses same package versions  
✅ **Simple** - One script to set up everything  
✅ **Isolated** - No global NuGet pollution  

## Next Steps

- Read [QUICK-START.md](QUICK-START.md) for detailed guide
- Read [TERMINOLOGY.md](TERMINOLOGY.md) for concepts
- Check [DX.md](DX.md) for developer experience principles
- See [THE-PRODUCT.md](THE-PRODUCT.md) for product vision

## Need Help?

- **Issues**: [GitHub Issues](https://github.com/toonjd/netmx/issues)
- **Discussions**: [GitHub Discussions](https://github.com/toonjd/netmx/discussions)

---

**Welcome to NetMX!** 🚀

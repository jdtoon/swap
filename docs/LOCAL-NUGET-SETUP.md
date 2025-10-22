# Local NuGet Feed Setup

**Purpose**: Test module installation without publishing to NuGet.org

**Date**: October 22, 2025

---

## Overview

A local NuGet feed lets you:
- ✅ Test `netmx add module Identity` without publishing
- ✅ Validate NuGet packaging works correctly
- ✅ Simulate real-world module installation
- ✅ Catch dependency issues before publishing

---

## Option 1: File-Based Local Feed (Simplest)

### Step 1: Create Local Feed Directory

```powershell
# Create a local NuGet repository folder
mkdir C:\LocalNuGet
```

### Step 2: Configure NuGet Source

```powershell
# Add local feed to NuGet sources
dotnet nuget add source C:\LocalNuGet --name "NetMX-Local"

# Verify it was added
dotnet nuget list source
```

**Expected Output:**
```
Registered Sources:
  1. nuget.org [Enabled]
     https://api.nuget.org/v3/index.json
  2. NetMX-Local [Enabled]
     C:\LocalNuGet
```

### Step 3: Pack Framework Packages

```powershell
cd c:\jd\netmx\framework

# Pack all framework packages to local feed
dotnet pack NetMX.sln --configuration Release --output C:\LocalNuGet

# Or pack individually with version
dotnet pack NetMX.Core --configuration Release --output C:\LocalNuGet /p:Version=0.2.0-local
dotnet pack NetMX.AspNetCore.Mvc --configuration Release --output C:\LocalNuGet /p:Version=0.2.0-local
# ... etc
```

**Result**: `.nupkg` files in `C:\LocalNuGet\`

### Step 4: Pack Module Packages

```powershell
cd c:\jd\netmx\modules

# Pack Authorization module
cd Authorization
dotnet pack Authorization.Web --configuration Release --output C:\LocalNuGet /p:Version=0.2.0-local

# Pack Identity module
cd ..\Identity
dotnet pack NetMX.Identity.Web --configuration Release --output C:\LocalNuGet /p:Version=0.2.0-local

# Pack Audit module
cd ..\Audit
dotnet pack Audit.Web --configuration Release --output C:\LocalNuGet /p:Version=0.2.0-local
```

### Step 5: Test Installation

```powershell
# Create test project
cd c:\temp
dotnet new web -o TestApp
cd TestApp

# Install from local feed
dotnet add package NetMX.Identity.Web --version 0.2.0-local --source C:\LocalNuGet

# Verify it installed
dotnet list package
```

**Expected Output:**
```
Project 'TestApp' has the following package references
   [net9.0]:
   Top-level Package                  Requested   Resolved
   > NetMX.Identity.Web               0.2.0-local 0.2.0-local
```

---

## Option 2: NuGet.Server (More Advanced)

### Why Use NuGet.Server?

- Multiple developers can share packages
- Supports package push (like NuGet.org)
- Web-based package browser
- Better for team environments

### Setup NuGet.Server

```powershell
# Create NuGet server project
mkdir C:\NuGetServer
cd C:\NuGetServer

dotnet new web -o NuGetServer
cd NuGetServer

# Add NuGet.Server package
dotnet add package NuGet.Server

# Configure in Program.cs
```

**Program.cs:**
```csharp
var builder = WebApplication.CreateBuilder(args);

// Add NuGet.Server
builder.Services.AddNuGetServer();

var app = builder.Build();

app.UseNuGetServer();

app.Run();
```

**appsettings.json:**
```json
{
  "NuGet": {
    "PackagesPath": "C:\\LocalNuGet\\Packages",
    "AllowOverwrite": true,
    "AllowPush": true,
    "ApiKey": "local-dev-key-123"
  }
}
```

### Run NuGet.Server

```powershell
dotnet run --urls "http://localhost:5555"
```

### Add Server as Source

```powershell
dotnet nuget add source http://localhost:5555/nuget --name "NetMX-Server"
```

### Push Packages

```powershell
dotnet nuget push NetMX.Core.0.2.0-local.nupkg --source http://localhost:5555/nuget --api-key local-dev-key-123
```

---

## Option 3: BaGet (Most Feature-Rich)

### Why BaGet?

- ✅ Open source NuGet server
- ✅ Beautiful web UI
- ✅ Symbol server support
- ✅ Search and package browsing
- ✅ Production-ready

### Install BaGet via Docker

```powershell
# Run BaGet container
docker run --rm --name baget `
  -p 5000:80 `
  -e ApiKey=local-dev-key-123 `
  -e Storage__Type=FileSystem `
  -e Storage__Path=/var/baget/packages `
  -e Database__Type=Sqlite `
  -e Database__ConnectionString="Data Source=/var/baget/baget.db" `
  -e Search__Type=Database `
  -v C:\LocalNuGet\BaGet:/var/baget `
  loicsharma/baget:latest
```

### Add BaGet as Source

```powershell
dotnet nuget add source http://localhost:5000/v3/index.json --name "NetMX-BaGet"
```

### Push Packages

```powershell
dotnet nuget push NetMX.Core.0.2.0-local.nupkg --source http://localhost:5000/v3/index.json --api-key local-dev-key-123
```

### Browse Packages

Open browser: `http://localhost:5000`

**Features:**
- Search packages
- View package details
- Download .nupkg files
- View dependencies
- Package stats

---

## Recommended Setup (For NetMX Development)

**Use Option 1 (File-Based) for now**

**Why?**
- ✅ Zero setup, works immediately
- ✅ No server to run
- ✅ Perfect for single developer
- ✅ Easy to clean up (`rmdir C:\LocalNuGet /s`)

**Later**: Upgrade to BaGet when you have multiple developers or want package browsing UI.

---

## NuGet.config in Repository

**Location**: `c:\jd\netmx\NuGet.config`

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <!-- Official NuGet.org -->
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
    
    <!-- Local development feed (optional - developers add locally) -->
    <!-- <add key="NetMX-Local" value="C:\LocalNuGet" /> -->
  </packageSources>
  
  <config>
    <!-- Include pre-release packages for NetMX development -->
    <add key="includePreRelease" value="true" />
  </config>
</configuration>
```

**Note**: Don't commit local paths to git. Each developer adds their own local source.

---

## PowerShell Helper Scripts

### Pack All Framework Packages

**File**: `scripts/pack-framework.ps1`

```powershell
#!/usr/bin/env pwsh
param(
    [string]$OutputPath = "C:\LocalNuGet",
    [string]$Version = "0.2.0-local",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

Write-Host "Packing NetMX Framework packages..." -ForegroundColor Cyan
Write-Host "  Output: $OutputPath" -ForegroundColor Gray
Write-Host "  Version: $Version" -ForegroundColor Gray
Write-Host "  Configuration: $Configuration" -ForegroundColor Gray
Write-Host ""

# Ensure output directory exists
New-Item -ItemType Directory -Force -Path $OutputPath | Out-Null

# Navigate to framework directory
Push-Location "$PSScriptRoot\..\framework"

try {
    # Pack all framework projects
    $projects = @(
        "NetMX.Core",
        "NetMX.Events",
        "NetMX.Ddd.Domain",
        "NetMX.Ddd.Application.Contracts",
        "NetMX.Ddd.Application",
        "NetMX.AspNetCore.Core",
        "NetMX.AspNetCore.Mvc",
        "NetMX.EntityFrameworkCore",
        "NetMX.Data",
        "NetMX.Htmx"
    )
    
    foreach ($project in $projects) {
        Write-Host "Packing $project..." -ForegroundColor Yellow
        
        dotnet pack "$project/$project.csproj" `
            --configuration $Configuration `
            --output $OutputPath `
            /p:Version=$Version `
            --nologo
        
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to pack $project"
        }
        
        Write-Host "  ✓ $project.nupkg created" -ForegroundColor Green
    }
    
    Write-Host ""
    Write-Host "✓ All framework packages packed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Packages location: $OutputPath" -ForegroundColor Cyan
    
    # List created packages
    Get-ChildItem -Path $OutputPath -Filter "NetMX.*.nupkg" | ForEach-Object {
        Write-Host "  - $($_.Name)" -ForegroundColor Gray
    }
}
finally {
    Pop-Location
}
```

### Pack All Module Packages

**File**: `scripts/pack-modules.ps1`

```powershell
#!/usr/bin/env pwsh
param(
    [string]$OutputPath = "C:\LocalNuGet",
    [string]$Version = "0.2.0-local",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

Write-Host "Packing NetMX Module packages..." -ForegroundColor Cyan
Write-Host "  Output: $OutputPath" -ForegroundColor Gray
Write-Host "  Version: $Version" -ForegroundColor Gray
Write-Host ""

New-Item -ItemType Directory -Force -Path $OutputPath | Out-Null

Push-Location "$PSScriptRoot\..\modules"

try {
    $modules = @(
        @{ Path = "Authorization/Authorization.Web"; Name = "Authorization" },
        @{ Path = "Identity/NetMX.Identity.Web"; Name = "Identity" },
        @{ Path = "Audit/Audit.Web"; Name = "Audit" }
    )
    
    foreach ($module in $modules) {
        Write-Host "Packing $($module.Name)..." -ForegroundColor Yellow
        
        dotnet pack "$($module.Path)" `
            --configuration $Configuration `
            --output $OutputPath `
            /p:Version=$Version `
            --nologo
        
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to pack $($module.Name)"
        }
        
        Write-Host "  ✓ $($module.Name).Web.nupkg created" -ForegroundColor Green
    }
    
    Write-Host ""
    Write-Host "✓ All module packages packed successfully!" -ForegroundColor Green
}
finally {
    Pop-Location
}
```

### Usage

```powershell
# Pack framework packages
.\scripts\pack-framework.ps1

# Pack module packages
.\scripts\pack-modules.ps1

# Pack with custom version
.\scripts\pack-framework.ps1 -Version "0.3.0-alpha"

# Pack to different location
.\scripts\pack-modules.ps1 -OutputPath "D:\NuGetPackages"
```

---

## Testing Module Installation

### Manual Test

```powershell
# 1. Pack modules
.\scripts\pack-modules.ps1 -Version "0.2.0-test"

# 2. Create test app
cd c:\temp
netmx new modular TestShop

# 3. Add local source (if not already added)
cd TestShop
dotnet nuget add source C:\LocalNuGet --name "NetMX-Local"

# 4. Install module
dotnet add src/TestShop.Web package NetMX.Identity.Web --version 0.2.0-test

# 5. Verify dependencies resolved
dotnet restore
dotnet build

# 6. Check installed packages
dotnet list package
```

**Expected Output:**
```
Project 'TestShop.Web' has the following package references
   [net9.0]:
   Top-level Package                  Requested      Resolved
   > NetMX.Identity.Web               0.2.0-test     0.2.0-test
   
   Transitive Package                 Resolved
   > NetMX.Core                       0.2.0-test
   > NetMX.AspNetCore.Mvc             0.2.0-test
   > NetMX.Events                     0.2.0-test
   > NetMX.Ddd.Domain                 0.2.0-test
   ... (all dependencies auto-installed!)
```

---

## CLI Integration (Future)

**Goal**: `netmx add module Identity` installs from local feed during development

**Implementation**:

```csharp
// In CLI: AddModuleCommand.cs
public override async Task<int> ExecuteAsync(CommandContext context, AddModuleSettings settings)
{
    // 1. Check for local feed first
    var localFeed = "C:\\LocalNuGet";
    if (Directory.Exists(localFeed))
    {
        AnsiConsole.MarkupLine("[yellow]Using local NuGet feed for development[/]");
        
        await RunProcessAsync("dotnet", 
            $"add package NetMX.{settings.ModuleName}.Web --source {localFeed}");
    }
    else
    {
        // 2. Fall back to NuGet.org
        await RunProcessAsync("dotnet", 
            $"add package NetMX.{settings.ModuleName}.Web");
    }
    
    // 3. Auto-wire module (add services, migrations, etc.)
    await WireModuleAsync(settings.ModuleName);
    
    return 0;
}
```

---

## Cleaning Up

### Remove Local Feed

```powershell
# Remove NuGet source
dotnet nuget remove source NetMX-Local

# Delete packages
Remove-Item C:\LocalNuGet -Recurse -Force
```

### Clear NuGet Cache

```powershell
# Clear all NuGet caches
dotnet nuget locals all --clear

# Clear only http-cache (keeps global-packages)
dotnet nuget locals http-cache --clear
```

---

## Troubleshooting

### Issue: Package Not Found

**Symptom**: `error NU1101: Unable to find package NetMX.Identity.Web`

**Solution**:
```powershell
# Verify source is enabled
dotnet nuget list source

# Check package exists
dir C:\LocalNuGet\NetMX.Identity.Web*.nupkg

# Try with explicit source
dotnet add package NetMX.Identity.Web --version 0.2.0-local --source C:\LocalNuGet
```

### Issue: Wrong Version Resolved

**Symptom**: Installs old version instead of latest

**Solution**:
```powershell
# Clear NuGet cache
dotnet nuget locals http-cache --clear

# Restore with force
dotnet restore --force

# Or delete bin/obj folders
Remove-Item bin,obj -Recurse -Force
```

### Issue: Dependency Resolution Failed

**Symptom**: `Package 'NetMX.Core 0.2.0-local' is not found`

**Solution**:
```powershell
# Make sure ALL framework packages are packed
.\scripts\pack-framework.ps1 -Version "0.2.0-local"

# Verify all packages present
dir C:\LocalNuGet\NetMX.*.nupkg
```

---

## Summary

**Recommended Workflow:**

1. **Setup** (once):
   ```powershell
   mkdir C:\LocalNuGet
   dotnet nuget add source C:\LocalNuGet --name "NetMX-Local"
   ```

2. **Development** (daily):
   ```powershell
   # After making framework changes
   .\scripts\pack-framework.ps1 -Version "0.2.0-dev"
   
   # After making module changes
   .\scripts\pack-modules.ps1 -Version "0.2.0-dev"
   ```

3. **Testing**:
   ```powershell
   # Create test app
   netmx new modular TestApp
   
   # Install module
   dotnet add package NetMX.Identity.Web --version 0.2.0-dev --source C:\LocalNuGet
   ```

4. **Release**:
   - Build packages with production version
   - Test from local feed
   - Publish to NuGet.org when confident

**Next**: Integrate local feed detection into `netmx add module` command!

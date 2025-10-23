#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Sets up local development environment for NetMX
    
.DESCRIPTION
    This script:
    1. Creates .nuget folder for local packages
    2. Rebuilds all framework packages
    3. Rebuilds all module packages
    4. Packages CLI tool
    5. Installs CLI globally
    
    Run this once per workstation to get started with NetMX development.
    
.EXAMPLE
    .\setup-local-dev.ps1
    
.NOTES
    This script is workstation-agnostic and uses relative paths only.
#>

param(
    [switch]$SkipCLI,
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"

# Colors for output
function Write-Step {
    param([string]$Message)
    Write-Host "`n📋 $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor Green
}

function Write-Info {
    param([string]$Message)
    Write-Host "ℹ️  $Message" -ForegroundColor Blue
}

function Write-Error {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor Red
}

# Get repository root (parent of scripts folder)
$repoRoot = Split-Path -Parent $PSScriptRoot
$localNuGet = Join-Path $repoRoot ".nuget"

Write-Host @"

╔══════════════════════════════════════════╗
║   NetMX Local Development Setup          ║
╚══════════════════════════════════════════╝

"@ -ForegroundColor Magenta

Write-Info "Repository Root: $repoRoot"
Write-Info "Local NuGet:     $localNuGet"

# Step 1: Create .nuget folder
Write-Step "Creating local NuGet folder"
if (-not (Test-Path $localNuGet)) {
    New-Item -ItemType Directory -Force -Path $localNuGet | Out-Null
    Write-Success "Created .nuget folder"
} else {
    Write-Info ".nuget folder already exists"
}

# Step 2: Clean old packages (optional)
Write-Step "Cleaning old packages"
$oldPackages = Get-ChildItem -Path $localNuGet -Filter "*.nupkg" -ErrorAction SilentlyContinue
if ($oldPackages) {
    Write-Info "Removing $($oldPackages.Count) old package(s)"
    $oldPackages | Remove-Item -Force
    Write-Success "Cleaned old packages"
} else {
    Write-Info "No old packages to clean"
}

# Step 3: Build and pack framework packages
Write-Step "Building framework packages"
Push-Location (Join-Path $repoRoot "framework")
try {
    # Build solution first
    Write-Info "Building NetMX.sln..."
    dotnet build NetMX.sln -c Release --nologo | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Framework build failed"
    }
    
    # Pack each project
    $frameworkProjects = @(
        "NetMX.Core",
        "NetMX.Events",
        "NetMX.Ddd.Domain",
        "NetMX.Ddd.Application.Contracts",
        "NetMX.Ddd.Application",
        "NetMX.Data",
        "NetMX.EntityFrameworkCore",
        "NetMX.AspNetCore.Core",
        "NetMX.AspNetCore.Mvc",
        "NetMX.Htmx"
    )
    
    $packedCount = 0
    foreach ($project in $frameworkProjects) {
        $projectPath = Join-Path $repoRoot "framework" $project "$project.csproj"
        if (Test-Path $projectPath) {
            if ($Verbose) {
                Write-Info "Packing $project..."
            }
            dotnet pack $projectPath -c Release -o $localNuGet --nologo --verbosity quiet
            if ($LASTEXITCODE -eq 0) {
                $packedCount++
            }
        }
    }
    
    Write-Success "Packed $packedCount framework packages"
} finally {
    Pop-Location
}

# Step 4: Build and pack module packages
Write-Step "Building module packages"
$moduleProjects = @(
    "modules/Identity/NetMX.Identity.Web",
    "modules/Authorization/Authorization.Web",
    "modules/Audit/Audit.Web"
)

$packedModules = 0
foreach ($modulePath in $moduleProjects) {
    $fullPath = Join-Path $repoRoot $modulePath
    if (Test-Path $fullPath) {
        $projectFile = Get-ChildItem -Path $fullPath -Filter "*.csproj" -ErrorAction SilentlyContinue | Select-Object -First 1
        
        if ($projectFile) {
            if ($Verbose) {
                Write-Info "Packing $($projectFile.BaseName)..."
            }
            dotnet pack $projectFile.FullName -c Release -o $localNuGet --nologo --verbosity quiet
            if ($LASTEXITCODE -eq 0) {
                $packedModules++
            }
        }
    } else {
        if ($Verbose) {
            Write-Info "Module not found: $modulePath (skipping)"
        }
    }
}

Write-Success "Packed $packedModules module packages"

# Step 5: Build and install CLI
if (-not $SkipCLI) {
    Write-Step "Building and installing NetMX CLI"
    Push-Location (Join-Path $repoRoot "tools" "NetMX.CLI")
    try {
        # Build CLI
        Write-Info "Building CLI..."
        dotnet build -c Release --nologo --verbosity quiet
        if ($LASTEXITCODE -ne 0) {
            throw "CLI build failed"
        }
        
        # Pack CLI
        Write-Info "Packing CLI..."
        $cliNupkg = Join-Path $repoRoot "tools" "NetMX.CLI" "nupkg"
        if (-not (Test-Path $cliNupkg)) {
            New-Item -ItemType Directory -Force -Path $cliNupkg | Out-Null
        }
        dotnet pack -c Release -o $cliNupkg --nologo --verbosity quiet
        
        # Uninstall old CLI
        Write-Info "Uninstalling old CLI..."
        dotnet tool uninstall -g NetMX.CLI 2>&1 | Out-Null
        
        # Install new CLI
        Write-Info "Installing new CLI..."
        dotnet tool install -g --add-source $cliNupkg NetMX.CLI
        if ($LASTEXITCODE -eq 0) {
            Write-Success "CLI installed successfully"
        } else {
            Write-Error "CLI installation failed"
        }
    } finally {
        Pop-Location
    }
} else {
    Write-Info "Skipping CLI installation (--SkipCLI flag)"
}

# Step 6: Summary
Write-Host @"

╔══════════════════════════════════════════╗
║   Setup Complete!                        ║
╚══════════════════════════════════════════╝

"@ -ForegroundColor Green

Write-Success "Local NuGet: $localNuGet"

$packageCount = (Get-ChildItem -Path $localNuGet -Filter "*.nupkg" | Measure-Object).Count
Write-Success "Total Packages: $packageCount"

Write-Host @"

📋 Next Steps:
   1. Create a new project:
      netmx new modular MyApp

   2. Navigate to project:
      cd MyApp

   3. Generate a feature:
      cd src/MyApp.Web
      netmx generate feature Product

   4. Run the app:
      dotnet run

💡 Tip: Run this script again anytime to rebuild all packages

"@ -ForegroundColor Cyan

Write-Host "Happy coding! 🚀`n" -ForegroundColor Magenta

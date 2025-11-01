#!/usr/bin/env pwsh
# Reinstalls the Swap CLI tool and framework packages locally for testing

param(
    [switch]$Force
)

$ErrorActionPreference = "Stop"

Write-Host "🔄 Reinstalling Swap CLI and Framework Packages..." -ForegroundColor Cyan
Write-Host ""

# Get the root directory
$rootDir = Split-Path $PSScriptRoot -Parent
$localFeed = Join-Path $rootDir ".nuget" "local"

# Create or clear local feed
if (Test-Path $localFeed) {
    Write-Host "🗑️  Clearing old packages from local feed..." -ForegroundColor Yellow
    Remove-Item "$localFeed\*.nupkg" -Force -ErrorAction SilentlyContinue
    Write-Host "   ✅ Old packages cleared" -ForegroundColor Green
    Write-Host ""
} else {
    New-Item -ItemType Directory -Path $localFeed -Force | Out-Null
}

# Optionally clear NuGet caches
if ($Force) {
    Write-Host "🧹 Clearing global NuGet caches..." -ForegroundColor Yellow
    dotnet nuget locals all --clear | Out-Null
    Write-Host "   ✅ NuGet cache cleared" -ForegroundColor Green
    Write-Host ""
}

# Uninstall existing CLI
Write-Host "🗑️  Uninstalling existing Swap CLI..." -ForegroundColor Yellow
try {
    dotnet tool uninstall -g Swap.CLI | Out-Null
    Write-Host "   ✅ Existing CLI uninstalled" -ForegroundColor Green
} catch {
    Write-Host "   ℹ️  No existing CLI found" -ForegroundColor Gray
}
Write-Host ""

# Clean local tool caches completely
Write-Host "🧽 Removing any previously installed package versions..." -ForegroundColor Yellow

$dotnetToolsPath = Join-Path $env:USERPROFILE ".dotnet\tools"
$dotnetStorePath = Join-Path $env:USERPROFILE ".dotnet\tools\.store"
$nugetGlobalPath = Join-Path $env:USERPROFILE ".nuget\packages"

$packageNames = @("swap.cli", "swap.htmx", "swap.patterns", "swap.testing")

foreach ($pkg in $packageNames) {
    $pkgPaths = @(
        (Join-Path $dotnetStorePath $pkg),
        (Join-Path $nugetGlobalPath $pkg)
    )
    foreach ($pkgPath in $pkgPaths) {
        if (Test-Path $pkgPath) {
            Remove-Item $pkgPath -Recurse -Force -ErrorAction SilentlyContinue
            Write-Host "   🗑️  Removed cached $pkg" -ForegroundColor Gray
        }
    }
}

Write-Host "   ✅ Local caches cleaned" -ForegroundColor Green
Write-Host ""

# Pack framework packages
Write-Host "📦 Packing Framework Packages..." -ForegroundColor Yellow

$frameworkProjects = @(
    "framework\Swap.Htmx\Swap.Htmx.csproj",
    "framework\Swap.Patterns\Swap.Patterns.csproj",
    "framework\Swap.Testing\Swap.Testing.csproj"
)

foreach ($project in $frameworkProjects) {
    $projectPath = Join-Path $rootDir $project
    $projectName = (Split-Path $project -Leaf) -replace '\.csproj$', ''
    Write-Host "   📦 Packing $projectName..." -ForegroundColor Cyan
    dotnet pack $projectPath -c Release -o $localFeed --no-build --nologo
    if ($LASTEXITCODE -ne 0) {
        Write-Host "   ❌ Failed to pack $projectName" -ForegroundColor Red
        exit 1
    }
    Write-Host "   ✅ $projectName packed successfully" -ForegroundColor Green
}
Write-Host ""

# Pack CLI
Write-Host "📦 Packing Swap.CLI..." -ForegroundColor Yellow
Set-Location "$rootDir\tools\Swap.CLI"

[xml]$csproj = Get-Content "Swap.CLI.csproj"
$version = $csproj.Project.PropertyGroup.Version | Select-Object -First 1

if ([string]::IsNullOrWhiteSpace($version)) {
    Write-Host "   ❌ Could not find version in Swap.CLI.csproj" -ForegroundColor Red
    exit 1
}

Write-Host "   Version: $version" -ForegroundColor Cyan
dotnet pack -c Release -o $localFeed --nologo
if ($LASTEXITCODE -ne 0) {
    Write-Host "   ❌ Failed to pack Swap.CLI" -ForegroundColor Red
    exit 1
}
Write-Host "   ✅ CLI packed successfully" -ForegroundColor Green
Write-Host ""

# List all packages in local feed
Write-Host "📋 Packages in local feed:" -ForegroundColor Cyan
Get-ChildItem $localFeed -Filter "*.nupkg" | ForEach-Object {
    Write-Host "   • $($_.Name)" -ForegroundColor Gray
}
Write-Host ""

# Deep clean CLI tool locations
Write-Host "🧼 Clearing old CLI binaries from .dotnet/tools..." -ForegroundColor Yellow
$swapExe = Join-Path $dotnetToolsPath "swap.exe"
if (Test-Path $swapExe) {
    Remove-Item $swapExe -Force -ErrorAction SilentlyContinue
    Write-Host "   🗑️  Removed old swap.exe" -ForegroundColor Gray
}

# Remove any leftover store entries
$swapStore = Get-ChildItem $dotnetStorePath -Recurse -ErrorAction SilentlyContinue | Where-Object { $_.Name -match "swap" }
if ($swapStore) {
    $swapStore | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "   🗑️  Removed old swap entries from store" -ForegroundColor Gray
}
Write-Host "   ✅ CLI cache cleared" -ForegroundColor Green
Write-Host ""

# Install CLI from local feed (always force latest)
Write-Host "⚙️  Installing Swap CLI from local feed..." -ForegroundColor Yellow
Write-Host "   Installing version: $version" -ForegroundColor Gray

# Build base arguments
$installArgs = @("tool", "install", "-g", "Swap.CLI", "--add-source", $localFeed)
$includePrerelease = $version -match '-'

# If version provided, skip --prerelease (since .NET disallows both)
if (-not [string]::IsNullOrWhiteSpace($version)) {
    $installArgs += @("--version", $version)
} elseif ($includePrerelease) {
    $installArgs += "--prerelease"
}

# Try a clean install
& dotnet @installArgs 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "   🔁 Retrying installation..." -ForegroundColor Yellow
    dotnet tool uninstall -g Swap.CLI 2>$null | Out-Null
    & dotnet @installArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Host "   ❌ Failed to install Swap.CLI" -ForegroundColor Red
        exit 1
    }
}

Write-Host "   ✅ CLI installed successfully" -ForegroundColor Green
Write-Host ""



# Verify final installation
Write-Host "✅ Swap CLI and Framework Packages are ready!" -ForegroundColor Green
Write-Host ""
Write-Host "📋 Installed CLI version:" -ForegroundColor Cyan
swap --version
Write-Host ""
Write-Host "💡 Try it out:" -ForegroundColor Cyan
Write-Host "   swap new MyTestApp --database sqlite --local-nuget" -ForegroundColor White
Write-Host "   cd MyTestApp" -ForegroundColor White
Write-Host "   dotnet run" -ForegroundColor White
Write-Host ""

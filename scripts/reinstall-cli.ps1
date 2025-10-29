#!/usr/bin/env pwsh
# Reinstalls the Swap CLI tool locally for testing

$ErrorActionPreference = "Stop"

Write-Host "🔄 Reinstalling Swap CLI..." -ForegroundColor Cyan
Write-Host ""

# Get the root directory
$rootDir = Split-Path $PSScriptRoot -Parent
$localFeed = Join-Path $rootDir ".nuget" "local"

# Uninstall existing CLI
Write-Host "🗑️  Uninstalling existing Swap CLI..." -ForegroundColor Yellow
try {
    dotnet tool uninstall -g Swap.CLI 2>$null
    Write-Host "   ✅ Existing CLI uninstalled" -ForegroundColor Green
} catch {
    Write-Host "   ℹ️  No existing CLI found" -ForegroundColor Gray
}

Write-Host ""

# Pack the CLI
Write-Host "📦 Packing Swap.CLI..." -ForegroundColor Yellow
Set-Location "$rootDir\tools\Swap.CLI"

# Get version from csproj
[xml]$csproj = Get-Content "Swap.CLI.csproj"
$version = $csproj.Project.PropertyGroup.Version | Select-Object -First 1

if ([string]::IsNullOrWhiteSpace($version)) {
    Write-Host "   ❌ Could not find version in Swap.CLI.csproj" -ForegroundColor Red
    exit 1
}

Write-Host "   Version: $version" -ForegroundColor Cyan

dotnet pack -c Release -o $localFeed

if ($LASTEXITCODE -ne 0) {
    Write-Host "   ❌ Failed to pack Swap.CLI" -ForegroundColor Red
    exit 1
}

Write-Host "   ✅ CLI packed successfully" -ForegroundColor Green
Write-Host ""

# Install the CLI from local feed
Write-Host "⚙️  Installing Swap CLI from local feed..." -ForegroundColor Yellow
dotnet tool install -g Swap.CLI --add-source $localFeed --version $version

if ($LASTEXITCODE -ne 0) {
    Write-Host "   ❌ Failed to install Swap.CLI" -ForegroundColor Red
    exit 1
}

Write-Host "   ✅ CLI installed successfully" -ForegroundColor Green
Write-Host ""

# Verify installation
Write-Host "✅ Swap CLI is ready!" -ForegroundColor Green
Write-Host ""
Write-Host "📋 Installed version:" -ForegroundColor Cyan
swap --version

Write-Host ""
Write-Host "💡 Try it out:" -ForegroundColor Cyan
Write-Host "   swap new MyTestApp --database sqlite" -ForegroundColor White
Write-Host "   cd MyTestApp" -ForegroundColor White
Write-Host "   dotnet run" -ForegroundColor White

#!/usr/bin/env pwsh
# Pack all Swap framework packages and publish to local NuGet feed
# Run this after making changes to framework packages during development

Write-Host "🔨 Building and packing Swap framework packages..." -ForegroundColor Cyan

$ErrorActionPreference = "Stop"
$rootDir = Split-Path $PSScriptRoot -Parent
$localFeed = Join-Path $rootDir ".nuget" "local"

# Ensure local feed directory exists
if (!(Test-Path $localFeed)) {
    New-Item -ItemType Directory -Path $localFeed -Force | Out-Null
}

# Clean old packages from local feed (optional - keeps only latest)
Write-Host "🧹 Cleaning old packages from local feed..." -ForegroundColor Yellow
Remove-Item "$localFeed\*.nupkg" -Force -ErrorAction SilentlyContinue

# Pack Swap.Htmx
Write-Host "`n📦 Packing Swap.Htmx..." -ForegroundColor Green
Set-Location "$rootDir\framework\Swap.Htmx"
dotnet pack -c Release -o $localFeed
if ($LASTEXITCODE -ne 0) { throw "Failed to pack Swap.Htmx" }

# Pack Swap.Patterns
Write-Host "`n📦 Packing Swap.Patterns..." -ForegroundColor Green
Set-Location "$rootDir\framework\Swap.Patterns"
dotnet pack -c Release -o $localFeed
if ($LASTEXITCODE -ne 0) { throw "Failed to pack Swap.Patterns" }

# Pack Swap.Testing
Write-Host "`n📦 Packing Swap.Testing..." -ForegroundColor Green
Set-Location "$rootDir\framework\Swap.Testing"
dotnet pack -c Release -o $localFeed
if ($LASTEXITCODE -ne 0) { throw "Failed to pack Swap.Testing" }

Set-Location $rootDir

Write-Host "`n✅ All packages packed successfully!" -ForegroundColor Green
Write-Host "`n📦 Local packages available:" -ForegroundColor Cyan
Get-ChildItem "$localFeed\*.nupkg" | ForEach-Object { 
    Write-Host "   - $($_.Name)" -ForegroundColor White
}

Write-Host "`n💡 Tip: Projects using nuget.config will automatically use these local packages" -ForegroundColor Yellow

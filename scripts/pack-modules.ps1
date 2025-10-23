#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Packs all NetMX module packages to local NuGet feed
.DESCRIPTION
    Builds and packs all module packages with a consistent version number
.PARAMETER OutputPath
    Path to local NuGet repository (default: C:\LocalNuGet)
.PARAMETER Version
    Version number for packages (default: 0.2.0-local)
.PARAMETER Configuration
    Build configuration (default: Release)
.EXAMPLE
    .\pack-modules.ps1
    .\pack-modules.ps1 -Version "0.3.0-alpha" -OutputPath "D:\NuGetPackages"
#>

param(
    [string]$OutputPath,
    [string]$Version = "0.2.0-local",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

# Use repository-relative .nuget folder by default
$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $OutputPath) {
    $OutputPath = Join-Path $repoRoot ".nuget"
}

Write-Host ""
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host " Packing NetMX Module Packages" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "  Output:         $OutputPath" -ForegroundColor Gray
Write-Host "  Version:        $Version" -ForegroundColor Gray
Write-Host "  Configuration:  $Configuration" -ForegroundColor Gray
Write-Host ""

New-Item -ItemType Directory -Force -Path $OutputPath | Out-Null

Push-Location "$PSScriptRoot\..\modules"

try {
    $modules = @(
        @{ Path = "Authorization/Authorization.Web"; Name = "Authorization.Web" },
        @{ Path = "Identity/NetMX.Identity.Web"; Name = "NetMX.Identity.Web" },
        @{ Path = "Audit/Audit.Web"; Name = "Audit.Web" }
    )
    
    $successCount = 0
    $totalCount = $modules.Count
    
    foreach ($module in $modules) {
        $num = $successCount + 1
        Write-Host "[$num/$totalCount] Packing $($module.Name)..." -ForegroundColor Yellow
        
        dotnet pack "$($module.Path)" `
            --configuration $Configuration `
            --output $OutputPath `
            /p:Version=$Version `
            --nologo `
            --verbosity quiet
        
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to pack $($module.Name)"
        }
        
        $successCount++
        Write-Host "          ✓ $($module.Name).nupkg" -ForegroundColor Green
    }
    
    Write-Host ""
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Green
    Write-Host " ✓ All $totalCount module packages packed successfully!" -ForegroundColor Green
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Green
    Write-Host ""
    Write-Host "Packages location: $OutputPath" -ForegroundColor Cyan
    Write-Host ""
    
    # List created packages
    Get-ChildItem -Path $OutputPath -Filter "*.$Version.nupkg" | Where-Object { 
        $_.Name -like "Authorization.*" -or $_.Name -like "NetMX.Identity.*" -or $_.Name -like "Audit.*" 
    } | ForEach-Object {
        Write-Host "  📦 $($_.Name)" -ForegroundColor Gray
    }
    
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "  1. Install module in your project:" -ForegroundColor Gray
    Write-Host "     dotnet add package NetMX.Identity.Web --version $Version --source $OutputPath" -ForegroundColor White
    Write-Host ""
    Write-Host "  2. Or use NetMX CLI (future):" -ForegroundColor Gray
    Write-Host "     netmx add module Identity" -ForegroundColor White
    Write-Host ""
}
catch {
    Write-Host ""
    Write-Host "❌ Error: $_" -ForegroundColor Red
    exit 1
}
finally {
    Pop-Location
}

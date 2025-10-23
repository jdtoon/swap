#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Packs all NetMX framework packages to local NuGet feed
.DESCRIPTION
    Builds and packs all framework packages with a consistent version number
.PARAMETER OutputPath
    Path to local NuGet repository (default: C:\LocalNuGet)
.PARAMETER Version
    Version number for packages (default: 0.2.0-local)
.PARAMETER Configuration
    Build configuration (default: Release)
.EXAMPLE
    .\pack-framework.ps1
    .\pack-framework.ps1 -Version "0.3.0-alpha" -OutputPath "D:\NuGetPackages"
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
Write-Host " Packing NetMX Framework Packages" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "  Output:         $OutputPath" -ForegroundColor Gray
Write-Host "  Version:        $Version" -ForegroundColor Gray
Write-Host "  Configuration:  $Configuration" -ForegroundColor Gray
Write-Host ""

# Ensure output directory exists
New-Item -ItemType Directory -Force -Path $OutputPath | Out-Null

# Navigate to framework directory
Push-Location "$PSScriptRoot\..\framework"

try {
    # Pack all framework projects in dependency order
    $projects = @(
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
    
    $successCount = 0
    $totalCount = $projects.Count
    
    foreach ($project in $projects) {
        $num = $successCount + 1
        Write-Host "[$num/$totalCount] Packing $project..." -ForegroundColor Yellow
        
        dotnet pack "$project/$project.csproj" `
            --configuration $Configuration `
            --output $OutputPath `
            /p:Version=$Version `
            --nologo `
            --verbosity quiet
        
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to pack $project"
        }
        
        $successCount++
        Write-Host "          ✓ $project.nupkg" -ForegroundColor Green
    }
    
    Write-Host ""
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Green
    Write-Host " ✓ All $totalCount framework packages packed successfully!" -ForegroundColor Green
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Green
    Write-Host ""
    Write-Host "Packages location: $OutputPath" -ForegroundColor Cyan
    Write-Host ""
    
    # List created packages
    Get-ChildItem -Path $OutputPath -Filter "NetMX.*.$Version.nupkg" | ForEach-Object {
        Write-Host "  📦 $($_.Name)" -ForegroundColor Gray
    }
    
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "  1. Add NuGet source (if not already added):" -ForegroundColor Gray
    Write-Host "     dotnet nuget add source $OutputPath --name NetMX-Local" -ForegroundColor White
    Write-Host ""
    Write-Host "  2. Install package in your project:" -ForegroundColor Gray
    Write-Host "     dotnet add package NetMX.Core --version $Version --source $OutputPath" -ForegroundColor White
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

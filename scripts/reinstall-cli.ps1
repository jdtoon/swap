#!/usr/bin/env pwsh
# Reinstalls the Swap CLI tool and framework packages locally for testing

$ErrorActionPreference = "Stop"

Write-Host "🔄 Reinstalling Swap CLI and Framework Packages..." -ForegroundColor Cyan
Write-Host ""

# Get the root directory
$rootDir = Split-Path $PSScriptRoot -Parent
$localFeed = Join-Path $rootDir ".nuget" "local"

# Create local feed directory if it doesn't exist, or clear it
if (Test-Path $localFeed) {
    Write-Host "🗑️  Clearing old packages from local feed..." -ForegroundColor Yellow
    Remove-Item "$localFeed\*.nupkg" -Force -ErrorAction SilentlyContinue
    Write-Host "   ✅ Old packages cleared" -ForegroundColor Green
    Write-Host ""
} else {
    New-Item -ItemType Directory -Path $localFeed -Force | Out-Null
}

# Uninstall existing CLI
Write-Host "🗑️  Uninstalling existing Swap CLI..." -ForegroundColor Yellow
try {
    dotnet tool uninstall -g Swap.CLI 2>$null
    Write-Host "   ✅ Existing CLI uninstalled" -ForegroundColor Green
} catch {
    Write-Host "   ℹ️  No existing CLI found" -ForegroundColor Gray
}

Write-Host ""

# Pack all framework packages
Write-Host "📦 Packing Framework Packages..." -ForegroundColor Yellow

$frameworkProjects = @(
    "framework\Swap.Htmx\Swap.Htmx.csproj",
    "framework\Swap.Patterns\Swap.Patterns.csproj",
    "framework\Swap.Testing\Swap.Testing.csproj"
)

foreach ($project in $frameworkProjects) {
    $projectPath = Join-Path $rootDir $project
    $projectName = Split-Path $project -Leaf
    $projectName = $projectName -replace '\.csproj$', ''
    
    Write-Host "   📦 Packing $projectName..." -ForegroundColor Cyan
    
    dotnet pack $projectPath -c Release -o $localFeed --nologo
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "   ❌ Failed to pack $projectName" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "   ✅ $projectName packed successfully" -ForegroundColor Green
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
Write-Host "✅ Swap CLI and Framework Packages are ready!" -ForegroundColor Green
Write-Host ""
Write-Host "📋 Installed CLI version:" -ForegroundColor Cyan
swap --version

Write-Host ""
Write-Host "💡 Try it out:" -ForegroundColor Cyan
Write-Host "   swap new MyTestApp --database sqlite --local-nuget" -ForegroundColor White
Write-Host "   cd MyTestApp" -ForegroundColor White
Write-Host "   dotnet run" -ForegroundColor White

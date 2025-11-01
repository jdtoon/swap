#!/usr/bin/env pwsh
# Recipe: Self-Reference Relationship Test
# Tests self-referencing relationship (Category -> ParentCategory)

$ErrorActionPreference = "Stop"

Write-Host "🧪 Self-Reference Relationship Test Recipe" -ForegroundColor Cyan
Write-Host ""

$rootDir = Split-Path $PSScriptRoot -Parent
$testAppPath = Join-Path $rootDir "testApps" "SelfReferenceTest"

# Clean up existing test app
if (Test-Path $testAppPath) {
    Write-Host "🗑️  Removing existing SelfReferenceTest..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force $testAppPath
    Write-Host "   ✅ Cleaned up" -ForegroundColor Green
}

Write-Host ""
Write-Host "📦 Creating new project..." -ForegroundColor Yellow
Set-Location $rootDir
swap new SelfReferenceTest --skip-setup --output testApps\SelfReferenceTest

Write-Host ""
Write-Host "🏗️  Generating model..." -ForegroundColor Yellow
Set-Location $testAppPath

# Generate Category model with Name and Description
swap g m Category -f "Name:string Description:string"

Write-Host ""
Write-Host "🔗 Creating self-referencing relationship..." -ForegroundColor Yellow
# Category references itself via ParentCategoryId -> ParentCategory
swap g relationship --source Category --target Category --type many-to-one --nav Parent

Write-Host ""
Write-Host "🎨 Generating controller with navigation..." -ForegroundColor Yellow
swap g c Category --add-nav

Write-Host ""
Write-Host "📦 Installing dependencies..." -ForegroundColor Yellow
npm install --silent
libman restore
npm run build:css

Write-Host ""
Write-Host "🌱 Generating seeder..." -ForegroundColor Yellow
swap g seed Category --count 15

Write-Host ""
Write-Host "🗄️  Creating and applying migration..." -ForegroundColor Yellow
dotnet ef migrations add InitSelfReference --no-build
dotnet ef database update --no-build

Write-Host ""
Write-Host "✅ SelfReferenceTest app ready!" -ForegroundColor Green
Write-Host ""
Write-Host "🚀 To run with seeding:" -ForegroundColor Cyan
Write-Host "   `$env:SEED_COUNT=15; `$env:SEED_LOCALE='en'; dotnet run" -ForegroundColor White
Write-Host ""
Write-Host "🧪 Test scenarios:" -ForegroundColor Cyan
Write-Host "   1. Create root category → Parent dropdown shows '-- Select Parent --' ✅" -ForegroundColor White
Write-Host "   2. Create child category → Select parent from dropdown ✅" -ForegroundColor White
Write-Host "   3. Edit category → Cannot select itself as parent (excluded from dropdown) ✅" -ForegroundColor White
Write-Host "   4. View parent in list → Shows parent's Name in Parent column ✅" -ForegroundColor White
Write-Host "   5. Sort by Parent → Hierarchical relationships visible ✅" -ForegroundColor White
Write-Host ""
Write-Host "💡 Example hierarchy:" -ForegroundColor Yellow
Write-Host "   - Electronics (root)" -ForegroundColor White
Write-Host "     └── Computers (parent: Electronics)" -ForegroundColor White
Write-Host "         └── Laptops (parent: Computers)" -ForegroundColor White
Write-Host ""
Write-Host "📍 Visit: http://localhost:5000" -ForegroundColor Cyan

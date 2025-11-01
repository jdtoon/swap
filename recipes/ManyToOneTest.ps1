#!/usr/bin/env pwsh
# Recipe: ManyToOne Relationship Test
# Tests many-to-one relationship (Order -> Customer) with full CRUD operations

$ErrorActionPreference = "Stop"

Write-Host "🧪 ManyToOne Relationship Test Recipe" -ForegroundColor Cyan
Write-Host ""

$rootDir = Split-Path $PSScriptRoot -Parent
$testAppPath = Join-Path $rootDir "testApps" "ManyToOneTest"

# Clean up existing test app
if (Test-Path $testAppPath) {
    Write-Host "🗑️  Removing existing ManyToOneTest..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force $testAppPath
    Write-Host "   ✅ Cleaned up" -ForegroundColor Green
}

Write-Host ""
Write-Host "📦 Creating new project..." -ForegroundColor Yellow
Set-Location $rootDir
swap new ManyToOneTest --skip-setup --output testApps\ManyToOneTest

Write-Host ""
Write-Host "🏗️  Generating models..." -ForegroundColor Yellow
Set-Location $testAppPath

# Generate Customer model
swap g m Customer -f "Name:string Email:string"

# Generate Order model
swap g m Order -f "OrderNumber:string OrderDate:DateTime TotalAmount:decimal"

Write-Host ""
Write-Host "🔗 Creating many-to-one relationship..." -ForegroundColor Yellow
swap g relationship --source Order --target Customer --type many-to-one

Write-Host ""
Write-Host "🎨 Generating controllers with navigation..." -ForegroundColor Yellow
swap g c Order --add-nav
swap g c Customer --add-nav

Write-Host ""
Write-Host "📦 Installing dependencies..." -ForegroundColor Yellow
npm install --silent
libman restore
npm run build:css

Write-Host ""
Write-Host "🌱 Generating seeders..." -ForegroundColor Yellow
swap g seed Customer --count 10
swap g seed Order --count 20

Write-Host ""
Write-Host "🗄️  Creating and applying migration..." -ForegroundColor Yellow
dotnet ef migrations add InitManyToOne --no-build
dotnet ef database update --no-build

Write-Host ""
Write-Host "✅ ManyToOneTest app ready!" -ForegroundColor Green
Write-Host ""
Write-Host "🚀 To run with seeding:" -ForegroundColor Cyan
Write-Host "   `$env:SEED_COUNT=10; `$env:SEED_LOCALE='en'; dotnet run" -ForegroundColor White
Write-Host ""
Write-Host "🧪 Test scenarios:" -ForegroundColor Cyan
Write-Host "   1. Sort by Customer column, change pages → sort persists" -ForegroundColor White
Write-Host "   2. Go to page 2, select all → only page 2 items selected" -ForegroundColor White
Write-Host "   3. Edit TotalAmount with '123.11' or '123,11' → both work" -ForegroundColor White
Write-Host "   4. Navigate between pages → smooth fade transitions" -ForegroundColor White
Write-Host ""
Write-Host "📍 Visit: http://localhost:5000" -ForegroundColor Cyan

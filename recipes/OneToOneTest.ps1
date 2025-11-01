#!/usr/bin/env pwsh
# Recipe: OneToOne Relationship Test
# Tests one-to-one relationship (User -> UserProfile) with unique constraint

$ErrorActionPreference = "Stop"

Write-Host "🧪 OneToOne Relationship Test Recipe" -ForegroundColor Cyan
Write-Host ""

$rootDir = Split-Path $PSScriptRoot -Parent
$testAppPath = Join-Path $rootDir "testApps" "OneToOneTest"

# Clean up existing test app
if (Test-Path $testAppPath) {
    Write-Host "🗑️  Removing existing OneToOneTest..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force $testAppPath
    Write-Host "   ✅ Cleaned up" -ForegroundColor Green
}

Write-Host ""
Write-Host "📦 Creating new project..." -ForegroundColor Yellow
Set-Location $rootDir
swap new OneToOneTest --skip-setup --output testApps\OneToOneTest

Write-Host ""
Write-Host "🏗️  Generating models..." -ForegroundColor Yellow
Set-Location $testAppPath

# Generate User model (principal)
swap g m User -f "Username:string Email:string"

# Generate UserProfile model (dependent)
swap g m UserProfile -f "Bio:string Avatar:string Location:string"

Write-Host ""
Write-Host "🔗 Creating one-to-one relationship..." -ForegroundColor Yellow
# UserProfile is dependent by default (gets the FK + unique constraint)
swap g relationship --source UserProfile --target User --type one-to-one

Write-Host ""
Write-Host "🎨 Generating controllers with navigation..." -ForegroundColor Yellow
swap g c UserProfile --add-nav
swap g c User --add-nav

Write-Host ""
Write-Host "📦 Installing dependencies..." -ForegroundColor Yellow
npm install --silent
libman restore
npm run build:css

Write-Host ""
Write-Host "🌱 Generating seeders..." -ForegroundColor Yellow
swap g seed User --count 10
swap g seed UserProfile --count 10

Write-Host ""
Write-Host "🗄️  Creating and applying migration..." -ForegroundColor Yellow
dotnet ef migrations add InitOneToOne --no-build
dotnet ef database update --no-build

Write-Host ""
Write-Host "✅ OneToOneTest app ready!" -ForegroundColor Green
Write-Host ""
Write-Host "🚀 To run with seeding:" -ForegroundColor Cyan
Write-Host "   `$env:SEED_COUNT=10; `$env:SEED_LOCALE='en'; dotnet run" -ForegroundColor White
Write-Host ""
Write-Host "🧪 Test scenarios:" -ForegroundColor Cyan
Write-Host "   1. Create UserProfile → Select User from dropdown ✅" -ForegroundColor White
Write-Host "   2. Edit UserProfile → User field is readonly (unique constraint protection) ✅" -ForegroundColor White
Write-Host "   3. Try to assign same User to two profiles → Prevented by unique constraint ✅" -ForegroundColor White
Write-Host "   4. Sort by User column → Sort persists across pages ✅" -ForegroundColor White
Write-Host "   5. Select all on page 2 → Only page 2 items selected ✅" -ForegroundColor White
Write-Host ""
Write-Host "⚠️  Known Limitation:" -ForegroundColor Yellow
Write-Host "   - User form shows text input for UserProfile (principal side)" -ForegroundColor White
Write-Host "   - Workaround: Always manage from UserProfile side (dependent with FK)" -ForegroundColor White
Write-Host ""
Write-Host "📍 Visit: http://localhost:5000" -ForegroundColor Cyan

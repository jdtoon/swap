# Update all repository references from jdtoon to toonjd organization
# Run this script from the repository root: .\scripts\update-org-references.ps1

$oldOrg = "jdtoon"
$newOrg = "toonjd"
$oldAuthor = "jdtoon"
$newAuthor = "ToonJD"  # Or keep as "jdtoon" if you prefer

Write-Host "🔄 Updating repository references from $oldOrg to $newOrg..." -ForegroundColor Cyan

# Files to update
$filesToUpdate = @(
    "README.md",
    "docs/GITHUB-SETUP.md",
    "docs/GITHUB-DETAILED-SETUP.md",
    "docs/QUICK-START-SETUP.md",
    "docs/SETUP-COMPLETE.md",
    "docs/ROADMAP.md",
    "framework/NetMX.Core/NetMX.Core.csproj",
    "framework/NetMX.Ddd.Domain/NetMX.Ddd.Domain.csproj",
    "framework/NetMX.Ddd.Application.Contracts/NetMX.Ddd.Application.Contracts.csproj",
    "framework/NetMX.Ddd.Application/NetMX.Ddd.Application.csproj",
    "framework/NetMX.Data/NetMX.Data.csproj",
    "framework/NetMX.EntityFrameworkCore/NetMX.EntityFrameworkCore.csproj",
    "framework/NetMX.AspNetCore.Core/NetMX.AspNetCore.Core.csproj",
    "framework/NetMX.AspNetCore.Mvc/NetMX.AspNetCore.Mvc.csproj",
    "framework/NetMX.Htmx/NetMX.Htmx.csproj",
    "scripts/setup-github.ps1"
)

$updatedCount = 0

foreach ($file in $filesToUpdate) {
    $fullPath = Join-Path $PSScriptRoot "..\$file"
    
    if (Test-Path $fullPath) {
        Write-Host "  Updating: $file" -ForegroundColor Yellow
        
        $content = Get-Content $fullPath -Raw
        $originalContent = $content
        
        # Update GitHub URLs
        $content = $content -replace "github\.com/$oldOrg/", "github.com/$newOrg/"
        $content = $content -replace "nuget\.pkg\.github\.com/$oldOrg/", "nuget.pkg.github.com/$newOrg/"
        
        # Update author names in .csproj files (optional - comment out if you want to keep jdtoon)
        # $content = $content -replace "<Authors>$oldAuthor</Authors>", "<Authors>$newAuthor</Authors>"
        
        if ($content -ne $originalContent) {
            Set-Content -Path $fullPath -Value $content -NoNewline
            $updatedCount++
            Write-Host "    ✅ Updated" -ForegroundColor Green
        } else {
            Write-Host "    ⏭️  No changes needed" -ForegroundColor Gray
        }
    } else {
        Write-Host "  ⚠️  File not found: $file" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "✅ Updated $updatedCount files" -ForegroundColor Green
Write-Host ""
Write-Host "📋 Next steps:" -ForegroundColor Cyan
Write-Host "  1. Review changes: git diff" -ForegroundColor White
Write-Host "  2. Commit: git add -A && git commit -m 'chore: Update repository references to toonjd organization'" -ForegroundColor White
Write-Host "  3. Push: git push" -ForegroundColor White

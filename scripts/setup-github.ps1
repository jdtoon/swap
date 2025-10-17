# NetMX GitHub & NuGet Setup Script
# Run this after installing GitHub CLI

Write-Host "🚀 NetMX GitHub & NuGet Setup" -ForegroundColor Cyan
Write-Host ""

# Step 1: Authenticate with GitHub CLI
Write-Host "Step 1: GitHub CLI Authentication" -ForegroundColor Yellow
Write-Host "-------------------------------------"
$ghAuth = Read-Host "Have you authenticated with GitHub CLI? (y/n)"
if ($ghAuth -ne "y") {
    Write-Host "Running: gh auth login" -ForegroundColor Green
    gh auth login
}

# Step 2: Check current repository
Write-Host ""
Write-Host "Step 2: Verify Repository" -ForegroundColor Yellow
Write-Host "-------------------------------------"
$repoInfo = gh repo view --json name,owner,isPrivate,url | ConvertFrom-Json
Write-Host "Repository: $($repoInfo.owner.login)/$($repoInfo.name)"
Write-Host "Visibility: $(if ($repoInfo.isPrivate) { 'Private' } else { 'Public' })"
Write-Host "URL: $($repoInfo.url)"

$makePublic = Read-Host "`nDo you want to make this repository PUBLIC for open source? (y/n)"
if ($makePublic -eq "y") {
    Write-Host "Making repository public..." -ForegroundColor Green
    gh repo edit --visibility public
    Write-Host "✅ Repository is now public!" -ForegroundColor Green
}

# Step 3: Configure GitHub Environments
Write-Host ""
Write-Host "Step 3: GitHub Environments" -ForegroundColor Yellow
Write-Host "-------------------------------------"
Write-Host "Creating GitHub environments for deployment..."
Write-Host ""
Write-Host "⚠️  GitHub environments must be created via the web UI:" -ForegroundColor Red
Write-Host "   1. Go to: https://github.com/$($repoInfo.owner.login)/$($repoInfo.name)/settings/environments"
Write-Host "   2. Create environment: 'nuget-dev' (no protection rules needed)"
Write-Host "   3. Create environment: 'nuget-production' with:"
Write-Host "      - Required reviewers: Add yourself"
Write-Host "      - Deployment branches: Selected branches -> master only"
Write-Host ""
$envReady = Read-Host "Press Enter when environments are created..."

# Step 4: NuGet API Key
Write-Host ""
Write-Host "Step 4: NuGet.org API Key" -ForegroundColor Yellow
Write-Host "-------------------------------------"
Write-Host "You need a NuGet.org API key for production deployments."
Write-Host ""
Write-Host "Create one at: https://www.nuget.org/account/apikeys"
Write-Host "  - Key Name: NetMX GitHub Actions"
Write-Host "  - Glob Pattern: NetMX.*"
Write-Host "  - Scopes: Push new packages and package versions"
Write-Host ""
$hasApiKey = Read-Host "Do you have a NuGet.org API key? (y/n)"

if ($hasApiKey -eq "y") {
    $apiKey = Read-Host "Enter your NuGet API key (it will be hidden)" -AsSecureString
    $apiKeyPlainText = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [Runtime.InteropServices.Marshal]::SecureStringToBSTR($apiKey))
    
    Write-Host "Setting NUGET_API_KEY secret..." -ForegroundColor Green
    $apiKeyPlainText | gh secret set NUGET_API_KEY
    Write-Host "✅ Secret set successfully!" -ForegroundColor Green
} else {
    Write-Host "⚠️  You can set the NUGET_API_KEY secret later using:" -ForegroundColor Yellow
    Write-Host "   gh secret set NUGET_API_KEY"
}

# Step 5: Update Package Metadata
Write-Host ""
Write-Host "Step 5: Package Metadata" -ForegroundColor Yellow
Write-Host "-------------------------------------"
Write-Host "Current framework projects:"
Get-ChildItem -Path "framework" -Filter "*.csproj" -Recurse | ForEach-Object {
    Write-Host "  - $($_.Directory.Name)" -ForegroundColor Cyan
}

Write-Host ""
$updateMetadata = Read-Host "Do you want to update package metadata now? (y/n)"
if ($updateMetadata -eq "y") {
    Write-Host "✅ Package metadata will be updated in next step" -ForegroundColor Green
    Write-Host "   See: docs/GITHUB-SETUP.md for metadata template"
} else {
    Write-Host "⚠️  Remember to update .csproj files with package metadata" -ForegroundColor Yellow
}

# Step 6: Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "✅ Setup Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Update package metadata in all framework/.csproj files"
Write-Host "2. Commit and push changes:"
Write-Host "   git add ."
Write-Host "   git commit -m 'ci: Configure GitHub Actions and NuGet publishing'"
Write-Host "   git push origin master"
Write-Host ""
Write-Host "3. Test CI build:"
Write-Host "   - Go to: https://github.com/$($repoInfo.owner.login)/$($repoInfo.name)/actions"
Write-Host "   - Watch 'CI Build & Test' workflow run"
Write-Host ""
Write-Host "4. Create first pre-release:"
Write-Host "   gh release create v0.1.0-alpha --prerelease --title 'NetMX v0.1.0-alpha' --notes 'First alpha release'"
Write-Host ""
Write-Host "📚 Full documentation: docs/GITHUB-SETUP.md"
Write-Host ""

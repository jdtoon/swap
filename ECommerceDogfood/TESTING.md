# NetMX Template Testing

This template includes automated endpoint testing scripts to validate your CRUD operations.

## Quick Start

### 1. Start Your App

```powershell
cd src/ECommerceDogfood.Web
dotnet run --urls "http://localhost:5263"
```

### 2. Run Tests (In Another Terminal)

```powershell
# Test all Product endpoints
./test-endpoints.ps1 -Entity "Product"

# Test with custom base URL
./test-endpoints.ps1 -BaseUrl "http://localhost:8080" -Entity "Order"
```

## What Gets Tested

The script validates a complete CRUD cycle:

1. ✅ **GET /Entity/List** - Lists all items (empty state)
2. ✅ **GET /Entity/Create** - Loads create form
3. ✅ **POST /Entity/Create** - Creates new item
   - Validates HTTP 200 status
   - Validates `HX-Trigger: entity.created` header
4. ✅ **GET /Entity/List** - Verifies item appears
5. ✅ **GET /Entity/Edit/{id}** - Loads edit form
6. ✅ **POST /Entity/Edit** - Updates item
   - Validates HTTP 200 status
   - Validates `HX-Trigger: entity.updated` header
7. ✅ **DELETE /Entity/Delete/{id}** - Deletes item
   - Validates HTTP 200 status
   - Validates `HX-Trigger: entity.deleted` header
   - Validates `HX-Reswap: delete` header
8. ✅ **GET /Entity/List** - Verifies item removed

## Output Example

```
╔════════════════════════════════════════╗
║   NetMX Endpoint Test Suite           ║
╚════════════════════════════════════════╝

Waiting for app to be ready...
✓ App is ready!

[1] Testing GET /Product/List (Empty)
  Status: 200
  ✓ PASSED

[2] Testing GET /Product/Create (Form)
  Status: 200
  Form: Found
  ✓ PASSED

[3] Testing POST /Product/Create
  Status: 200
  HX-Trigger: product.created
  Entity ID: a1b2c3d4-e5f6-...
  ✓ PASSED

... (5 more tests)

╔════════════════════════════════════════╗
║   Test Results                         ║
╚════════════════════════════════════════╝

  Passed: 8
  Failed: 0
  Total:  8

✓ All tests passed!
```

## CI/CD Integration

### GitHub Actions

```yaml
name: Test Endpoints

on: [push, pull_request]

jobs:
  test:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'
      
      - name: Build
        run: dotnet build
        working-directory: src/ECommerceDogfood.Web
      
      - name: Start app in background
        run: Start-Job { dotnet run --urls "http://localhost:5263" }
        working-directory: src/ECommerceDogfood.Web
      
      - name: Run endpoint tests
        run: ./test-endpoints.ps1
        
      - name: Stop app
        run: Get-Job | Stop-Job
```

### Azure DevOps

```yaml
trigger:
  - develop
  - main

pool:
  vmImage: 'windows-latest'

steps:
- task: UseDotNet@2
  inputs:
    version: '9.0.x'

- script: dotnet build
  displayName: 'Build'
  workingDirectory: src/ECommerceDogfood.Web

- powershell: |
    Start-Job { dotnet run --urls "http://localhost:5263" }
    Start-Sleep -Seconds 10
  displayName: 'Start app'
  workingDirectory: src/ECommerceDogfood.Web

- powershell: ./test-endpoints.ps1
  displayName: 'Run tests'

- powershell: Get-Job | Stop-Job
  displayName: 'Stop app'
  condition: always()
```

## Customizing Tests

### Testing Custom Entity

```powershell
# Test your custom entity
./test-endpoints.ps1 -Entity "Customer"
./test-endpoints.ps1 -Entity "Order"
./test-endpoints.ps1 -Entity "Invoice"
```

### Custom Form Data

Edit the script to match your entity properties:

```powershell
# Line ~85 - POST Create
$body = "Name=Test&Description=Test&Price=99.99&CategoryId=$categoryId"

# Line ~150 - POST Edit
$body = "Id=$entityId&Name=Updated&Description=Updated&Price=149.99"
```

### Testing Authentication

If your endpoints require authentication:

```powershell
# Add auth headers
$headers = @{ 
    "Authorization" = "Bearer $token"
    "HX-Request" = "true" 
}

$response = Invoke-WebRequest -Uri "$BaseUrl/$Entity/List" -Headers $headers -UseBasicParsing
```

## Benefits

### For Developers
- ✅ **Rapid validation** - Test changes in 15 seconds
- ✅ **No manual clicking** - Fully automated
- ✅ **Repeatable** - Same results every time
- ✅ **Fast feedback** - Know if it works immediately

### For CI/CD
- ✅ **Pipeline integration** - Works in GitHub Actions, Azure DevOps
- ✅ **Fast execution** - Complete in under 1 minute
- ✅ **No Selenium** - Lightweight PowerShell only
- ✅ **Cross-platform** - PowerShell Core on Linux/Mac

### For Quality
- ✅ **Regression prevention** - Catch breaking changes
- ✅ **HTMX validation** - Verify headers work
- ✅ **Event validation** - Confirm events trigger
- ✅ **Complete coverage** - Test full CRUD cycle

## Troubleshooting

### App Not Responding

```powershell
# Check if app is running
Get-Process -Name "dotnet"

# Start app manually
cd src/ECommerceDogfood.Web
dotnet run --urls "http://localhost:5263"
```

### Tests Failing

```powershell
# Run with verbose output
$VerbosePreference = "Continue"
./test-endpoints.ps1 -Entity "Product"

# Check specific endpoint manually
Invoke-WebRequest -Uri "http://localhost:5263/Product/List" -UseBasicParsing
```

### Port Conflicts

```powershell
# Use different port
dotnet run --urls "http://localhost:8080"
./test-endpoints.ps1 -BaseUrl "http://localhost:8080" -Entity "Product"
```

## Learn More

- Full guide: `docs/AUTOMATED-ENDPOINT-TESTING.md`
- Quick start: `docs/QUICK-START.md`
- HTMX patterns: `docs/HTMX-PATTERNS.md`

---

**Happy testing!** 🚀

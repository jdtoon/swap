# Automated HTTP Endpoint Testing Guide

**Purpose**: Test NetMX CRUD endpoints programmatically without browser intervention  
**Status**: ✅ Validated October 23, 2025  
**Use Case**: CI/CD pipelines, rapid validation, automated testing

---

## Overview

NetMX endpoints can be **fully validated via console** using PowerShell HTTP cmdlets. This enables:

- ✅ **Zero browser interaction** - Fully scriptable testing
- ✅ **CI/CD integration** - Run in automated pipelines
- ✅ **Fast validation** - Tests complete in seconds
- ✅ **HTMX-aware** - Validates HTMX headers and behaviors
- ✅ **Complete coverage** - Test all CRUD operations

---

## Quick Start

### 1. Start Your App

```powershell
cd c:\jd\netmx\YourApp\src\YourApp.Web
dotnet run --urls "http://localhost:5263"
```

### 2. Test Endpoints

```powershell
# GET - List all items
$response = Invoke-WebRequest -Uri "http://localhost:5263/Product/List" -Method GET -UseBasicParsing
Write-Host "Status: $($response.StatusCode)"  # Should be 200

# POST - Create item (HTMX form submission)
$headers = @{ 
    "Content-Type" = "application/x-www-form-urlencoded"
    "HX-Request" = "true" 
}
$body = "Name=Test+Product&Description=Test&IsActive=true"
$response = Invoke-WebRequest -Uri "http://localhost:5263/Product/Create" -Method POST -Headers $headers -Body $body -UseBasicParsing

# Validate HTMX event
if ($response.Headers['HX-Trigger'] -match 'product.created') {
    Write-Host "✓ Event triggered!" -ForegroundColor Green
}

# DELETE - Remove item
$response = Invoke-WebRequest -Uri "http://localhost:5263/Product/Delete/{id}" -Method DELETE -Headers @{ "HX-Request" = "true" } -UseBasicParsing

# Validate HTMX headers
Write-Host "HX-Trigger: $($response.Headers['HX-Trigger'])"  # product.deleted
Write-Host "HX-Reswap: $($response.Headers['HX-Reswap'])"    # delete
```

---

## Complete CRUD Test Suite

### Full Test Script

```powershell
# Configuration
$baseUrl = "http://localhost:5263"
$entity = "Product"

Write-Host "`n=== Starting CRUD Test Suite ===" -ForegroundColor Cyan

# Test 1: GET List (Empty)
Write-Host "`n[1] Testing GET /$entity/List (Empty)" -ForegroundColor Yellow
$response = Invoke-WebRequest -Uri "$baseUrl/$entity/List" -Method GET -UseBasicParsing
if ($response.StatusCode -eq 200) {
    Write-Host "✓ Status: 200" -ForegroundColor Green
} else {
    Write-Host "✗ Unexpected status: $($response.StatusCode)" -ForegroundColor Red
}

# Test 2: GET Create Form
Write-Host "`n[2] Testing GET /$entity/Create (Form)" -ForegroundColor Yellow
$response = Invoke-WebRequest -Uri "$baseUrl/$entity/Create" -Method GET -UseBasicParsing
if ($response.StatusCode -eq 200 -and $response.Content -match 'form') {
    Write-Host "✓ Form loaded" -ForegroundColor Green
} else {
    Write-Host "✗ Form not found" -ForegroundColor Red
}

# Test 3: POST Create
Write-Host "`n[3] Testing POST /$entity/Create" -ForegroundColor Yellow
$headers = @{ 
    "Content-Type" = "application/x-www-form-urlencoded"
    "HX-Request" = "true" 
}
$body = "Name=Test+Product&Description=Automated+test&IsActive=true"
$response = Invoke-WebRequest -Uri "$baseUrl/$entity/Create" -Method POST -Headers $headers -Body $body -UseBasicParsing

if ($response.StatusCode -eq 200) {
    Write-Host "✓ Status: 200" -ForegroundColor Green
}

if ($response.Headers['HX-Trigger'] -match "$($entity.ToLower()).created") {
    Write-Host "✓ HX-Trigger: $($entity.ToLower()).created" -ForegroundColor Green
} else {
    Write-Host "✗ Missing or incorrect HX-Trigger" -ForegroundColor Red
}

# Test 4: GET List (With Data)
Write-Host "`n[4] Testing GET /$entity/List (After Create)" -ForegroundColor Yellow
Start-Sleep -Seconds 1
$response = Invoke-WebRequest -Uri "$baseUrl/$entity/List" -Method GET -UseBasicParsing

# Extract product ID from list
if ($response.Content -match 'row-([a-f0-9\-]+)') {
    $productId = $Matches[1]
    Write-Host "✓ Product found: $productId" -ForegroundColor Green
} else {
    Write-Host "✗ Product not found in list" -ForegroundColor Red
    exit 1
}

# Test 5: GET Edit Form
Write-Host "`n[5] Testing GET /$entity/Edit/{id}" -ForegroundColor Yellow
$response = Invoke-WebRequest -Uri "$baseUrl/$entity/Edit/$productId" -Method GET -UseBasicParsing
if ($response.StatusCode -eq 200 -and $response.Content -match 'form') {
    Write-Host "✓ Edit form loaded" -ForegroundColor Green
} else {
    Write-Host "✗ Edit form not found" -ForegroundColor Red
}

# Test 6: POST Edit
Write-Host "`n[6] Testing POST /$entity/Edit" -ForegroundColor Yellow
$body = "Id=$productId&Name=Updated+Product&Description=This+was+edited&IsActive=true"
$response = Invoke-WebRequest -Uri "$baseUrl/$entity/Edit" -Method POST -Headers $headers -Body $body -UseBasicParsing

if ($response.StatusCode -eq 200) {
    Write-Host "✓ Status: 200" -ForegroundColor Green
}

if ($response.Headers['HX-Trigger'] -match "$($entity.ToLower()).updated") {
    Write-Host "✓ HX-Trigger: $($entity.ToLower()).updated" -ForegroundColor Green
} else {
    Write-Host "✗ Missing or incorrect HX-Trigger" -ForegroundColor Red
}

# Test 7: DELETE
Write-Host "`n[7] Testing DELETE /$entity/Delete/{id}" -ForegroundColor Yellow
$response = Invoke-WebRequest -Uri "$baseUrl/$entity/Delete/$productId" -Method DELETE -Headers @{ "HX-Request" = "true" } -UseBasicParsing

if ($response.StatusCode -eq 200) {
    Write-Host "✓ Status: 200" -ForegroundColor Green
}

if ($response.Headers['HX-Trigger'] -match "$($entity.ToLower()).deleted") {
    Write-Host "✓ HX-Trigger: $($entity.ToLower()).deleted" -ForegroundColor Green
}

if ($response.Headers['HX-Reswap'] -eq 'delete') {
    Write-Host "✓ HX-Reswap: delete" -ForegroundColor Green
} else {
    Write-Host "✗ Missing or incorrect HX-Reswap" -ForegroundColor Red
}

# Test 8: Verify Deletion
Write-Host "`n[8] Testing GET /$entity/List (After Delete)" -ForegroundColor Yellow
Start-Sleep -Seconds 1
$response = Invoke-WebRequest -Uri "$baseUrl/$entity/List" -Method GET -UseBasicParsing

if ($response.Content -notmatch $productId) {
    Write-Host "✓ Product successfully deleted" -ForegroundColor Green
} else {
    Write-Host "✗ Product still in list" -ForegroundColor Red
}

Write-Host "`n=== CRUD Test Suite Complete ===" -ForegroundColor Cyan
```

---

## PowerShell HTTP Cmdlets

### `Invoke-WebRequest` (Full Details)

Use when you need **complete HTTP information** (headers, status, content):

```powershell
$response = Invoke-WebRequest -Uri "http://localhost:5263/Product" -Method GET -UseBasicParsing

# Access response details
$response.StatusCode           # HTTP status code (200, 404, etc.)
$response.Headers              # All response headers (hashtable)
$response.Headers['HX-Trigger'] # Specific HTMX header
$response.Content              # Response body (HTML, JSON, etc.)
$response.RawContent          # Full HTTP response (headers + body)
```

**Parameters**:
- `-Uri`: Target URL
- `-Method`: GET, POST, PUT, DELETE, etc.
- `-Headers`: Custom headers (hashtable)
- `-Body`: Request body (string or hashtable)
- `-ContentType`: Content-Type header (e.g., "application/json")
- `-UseBasicParsing`: Faster, no IE dependencies (recommended)

### `Invoke-RestMethod` (Simplified)

Use for **JSON APIs** when you only need the parsed response:

```powershell
$data = Invoke-RestMethod -Uri "http://localhost:5263/api/products" -Method GET

# Returns parsed object (JSON → PowerShell object)
$data.name
$data.description
```

---

## Validating HTMX Behaviors

### 1. Event Triggers (HX-Trigger)

NetMX uses **type-safe events** (`Events.Product.Created`) that translate to HTMX headers:

```powershell
$response = Invoke-WebRequest -Uri "$baseUrl/Product/Create" -Method POST -Headers $headers -Body $body -UseBasicParsing

# Validate event triggered
if ($response.Headers['HX-Trigger'] -match 'product.created') {
    Write-Host "✓ Event triggered correctly!" -ForegroundColor Green
} else {
    Write-Host "✗ Expected 'product.created' event" -ForegroundColor Red
}
```

**Expected Events**:
- `product.created` - After POST /Product/Create
- `product.updated` - After POST /Product/Edit
- `product.deleted` - After DELETE /Product/Delete/{id}

### 2. Swap Behavior (HX-Reswap)

DELETE operations should set `HX-Reswap: delete` to remove the element:

```powershell
$response = Invoke-WebRequest -Uri "$baseUrl/Product/Delete/$id" -Method DELETE -Headers @{ "HX-Request" = "true" } -UseBasicParsing

# Validate swap behavior
if ($response.Headers['HX-Reswap'] -eq 'delete') {
    Write-Host "✓ Element will be removed from DOM" -ForegroundColor Green
}
```

### 3. Partial Responses

HTMX requests should return **HTML partials**, not full pages:

```powershell
$headers = @{ "HX-Request" = "true" }
$response = Invoke-WebRequest -Uri "$baseUrl/Product/List" -Headers $headers -UseBasicParsing

# Validate partial response (no <!DOCTYPE html>)
if ($response.Content -notmatch '<!DOCTYPE html>') {
    Write-Host "✓ Partial response (no full page)" -ForegroundColor Green
}
```

---

## Advanced Testing Patterns

### Testing with JSON Payloads

If your API accepts JSON:

```powershell
$headers = @{ 
    "Content-Type" = "application/json"
    "HX-Request" = "true"
}

$body = @{
    Name = "Test Product"
    Description = "Test"
    IsActive = $true
} | ConvertTo-Json

$response = Invoke-WebRequest -Uri "$baseUrl/api/products" -Method POST -Headers $headers -Body $body -UseBasicParsing
```

### Testing with Authentication

Add authorization headers:

```powershell
$token = "your-jwt-token"
$headers = @{ 
    "Authorization" = "Bearer $token"
    "HX-Request" = "true"
}

$response = Invoke-WebRequest -Uri "$baseUrl/Product/List" -Headers $headers -UseBasicParsing
```

### Testing Error Responses

```powershell
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/Product/Edit/invalid-id" -Method GET -UseBasicParsing
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    if ($statusCode -eq 404) {
        Write-Host "✓ Correctly returns 404 for invalid ID" -ForegroundColor Green
    }
}
```

### Testing Validation Errors

```powershell
# Submit invalid data
$body = "Name=&Description=&IsActive=true"  # Empty name (required)
$response = Invoke-WebRequest -Uri "$baseUrl/Product/Create" -Method POST -Headers $headers -Body $body -UseBasicParsing

# Should return 200 with validation errors in form
if ($response.StatusCode -eq 200 -and $response.Content -match 'validation|error|required') {
    Write-Host "✓ Validation error displayed" -ForegroundColor Green
}
```

---

## CI/CD Integration

### GitHub Actions Example

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
        working-directory: src/YourApp.Web
      
      - name: Start app
        run: |
          Start-Process -NoNewWindow -FilePath "dotnet" -ArgumentList "run","--urls","http://localhost:5263"
          Start-Sleep -Seconds 10
        working-directory: src/YourApp.Web
      
      - name: Run endpoint tests
        run: |
          $response = Invoke-WebRequest -Uri "http://localhost:5263/Product/List" -UseBasicParsing
          if ($response.StatusCode -ne 200) { exit 1 }
```

### Azure DevOps Example

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
  workingDirectory: src/YourApp.Web

- powershell: |
    Start-Process -NoNewWindow -FilePath "dotnet" -ArgumentList "run","--urls","http://localhost:5263"
    Start-Sleep -Seconds 10
  displayName: 'Start app'
  workingDirectory: src/YourApp.Web

- powershell: |
    $response = Invoke-WebRequest -Uri "http://localhost:5263/Product/List" -UseBasicParsing
    if ($response.StatusCode -ne 200) { exit 1 }
  displayName: 'Test endpoints'
```

---

## Validation Results Example

**October 23, 2025 Test Run**:

| Endpoint | Method | Status | HTMX Headers | Result |
|----------|--------|--------|--------------|--------|
| `/Product` | GET | ✅ 200 | N/A | Full page loads |
| `/Product/List` | GET | ✅ 200 | N/A | Partial view |
| `/Product/Create` | GET | ✅ 200 | N/A | Form modal |
| `/Product/Create` | POST | ✅ 200 | `HX-Trigger: product.created` | Created |
| `/Product/Edit/{id}` | GET | ✅ 200 | N/A | Edit form |
| `/Product/Edit` | POST | ✅ 200 | `HX-Trigger: product.updated` | Updated |
| `/Product/Delete/{id}` | DELETE | ✅ 200 | `HX-Trigger: product.deleted`<br>`HX-Reswap: delete` | Deleted |

**Total Test Time**: ~15 seconds  
**Manual Browser Clicks Saved**: 20+  
**Automation Level**: 100%

---

## Benefits Summary

### Developer Benefits
- ✅ **Rapid validation** - Test changes in seconds
- ✅ **No manual clicking** - Fully scripted workflow
- ✅ **Repeatable** - Same results every time
- ✅ **Comprehensive** - Test all CRUD operations

### CI/CD Benefits
- ✅ **Pipeline integration** - Run in GitHub Actions, Azure DevOps
- ✅ **Fast feedback** - Detect issues before merge
- ✅ **Zero dependencies** - No Selenium, Playwright setup needed
- ✅ **Cross-platform** - PowerShell Core on Linux/Mac

### Quality Benefits
- ✅ **Regression prevention** - Catch breaking changes early
- ✅ **HTMX validation** - Verify headers and behaviors
- ✅ **Event validation** - Confirm type-safe events work
- ✅ **Complete coverage** - Test full CRUD lifecycle

---

## Troubleshooting

### Issue: Connection Refused

**Problem**: `Invoke-WebRequest: Unable to connect to the remote server`

**Solution**:
```powershell
# Check if app is running
Get-Process -Name "dotnet"

# Start app manually
cd src/YourApp.Web
dotnet run --urls "http://localhost:5263"

# Wait for app to start
Start-Sleep -Seconds 5
```

### Issue: 404 Not Found

**Problem**: Endpoint returns 404

**Solution**:
```powershell
# Check controller routing
# Verify route matches: /Product/Create not /api/Product/Create

# Test health endpoint first
Invoke-WebRequest -Uri "http://localhost:5263/health" -UseBasicParsing
```

### Issue: Missing HTMX Headers

**Problem**: `HX-Trigger` header not present

**Solution**:
```powershell
# Add HX-Request header to simulate HTMX
$headers = @{ "HX-Request" = "true" }
$response = Invoke-WebRequest -Uri "$url" -Headers $headers -UseBasicParsing

# Verify controller uses HxTrigger() extension method
# Check: this.HxTrigger(Events.Product.Created);
```

### Issue: Form Submission Fails

**Problem**: POST returns 400 Bad Request

**Solution**:
```powershell
# Use form-encoded data, not JSON
$body = "Name=Test&Description=Test&IsActive=true"
$headers = @{ "Content-Type" = "application/x-www-form-urlencoded" }

# Check ModelState validation in controller
# Add validation error logging
```

---

## Next Steps

1. **Save test script** - Create `test-endpoints.ps1` in your project
2. **Run after changes** - Quick validation before commits
3. **Add to CI/CD** - Automate in GitHub Actions
4. **Expand coverage** - Test edge cases, validation errors
5. **Monitor performance** - Track response times

---

**Remember**: This is **not a replacement** for E2E tests (Playwright), but a **fast validation** tool for CRUD operations!

For full E2E testing with JavaScript validation, use Playwright/Selenium.

For API/endpoint validation, **PowerShell is perfect**! 🚀

# NetMX Endpoint Test Script
# Generated: {DATE}
# Usage: ./test-endpoints.ps1 -BaseUrl "http://localhost:5263" -Entity "Product"

param(
    [string]$BaseUrl = "http://localhost:5263",
    [string]$Entity = "Product"
)

Write-Host "`n╔════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   NetMX Endpoint Test Suite           ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════╝`n" -ForegroundColor Cyan

$testsPassed = 0
$testsFailed = 0

function Test-Endpoint {
    param(
        [string]$Name,
        [scriptblock]$TestBlock
    )
    
    Write-Host "[$($testsPassed + $testsFailed + 1)] Testing $Name" -ForegroundColor Yellow
    
    try {
        & $TestBlock
        $script:testsPassed++
        Write-Host "  ✓ PASSED`n" -ForegroundColor Green
    }
    catch {
        $script:testsFailed++
        Write-Host "  ✗ FAILED: $($_.Exception.Message)`n" -ForegroundColor Red
    }
}

# Wait for app to be ready
Write-Host "Waiting for app to be ready..." -ForegroundColor Gray
$maxRetries = 10
$retryCount = 0
$appReady = $false

while ($retryCount -lt $maxRetries -and -not $appReady) {
    try {
        $response = Invoke-WebRequest -Uri "$BaseUrl/health" -Method GET -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
        if ($response.StatusCode -eq 200) {
            $appReady = $true
            Write-Host "✓ App is ready!`n" -ForegroundColor Green
        }
    }
    catch {
        $retryCount++
        Write-Host "  Retry $retryCount/$maxRetries..." -ForegroundColor Gray
        Start-Sleep -Seconds 1
    }
}

if (-not $appReady) {
    Write-Host "✗ App is not responding. Make sure it's running on $BaseUrl" -ForegroundColor Red
    exit 1
}

# Test 1: GET List (Empty)
Test-Endpoint "GET /$Entity/List (Empty)" {
    $response = Invoke-WebRequest -Uri "$BaseUrl/$Entity/List" -Method GET -UseBasicParsing
    if ($response.StatusCode -ne 200) {
        throw "Expected status 200, got $($response.StatusCode)"
    }
    Write-Host "  Status: $($response.StatusCode)" -ForegroundColor Gray
}

# Test 2: GET Create Form
Test-Endpoint "GET /$Entity/Create (Form)" {
    $response = Invoke-WebRequest -Uri "$BaseUrl/$Entity/Create" -Method GET -UseBasicParsing
    if ($response.StatusCode -ne 200) {
        throw "Expected status 200, got $($response.StatusCode)"
    }
    if ($response.Content -notmatch 'form') {
        throw "Response does not contain form"
    }
    Write-Host "  Status: $($response.StatusCode)" -ForegroundColor Gray
    Write-Host "  Form: Found" -ForegroundColor Gray
}

# Test 3: POST Create
Test-Endpoint "POST /$Entity/Create" {
    $headers = @{ 
        "Content-Type" = "application/x-www-form-urlencoded"
        "HX-Request" = "true" 
    }
    $body = "Name=Test+Entity&Description=Automated+test&IsActive=true"
    $response = Invoke-WebRequest -Uri "$BaseUrl/$Entity/Create" -Method POST -Headers $headers -Body $body -UseBasicParsing
    
    if ($response.StatusCode -ne 200) {
        throw "Expected status 200, got $($response.StatusCode)"
    }
    
    $expectedEvent = "$($Entity.ToLower()).created"
    if ($response.Headers['HX-Trigger'] -notmatch $expectedEvent) {
        throw "Expected HX-Trigger '$expectedEvent', got '$($response.Headers['HX-Trigger'])'"
    }
    
    Write-Host "  Status: $($response.StatusCode)" -ForegroundColor Gray
    Write-Host "  HX-Trigger: $($response.Headers['HX-Trigger'])" -ForegroundColor Gray
    
    # Store entity ID for later tests
    Start-Sleep -Seconds 1
    $listResponse = Invoke-WebRequest -Uri "$BaseUrl/$Entity/List" -Method GET -UseBasicParsing
    if ($listResponse.Content -match 'row-([a-f0-9\-]+)') {
        $script:entityId = $Matches[1]
        Write-Host "  Entity ID: $entityId" -ForegroundColor Gray
    }
}

# Test 4: GET List (With Data)
Test-Endpoint "GET /$Entity/List (After Create)" {
    $response = Invoke-WebRequest -Uri "$BaseUrl/$Entity/List" -Method GET -UseBasicParsing
    if ($response.StatusCode -ne 200) {
        throw "Expected status 200, got $($response.StatusCode)"
    }
    if ($response.Content -notmatch 'row-') {
        throw "No entities found in list"
    }
    Write-Host "  Status: $($response.StatusCode)" -ForegroundColor Gray
    Write-Host "  Entities: Found" -ForegroundColor Gray
}

# Test 5: GET Edit Form
Test-Endpoint "GET /$Entity/Edit/{id}" {
    if (-not $script:entityId) {
        throw "No entity ID available"
    }
    
    $response = Invoke-WebRequest -Uri "$BaseUrl/$Entity/Edit/$entityId" -Method GET -UseBasicParsing
    if ($response.StatusCode -ne 200) {
        throw "Expected status 200, got $($response.StatusCode)"
    }
    if ($response.Content -notmatch 'form') {
        throw "Response does not contain form"
    }
    Write-Host "  Status: $($response.StatusCode)" -ForegroundColor Gray
    Write-Host "  Form: Found" -ForegroundColor Gray
}

# Test 6: POST Edit
Test-Endpoint "POST /$Entity/Edit" {
    if (-not $script:entityId) {
        throw "No entity ID available"
    }
    
    $headers = @{ 
        "Content-Type" = "application/x-www-form-urlencoded"
        "HX-Request" = "true" 
    }
    $body = "Id=$entityId&Name=Updated+Entity&Description=This+was+edited&IsActive=true"
    $response = Invoke-WebRequest -Uri "$BaseUrl/$Entity/Edit" -Method POST -Headers $headers -Body $body -UseBasicParsing
    
    if ($response.StatusCode -ne 200) {
        throw "Expected status 200, got $($response.StatusCode)"
    }
    
    $expectedEvent = "$($Entity.ToLower()).updated"
    if ($response.Headers['HX-Trigger'] -notmatch $expectedEvent) {
        throw "Expected HX-Trigger '$expectedEvent', got '$($response.Headers['HX-Trigger'])'"
    }
    
    Write-Host "  Status: $($response.StatusCode)" -ForegroundColor Gray
    Write-Host "  HX-Trigger: $($response.Headers['HX-Trigger'])" -ForegroundColor Gray
}

# Test 7: DELETE
Test-Endpoint "DELETE /$Entity/Delete/{id}" {
    if (-not $script:entityId) {
        throw "No entity ID available"
    }
    
    $headers = @{ "HX-Request" = "true" }
    $response = Invoke-WebRequest -Uri "$BaseUrl/$Entity/Delete/$entityId" -Method DELETE -Headers $headers -UseBasicParsing
    
    if ($response.StatusCode -ne 200) {
        throw "Expected status 200, got $($response.StatusCode)"
    }
    
    $expectedEvent = "$($Entity.ToLower()).deleted"
    if ($response.Headers['HX-Trigger'] -notmatch $expectedEvent) {
        throw "Expected HX-Trigger '$expectedEvent', got '$($response.Headers['HX-Trigger'])'"
    }
    
    if ($response.Headers['HX-Reswap'] -ne 'delete') {
        throw "Expected HX-Reswap 'delete', got '$($response.Headers['HX-Reswap'])'"
    }
    
    Write-Host "  Status: $($response.StatusCode)" -ForegroundColor Gray
    Write-Host "  HX-Trigger: $($response.Headers['HX-Trigger'])" -ForegroundColor Gray
    Write-Host "  HX-Reswap: $($response.Headers['HX-Reswap'])" -ForegroundColor Gray
}

# Test 8: Verify Deletion
Test-Endpoint "GET /$Entity/List (After Delete)" {
    Start-Sleep -Seconds 1
    $response = Invoke-WebRequest -Uri "$BaseUrl/$Entity/List" -Method GET -UseBasicParsing
    
    if ($response.StatusCode -ne 200) {
        throw "Expected status 200, got $($response.StatusCode)"
    }
    
    if ($response.Content -match $entityId) {
        throw "Entity still appears in list after deletion"
    }
    
    Write-Host "  Status: $($response.StatusCode)" -ForegroundColor Gray
    Write-Host "  Entity: Deleted" -ForegroundColor Gray
}

# Summary
Write-Host "`n╔════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   Test Results                         ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════╝`n" -ForegroundColor Cyan

Write-Host "  Passed: $testsPassed" -ForegroundColor Green
Write-Host "  Failed: $testsFailed" -ForegroundColor $(if ($testsFailed -gt 0) { "Red" } else { "Green" })
Write-Host "  Total:  $($testsPassed + $testsFailed)`n"

if ($testsFailed -gt 0) {
    Write-Host "✗ Some tests failed" -ForegroundColor Red
    exit 1
}
else {
    Write-Host "✓ All tests passed!" -ForegroundColor Green
    exit 0
}

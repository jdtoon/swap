# Swap.Htmx End-to-End Tests

Playwright-based E2E tests for verifying Swap.Htmx framework features in real browsers.

## Overview

These tests verify that framework features work correctly in actual browser environments:

- **ToastTests**: Toast notification display and behavior
- **OobSwapTests**: Out-of-band swap functionality
- **CombinedTests**: Toast + OOB working together

## Prerequisites

1. **Playwright browsers** must be installed:
   ```bash
   pwsh bin/Debug/net10.0/playwright.ps1 install
   ```

2. **Test app must be running** at `http://localhost:5000`:
   ```bash
   cd ..\Swap.Htmx.TestApp\src
   dotnet run
   ```

## Running Tests

```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~ToastTests"

# Run with verbose output
dotnet test --verbosity normal
```

## Test Strategy

### Unit Tests vs E2E Tests

- **Unit tests** (`Swap.Htmx.Tests`): Verify methods work, headers set correctly
- **E2E tests** (`Swap.Htmx.E2ETests`): Verify browser displays toasts, OOB swaps update DOM

### Test Structure

Each test:
1. Navigates to test app (http://localhost:5000)
2. Interacts with UI elements using `data-test-id` selectors
3. Verifies DOM changes occur correctly

### Key Selectors

Tests use stable `data-test-id` attributes instead of CSS classes:

```csharp
await Page.GetByTestId("toast-success").ClickAsync();
var toast = Page.Locator(".toast").First;
await Expect(toast).ToBeVisibleAsync();
```

## Test Coverage

### Toast Tests (6 tests)
- ✅ Success toast displays
- ✅ Error toast displays
- ✅ Warning toast displays
- ✅ Info toast displays
- ✅ Multiple toasts display simultaneously
- ✅ Toasts auto-dismiss after 3.5 seconds

### OOB Swap Tests (5 tests)
- ✅ Single OOB update
- ✅ Multiple OOB updates (3 panels)
- ✅ Counter increment (innerHTML strategy)
- ✅ Multiple counter increments
- ✅ OOB doesn't affect main swap target

### Combined Tests (4 tests)
- ✅ Toast + OOB both trigger
- ✅ Main target and OOB both update
- ✅ Toast dismissal doesn't affect OOB content
- ✅ Multiple combined actions accumulate toasts

**Total: 16 E2E tests** (15 feature tests + 1 debug test)

## Browser Support

Playwright tests run on:
- Chromium (default)
- Firefox
- WebKit (Safari engine)

By default, tests run in Chromium. To test in multiple browsers, configure in `runsettings.json`.

## Debugging

### Screenshots on Failure

Playwright automatically captures screenshots on test failures (saved to `TestResults/`).

### Headed Mode

Run tests with visible browser:

```bash
# Set environment variable
$env:HEADED="true"
dotnet test
```

### Slow Motion

Slow down test execution for debugging:

```csharp
// In test setup
BrowserTypeLaunchOptions = new() { SlowMo = 500 }
```

## CI/CD Integration

E2E tests can run in CI pipelines:

```yaml
- name: Install Playwright browsers
  run: pwsh framework/Swap.Htmx.E2ETests/bin/Debug/net10.0/playwright.ps1 install

- name: Start test app
  run: |
    cd framework/Swap.Htmx.TestApp/src
    dotnet run &
    sleep 5

- name: Run E2E tests
  run: dotnet test framework/Swap.Htmx.E2ETests
```

## Maintenance

### Updating Test IDs

When test app UI changes, update `data-test-id` attributes in views and corresponding selectors in tests.

### Timing Adjustments

If tests are flaky due to timing, adjust delays:

```csharp
await Task.Delay(500); // Increase if needed
await Expect(element).ToBeVisibleAsync(new() { Timeout = 5000 }); // Increase timeout
```

## Troubleshooting

### Test app not running
```
Error: net::ERR_CONNECTION_REFUSED at http://localhost:5000
```
**Solution**: Start test app first: `cd ../Swap.Htmx.TestApp/src && dotnet run`

### Playwright browsers not installed
```
Error: Executable doesn't exist at C:\Users\...\ms-playwright\chromium-1194\chrome.exe
```
**Solution**: Run `pwsh bin/Debug/net10.0/playwright.ps1 install`

### Element not found
```
Error: Locator.click: Timeout 30000ms exceeded
```
**Solution**: Check `data-test-id` attributes match between tests and views

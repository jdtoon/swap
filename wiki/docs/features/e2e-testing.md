# End-to-End Testing

End-to-end (E2E) tests verify that Swap.Htmx features work correctly in real browsers, not just in unit tests. The framework includes comprehensive Playwright-based E2E tests.

## Overview

E2E tests complement unit tests by:
- **Unit tests**: Verify methods work, headers set correctly
- **E2E tests**: Verify browser displays toasts, DOM updates occur, user interactions work

## Test Infrastructure

### Technology Stack

- **Playwright**: Browser automation (Chromium, Firefox, WebKit)
- **NUnit**: Test framework
- **Test App**: Minimal ASP.NET Core app showcasing features

### Test Coverage

The E2E test suite includes **16 tests** across 4 categories:

#### Toast Tests (6 tests)
- Success/Error/Warning/Info toast display
- Multiple toasts stacking
- Auto-dismiss after 3 seconds

#### OOB Swap Tests (5 tests)
- Single OOB element update
- Multiple OOB elements updating simultaneously
- innerHTML strategy (counter increment)
- Multiple increments
- OOB not affecting main swap target

#### Combined Tests (4 tests)
- Toast + OOB in same response
- Main target + OOB both updating
- Toast dismissal not affecting OOB content
- Multiple combined actions accumulating toasts

#### Debug Tests (1 test)
- Diagnostic test for development

## Running E2E Tests

### Prerequisites

1. **Install Playwright browsers** (one-time setup):
   ```bash
   cd framework/Swap.Htmx.E2ETests
   pwsh bin/Debug/net10.0/playwright.ps1 install
   ```

2. **Start the test app**:
   ```bash
   cd framework/Swap.Htmx.TestApp/src
   dotnet run
   # App runs at http://localhost:5000
   ```

### Run Tests

In a separate terminal:

```bash
cd framework/Swap.Htmx.E2ETests

# Run all tests
dotnet test

# Run specific test category
dotnet test --filter "FullyQualifiedName~ToastTests"
dotnet test --filter "FullyQualifiedName~OobSwapTests"
dotnet test --filter "FullyQualifiedName~CombinedTests"

# Run single test
dotnet test --filter "FullyQualifiedName~ShowSuccessToast"
```

## Test Structure

### Test Selectors

Tests use stable `data-test-id` attributes instead of CSS classes:

```csharp
[Test]
public async Task ShowSuccessToast()
{
    // Click button by test ID
    await Page.Locator("[data-test-id='toast-success']")
              .ClickAsync(new() { Force = true });
    
    // Wait for toast to appear
    var toast = Page.Locator(".toast").First;
    await Expect(toast).ToBeVisibleAsync(new() { Timeout = 5000 });
    
    // Verify content
    var content = await toast.TextContentAsync();
    Assert.That(content, Does.Contain("Success"));
}
```

### Key Patterns

**Force Clicks**: Elements may not be "visible" to Playwright due to CSS/layout:
```csharp
await Page.Locator("[data-test-id='button']")
          .ClickAsync(new() { Force = true });
```

**Wait for HTMX**: Give HTMX time to process server response and update DOM:
```csharp
await Page.WaitForTimeoutAsync(1000);
```

**Fresh Locators After OOB**: OOB swaps replace DOM elements, so query fresh locators:
```csharp
// ❌ Wrong - caches locator before swap
var panel = Page.Locator("[data-test-id='panel']");
await Page.Locator("[data-test-id='update-button']").ClickAsync();
await Page.WaitForTimeoutAsync(1000);
var text = await panel.TextContentAsync(); // Fails - old element removed!

// ✅ Correct - query after swap
await Page.Locator("[data-test-id='update-button']").ClickAsync();
await Page.WaitForTimeoutAsync(1000);
var text = await Page.Locator("[data-test-id='panel']").TextContentAsync();
```

## Writing New Tests

### 1. Add Test ID to HTML

```html
<button hx-post="/products/delete" 
        hx-target="#result"
        data-test-id="delete-product">
    Delete
</button>
```

### 2. Write Test

```csharp
[Test]
public async Task DeleteProduct_RemovesFromList()
{
    // Arrange
    await Page.GotoAsync("http://localhost:5000/products");
    
    // Act
    await Page.Locator("[data-test-id='delete-product']")
              .ClickAsync(new() { Force = true });
    
    // Assert
    await Page.WaitForTimeoutAsync(1000);
    var confirmText = await Page.Locator("[data-test-id='result']")
                                 .TextContentAsync();
    Assert.That(confirmText, Does.Contain("Deleted"));
}
```

### 3. Ensure Test App Includes Feature

The test app must have endpoints and views for features being tested.

## Manual Testing

While tests run automatically, **manually test in a browser** to:
- See actual visual appearance
- Verify animations and transitions
- Test keyboard navigation
- Check different screen sizes
- Verify accessibility

Visit http://localhost:5000/test when the test app is running.

## Common Issues

### Connection Refused
**Problem**: `net::ERR_CONNECTION_REFUSED at http://localhost:5000`

**Solution**: Make sure test app is running:
```bash
cd framework/Swap.Htmx.TestApp/src
dotnet run
```

### Element Not Found
**Problem**: `Timeout waiting for Locator("[data-test-id='...'"])`

**Solutions**:
1. Check test app includes the `data-test-id` attribute
2. Verify element exists on page (check HTML in browser)
3. For OOB swaps, ensure controller includes `data-test-id` in response HTML

### Stale Element
**Problem**: `Element is not attached to the DOM`

**Solution**: Don't cache locators before HTMX operations. Query fresh after waits:
```csharp
// Query AFTER HTMX operation completes
await Page.WaitForTimeoutAsync(1000);
var elem = Page.Locator("[data-test-id='target']");
```

## Browser Matrix Testing

By default, tests run in Chromium. To test all browsers:

**playwright.runsettings:**
```xml
<RunSettings>
  <Playwright>
    <BrowserName>chromium</BrowserName>
    <LaunchOptions>
      <Headless>true</Headless>
    </LaunchOptions>
  </Playwright>
</RunSettings>
```

Run specific browser:
```bash
dotnet test --settings:chromium.runsettings
dotnet test --settings:firefox.runsettings
dotnet test --settings:webkit.runsettings
```

## Debugging Tests

### Screenshots

Tests automatically capture screenshots on failure to `bin/Debug/net10.0/screenshots/`.

### Headed Mode

Run tests with visible browser:

```csharp
public override BrowserNewContextOptions ContextOptions()
{
    return new BrowserNewContextOptions
    {
        // Set to false to see browser
        Headless = false
    };
}
```

### Slow Motion

Add delays to see what's happening:

```csharp
await Page.GotoAsync("http://localhost:5000/test");
await Page.WaitForTimeoutAsync(2000); // Pause to observe
```

## Best Practices

✅ **Use data-test-id** - More stable than CSS classes which change
✅ **Wait appropriately** - Use `WaitForTimeoutAsync` after HTMX operations
✅ **Test user journeys** - Not just individual features
✅ **Keep tests focused** - One assertion per test when possible
✅ **Run manually** - Automated tests don't catch visual/UX issues

❌ **Don't rely on fixed delays** - Use Playwright expectations when possible
❌ **Don't cache locators** - Query fresh, especially after DOM updates
❌ **Don't skip manual testing** - E2E tests complement, don't replace, human testing

## CI/CD Integration

E2E tests can run in CI/CD pipelines:

```yaml
# GitHub Actions example
- name: Install Playwright
  run: pwsh framework/Swap.Htmx.E2ETests/bin/Debug/net10.0/playwright.ps1 install

- name: Start Test App
  run: dotnet run --project framework/Swap.Htmx.TestApp/src &
  
- name: Wait for App
  run: sleep 5

- name: Run E2E Tests
  run: dotnet test framework/Swap.Htmx.E2ETests
```

## See Also

- [Testing Framework](./testing-framework.md) - Unit testing
- [Toast Notifications](./toast-notifications.md) - Feature being tested
- [OOB Swaps](./oob-swaps.md) - Feature being tested
- [E2E Test README](https://github.com/jdtoon/swap/blob/main/framework/Swap.Htmx.E2ETests/README.md) - Full test documentation

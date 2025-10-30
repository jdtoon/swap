# Swap.Htmx.Tests

Unit tests for the Swap.Htmx framework package.

## Test Coverage

### SwapControllerTests (7 tests)
Tests for the `SwapController` base class that provides HTMX-aware view rendering:

- ✅ Returns `PartialView` when HX-Request header is present
- ✅ Returns `View` when HX-Request header is absent
- ✅ Handles custom view names correctly
- ✅ Handles null models
- ✅ Uses conventional view names when view name is null

### SwapHtmxExtensionsTests (18 tests)
Tests for extension methods that work with HTMX requests and responses:

**Request Extensions:**
- ✅ `IsHtmxRequest()` - Detects HX-Request header
- ✅ `IsHtmxBoosted()` - Detects HX-Boosted header
- ✅ `GetHtmxCurrentUrl()` - Gets current URL from header
- ✅ `GetHtmxTarget()` - Gets target element ID
- ✅ `GetHtmxTrigger()` - Gets trigger element ID

**Response Extensions:**
- ✅ `HxTrigger()` - Triggers client-side event
- ✅ `HxTriggerWithDetails()` - Triggers event with JSON payload
- ✅ `HxPushUrl()` - Pushes URL to browser history
- ✅ `HxPreventPushUrl()` - Prevents history update
- ✅ `HxReplaceUrl()` - Replaces current URL in history
- ✅ `HxRedirect()` - Performs client-side redirect
- ✅ `HxRefresh()` - Forces full page refresh
- ✅ `HxRetarget()` - Changes target element
- ✅ `HxReswap()` - Changes swap strategy

### SwapHtmxShellMiddlewareTests (10 tests)
Tests for middleware that enforces proper HTMX partial view responses:

- ✅ Returns 500 error when full page returned for HTMX request
- ✅ Passes through partial views correctly
- ✅ Allows full pages for boosted requests
- ✅ Allows full pages for non-HTMX requests
- ✅ Detects full pages with DOCTYPE tag
- ✅ Detects full pages with HTML + HEAD tags
- ✅ Allows HTML tags without HEAD (considered partial)
- ✅ Passes through non-200 status codes
- ✅ Passes through non-HTML content types
- ✅ Rethrows exceptions from pipeline
- ✅ Includes request details in error messages

## Running Tests

```bash
# Run all tests
dotnet test

# Run with verbose output
dotnet test -v detailed

# Run specific test class
dotnet test --filter "FullyQualifiedName~SwapControllerTests"
```

## Test Structure

All tests use:
- **xUnit** - Test framework
- **Moq** - Mocking library (for TempData provider)
- **Microsoft.AspNetCore.Mvc.Testing** - ASP.NET Core testing helpers

Tests create real `HttpContext` instances with configured headers to simulate HTMX behavior.

## Coverage Goals

Target: **80%+ code coverage**

Current coverage includes:
- All public methods in `SwapController`
- All extension methods in `SwapHtmxExtensions`
- All middleware logic paths in `SwapHtmxShellMiddleware`

## Contributing

When adding new features to Swap.Htmx:
1. Write tests first (TDD approach)
2. Ensure all existing tests pass
3. Aim for high code coverage
4. Test both happy paths and error cases
5. Include edge cases (null values, empty strings, etc.)

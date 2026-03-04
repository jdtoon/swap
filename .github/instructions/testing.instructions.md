---
description: "Use when writing, modifying, or reviewing tests for Swap framework libraries. Covers unit test structure, integration test patterns with Swap.Testing, assertion standards, and the mandatory three-layer testing approach."
applyTo: "**/*Tests*/**"
---
# Swap Testing Standards

## Mandatory Testing Workflow

Every code change in `lib/` or `framework/` requires:

1. **Write tests first or alongside the code** — never after-the-fact
2. **Run the full suite** — `dotnet test --configuration Release --verbosity normal`
3. **All tests green** before the change is considered complete

## Three-Layer Test Pattern

Every feature touching HTTP/HTMX behavior must be tested at all three layers:

### Layer 1 — Full Page (Browser Navigation)
```csharp
var response = await client.GetAsync("/items");
await response
    .AssertSuccess()
    .AssertContainsAsync("<html")           // Full layout present
    .AssertElementExistsAsync("#main-content");
```

### Layer 2 — Partial Isolation (HTMX Request)
```csharp
var response = await client.HtmxGetAsync("/items/list", target: "#item-list");
await response
    .AssertSuccess()
    .AssertPartialViewAsync()               // No layout wrapper
    .AssertElementExistsAsync(".item-row");
```

### Layer 3 — User Flow (Multi-Step Interaction)
```csharp
// Step 1: Land on page
var page = await client.GetAsync("/items");
await page.AssertSuccess();

// Step 2: HTMX interaction
var list = await client.HtmxGetAsync("/items/list");
await list.AssertSuccess();

// Step 3: Form submission
var created = await client.HtmxPostAsync("/items", new Dictionary<string, string>
{
    { "Name", "Test Item" }
});
await created.AssertSuccess().AssertToast("Created", "success");
```

## Unit Test Conventions

- File naming: `{ClassUnderTest}Tests.cs`
- One test class per class under test
- Use `[Fact]` for single-case tests, `[Theory]` with `[InlineData]` for parameterized
- Arrange-Act-Assert structure — no comments needed if the sections are clear
- Test method naming: `MethodName_Scenario_ExpectedResult`

## Integration Test Conventions

- Use `HtmxTestFixture<Program>` or `HtmxTestClient` from `Swap.Testing`
- Test both HTMX requests (`HtmxGetAsync`) and full page requests (`GetAsync`)
- Assert HTMX response headers: `AssertHxRedirect`, `AssertTrigger`, `AssertToast`
- Assert DOM structure: `AssertElementExistsAsync`, `AssertElementTextAsync`
- Use snapshot testing (`AssertMatchesSnapshotAsync`) for complex HTML output

## Public API Snapshot Tests

Any change to a public API surface must update snapshot tests:

- Location: `lib/Swap.Htmx.Tests/PublicApiSnapshots/`
- Run with `UPDATE_SNAPSHOTS=true` to regenerate, then review diff
- Snapshots are the contract — unintentional API changes will fail CI

## What Must Be Tested

| Change Type | Required Tests |
|-------------|---------------|
| New public method | Unit test + integration test if HTTP-facing |
| Bug fix | Regression test proving the fix |
| New middleware | Unit test for the middleware + integration test for the pipeline |
| New tag helper | Rendered HTML test + integration test in a view |
| Event system change | Event firing + handler execution + OOB swap output |
| SwapState change | Model binding + hidden field rendering + change tracking |
| Source generator change | Generator output snapshot test |

## Manual Verification

After automated tests pass, verify features work end-to-end:

- Start a relevant demo app (`dotnet run` in the demo directory)
- Exercise the feature in a browser — confirm HTMX swaps, toasts, navigation work
- Check browser DevTools Network tab for correct HX-* response headers
- Confirm no JavaScript console errors

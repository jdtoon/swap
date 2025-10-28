---
sidebar_position: 2
---

# HTMX Testing Framework

Swap.Testing is a fluent testing library for ASP.NET Core + HTMX apps.

## Highlights

- HTMX-aware client: `HtmxTestClient<TProgram>`
- Fluent assertions for HTML and HTMX attributes
- Partial view detection: `AssertPartialViewAsync()`
- Snapshot testing: `AssertMatchesSnapshotAsync()` with `UPDATE_SNAPSHOTS=true`
- Header assertions: `AssertHxRedirect`, `AssertHxPushUrl`, `AssertHxReswap`, `AssertHxRetarget`, `AssertHxTriggerHeaderContains`, `AssertHxLocationContains`

## Quick Start

```csharp
public class TodosTests : IClassFixture<HtmxTestFixture<Program>>
{
    private readonly HtmxTestClient<Program> _client;
    public TodosTests(HtmxTestFixture<Program> fixture) => _client = fixture.Client;

    [Fact]
    public async Task EditForm_IsPartial_WithHtmxAttributes()
    {
        var response = await _client.HtmxGetAsync("/todos/1/edit");
        await response
            .AssertSuccess()
            .AssertPartialViewAsync()
            .AssertElementExistsAsync("form")
            .AssertHxPostAsync("form", "/todos/1")
            .AssertHxTargetAsync("form", "#todo-1")
            .AssertHxSwapAsync("form", "outerHTML");
    }
}
```

## Snapshot Testing

```csharp
var response = await _client.HtmxGetAsync("/todos");
await response
    .AssertSuccess()
    .AssertMatchesSnapshotAsync("todo-list");
```

Update snapshots:

```bash
UPDATE_SNAPSHOTS=true dotnet test
```

## Available Assertions (Selected)

- `AssertStatus`, `AssertSuccess`
- `AssertContainsAsync`, `AssertDoesNotContainAsync`
- `AssertElementExistsAsync`, `AssertElementNotExistsAsync`, `AssertElementCountAsync`, `AssertElementTextAsync`
- `AssertHasCssClassAsync`, `AssertAttributeContainsAsync`
- `AssertHxGetAsync`, `AssertHxPostAsync`, `AssertHxPutAsync`, `AssertHxTargetAsync`, `AssertHxSwapAsync`, `AssertHxSwapOobAsync`, `AssertHxTriggerAsync`
- `AssertHxRedirect`, `AssertHxPushUrl`, `AssertHxReswap`, `AssertHxRetarget`, `AssertHxRefresh`, `AssertHxTriggerHeaderContains`, `AssertHxLocationContains`

See the repository at `framework/Swap.Testing/README.md` for full API and examples.

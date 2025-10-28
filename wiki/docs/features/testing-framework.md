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
- Header assertions: `AssertHxRedirect`, `AssertHxPushUrl`, `AssertHxReswap`, `AssertHxRetarget`, `AssertHxTriggerHeaderContains`, `AssertHxLocationContains`
- Typed HX header helpers: HX-Location JSON (`GetHxLocationJson` + field checks) and HX-Trigger parsing (event lists and JSON payloads)
- Form helper: `SubmitFormAsync(response, selector, overrides)`
- Follow redirects: `FollowHxRedirectAsync(response)`
- Validation assertions: `AssertHasValidationErrorsAsync`, `AssertFieldValidationErrorAsync`
- Partial root helpers: `AssertPartialRootIdAsync`, `AssertPartialRootMatchesAsync`
- Snapshot scrubbers: enable by default; customize via `SnapshotManager`

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
        response.AssertSuccess();
        await response.AssertPartialViewAsync();
        await response.AssertElementExistsAsync("form");
        await response.AssertHxPostAsync("form", "/todos/1");
        await response.AssertHxTargetAsync("form", "#todo-1");
        await response.AssertHxSwapAsync("form", "outerHTML");
    }
}
```

## Snapshot Testing

```csharp
var response = await _client.HtmxGetAsync("/todos");
response.AssertSuccess();
await response.AssertMatchesSnapshotAsync("todo-list");
```

Update snapshots:

```bash
UPDATE_SNAPSHOTS=true dotnet test
```

## Submitting forms and following redirects

```csharp
// Get create form
var getResp = await _client.HtmxGetAsync("/posts/create");
getResp.AssertSuccess();
await getResp.AssertPartialViewAsync();

// Submit with overrides
var postResp = await _client.SubmitFormAsync(getResp, "form", new()
{
    ["Title"] = "Hello",
    ["PublishedAt"] = DateTime.UtcNow.ToString("O"),
    ["AuthorId"] = "1"
});

postResp.AssertSuccess();

// If server set HX-Redirect, follow it
var maybeRedirect = await _client.FollowHxRedirectAsync(postResp);
maybeRedirect.AssertSuccess();
```

## Validation helpers

```csharp
var invalid = await _client.HtmxPostAsync("/posts/create", new()
{
    ["Title"] = "" // required
});

await invalid.AssertHasValidationErrorsAsync();
await invalid.AssertFieldValidationErrorAsync("Title");
```

## Snapshot scrubbers

Default scrubbers replace GUIDs, ISO timestamps, and anti-forgery tokens for stable snapshots.

```csharp
// Turn off defaults
SnapshotManager.UseDefaultScrubbers(false);

// Add a custom scrubber
SnapshotManager.AddScrubber(html => html.Replace("123.45", "[PRICE]"));

// Clear custom scrubbers
SnapshotManager.ClearScrubbers();
```

## Setup tips

- In your web app's Program.cs, add:

```csharp
public partial class Program { }
```

- Keep tests in a separate project (e.g., MyApp.Tests). If you keep a Tests folder under your app for scaffolds, exclude it from compilation in the app csproj:

```xml
<ItemGroup>
    <Compile Remove="Tests\**\*.cs" />
    <None Include="Tests\**\*.cs" />
    <!-- Exclude demo-only seeders or code not present in your model as needed -->
</ItemGroup>
```

## Available Assertions (Selected)

- `AssertStatus`, `AssertSuccess`
- `AssertContainsAsync`, `AssertDoesNotContainAsync`
- `AssertElementExistsAsync`, `AssertElementNotExistsAsync`, `AssertElementCountAsync`, `AssertElementTextAsync`
- `AssertHasCssClassAsync`, `AssertAttributeContainsAsync`
- `AssertHxGetAsync`, `AssertHxPostAsync`, `AssertHxPutAsync`, `AssertHxTargetAsync`, `AssertHxSwapAsync`, `AssertHxSwapOobAsync`, `AssertHxTriggerAsync`
- `AssertHxRedirect`, `AssertHxPushUrl`, `AssertHxPushUrlTrue`, `AssertHxPushUrlFalse`, `AssertHxPushUrlUrl`, `AssertHxReswap`, `AssertHxRetarget`, `AssertHxRefresh`, `AssertHxTriggerHeaderContains`, `AssertHxLocationContains`
- HX-Location JSON: `GetHxLocationJson`, `AssertHxLocationFieldEquals`, `AssertHxLocationFieldContains`
- HX-Trigger typed: `GetHxTriggerRaw`, `GetHxTriggerJson`, `GetHxTriggerEventNames`, `AssertHxTriggered`, `AssertHxTriggeredAfterSwap`, `AssertHxTriggeredAfterSettle`, `AssertHxTriggerFieldEquals`, `AssertHxTriggerFieldContains`, `AssertHxTriggerAfterSwapFieldEquals`, `AssertHxTriggerAfterSettleFieldEquals`, `AssertHxTriggerAfterSwapFieldContains`, `AssertHxTriggerAfterSettleFieldContains`

See the repository at `framework/Swap.Testing/README.md` for full API and examples.

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
- `AssertHxRedirect`, `AssertHxPushUrl`, `AssertHxReswap`, `AssertHxRetarget`, `AssertHxRefresh`, `AssertHxTriggerHeaderContains`, `AssertHxLocationContains`

See the repository at `framework/Swap.Testing/README.md` for full API and examples.

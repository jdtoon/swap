# Swap.Testing

[![NuGet](https://img.shields.io/nuget/v/Swap.Testing.svg)](https://www.nuget.org/packages/Swap.Testing)

`Swap.Testing` provides small helpers for writing integration tests against HTMX-powered ASP.NET Core MVC applications.

It is designed to work alongside `Swap.Htmx` but does not require it.

## Install

- NuGet: https://www.nuget.org/packages/Swap.Testing

```bash
dotnet add package Swap.Testing
```

## Key types

- `HtmxTestFixture<TProgram>` – wraps `WebApplicationFactory<TProgram>` and gives you an `HtmxTestClient<TProgram>`.
- `HtmxTestClient<TProgram>` – fluent client with HTMX-aware helpers (`HtmxGetAsync`, `HtmxPostAsync`, `SubmitFormAsync`, …).
- `HtmxTestResponse` – wraps `HttpResponseMessage` and exposes HTML and HTMX assertions (elements, attributes, HX headers, snapshots, validation, etc.).
- `SnapshotManager` – optional snapshot testing helper for HTML responses.

## Basic usage

1. **Create a test project** and reference your ASP.NET Core app project.
2. **Add Swap.Testing** to the test project.
3. **Use `HtmxTestFixture<TProgram>` as an xUnit fixture**:

```csharp
public class TodosTests : IClassFixture<HtmxTestFixture<Program>>
{
    private readonly HtmxTestClient<Program> _client;

    public TodosTests(HtmxTestFixture<Program> fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    public async Task GetTodos_ReturnsPartialWithItems()
    {
        var response = await _client.HtmxGetAsync("/todos");

        await response
            .AssertSuccess()
            .AssertPartialViewAsync()
            .AssertElementCountAsync(".todo-item", expectedCount: 5);
    }

    [Fact]
    public async Task UpdateCounter_RazorPage_ReturnsUpdatedCount()
    {
        // Use the Razor Pages helper to target a specific handler
        var response = await _client.HtmxGetPageHandlerAsync("/Counter", "Increment", new { count = 5 });

        await response
            .AssertSuccess()
            .AssertContainsAsync("Count: 6");
    }
}

```

## Form and redirect helpers

- `SubmitFormAsync` – finds a `<form>` in a previous HTML response, reads `hx-*` or `action/method`, collects inputs, and submits as an HTMX request.
- `FollowHxRedirectAsync` – follows an `HX-Redirect` header and returns the resulting HTMX response.

## Snapshot testing

Use `AssertMatchesSnapshotAsync` to compare HTML output against a stored snapshot.

```csharp
await response
    .AssertSuccess()
    .AssertMatchesSnapshotAsync("todo-list-partial");
```

The first run creates `__snapshots__/todo-list-partial.html`. Future runs compare normalized HTML. Set `UPDATE_SNAPSHOTS=true` to update snapshots.

## More examples

See `EXAMPLE_TESTS.cs` in this folder for a longer example suite using most of the helpers.

## Cookie Persistence

Cookies automatically persist across requests within the same `HtmxTestClient` instance, enabling session-based and authentication testing:

```csharp
// Login sets a session cookie — persists automatically
await _client.HtmxPostAsync("/auth/login", loginData);

// Subsequent requests include the session cookie
var dashboard = await _client.GetAsync("/dashboard");
await dashboard.AssertSuccess().AssertContainsAsync("Welcome");

// Inspect cookies
var cookies = _client.Cookies.GetAllCookies();

// Clear for test isolation
_client.ClearCookies();
```

## OOB Swap Introspection

Inspect and assert out-of-band swap elements in responses:

```csharp
var response = await _client.HtmxPostAsync("/shop/purchase", data);

// Get all OOB swaps as structured data
var swaps = await response.GetOobSwapsAsync();
// Each OobSwap has: TargetId, SwapMode, HtmlContent

// Fluent assertions
await response
    .AssertOobSwapExistsAsync("cart-count")
    .AssertOobSwapContentAsync("cart-count", "5")
    .AssertOobSwapCountAsync(3);
```

## Trigger Payload Assertions

Deep-inspect HX-Trigger JSON payloads:

```csharp
response
    .AssertTriggerPayload("showToast", "message", "Created")
    .AssertTriggerPayload("itemCreated", "data.id", "123")
    .AssertTriggerCount(2);

// Typed deserialization
var toast = response.GetTriggerPayload<ToastData>("showToast");
```

## Form Field Assertions

Assert form field existence and values:

```csharp
await response
    .AssertFormFieldExistsAsync("Email")
    .AssertFormValueAsync("Name", "John")
    .AssertFormValueAsync("Active", "true");   // checkbox
```

## Snapshot Scrubbers

The `SnapshotManager` supports custom scrubbers for dynamic content:

```csharp
SnapshotManager.ScrubUrls();                            // Replace URLs → [URL]
SnapshotManager.ScrubRegex(@"tenant-\w+", "[TENANT]"); // Custom pattern
SnapshotManager.ClearScrubbers();                       // Reset
```

Built-in default scrubbers automatically handle GUIDs → `[GUID]`, ISO dates → `[DATETIME]`, and anti-forgery tokens → `[TOKEN]`.
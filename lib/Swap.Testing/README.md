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
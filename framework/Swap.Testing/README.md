# Swap.Testing

A fluent testing framework for ASP.NET Core applications using HTMX, designed to make testing partial views and HTMX interactions delightful.

## Features

- 🚀 **Fluent API** - Chainable assertion methods for readable tests
- 🎯 **HTMX-Aware** - Built-in support for HX-Request headers and HTMX attributes
- 🔍 **HTML Parsing** - Query and assert on HTML structure using CSS selectors
- ⚡ **Partial View Testing** - Verify your HTMX endpoints return proper partials
- 🧪 **Integration Testing** - Built on ASP.NET Core's WebApplicationFactory
- 📝 **Form Submission Helper** - Submit forms from a prior response via `SubmitFormAsync`
- 🔁 **Follow Redirects** - One-liner to follow `HX-Redirect` via `FollowHxRedirectAsync`
- ✅ **Validation Assertions** - Assert summary or field-level validation errors
- 🧽 **Snapshot Scrubbers** - Replace GUIDs/timestamps/anti-forgery tokens for stable snapshots

## Installation

```bash
dotnet add package Swap.Testing
```

## Quick Start

### 1. Create a Test Fixture

```csharp
using Swap.Testing;
using Xunit;

namespace MyApp.Tests;

public class HomeControllerTests : IClassFixture<HtmxTestFixture<Program>>
{
    private readonly HtmxTestClient<Program> _client;

    public HomeControllerTests(HtmxTestFixture<Program> fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    public async Task Index_ReturnsSuccessAndCorrectContent()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert
        response.AssertSuccess();
        await response.AssertContainsAsync("Welcome to My App");
    }
}
```

### 2. Test HTMX Partials

```csharp
[Fact]
public async Task GetTodoPartial_ReturnsPartialWithHtmxAttributes()
{
    // Act - Make an HTMX request
    var response = await _client.HtmxGetAsync("/todos/1/edit");

    // Assert - Verify it's a partial and has HTMX attributes
    response.AssertSuccess();
    await response.AssertPartialViewAsync();
    await response.AssertHxPostAsync("form", "/todos/1");
    await response.AssertHxTargetAsync("form", "#todo-1");
    await response.AssertHxSwapAsync("form", "outerHTML");
}
```

### 3. Test HTML Structure

```csharp
[Fact]
public async Task TodoList_DisplaysAllTodos()
{
    // Act
    var response = await _client.GetAsync("/todos");

    // Assert - Query HTML structure
    response.AssertSuccess();
    await response.AssertElementCountAsync(".todo-item", 3);
    await response.AssertElementTextAsync("h1", "My Todos");
    await response.AssertElementExistsAsync("#add-todo-button");
}
```

### 4. Test POST Requests

```csharp
[Fact]
public async Task CreateTodo_WithHtmx_ReturnsNewTodoPartial()
{
    // Arrange
    var formData = new Dictionary<string, string>
    {
        ["title"] = "Buy groceries",
        ["completed"] = "false"
    };

    // Act - POST as HTMX request
    var response = await _client.HtmxPostAsync("/todos", formData);

    // Assert
    response.AssertStatus(HttpStatusCode.Created);
    await response.AssertPartialViewAsync();
    await response.AssertContainsAsync("Buy groceries");
    await response.AssertHxGetAsync(".edit-button", "/todos/");
}
```

## API Reference

### HtmxTestClient<TProgram>

The main client for making requests to your application.

#### HTTP Methods

```csharp
Task<HtmxTestResponse> GetAsync(string path)
Task<HtmxTestResponse> PostAsync(string path, Dictionary<string, string>? formData = null)
Task<HtmxTestResponse> PutAsync(string path, Dictionary<string, string>? formData = null)
Task<HtmxTestResponse> DeleteAsync(string path)
```

#### HTMX Methods

```csharp
// Automatically adds HX-Request: true header
Task<HtmxTestResponse> HtmxGetAsync(string path, string? target = null, string? trigger = null)
Task<HtmxTestResponse> HtmxPostAsync(string path, Dictionary<string, string>? formData = null, string? target = null, string? trigger = null)
Task<HtmxTestResponse> HtmxPutAsync(string path, Dictionary<string, string>? formData = null, string? target = null, string? trigger = null)
Task<HtmxTestResponse> HtmxDeleteAsync(string path, string? target = null, string? trigger = null)

// Helpers
Task<HtmxTestResponse> SubmitFormAsync(HtmxTestResponse response, string formSelector, Dictionary<string,string>? overrides = null, string? target = null, string? trigger = null)
Task<HtmxTestResponse> FollowHxRedirectAsync(HtmxTestResponse response, string? target = null, string? trigger = null)
```

#### Configuration

```csharp
HtmxTestClient<TProgram> WithHeader(string name, string value)
HtmxTestClient<TProgram> AsHtmxRequest()
```

### HtmxTestResponse

Fluent assertion methods for HTTP responses.

#### Status Assertions

```csharp
HtmxTestResponse AssertStatus(HttpStatusCode expectedStatus)
HtmxTestResponse AssertSuccess() // 2xx status codes
```

#### Content Assertions

```csharp
Task<HtmxTestResponse> AssertContainsAsync(string expectedText)
Task<HtmxTestResponse> AssertDoesNotContainAsync(string unexpectedText)
```

#### Header Assertions

```csharp
HtmxTestResponse AssertHeader(string headerName, string? expectedValue = null)
```

#### HTML Element Assertions

```csharp
Task<HtmxTestResponse> AssertElementExistsAsync(string cssSelector)
Task<HtmxTestResponse> AssertElementNotExistsAsync(string cssSelector)
Task<HtmxTestResponse> AssertElementCountAsync(string cssSelector, int expectedCount)
Task<HtmxTestResponse> AssertElementTextAsync(string cssSelector, string expectedText)
Task<HtmxTestResponse> AssertHasCssClassAsync(string cssSelector, string className)
Task<HtmxTestResponse> AssertAttributeContainsAsync(string cssSelector, string attribute, string expectedSubstring)
```

#### HTMX Attribute Assertions

```csharp
Task<HtmxTestResponse> AssertHxGetAsync(string cssSelector, string? expectedUrl = null)
Task<HtmxTestResponse> AssertHxPostAsync(string cssSelector, string? expectedUrl = null)
Task<HtmxTestResponse> AssertHxTargetAsync(string cssSelector, string? expectedTarget = null)
Task<HtmxTestResponse> AssertHxSwapAsync(string cssSelector, string? expectedSwap = null)
Task<HtmxTestResponse> AssertHxTriggerAsync(string cssSelector, string? expectedTrigger = null)
Task<HtmxTestResponse> AssertHxSwapOobAsync(string cssSelector, string? expectedValue = null)
Task<HtmxTestResponse> AssertHxAttributeAsync(string cssSelector, string attribute, string? expectedValue = null)
```

#### Partial View Assertions

```csharp
Task<HtmxTestResponse> AssertPartialViewAsync() // Verifies no <html> or <body> tags in raw content
Task<HtmxTestResponse> AssertAntiForgeryTokenAsync(string formSelector = "form")
Task<HtmxTestResponse> AssertPartialRootIdAsync(string expectedId)
Task<HtmxTestResponse> AssertPartialRootMatchesAsync(string cssSelector)
```

#### Custom Assertions

```csharp
Task<HtmxTestResponse> AssertAsync(Action<IHtmlDocument> assertion)
Task<HtmxTestResponse> AssertAsync(Func<IHtmlDocument, Task> assertion)
```

#### HTMX Header Assertions

```csharp
HtmxTestResponse AssertHxRedirect(string expectedUrl)
HtmxTestResponse AssertHxPushUrl(string? expectedValue = null) // true or URL
HtmxTestResponse AssertHxReswap(string? expectedValue = null)
HtmxTestResponse AssertHxRetarget(string? expectedValue = null)
HtmxTestResponse AssertHxRefresh(bool? expected = null)        // presence or explicit
HtmxTestResponse AssertHxTriggerHeaderContains(string substring)
HtmxTestResponse AssertHxLocationContains(string substring)     // hx-location JSON contains
```

#### Snapshot Testing
#### Validation Assertions

```csharp
Task<HtmxTestResponse> AssertHasValidationErrorsAsync()
Task<HtmxTestResponse> AssertFieldValidationErrorAsync(string fieldName, string? messageContains = null)
Task<HtmxTestResponse> AssertNoValidationErrorsAsync()
```

#### OOB Convenience

```csharp
Task<HtmxTestResponse> AssertOutOfBandAsync(string cssSelector, string? expectedContains = null)
```

```csharp
Task<HtmxTestResponse> AssertMatchesSnapshotAsync(string snapshotName, string? snapshotDirectory = null, bool? updateSnapshots = null)
```

## Advanced Usage

### Custom WebApplicationFactory Configuration

```csharp
public class CustomTestFixture : IDisposable
{
    public HtmxTestClient<Program> Client { get; }
    private readonly WebApplicationFactory<Program> _factory;

    public CustomTestFixture()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Override services for testing
                    services.AddScoped<IMyService, MockMyService>();
                });
            });

        Client = new HtmxTestClient<Program>(_factory);
    }

    public void Dispose() => _factory?.Dispose();
}
```

### Testing HTMX Triggers and Targets

```csharp
[Fact]
public async Task DeleteTodo_ReturnsEmptyWithSwapOutOfBand()
{
    var response = await _client
        .AsHtmxRequest()
        .WithHeader("HX-Target", "#todo-5")
        .DeleteAsync("/todos/5");

    response.AssertSuccess();
    response.AssertHeader("HX-Trigger", "todoDeleted");
    await response.AssertContainsAsync("<div id=\"todo-5\"></div>");
}
```

### Complex HTML Assertions

```csharp
[Fact]
public async Task TodoList_HasCorrectStructure()
{
    var response = await _client.GetAsync("/todos");

    await response.AssertAsync(async doc =>
    {
        var todos = doc.QuerySelectorAll(".todo-item");
        Assert.Equal(5, todos.Length);

        foreach (var todo in todos)
        {
            Assert.NotNull(todo.QuerySelector(".todo-title"));
            Assert.NotNull(todo.QuerySelector("button[hx-delete]"));
        }
    });
}
```

### Snapshot Testing
### Submitting forms from a prior response

```csharp
// GET the form as an HTMX partial
var getResponse = await _client.HtmxGetAsync("/posts/create");
getResponse.AssertSuccess();
await getResponse.AssertPartialViewAsync();

// Submit the form with overrides
var postResponse = await _client.SubmitFormAsync(getResponse, "form", new Dictionary<string, string>
{
    ["Title"] = "Hello",
    ["Body"] = "World",
    ["PublishedAt"] = DateTime.UtcNow.ToString("O"),
    ["AuthorId"] = "1"
});

postResponse.AssertSuccess();
postResponse.AssertHxTriggerHeaderContains("refreshPostList");
```

### Following HX-Redirect

```csharp
var resp = await _client.HtmxPostAsync("/account/login", credentials);
resp.AssertSuccess();

// If server sets HX-Redirect: /dashboard, follow it
var redirectResp = await _client.FollowHxRedirectAsync(resp);
redirectResp.AssertSuccess();
```

### Asserting validation errors
### Snapshot scrubbers

Built-in scrubbers make snapshots stable by replacing volatile values:
- GUIDs → [GUID]
- ISO date/time strings → [DATETIME]
- Anti-forgery token values → [TOKEN]

They are enabled by default. You can customize:

```csharp
// Disable defaults
SnapshotManager.UseDefaultScrubbers(false);

// Add your own scrubber
SnapshotManager.AddScrubber(content => content.Replace("8.99", "[PRICE]"));

// Remove custom scrubbers
SnapshotManager.ClearScrubbers();
```

```csharp
var invalid = await _client.HtmxPostAsync("/posts/create", new Dictionary<string,string>
{
    ["Title"] = "", // required
    ["AuthorId"] = "1",
    ["PublishedAt"] = DateTime.UtcNow.ToString("O")
});

invalid.AssertSuccess()
    .AssertHxRetarget("#modal-container")
    .AssertHxReswap("innerHTML");

await invalid.AssertHasValidationErrorsAsync();
await invalid.AssertFieldValidationErrorAsync("Title");
```

Snapshot testing captures the HTML output and compares it on future runs to detect unintended changes.

```csharp
[Fact]
public async Task TodoList_MatchesSnapshot()
{
    // Act
    var response = await _client.HtmxGetAsync("/todos");

    // Assert - Compare against saved snapshot
    response.AssertSuccess();
    await response.AssertMatchesSnapshotAsync("todo-list");
}
```

**Update snapshots** when you intentionally change HTML:

```bash
# Set environment variable to update all snapshots
UPDATE_SNAPSHOTS=true dotnet test

# Or update specific test
UPDATE_SNAPSHOTS=true dotnet test --filter TodoList_MatchesSnapshot
```

**Snapshot files** are saved in `__snapshots__/` directory:
- `todo-list.html` - Expected snapshot
- `todo-list.diff.html` - Created when mismatch occurs (actual content)

```csharp
// Custom snapshot directory
await response.AssertMatchesSnapshotAsync(
    "todo-list",
    snapshotDirectory: "Tests/__snapshots__");

// Force update in code (not recommended)
await response.AssertMatchesSnapshotAsync(
    "todo-list",
    updateSnapshots: true);
```


await response.AssertAsync(async doc =>
{
    var todos = doc.QuerySelectorAll(".todo-item");
    Assert.Equal(5, todos.Length);

    foreach (var todo in todos)
    {
        Assert.NotNull(todo.QuerySelector(".todo-title"));
        Assert.NotNull(todo.QuerySelector("button[hx-delete]"));
    }
});
```

## Test Project Setup Tips

- Expose your Program class so WebApplicationFactory can find it:

```csharp
// At the end of Program.cs in your web app
public partial class Program { }
```

- Keep your test files in a separate test project (e.g., MyApp.Tests) and exclude any Tests/** from your web app csproj:

```xml
<ItemGroup>
  <Compile Remove="Tests\**\*.cs" />
  <None Include="Tests\**\*.cs" />
  <!-- exclude any demo-only seeders not present in your model -->
  <!-- <Compile Remove="Data\Seeders\SomeDemoSeeder.cs" /> -->
  <!-- <None Include="Data\Seeders\SomeDemoSeeder.cs" /> -->
  
</ItemGroup>
```

## Best Practices

1. **Use Test Fixtures** - Reuse `HtmxTestFixture<TProgram>` across tests with `IClassFixture<T>`
2. **Test HTMX Attributes** - Verify your partials have correct hx-get, hx-post, hx-target, etc.
3. **Assert Partial Views** - Use `AssertPartialViewAsync()` to ensure HTMX endpoints don't return full pages
4. **Chain Assertions** - Leverage the fluent API for readable, maintainable tests
5. **Use CSS Selectors** - Query HTML with specific, stable selectors

## Philosophy

Swap.Testing is built with minimal external dependencies, focusing on:

- **Quality over quantity** - Every feature is well-crafted
- **Developer experience** - Fluent, intuitive API that reads like documentation
- **HTMX-first** - Designed specifically for testing hypermedia-driven applications
- **Zero magic** - Clear, explicit behavior

## License

MIT

## Contributing

Contributions welcome! Please ensure tests pass and maintain the coding style.

# Razor Pages Support

Swap.Htmx provides first-class support for ASP.NET Core Razor Pages. You can use the same fluent API available in Controllers directly within your `PageModel`.

## Setup

Ensure you have registered the services in `Program.cs`:

```csharp
builder.Services.AddRazorPages();
builder.Services.AddSwapHtmx();
```

## Usage

Import the namespace in your `PageModel`:

```csharp
using Swap.Htmx;
```

Use the `this.SwapResponse()` extension method to start building a response.

### Basic Example

```csharp
public class CounterModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int Count { get; set; }

    public void OnGet()
    {
        // Initial load logic
    }

    public IActionResult OnGetIncrement()
    {
        Count++;
        
        // Return a partial view update
        return this.SwapResponse()
            .WithView("_Counter", this)
            .Build();
    }
}
```

### Coordinated Updates

You can perform multiple updates (OOB swaps), trigger events, and show toasts just like in Controllers.

```csharp
public IActionResult OnPostAddTodo(string title)
{
    var todo = _service.Add(title);
    
    return this.SwapResponse()
        .WithView("_TodoItem", todo) // Main update
        .AlsoUpdate("todo-count", "_TodoCount", _service.Count) // OOB update
        .WithSuccessToast("Todo added!")
        .WithTrigger("todo-added")
        .Build();
}
```

### Event Chains

You can also trigger configured event chains from a PageModel:

```csharp
public IActionResult OnPostComplete(int id)
{
    return this.SwapEvent(TodoEvents.Completed, new { Id = id })
        .Build();
}
```

## Testing

`Swap.Testing` includes helpers specifically for Razor Pages to make testing handlers easy.

```csharp
[Fact]
public async Task Increment_ReturnsUpdatedCounter()
{
    // Arrange
    var client = _fixture.Client;

    // Act
    // Automatically handles ?handler=Increment query string
    var response = await client.HtmxGetPageHandlerAsync("/Counter", "Increment", new { count = 5 });

    // Assert
    await response
        .AssertSuccess()
        .AssertContainsAsync("Count: 6");
}
```

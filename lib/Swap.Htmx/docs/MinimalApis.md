# Minimal APIs Support

Swap provides first-class support for ASP.NET Core Minimal APIs. You can return HTMX responses, render Razor views, and trigger client-side events directly from your route handlers without using Controllers.

## Setup

Since Swap relies on the Razor View Engine to render HTML, you still need to register the necessary services in your `Program.cs`.

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register Razor View Engine and Swap services
builder.Services.AddControllersWithViews();
builder.Services.AddSwapHtmx();

var app = builder.Build();

// ... middleware setup ...
```

## Basic Usage

Use the `SwapResults` static class to create responses. This is the Minimal API equivalent of the `this.SwapResponse()` extension method used in Controllers.

### Returning a View

```csharp
app.MapGet("/hello", () => 
{
    return SwapResults.Response()
        .WithView("Hello", new { Name = "World" });
});
```

### Returning a Partial View

```csharp
app.MapGet("/todo/{id}", (int id, ITodoService service) => 
{
    var todo = service.Get(id);
    return SwapResults.Response()
        .WithView("_TodoItem", todo);
});
```

## Out-of-Band Swaps

You can perform Out-of-Band (OOB) swaps to update multiple parts of the page in a single response.

```csharp
app.MapPost("/todo", (TodoItem item, ITodoService service) => 
{
    service.Add(item);
    var count = service.Count();

    return SwapResults.Response()
        .WithView("_TodoItem", item) // Render the new item
        .AlsoUpdate("todo-count", "_TodoCount", count); // Update the counter elsewhere
});
```

## Client-Side Events (Triggers)

Trigger client-side events using `WithTrigger`.

```csharp
app.MapDelete("/todo/{id}", (int id, ITodoService service) => 
{
    service.Delete(id);

    return SwapResults.Response()
        .WithTrigger("todoDeleted", new { id })
        .WithSuccessToast("Todo item deleted");
});
```

## Triggering Distributed Handlers

You can trigger events that are handled by `ISwapEventHandler<T>` implementations.

```csharp
app.MapPost("/complete/{id}", (int id) => 
{
    // Trigger an event for distributed handlers
    return SwapResults.Event(new TaskCompletedEvent { Id = id });
});
```

## Form Validation

For validation, you can use `SwapValidationErrors` manually or integrate with a validation library.

```csharp
app.MapPost("/contact", (ContactForm form) => 
{
    var errors = new SwapValidationErrors();
    
    if (string.IsNullOrEmpty(form.Email))
    {
        errors.Add("Email", "Email is required");
    }

    if (!errors.IsValid)
    {
        return SwapResults.Response()
            .WithView("ContactForm", form)
            .WithTrigger("validationFailed");
    }

    // Process form...
    return SwapResults.Response().WithSuccessToast("Message sent!");
});
```

# Swap.Htmx.Generators

Roslyn Source Generators for `Swap.Htmx`.

## Overview
This project provides build-time code generation to enhance the developer experience when working with `Swap.Htmx` events.

## Features

### Event Source Generation
Automatically generates strongly-typed `EventKey` hierarchies from string constants.

**Input:**
```csharp
[SwapEventSource]
public partial class AppEvents
{
    public const string UserCreated = "user.created";
    public const string OrderShipped = "order.shipped";
}
```

**Generated Output:**
```csharp
public partial class AppEvents
{
    public static partial class User
    {
        public static readonly EventKey Created = new EventKey("user.created");
    }
    public static partial class Order
    {
        public static readonly EventKey Shipped = new EventKey("order.shipped");
    }
}
```

## Usage
1. Add a reference to `Swap.Htmx.Generators` in your project (usually handled via `Directory.Build.props` or the main `Swap.Htmx` package).
2. Mark a class with `[SwapEventSource]`.
3. Define your events as `public const string`.
4. Build the project to generate the types.

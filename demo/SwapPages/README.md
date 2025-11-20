# SwapPages Demo

This project demonstrates how to use **Swap.Htmx** with ASP.NET Core Razor Pages.

## Features

- **Razor Pages Integration**: Uses `this.SwapResponse()` extension method within `PageModel`.
- **Partial Rendering**: Updates specific parts of the page using Razor partials.
- **HTMX Integration**: Demonstrates `hx-get` targeting a specific page handler.
- **Fluent API**: Shows the fluent builder pattern for constructing responses.

## Getting Started

1.  Navigate to the `src` directory:
    ```bash
    cd src
    ```
2.  Run the application:
    ```bash
    dotnet run
    ```
3.  Open your browser to `http://localhost:5000` (or the port shown in the terminal).
4.  Interact with the counter to see partial page updates.

## Code Structure

- **Pages/Index.cshtml.cs**: The PageModel demonstrating the `SwapResponse()` usage.
- **Pages/Shared/_Counter.cshtml**: The partial view used for the counter component.

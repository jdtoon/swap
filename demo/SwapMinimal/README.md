# SwapMinimal Demo

This project demonstrates how to use **Swap.Htmx** with ASP.NET Core Minimal APIs.

## Features

- **Minimal API Integration**: Uses `SwapResults` to return fluent responses directly from endpoints.
- **Razor Views**: Renders Razor partial views from Minimal API endpoints.
- **HTMX Integration**: Demonstrates basic HTMX interactions (hx-get, hx-target, hx-swap).
- **Toasts**: Shows how to trigger client-side toasts from the server.

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
4.  Click the button to trigger an HTMX request and see the partial update and toast notification.

## Code Structure

- **Program.cs**: Configures services and defines the Minimal API endpoints.
- **Views/Shared/_Message.cshtml**: The Razor partial view rendered by the endpoint.

# SwapStateDemo

A simple demonstration of the `<swap-hidden>` tag helper for preserving state in HTMX partial updates.

## What This Demo Shows

The `<swap-hidden>` tag helper is a **convenience wrapper** for `<input type="hidden">` that provides:

- **Auto-formatting** for dates (`yyyy-MM-dd`), booleans (`true`/`false`), and collections (comma-separated)
- **Null handling** (renders empty string)

### The Pattern

```html
<div id="my-container">
    <!-- Hidden fields preserve state -->
    <swap-hidden name="search" value="@Model.Search" />
    <swap-hidden name="page" value="@Model.Page" />
    
    <!-- Controls include siblings via hx-include -->
    <button hx-get="/search" 
            hx-include="closest div"
            hx-target="#my-container">
        Search
    </button>
</div>
```

This is equivalent to:

```html
<div id="my-container">
    <input type="hidden" name="search" value="@Model.Search" />
    <input type="hidden" name="page" value="@Model.Page" />
    
    <button hx-get="/search" 
            hx-include="closest div"
            hx-target="#my-container">
        Search
    </button>
</div>
```

## Running

```bash
dotnet run
```

Then open http://localhost:5002

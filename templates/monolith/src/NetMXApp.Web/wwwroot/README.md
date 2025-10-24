# NetMX Frontend Asset Management

This directory contains all client-side assets for the NetMX application. All frontend libraries are managed via **LibMan** (Library Manager), ensuring consistent, version-controlled, and easily updatable dependencies.

## Philosophy

NetMX follows a **zero unnecessary dependencies** approach:
- **HTMX 2.0.4**: For server-driven interactivity without complex JavaScript
- **Bulma 1.0.4**: CSS-only framework with zero JavaScript dependencies
- **No jQuery, no Bootstrap JS, no heavy frameworks**

## Current Libraries

### HTMX 2.0.4
- **Location**: `wwwroot/lib/htmx/dist/htmx.min.js`
- **Purpose**: Enables AJAX, CSS Transitions, WebSockets, and Server-Sent Events directly in HTML
- **Documentation**: https://htmx.org/

**Why HTMX?**
- Server-rendered UI with rich interactivity
- Minimal JavaScript required
- Reduces frontend complexity
- Perfect for enterprise applications

### Bulma 1.0.4
- **Location**: `wwwroot/lib/bulma/css/bulma.min.css`
- **Purpose**: Modern, responsive CSS framework
- **Documentation**: https://bulma.io/

**Why Bulma?**
- Pure CSS (no JavaScript)
- Clean, semantic class names
- Flexbox-based grid system
- Highly customizable
- Lightweight (~200KB unminified)

## Managing Libraries

All libraries are defined in `libman.json` at the project root.

### Viewing Current Libraries
```bash
# From the NetMXApp.Web directory
dotnet tool install -g Microsoft.Web.LibraryManager.Cli
libman list
```

### Adding a New Library
```bash
# Example: Adding a new library
libman install <library-name>@<version> --provider cdnjs --destination wwwroot/lib/<library-name>/
```

### Updating Libraries
```bash
# Update all libraries
libman restore

# Update a specific library - edit version in libman.json, then:
libman restore
```

### Removing Libraries
```bash
libman uninstall <library-name>
```

## Project Structure

```
wwwroot/
├── lib/                    # LibMan-managed libraries
│   ├── htmx/
│   │   └── dist/
│   │       └── htmx.min.js
│   └── bulma/
│       └── css/
│           └── bulma.min.css
├── css/                    # Custom application styles (future)
├── js/                     # Custom JavaScript (minimal, as needed)
├── images/                 # Application images
└── README.md              # This file
```

## Best Practices

### 1. **Use HTMX for Interactivity**
```html
<!-- Load content without page refresh -->
<button hx-get="/api/data" hx-target="#result">Load Data</button>
<div id="result"></div>
```

### 2. **Use Bulma for Styling**
```html
<!-- Clean, semantic classes -->
<button class="button is-primary">Primary Button</button>
<div class="notification is-success">Success message</div>
```

### 3. **Minimize Custom JavaScript**
- Prefer HTMX attributes over custom JS
- Use vanilla JavaScript when needed
- Avoid heavy frameworks unless absolutely necessary

### 4. **Keep Libraries Updated**
- Regularly check for updates
- Test thoroughly after updates
- Document any breaking changes

## Future Enhancements

As NetMX Pro develops, we will build:
- **Custom Component Library**: Reusable, HTMX-powered components
- **Theme System**: Customizable Bulma themes
- **Advanced Interactions**: WebSocket support, SSE for real-time features

## Version History

| Date | HTMX Version | Bulma Version | Notes |
|------|--------------|---------------|-------|
| 2025-10-16 | 2.0.4 | 1.0.4 | Initial setup |

## References

- [LibMan Documentation](https://learn.microsoft.com/en-us/aspnet/core/client-side/libman/)
- [HTMX Documentation](https://htmx.org/docs/)
- [Bulma Documentation](https://bulma.io/documentation/)
- [NetMX Framework Documentation](../../README.md)

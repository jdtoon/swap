# NetMX Theming Strategy

**Status**: Planned for Phase 3  
**Priority**: Medium (after core infrastructure)  
**Timeline**: Week 4-5 of Phase 3

---

## Overview

NetMX will provide a flexible theming system that allows:
1. **Default theme** out-of-the-box (clean, professional)
2. **Easy customization** via CSS variables
3. **Theme switching** (light/dark/custom)
4. **Module-level themes** (each module can have its own styles)
5. **Override-friendly** (developers can replace anything)

---

## Architecture

### 1. Theme Structure

```
wwwroot/
├── lib/
│   └── netmx/
│       ├── css/
│       │   ├── netmx-core.css        # Core framework styles
│       │   ├── netmx-themes.css      # Theme variables
│       │   ├── themes/
│       │   │   ├── light.css         # Light theme
│       │   │   ├── dark.css          # Dark theme
│       │   │   └── custom.css        # User custom theme
│       │   └── modules/
│       │       ├── identity.css      # Identity module styles
│       │       ├── audit.css         # Audit module styles
│       │       └── ...
│       └── js/
│           └── theme-switcher.js     # Client-side theme switching
```

### 2. CSS Variables (Design Tokens)

```css
/* netmx-themes.css */
:root {
  /* Brand Colors */
  --netmx-primary: #3b82f6;
  --netmx-primary-hover: #2563eb;
  --netmx-secondary: #6b7280;
  --netmx-accent: #10b981;
  
  /* Semantic Colors */
  --netmx-success: #10b981;
  --netmx-warning: #f59e0b;
  --netmx-error: #ef4444;
  --netmx-info: #3b82f6;
  
  /* Backgrounds */
  --netmx-bg-primary: #ffffff;
  --netmx-bg-secondary: #f9fafb;
  --netmx-bg-tertiary: #f3f4f6;
  
  /* Text */
  --netmx-text-primary: #111827;
  --netmx-text-secondary: #6b7280;
  --netmx-text-tertiary: #9ca3af;
  
  /* Borders */
  --netmx-border-color: #e5e7eb;
  --netmx-border-radius: 0.375rem;
  
  /* Shadows */
  --netmx-shadow-sm: 0 1px 2px 0 rgb(0 0 0 / 0.05);
  --netmx-shadow-md: 0 4px 6px -1px rgb(0 0 0 / 0.1);
  --netmx-shadow-lg: 0 10px 15px -3px rgb(0 0 0 / 0.1);
  
  /* Spacing */
  --netmx-spacing-xs: 0.25rem;
  --netmx-spacing-sm: 0.5rem;
  --netmx-spacing-md: 1rem;
  --netmx-spacing-lg: 1.5rem;
  --netmx-spacing-xl: 2rem;
  
  /* Typography */
  --netmx-font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
  --netmx-font-size-xs: 0.75rem;
  --netmx-font-size-sm: 0.875rem;
  --netmx-font-size-base: 1rem;
  --netmx-font-size-lg: 1.125rem;
  --netmx-font-size-xl: 1.25rem;
}

/* Dark Theme */
[data-theme="dark"] {
  --netmx-bg-primary: #111827;
  --netmx-bg-secondary: #1f2937;
  --netmx-bg-tertiary: #374151;
  
  --netmx-text-primary: #f9fafb;
  --netmx-text-secondary: #d1d5db;
  --netmx-text-tertiary: #9ca3af;
  
  --netmx-border-color: #374151;
}
```

### 3. Theme Switching (Client-Side)

```javascript
// theme-switcher.js
class NetMXThemeSwitcher {
    constructor() {
        this.currentTheme = localStorage.getItem('netmx-theme') || 'light';
        this.applyTheme(this.currentTheme);
    }
    
    applyTheme(theme) {
        document.documentElement.setAttribute('data-theme', theme);
        localStorage.setItem('netmx-theme', theme);
        this.currentTheme = theme;
        
        // Emit event for components to react
        window.dispatchEvent(new CustomEvent('netmx:theme-changed', { 
            detail: { theme } 
        }));
    }
    
    toggle() {
        const newTheme = this.currentTheme === 'light' ? 'dark' : 'light';
        this.applyTheme(newTheme);
    }
}

// Auto-initialize
window.netmxTheme = new NetMXThemeSwitcher();
```

### 4. Theme Switcher UI Component

```html
<!-- _ThemeSwitcher.cshtml (Razor partial) -->
<div class="netmx-theme-switcher">
    <button onclick="window.netmxTheme.toggle()" 
            class="btn btn-icon" 
            title="Toggle theme"
            hx-swap="none">
        <span class="theme-icon-light">🌞</span>
        <span class="theme-icon-dark">🌙</span>
    </button>
</div>

<style>
.netmx-theme-switcher {
    position: fixed;
    bottom: 1rem;
    right: 1rem;
    z-index: 1000;
}

[data-theme="light"] .theme-icon-dark,
[data-theme="dark"] .theme-icon-light {
    display: none;
}
</style>
```

---

## Implementation Plan

### Phase 1: Foundation (Week 1)
1. Create `netmx-core.css` with base styles (typography, buttons, forms, tables)
2. Create `netmx-themes.css` with CSS variables
3. Create light/dark themes
4. Add theme switcher JavaScript
5. Test in sample app

### Phase 2: Module Integration (Week 2)
1. Create module-specific stylesheets (identity.css, audit.css, etc.)
2. Ensure modules inherit theme variables
3. Test dark mode compatibility across all modules
4. Update CLI to copy theme files to wwwroot

### Phase 3: Customization (Week 3)
1. Document theme customization guide
2. Create theme generator tool (CLI command: `netmx theme create MyTheme`)
3. Add pre-built themes (professional, minimal, vibrant, etc.)
4. Add theme preview in Studio (future)

---

## CLI Integration

### When Adding Module
```bash
netmx add module Identity

# CLI should:
# 1. Copy module CSS to wwwroot/lib/netmx/css/modules/identity.css
# 2. Reference in _Layout.cshtml: <link href="~/lib/netmx/css/modules/identity.css" />
# 3. Copy theme CSS if not exists: netmx-core.css, netmx-themes.css
# 4. Copy theme-switcher.js
```

### Custom Theme Creation
```bash
netmx theme create MyCompanyTheme --based-on light

# Generates: wwwroot/css/themes/mycompanytheme.css
# Provides template with all CSS variables to customize
```

---

## Developer Experience

### Using Themes in Views

```html
<!-- Use theme variables in custom styles -->
<div style="background-color: var(--netmx-bg-secondary); 
            color: var(--netmx-text-primary);
            padding: var(--netmx-spacing-md);
            border-radius: var(--netmx-border-radius);">
    Themed content
</div>

<!-- Or use theme classes -->
<div class="netmx-card">
    <h3 class="netmx-heading">Card Title</h3>
    <p class="netmx-text-secondary">Description text</p>
    <button class="netmx-btn netmx-btn-primary">Action</button>
</div>
```

### Overriding Themes

```css
/* In your app's site.css */
:root {
  /* Override primary color */
  --netmx-primary: #8b5cf6;  /* Purple instead of blue */
  --netmx-primary-hover: #7c3aed;
}

/* Or create complete custom theme */
[data-theme="mycompany"] {
  --netmx-primary: #ff6b6b;
  --netmx-bg-primary: #f8f9fa;
  /* ... all other variables */
}
```

---

## Benefits

1. **Consistency**: All modules use same design tokens
2. **Flexibility**: Easy to customize without touching framework CSS
3. **Accessibility**: Dark mode built-in (reduces eye strain)
4. **Performance**: CSS variables are native (no build step needed)
5. **Developer-friendly**: Simple class names, clear documentation
6. **Framework-agnostic**: Works with HTMX, Blazor, React, etc.

---

## Compatibility with Bulma

**Current**: NetMX uses Bulma CSS framework  
**Strategy**: 
1. Phase 3A: Create NetMX theme that styles Bulma components
2. Phase 3B: Gradually replace Bulma with NetMX-native components
3. Phase 3C: Make Bulma optional (for backward compatibility)

**Migration Path**:
```html
<!-- Current (Bulma) -->
<button class="button is-primary">Click</button>

<!-- Future (NetMX) -->
<button class="netmx-btn netmx-btn-primary">Click</button>

<!-- During transition (both work) -->
<button class="button is-primary netmx-btn netmx-btn-primary">Click</button>
```

---

## Related Documents

- [HTMX-PATTERNS.md](HTMX-PATTERNS.md) - HTMX integration
- [CLI-IMPROVEMENTS.md](CLI-IMPROVEMENTS.md) - CLI theme commands
- [STUDIO-SUITE-VISION.md](STUDIO-SUITE-VISION.md) - Theme marketplace in Studio

---

## Success Metrics

- **Developer Time**: 0 minutes to get good-looking UI (default theme)
- **Customization Time**: 5-10 minutes to apply brand colors
- **Accessibility**: WCAG AA compliant, dark mode support
- **Performance**: <5KB additional CSS (compressed)

---

**Conclusion**: Theming makes NetMX apps look professional out-of-the-box while allowing complete customization. This is a key differentiator vs. raw ASP.NET Core projects.

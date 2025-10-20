# HTMX Patterns & Best Practices

**NetMX HTMX Guide** - Learn how to build interactive web applications with HTMX and .NET

> 📍 **Live Demo**: See all these patterns in action at `/Demo` in your NetMX template

---

## Table of Contents

1. [Introduction](#introduction)
2. [Core Concepts](#core-concepts)
3. [Common Patterns](#common-patterns)
   - [Click to Edit](#pattern-1-click-to-edit)
   - [Delete with Confirmation](#pattern-2-delete-with-confirmation)
   - [Infinite Scroll](#pattern-3-infinite-scroll)
   - [Search with Debounce](#pattern-4-search-with-debounce)
   - [Tab Switching](#pattern-5-tab-switching)
   - [Form Validation](#pattern-6-form-validation)
   - [Out-of-Band Updates](#pattern-7-out-of-band-updates)
   - [Lazy Loading](#pattern-8-lazy-loading)
4. [HTMX Helpers Reference](#htmx-helpers-reference)
5. [Best Practices](#best-practices)
6. [Troubleshooting](#troubleshooting)

---

## Introduction

HTMX allows you to build modern, interactive web applications using **server-rendered HTML** instead of complex JavaScript frameworks. With HTMX, you can:

- ✅ **Update parts of the page** without full page refreshes
- ✅ **Handle user interactions** declaratively with HTML attributes
- ✅ **Keep logic on the server** where it belongs
- ✅ **Reduce complexity** - no build tools, no heavy JS frameworks

NetMX provides **first-class HTMX support** with strongly-typed helpers, extension methods, and best practices built-in.

---

## Core Concepts

### HTMX Attributes

HTMX uses HTML attributes to add behavior:

| Attribute | Purpose | Example |
|-----------|---------|---------|
| `hx-get` | Issue GET request | `hx-get="/api/users"` |
| `hx-post` | Issue POST request | `hx-post="/api/users"` |
| `hx-delete` | Issue DELETE request | `hx-delete="/api/users/1"` |
| `hx-target` | Where to put response | `hx-target="#results"` |
| `hx-swap` | How to swap content | `hx-swap="outerHTML"` |
| `hx-trigger` | When to fire request | `hx-trigger="click"` |

### NetMX HTMX Helpers

NetMX provides extension methods for controllers:

```csharp
using NetMX.AspNetCore.Mvc.Htmx;

// Request helpers
if (Request.IsHtmx()) { /* ... */ }
var targetId = Request.GetTarget();

// Response helpers
this.HxTrigger("my-event");
this.HxReswap(HtmxSwap.OuterHTML);
this.HxRetarget("#new-target");
this.HxRedirect("/another-page");
```

---

## Common Patterns

## Pattern 1: Click to Edit

**Use Case**: Allow users to edit content inline without navigating to a separate page.

### View Code

```html
<!-- Display mode -->
<div id="contact-display" class="box">
    <p class="title">@Model.Name</p>
    <p class="subtitle">@Model.Email</p>
    
    <button class="button is-info" 
            hx-get="/Demo/EditContact" 
            hx-target="#contact-container" 
            hx-swap="innerHTML">
        Edit
    </button>
</div>
```

```html
<!-- Edit mode (partial view) -->
<form hx-post="/Demo/SaveContact" 
      hx-target="#contact-container" 
      hx-swap="innerHTML">
    
    <input type="text" name="Name" value="@Model.Name">
    <input type="email" name="Email" value="@Model.Email">
    
    <button type="submit">Save</button>
    <button hx-delete="/Demo/CancelEdit" 
            hx-target="#contact-container">
        Cancel
    </button>
</form>
```

### Controller Code

```csharp
[HttpGet]
public IActionResult EditContact()
{
    var contact = _service.GetContact();
    return PartialView("_ContactEdit", contact);
}

[HttpPost]
public IActionResult SaveContact(ContactViewModel model)
{
    if (!ModelState.IsValid)
    {
        return PartialView("_ContactEdit", model);
    }
    
    _service.UpdateContact(model);
    return PartialView("_ContactDisplay", model);
}
```

**Key Points:**
- Use `hx-swap="innerHTML"` to replace content inside container
- Return different partials for display vs. edit modes
- Validation errors keep the form open automatically

---

## Pattern 2: Delete with Confirmation

**Use Case**: Delete items with user confirmation and smooth UI updates.

### View Code

```html
<tr id="item-@item.Id">
    <td>@item.Name</td>
    <td>$@item.Price</td>
    <td>
        <button class="button is-danger" 
                hx-delete="/Demo/DeleteItem?id=@item.Id" 
                hx-target="#item-@item.Id" 
                hx-confirm="Are you sure you want to delete @item.Name?">
            Delete
        </button>
    </td>
</tr>
```

### Controller Code

```csharp
[HttpDelete]
public IActionResult DeleteItem(int id)
{
    _service.DeleteItem(id);
    
    // Tell HTMX to delete the target element
    this.HxReswap(HtmxSwap.Delete);
    
    return Ok();
}
```

**Key Points:**
- `hx-confirm` provides built-in confirmation dialog
- `HtmxSwap.Delete` removes the element from DOM
- Target the specific row/item for surgical updates

---

## Pattern 3: Infinite Scroll

**Use Case**: Load more content automatically as user scrolls.

### View Code

```html
<div class="activity-feed" style="overflow-y: auto;">
    <div id="activity-list">
        <!-- Initial items loaded here -->
        
        <!-- Trigger for next page -->
        <div hx-get="/Demo/LoadMoreActivities?page=2" 
             hx-trigger="revealed" 
             hx-swap="outerHTML">
            <span>Loading...</span>
        </div>
    </div>
</div>
```

### Controller Code

```csharp
[HttpGet]
public IActionResult LoadMoreActivities(int page = 1)
{
    var items = _service.GetActivities(page, pageSize: 10);
    var hasMore = _service.HasMoreActivities(page);
    
    ViewBag.Page = page;
    ViewBag.HasMore = hasMore;
    
    return PartialView("_ActivityItems", items);
}
```

### Partial View

```html
@foreach (var item in Model)
{
    <div class="item">@item.Message</div>
}

@if (ViewBag.HasMore)
{
    <div hx-get="/Demo/LoadMoreActivities?page=@(ViewBag.Page + 1)" 
         hx-trigger="revealed" 
         hx-swap="outerHTML">
        Loading more...
    </div>
}
```

**Key Points:**
- `hx-trigger="revealed"` fires when element scrolls into view
- `hx-swap="outerHTML"` replaces loader with content + new loader
- Each response includes the next page trigger

---

## Pattern 4: Search with Debounce

**Use Case**: Live search that waits for user to stop typing.

### View Code

```html
<input type="text" 
       name="q"
       placeholder="Search products..."
       hx-get="/Demo/SearchProducts" 
       hx-trigger="keyup changed delay:500ms" 
       hx-target="#search-results"
       hx-indicator="#search-spinner">

<div id="search-spinner" class="htmx-indicator">
    Searching...
</div>

<div id="search-results"></div>
```

### Controller Code

```csharp
[HttpGet]
public IActionResult SearchProducts(string q = "")
{
    if (string.IsNullOrWhiteSpace(q))
    {
        return PartialView("_SearchResults", new List<string>());
    }
    
    var results = _service.SearchProducts(q);
    return PartialView("_SearchResults", results);
}
```

### CSS for Spinner

```css
.htmx-indicator {
    display: none;
}

.htmx-request .htmx-indicator,
.htmx-request.htmx-indicator {
    display: inline-block;
}
```

**Key Points:**
- `delay:500ms` waits 500ms after last keystroke
- `changed` only triggers if value actually changed
- `hx-indicator` shows/hides spinner automatically

---

## Pattern 5: Tab Switching

**Use Case**: Switch between content tabs without page reload.

### View Code

```html
<div class="tabs">
    <ul>
        <li class="is-active">
            <a hx-get="/Demo/TabContent?tab=overview" 
               hx-target="#tab-content">
                Overview
            </a>
        </li>
        <li>
            <a hx-get="/Demo/TabContent?tab=features" 
               hx-target="#tab-content">
                Features
            </a>
        </li>
    </ul>
</div>

<div id="tab-content" hx-get="/Demo/TabContent?tab=overview" hx-trigger="load">
    Loading...
</div>
```

### Controller Code

```csharp
[HttpGet]
public IActionResult TabContent(string tab)
{
    return tab switch
    {
        "overview" => PartialView("_TabOverview"),
        "features" => PartialView("_TabFeatures"),
        _ => Content("Unknown tab")
    };
}
```

**Key Points:**
- `hx-trigger="load"` loads default tab on page load
- Each tab link targets the same content area
- Use JavaScript or Alpine.js for active tab styling

---

## Pattern 6: Form Validation

**Use Case**: Server-side form validation with inline error display.

### View Code

```html
<form hx-post="/Demo/ValidateForm" 
      hx-target="#signup-container" 
      hx-swap="innerHTML">
    
    <div class="field">
        <input type="text" name="Username" value="@Model.Username">
        @if (!string.IsNullOrEmpty(Html.ViewData.ModelState["Username"]?.Errors.FirstOrDefault()?.ErrorMessage))
        {
            <p class="help is-danger">
                @Html.ViewData.ModelState["Username"]!.Errors.First().ErrorMessage
            </p>
        }
    </div>
    
    <button type="submit">Sign Up</button>
</form>
```

### Controller Code

```csharp
[HttpPost]
public IActionResult ValidateForm(SignupViewModel model)
{
    if (!ModelState.IsValid)
    {
        // Return form with validation errors
        return PartialView("_SignupForm", model);
    }
    
    // Success! Trigger an event
    this.HxTrigger("signup-success", new { email = model.Email });
    
    return PartialView("_SignupSuccess", model);
}
```

### Listen for Events (JavaScript)

```javascript
document.body.addEventListener('signup-success', function(evt) {
    console.log('User signed up:', evt.detail.email);
    // Show success notification
});
```

**Key Points:**
- Invalid forms return the form partial with errors
- Valid forms trigger events and return success view
- Use `HxTrigger()` to communicate with client-side JavaScript

---

## Pattern 7: Out-of-Band Updates

**Use Case**: Update multiple parts of the page with one request.

### View Code

```html
<div id="header">Not updated</div>
<div id="content">Main content</div>
<div id="footer">Not updated</div>

<button hx-post="/Demo/UpdateMultiple" hx-target="#content">
    Update All
</button>
```

### Controller Code

```csharp
[HttpPost]
public IActionResult UpdateMultiple()
{
    var timestamp = DateTime.Now.ToString("HH:mm:ss");
    
    // Update multiple elements using OOB swaps
    return this.HxOutOfBandSwaps(
        ("header", $"<span>Header: {timestamp}</span>", HtmxSwap.InnerHTML),
        ("content", $"<p>Content: {timestamp}</p>", HtmxSwap.InnerHTML),
        ("footer", $"<small>Footer: {timestamp}</small>", HtmxSwap.InnerHTML)
    );
}
```

**Key Points:**
- `HxOutOfBandSwaps()` updates multiple elements in one response
- Target element receives normal swap
- Other elements updated via `hx-swap-oob` attribute
- Powerful for dashboard-style UIs

---

## Pattern 8: Lazy Loading

**Use Case**: Load heavy content only when visible.

### View Code

```html
<div hx-get="/Demo/LazyImage" 
     hx-trigger="revealed" 
     hx-swap="outerHTML"
     style="min-height: 200px;">
    <span>Loading image...</span>
</div>
```

### Controller Code

```csharp
[HttpGet]
public IActionResult LazyImage()
{
    // Simulate slow loading
    Thread.Sleep(1000);
    
    return PartialView("_LazyImage");
}
```

### Partial View

```html
<img src="https://example.com/large-image.jpg" alt="Lazy loaded">
```

**Key Points:**
- `hx-trigger="revealed"` waits until element is scrolled into view
- Great for images, charts, expensive content
- Reduces initial page load time dramatically

---

## HTMX Helpers Reference

### Request Detection

```csharp
// Check if request is from HTMX
if (Request.IsHtmx())
{
    return PartialView("_Content");
}
return View();

// Get HTMX request headers
var target = Request.GetTarget();           // hx-target element ID
var trigger = Request.GetTriggerId();       // Element that triggered request
var currentUrl = Request.GetCurrentUrl();   // Current browser URL
```

### Response Headers

```csharp
// Trigger client-side events
this.HxTrigger("my-event");
this.HxTrigger("user-updated", new { userId = 123 });
this.HxTriggerAfterSettle("settled-event");
this.HxTriggerAfterSwap("swapped-event");

// Change swap behavior
this.HxReswap(HtmxSwap.OuterHTML);
this.HxReswap(HtmxSwap.Delete);

// Retarget response
this.HxRetarget("#different-element");

// Client-side redirects
this.HxRedirect("/new-page");
this.HxPushUrl("/update-url");
this.HxReplaceUrl("/replace-url");

// Refresh the page
this.HxRefresh();
```

### Swap Modes

```csharp
HtmxSwap.InnerHTML     // Default - replace inner content
HtmxSwap.OuterHTML     // Replace entire element
HtmxSwap.BeforeBegin   // Insert before element
HtmxSwap.AfterBegin    // Insert as first child
HtmxSwap.BeforeEnd     // Insert as last child
HtmxSwap.AfterEnd      // Insert after element
HtmxSwap.Delete        // Delete the element
HtmxSwap.None          // Don't swap (for OOB only)
```

### Advanced Features

```csharp
// Out-of-band swaps
return this.HxOutOfBandSwaps(
    ("element-id-1", "<div>Content 1</div>", HtmxSwap.InnerHTML),
    ("element-id-2", "<div>Content 2</div>", HtmxSwap.OuterHTML)
);

// Scroll control
this.HxScrollTop();
this.HxScrollBottom();
this.HxScrollTo("#target-element");

// Polling
this.HxPoll(5000);  // Poll every 5 seconds
this.HxStopPoll();  // Stop polling
return this.HxStopPollingResponse();  // HTTP 286
```

---

## Best Practices

### 1. **Use Partial Views for HTMX Responses**

```csharp
// ❌ Don't return full views
public IActionResult GetUsers()
{
    return View(users);  // Sends layout, navbar, etc.
}

// ✅ Return partials for HTMX
public IActionResult GetUsers()
{
    if (Request.IsHtmx())
    {
        return PartialView("_UserList", users);
    }
    return View(users);  // Full page for direct navigation
}
```

### 2. **Target Specific Elements**

```html
<!-- ❌ Don't target large containers -->
<button hx-get="/api/users" hx-target="body">Load</button>

<!-- ✅ Target specific elements -->
<button hx-get="/api/users" hx-target="#user-list">Load</button>
```

### 3. **Use Appropriate Swap Modes**

```html
<!-- Replace inner content of container -->
<div hx-get="/api/item" hx-swap="innerHTML">...</div>

<!-- Replace entire element (row in table) -->
<tr hx-get="/api/item" hx-swap="outerHTML">...</tr>

<!-- Append to list (infinite scroll) -->
<div hx-get="/api/more" hx-swap="beforeend">...</div>
```

### 4. **Handle Loading States**

```html
<!-- Show spinner during request -->
<button hx-get="/api/slow" 
        hx-indicator="#spinner">
    Load Data
</button>

<div id="spinner" class="htmx-indicator">
    <span>Loading...</span>
</div>
```

### 5. **Validate on Server**

```csharp
// ✅ Always validate server-side
[HttpPost]
public IActionResult CreateUser(UserDto dto)
{
    if (!ModelState.IsValid)
    {
        return PartialView("_CreateUserForm", dto);
    }
    
    // Process valid data
    _service.CreateUser(dto);
    return PartialView("_UserCreated");
}
```

### 6. **Use Events for Loosely Coupled Components**

```csharp
// Controller triggers event
[HttpPost]
public IActionResult CreateOrder(OrderDto dto)
{
    var order = _service.CreateOrder(dto);
    
    // Notify other components
    this.HxTrigger("order-created", new { orderId = order.Id });
    
    return PartialView("_OrderSuccess", order);
}
```

```html
<!-- Other component listens -->
<div hx-get="/api/stats" 
     hx-trigger="order-created from:body">
    <!-- Auto-refreshes when order created -->
</div>
```

### 7. **Progressive Enhancement**

```html
<!-- Works without JavaScript -->
<form action="/users" method="post" 
      hx-post="/users" 
      hx-target="#result">
    <!-- Regular form still works if HTMX fails to load -->
</form>
```

---

## Troubleshooting

### Issue: Response Not Swapping

**Symptoms**: HTMX request succeeds but page doesn't update

**Solutions**:
1. Check `hx-target` selector is valid
2. Verify response contains HTML (not JSON)
3. Check browser console for errors
4. Ensure target element exists in DOM

### Issue: Form Not Submitting

**Symptoms**: Form submission doesn't trigger HTMX request

**Solutions**:
1. Use `hx-post` not `hx-get` for forms
2. Ensure button is `type="submit"`
3. Check form is not nested inside another form
4. Verify HTMX script is loaded

### Issue: Validation Errors Not Showing

**Symptoms**: Invalid forms don't display errors

**Solutions**:
1. Return same partial view with model errors
2. Check `ModelState` is populated
3. Ensure validation helpers are in view
4. Verify `hx-target` includes error container

### Issue: Events Not Triggering

**Symptoms**: `HxTrigger()` doesn't fire client events

**Solutions**:
1. Check event listener syntax: `addEventListener('event-name', ...)`
2. Listen on `document.body` for global events
3. Verify event name matches exactly (case-sensitive)
4. Check browser console for JavaScript errors

### Issue: Infinite Scroll Not Working

**Symptoms**: Next page doesn't load when scrolling

**Solutions**:
1. Ensure container has `overflow-y: auto`
2. Check trigger element is actually scrollable into view
3. Verify `hx-trigger="revealed"` is set
4. Ensure next page URL is correctly generated

---

## Additional Resources

- **HTMX Official Docs**: https://htmx.org/docs/
- **NetMX GitHub**: https://github.com/toonjd/netmx
- **NetMX Examples**: `/Demo` page in your template
- **HTMX Discord**: https://htmx.org/discord

---

## Summary

HTMX + NetMX gives you:

✅ **Server-side rendering** with interactive UX  
✅ **Type-safe helpers** in C#  
✅ **Progressive enhancement** - works without JS  
✅ **Simple mental model** - HTML attributes, not frameworks  
✅ **High productivity** - less code, fewer bugs  

**Next Steps:**
1. Explore the `/Demo` page
2. Try implementing patterns in your app
3. Check `DemoController.cs` for implementation examples
4. Read HTMX docs for advanced features

Happy coding! 🚀

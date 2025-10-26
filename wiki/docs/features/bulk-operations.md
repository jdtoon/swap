# Bulk Operations

**Feature Status**: ✅ Available (Phase 4.4 - October 26, 2025)

Bulk operations allow users to select multiple items in a list and perform actions on them simultaneously, such as deleting multiple records at once.

---

## 📋 Table of Contents

- [Overview](#overview)
- [Features](#features)
- [How It Works](#how-it-works)
- [User Experience](#user-experience)
- [Generated Code](#generated-code)
- [JavaScript Functions](#javascript-functions)
- [Customization](#customization)
- [Best Practices](#best-practices)
- [Technical Details](#technical-details)

---

## Overview

Every controller generated with `swap generate controller` includes full bulk operations support:

- **Selection checkboxes** in each table row
- **Select all/none** functionality in the header
- **Bulk action bar** that appears when items are selected
- **Bulk delete** with confirmation and transaction support
- **Toast notifications** for success/error feedback

**Example:**
```bash
swap g c Product --fields "Name:string Price:decimal InStock:bool"
```

Generates a Product list with:
- ☑️ Checkboxes for each product
- ☑️ "Select all" checkbox in header
- 🗑️ "Delete Selected" button (appears when items checked)
- ✨ Smooth HTMX-powered updates

---

## Features

### 1. Selection Management

**Individual Checkboxes**
- Each row has a checkbox with unique value (entity ID)
- Click to select/deselect individual items
- Visual feedback for selected state

**Select All**
- Header checkbox selects/deselects all items on current page
- Updates bulk action bar count automatically
- Independent per-page (doesn't affect other pages)

### 2. Bulk Action Bar

**Dynamic Visibility**
- Hidden when no items selected
- Appears when ≥1 item selected
- Shows count of selected items
- DaisyUI alert styling (info theme)

**Actions Available**
- **Delete Selected**: Delete all checked items at once
- **Clear Selection**: Deselect all items and hide bar

### 3. Bulk Delete

**Confirmation Dialog**
- Native browser confirm() with item count
- Example: "Are you sure you want to delete 3 product(s)?"
- Cancel does nothing, OK proceeds with deletion

**Transaction Support**
- All deletes wrapped in database transaction
- Atomic operation (all or nothing)
- Automatic rollback on any error
- Returns count of deleted items

**Feedback**
- Success toast: "Successfully deleted N product(s)"
- Error toast: "Failed to delete items" or specific error message
- Automatic list refresh after success
- Selection cleared after operation

---

## How It Works

### User Flow

```
1. User sees list of products
   ↓
2. User clicks checkboxes (or "Select All")
   ↓
3. Bulk action bar appears: "3 item(s) selected"
   ↓
4. User clicks "Delete Selected"
   ↓
5. Confirmation: "Are you sure you want to delete 3 product(s)?"
   ↓ (User clicks OK)
6. JavaScript sends POST to /Product/BulkDelete with [1, 2, 3]
   ↓
7. Controller starts transaction
   ↓
8. Controller finds entities with IDs 1, 2, 3
   ↓
9. Controller deletes all 3 entities
   ↓
10. Transaction commits
    ↓
11. Returns Ok({ deleted: 3 })
    ↓
12. JavaScript shows toast: "Successfully deleted 3 product(s)"
    ↓
13. JavaScript triggers HTMX list refresh
    ↓
14. Table updates with remaining products
    ↓
15. Selection cleared, action bar hidden
```

### Technical Flow

**Client Side:**
1. Checkbox click → `updateBulkActions()` called
2. Counts checked checkboxes
3. Shows/hides bulk action bar
4. Updates selected count display

**Bulk Delete:**
1. User clicks "Delete Selected" → `confirmBulkDelete()` called
2. Gets array of selected IDs → `getSelectedIds()`
3. Shows confirmation with count
4. If confirmed → `bulkDelete(ids)` called
5. Fetch POST to `/BulkDelete` with JSON array
6. On success → triggers list refresh, shows toast
7. On error → shows error toast

**Server Side:**
1. BulkDelete action receives `List<int> ids`
2. Validates IDs array (not null, not empty)
3. Starts database transaction
4. Queries entities: `Where(e => ids.Contains(e.Id))`
5. Removes range: `RemoveRange(entitiesToDelete)`
6. Saves changes
7. Commits transaction
8. Returns `Ok({ deleted: count })`
9. On exception → rollback, return 500 error

---

## User Experience

### Visual States

**No Selection (Initial)**
```
☐ [Select All]  |  Name  |  Price  |  Actions
☐  Widget       |  $9.99 |  [Edit] [Delete]
☐  Gadget       | $14.99 |  [Edit] [Delete]
☐  Doohickey    | $19.99 |  [Edit] [Delete]
```

**Some Items Selected**
```
☐ [Select All]  |  Name  |  Price  |  Actions
☑  Widget       |  $9.99 |  [Edit] [Delete]
☐  Gadget       | $14.99 |  [Edit] [Delete]
☑  Doohickey    | $19.99 |  [Edit] [Delete]

┌─────────────────────────────────────────────┐
│ ℹ️  2 item(s) selected                       │
│     [🗑️ Delete Selected] [Clear Selection]  │
└─────────────────────────────────────────────┘
```

**All Items Selected**
```
☑ [Select All]  |  Name  |  Price  |  Actions
☑  Widget       |  $9.99 |  [Edit] [Delete]
☑  Gadget       | $14.99 |  [Edit] [Delete]
☑  Doohickey    | $19.99 |  [Edit] [Delete]

┌─────────────────────────────────────────────┐
│ ℹ️  3 item(s) selected                       │
│     [🗑️ Delete Selected] [Clear Selection]  │
└─────────────────────────────────────────────┘
```

**After Deletion (Success)**
```
☐ [Select All]  |  Name  |  Price  |  Actions
☐  Gadget       | $14.99 |  [Edit] [Delete]

┌─────────────────────────────────────────────┐
│ ✅ Successfully deleted 2 product(s)         │
└─────────────────────────────────────────────┘
(Toast disappears after 3 seconds)
```

---

## Generated Code

### 1. Table Structure (Views/Product/_ProductList.cshtml)

```html
<!-- Bulk Actions Bar -->
<div id="bulk-actions" class="hidden alert alert-info mb-4">
    <div class="flex justify-between items-center w-full">
        <span><strong id="selected-count">0</strong> item(s) selected</span>
        <div class="flex gap-2">
            <button onclick="confirmBulkDelete()" class="btn btn-sm btn-error">
                <svg><!-- Delete icon --></svg>
                Delete Selected
            </button>
            <button onclick="clearSelection()" class="btn btn-sm btn-ghost">
                Clear Selection
            </button>
        </div>
    </div>
</div>

<table class="table">
    <thead>
        <tr>
            <!-- Select All Checkbox -->
            <th>
                <input type="checkbox" 
                       id="select-all" 
                       class="checkbox checkbox-sm"
                       onclick="toggleSelectAll(this)" />
            </th>
            <th>Name</th>
            <th>Price</th>
            <th>Actions</th>
        </tr>
    </thead>
    <tbody>
        @foreach (var item in Model.Items)
        {
            <tr>
                <!-- Row Checkbox -->
                <td>
                    <input type="checkbox" 
                           class="checkbox checkbox-sm product-checkbox" 
                           value="@item.Id"
                           onclick="updateBulkActions()" />
                </td>
                <td>@item.Name</td>
                <td>@item.Price</td>
                <td><!-- Action buttons --></td>
            </tr>
        }
    </tbody>
</table>
```

### 2. Controller Action (Controllers/ProductController.cs)

```csharp
[HttpPost]
public async Task<IActionResult> BulkDelete([FromBody] List<int> ids)
{
    if (ids == null || !ids.Any())
    {
        return BadRequest("No IDs provided");
    }

    using var transaction = await _context.Database.BeginTransactionAsync();
    
    try
    {
        var entitiesToDelete = await _context.Products
            .Where(e => ids.Contains(e.Id))
            .ToListAsync();

        if (!entitiesToDelete.Any())
        {
            return NotFound();
        }

        _context.Products.RemoveRange(entitiesToDelete);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(new { deleted = entitiesToDelete.Count });
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        return StatusCode(500, new { error = ex.Message });
    }
}
```

### 3. JavaScript Functions (Views/Product/_ProductList.cshtml)

```javascript
// Selection Management
function toggleSelectAll(checkbox) {
    const checkboxes = document.querySelectorAll('.product-checkbox');
    checkboxes.forEach(cb => cb.checked = checkbox.checked);
    updateBulkActions();
}

function updateBulkActions() {
    const selected = document.querySelectorAll('.product-checkbox:checked');
    const bulkActions = document.getElementById('bulk-actions');
    const selectedCount = document.getElementById('selected-count');
    
    if (selected.length > 0) {
        bulkActions.classList.remove('hidden');
        selectedCount.textContent = selected.length;
    } else {
        bulkActions.classList.add('hidden');
        document.getElementById('select-all').checked = false;
    }
}

function getSelectedIds() {
    const checkboxes = document.querySelectorAll('.product-checkbox:checked');
    return Array.from(checkboxes).map(cb => parseInt(cb.value));
}

function clearSelection() {
    const checkboxes = document.querySelectorAll('.product-checkbox');
    checkboxes.forEach(cb => cb.checked = false);
    document.getElementById('select-all').checked = false;
    updateBulkActions();
}

// Bulk Delete
function confirmBulkDelete() {
    const ids = getSelectedIds();
    if (ids.length === 0) return;
    
    if (confirm(`Are you sure you want to delete ${ids.length} product(s)? This action cannot be undone.`)) {
        bulkDelete(ids);
    }
}

function bulkDelete(ids) {
    fetch('@Url.Action("BulkDelete")', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
        },
        body: JSON.stringify(ids)
    })
    .then(response => {
        if (response.ok) {
            // Trigger list refresh
            htmx.trigger('#product-list', 'refreshProductList');
            clearSelection();
            
            // Show success toast
            showToast('success', `Successfully deleted ${ids.length} product(s)`);
        } else {
            showToast('error', 'Failed to delete items');
        }
    })
    .catch(error => {
        console.error('Error:', error);
        showToast('error', 'An error occurred while deleting items');
    });
}

function showToast(type, message) {
    const toast = document.createElement('div');
    toast.className = `alert alert-${type} fixed top-4 right-4 w-auto z-50 shadow-lg`;
    toast.innerHTML = `<span>${message}</span>`;
    document.body.appendChild(toast);
    setTimeout(() => toast.remove(), 3000);
}
```

---

## JavaScript Functions

### Selection Functions

| Function | Purpose | Parameters | Returns |
|----------|---------|------------|---------|
| `toggleSelectAll(checkbox)` | Select/deselect all items | checkbox element | void |
| `updateBulkActions()` | Show/hide bulk action bar | none | void |
| `getSelectedIds()` | Get array of selected IDs | none | `number[]` |
| `clearSelection()` | Deselect all items | none | void |

### Bulk Delete Functions

| Function | Purpose | Parameters | Returns |
|----------|---------|------------|---------|
| `confirmBulkDelete()` | Show confirmation dialog | none | void |
| `bulkDelete(ids)` | Execute bulk delete | `number[]` | `Promise<void>` |
| `showToast(type, message)` | Display toast notification | type, message | void |

---

## Customization

### Change Confirmation Message

```javascript
// Default
confirm(`Are you sure you want to delete ${ids.length} product(s)?`);

// Custom
confirm(`Delete ${ids.length} items? This cannot be undone!`);
```

### Add More Bulk Actions

```html
<!-- Add button to bulk action bar -->
<button onclick="bulkExport()" class="btn btn-sm btn-primary">
    Export Selected
</button>
```

```javascript
function bulkExport() {
    const ids = getSelectedIds();
    if (ids.length === 0) return;
    
    window.location.href = `/Product/Export?ids=${ids.join(',')}`;
}
```

### Change Toast Duration

```javascript
// Default: 3 seconds
setTimeout(() => toast.remove(), 3000);

// Custom: 5 seconds
setTimeout(() => toast.remove(), 5000);
```

### Customize Bulk Action Bar Styling

```html
<!-- Change alert type -->
<div id="bulk-actions" class="hidden alert alert-warning mb-4">

<!-- Add custom classes -->
<div id="bulk-actions" class="hidden alert alert-info mb-4 shadow-xl rounded-lg">
```

### Add Bulk Operations to Existing Controller

If you have an old controller without bulk operations:

1. **Add checkbox column to table header:**
```html
<th>
    <input type="checkbox" id="select-all" class="checkbox checkbox-sm" onclick="toggleSelectAll(this)" />
</th>
```

2. **Add checkbox to each row:**
```html
<td>
    <input type="checkbox" class="checkbox checkbox-sm product-checkbox" 
           value="@item.Id" onclick="updateBulkActions()" />
</td>
```

3. **Add bulk actions bar before table:**
```html
<div id="bulk-actions" class="hidden alert alert-info mb-4">
    <!-- Bulk action content -->
</div>
```

4. **Add JavaScript functions** (copy from generated file)

5. **Add BulkDelete action to controller:**
```csharp
[HttpPost]
public async Task<IActionResult> BulkDelete([FromBody] List<int> ids)
{
    // Implementation
}
```

---

## Best Practices

### 1. Always Use Transactions

✅ **Do:**
```csharp
using var transaction = await _context.Database.BeginTransactionAsync();
try {
    // Delete operations
    await transaction.CommitAsync();
}
catch {
    await transaction.RollbackAsync();
}
```

❌ **Don't:**
```csharp
// No transaction - partial deletes possible
_context.Products.RemoveRange(entities);
await _context.SaveChangesAsync();
```

### 2. Validate Input

✅ **Do:**
```csharp
if (ids == null || !ids.Any())
{
    return BadRequest("No IDs provided");
}
```

❌ **Don't:**
```csharp
// Assumes ids is valid
var entities = await _context.Products
    .Where(e => ids.Contains(e.Id))
    .ToListAsync();
```

### 3. Provide User Feedback

✅ **Do:**
```javascript
showToast('success', `Successfully deleted ${ids.length} product(s)`);
```

❌ **Don't:**
```javascript
// Silent success - user doesn't know what happened
htmx.trigger('#product-list', 'refresh');
```

### 4. Confirm Destructive Actions

✅ **Do:**
```javascript
if (confirm(`Delete ${ids.length} items?`)) {
    bulkDelete(ids);
}
```

❌ **Don't:**
```javascript
// No confirmation - accidental deletions possible
bulkDelete(ids);
```

### 5. Handle Errors Gracefully

✅ **Do:**
```csharp
catch (Exception ex)
{
    await transaction.RollbackAsync();
    return StatusCode(500, new { error = ex.Message });
}
```

❌ **Don't:**
```csharp
catch
{
    // Silent error - user doesn't know it failed
    return Ok();
}
```

---

## Technical Details

### Database Transaction Isolation

- **Level**: Read Committed (default)
- **Scope**: Single BulkDelete action
- **Duration**: From BeginTransactionAsync() to CommitAsync()
- **Rollback**: Automatic on exception or explicit on error

### Performance Considerations

**Query Optimization:**
```csharp
// Single query to get entities
var entities = await _context.Products
    .Where(e => ids.Contains(e.Id))
    .ToListAsync();

// Batch delete (not N queries)
_context.Products.RemoveRange(entities);
await _context.SaveChangesAsync();
```

**Limitations:**
- Select all only affects current page (not across all pages)
- Maximum IDs per request: Limited by browser/server (typically 1000+)
- Transaction timeout: Default 30 seconds (configurable)

### Browser Compatibility

- **Fetch API**: All modern browsers (IE11 not supported)
- **Arrow Functions**: All modern browsers
- **Template Literals**: All modern browsers
- **Async/Await**: All modern browsers

**For IE11 support:** Use polyfills or rewrite with XMLHttpRequest

### Security Considerations

**CSRF Protection:**
```javascript
headers: {
    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
}
```

**Authorization:**
- Add `[Authorize]` attribute to BulkDelete action
- Check user permissions before deletion
- Log bulk delete operations for audit trail

**Example:**
```csharp
[Authorize(Roles = "Admin")]
[HttpPost]
public async Task<IActionResult> BulkDelete([FromBody] List<int> ids)
{
    // Log operation
    _logger.LogWarning("User {UserId} bulk deleted {Count} products", 
        User.FindFirstValue(ClaimTypes.NameIdentifier), ids.Count);
    
    // Proceed with deletion
}
```

---

## Related Features

- [Pagination](pagination.md) - Page through large datasets
- [Search](search.md) - Real-time search with debouncing
- [Sorting](sorting.md) - Column sorting with visual indicators
- [Filtering](filtering.md) - Boolean filtering with dropdowns

---

## Troubleshooting

### Bulk action bar doesn't appear

**Check:**
1. JavaScript functions are loaded (view page source)
2. Element has `id="bulk-actions"`
3. Checkboxes have class `product-checkbox`
4. Console for JavaScript errors

### Select all doesn't work

**Check:**
1. Header checkbox has `onclick="toggleSelectAll(this)"`
2. Header checkbox has `id="select-all"`
3. Row checkboxes have class matching entity name + `-checkbox`

### Bulk delete fails silently

**Check:**
1. Controller action exists: `[HttpPost] BulkDelete`
2. Route is correct: `/Product/BulkDelete`
3. Request has CSRF token
4. Network tab shows 200 OK response
5. Server logs for exceptions

### Transaction timeout

**Solution:**
```csharp
// Increase timeout for large deletions
using var transaction = await _context.Database.BeginTransactionAsync(
    new TransactionOptions { Timeout = TimeSpan.FromMinutes(5) }
);
```

---

**Generated with**: Phase 4.4 - Bulk Operations (October 26, 2025)  
**CLI Version**: 0.2.0-dev  
**Last Updated**: October 26, 2025

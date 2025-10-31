# Relationship-Aware UI Generation - Design

## Problem Statement
Currently, `swap generate controller` creates CRUD but doesn't know about relationships. After adding relationships with `swap g rel`, forms still show text inputs for foreign keys instead of dropdowns.

## Desired Workflow

```bash
# 1. Create entities
swap g m Customer --fields "Name:string Email:string"
swap g m Order --fields "OrderDate:datetime Total:decimal"

# 2. Add relationship
swap g rel --source Order --target Customer --type one-to-many

# 3. Generate controller WITH relationship awareness
swap g controller Order --with-relationships  # NEW!
# OR regenerate existing:
swap g controller Order --force --with-relationships
```

## Solution Design

### Option A: Enhance Existing `generate controller` Command ⭐ RECOMMENDED
Add `--with-relationships` flag to detect and generate UI for relationships.

**Pros:**
- Single command for all CRUD generation
- Works with existing workflow
- Can regenerate when relationships change

**Implementation:**
1. Detect relationships in entity using `RelationshipUIGenerator.DetectRelationshipsAsync()`
2. For each FK property, generate dropdown instead of text input
3. In controller Create/Edit methods, populate `ViewBag.{Entity}List`
4. In Details/Index views, display related entity name instead of ID

### Option B: Separate `generate relationship-ui` Command
Create dedicated command just for UI generation.

**Pros:**
- Separation of concerns
- Can update UI independently

**Cons:**
- Extra step for users
- Two commands to maintain

### Option C: Auto-detect in All Generation
Always detect and generate relationship UI without flag.

**Pros:**
- Zero configuration
- Works automatically

**Cons:**
- Might surprise users
- Harder to opt-out

## Implementation Plan (Option A)

### Phase 1: Detection
```csharp
// In GenerateControllerCommand.cs
var relationships = await RelationshipUIGenerator.DetectRelationshipsAsync(modelPath);
```

### Phase 2: Form Field Generation
```csharp
foreach (var field in fields)
{
    // Check if this is a FK field
    var relationship = relationships.FirstOrDefault(r => r.ForeignKeyProperty == field.Name);
    
    if (relationship != null)
    {
        // Generate dropdown
        formFields.Add(RelationshipUIGenerator.GenerateDropdownFormField(relationship));
    }
    else
    {
        // Regular field
        formFields.Add(FieldHelper.GenerateFormField(field));
    }
}
```

### Phase 3: Controller Updates
Add to Create/Edit methods:
```csharp
// Populate dropdowns
ViewBag.CustomerList = await _context.Customers.ToListAsync();
ViewBag.ProductList = await _context.Products.ToListAsync();
```

### Phase 4: View Updates
**Details/Index views:**
```csharp
// Instead of: @Model.CustomerId
// Show: @Model.Customer?.Name
```

## Example Generated Code

### Before (No Relationships)
```csharp
// Form
<input type="number" name="CustomerId" value="@Model.CustomerId" />

// Display
<td>@item.CustomerId</td>
```

### After (With Relationships)
```csharp
// Form
<select name="CustomerId" class="select select-bordered">
    <option value="">-- Select Customer --</option>
    @foreach (var item in ViewBag.CustomerList)
    {
        <option value="@item.Id" @(Model.CustomerId == item.Id ? "selected" : "")>
            @item.Name
        </option>
    }
</select>

// Display
<td>@(item.Customer?.Name ?? "None")</td>

// Controller
public async Task<IActionResult> Create()
{
    ViewBag.CustomerList = await _context.Customers.ToListAsync();
    return View(new Order());
}
```

## Files to Modify

1. **tools/Swap.CLI/Commands/GenerateControllerCommand.cs**
   - Add `--with-relationships` flag
   - Call `RelationshipUIGenerator.DetectRelationshipsAsync()`
   - Generate dropdown fields for FKs
   - Add ViewBag population to controller methods

2. **tools/Swap.CLI/Infrastructure/FieldHelper.cs**
   - Add `IsRelationshipField()` helper
   - Skip FK fields in normal field processing

3. **templates/generate/controller/EntityController.cs.template**
   - Add ViewBag population template variable
   - Include in Create/Edit methods

4. **templates/generate/controller/Views/_EntityForm.cshtml.template**
   - Already uses {{FormFields}}, just need to generate dropdowns

5. **templates/generate/controller/Views/_EntityList.cshtml.template**
   - Use navigation property for display

## Auto-Detection Logic

```csharp
// RelationshipUIGenerator.cs already has:
DetectRelationshipsAsync(entityPath)

// Returns:
- ForeignKeyProperty: "CustomerId"
- TargetEntity: "Customer"
- NavigationProperty: "Customer"
- IsRequired: false
- RelationshipType: ManyToOne

// Auto-detect display field:
DetectDisplayFieldAsync(targetEntityPath, "Customer")
// Returns: "Name" or "Title" or "Email" (first string property)
```

## Testing Plan

1. **Unit Tests:**
   - Test relationship detection
   - Test dropdown generation
   - Test display field detection

2. **Integration Tests:**
   - Generate entities
   - Add relationships
   - Generate controller with --with-relationships
   - Verify form has dropdowns
   - Verify views display names

3. **Manual Tests:**
   - Test with Customer/Order
   - Test with Product/Category
   - Test with multiple FKs
   - Test required vs optional FKs

## Timeline

- **Phase 1 Detection:** 1 hour
- **Phase 2 Form Fields:** 2 hours
- **Phase 3 Controller:** 1 hour
- **Phase 4 Views:** 2 hours
- **Testing:** 2 hours

**Total:** ~8 hours (1 day)

## Command Reference

### Current
```bash
swap g controller Order --fields "OrderDate:datetime CustomerId:int Total:decimal"
# Generates text input for CustomerId
```

### Future
```bash
# Auto-detect relationships
swap g controller Order --with-relationships

# Force regeneration
swap g controller Order --force --with-relationships

# Skip relationships (explicit)
swap g controller Order --no-relationships
```

## Display Field Customization

Allow explicit display field specification:
```bash
swap g rel --source Order --target Customer --type one-to-many --display Email
# Forms will show Customer.Email in dropdown
```

## Next Steps

1. ✅ Create RelationshipUIGenerator.cs (DONE)
2. ⏳ Add --with-relationships flag to GenerateControllerCommand
3. ⏳ Integrate detection into controller generation
4. ⏳ Generate dropdown form fields
5. ⏳ Update controller template for ViewBag
6. ⏳ Update view templates for display
7. ⏳ Write unit tests
8. ⏳ Manual testing with Customer/Order example

## Notes

- Keep `swap g rel` focused on data model only
- UI generation is controller regeneration concern
- Users can re-generate controllers as relationships evolve
- This approach is more flexible and maintainable

---
**Status:** Design complete, ready for implementation  
**Target:** Complete remaining Phase 2 items before moving to Phase 3

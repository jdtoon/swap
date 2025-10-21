# E-Commerce Dogfooding - Pain Points

**Date**: October 21, 2025  
**Status**: 🔄 IN PROGRESS - Fixing bugs

## Bugs Found

### ✅ Bug #1: Missing Package References (FIXED)
**Status**: FIXED ✅  
**Impact**: HIGH  
**Found**: Initial Product generation  
**Description**: CLI generates code using NetMX.Ddd.Domain and NetMX.AspNetCore.Mvc but doesn't add package references automatically.

**Error**:
```
CS0246: The type or namespace name 'NetMX' could not be found
```

**Fix**: Added `EnsureRequiredPackagesAsync` method to GenerateFeatureCommand
- Detects missing packages
- Auto-adds with `dotnet add package`
- Shows clear console output

**Verified**: ✅ Packages added automatically

---

### ✅ Bug #2: Wrong Namespaces in Generated Files (FIXED)
**Status**: FIXED ✅  
**Impact**: HIGH  
**Found**: After fixing Bug #1  
**Description**: Generated DTOs, Services, Controllers use wrong namespaces (e.g., `namespace Dtos;` instead of `namespace ECommerce.Web.Dtos;`)

**Error**:
```
CS0246: The type or namespace name 'ProductDto' could not be found
```

**Fix**: 
1. Added `ProjectNamespace` property to `EntityGenerationOptions`
2. Added `DetectProjectNamespaceAsync` method to detect from .csproj
3. Updated all generators (DtoGenerator, ServiceGenerator, EntityGenerator, ControllerGenerator)
4. Fallback: Use project name if <RootNamespace> not found

**Verified**: ✅ Namespaces now correct (ECommerce.Web.Models, ECommerce.Web.Dtos, etc.)

---

### 🔄 Bug #3: Duplicate Using Statement in DbContext
**Status**: 🔄 IN PROGRESS  
**Impact**: MEDIUM  
**Found**: After fixing Bug #2  
**Description**: DbContextModifier adds duplicate using statement: `using Models;` alongside correct `using ECommerce.Web.Models;`

**Error**:
```
C:\jd\netmx\sampleApps\ECommerce\ECommerce.Web\Data\ECommerceDbContext.cs(3,7): error CS0246: The type or namespace name 'Models' could not be found
```

**Root Cause**: CodeModificationHelper.AddUsingDirective extracts namespace but doesn't handle fully qualified namespaces correctly

**Workaround**: Manually remove `using Models;` from DbContext

**TODO**: Fix AddUsingDirective to skip adding if already have qualified namespace

---

### ⏸️ Bug #4: Missing _ViewImports.cshtml
**Status**: ⏸️ NOT STARTED  
**Impact**: MEDIUM  
**Found**: After fixing Bug #2  
**Description**: Generated views use DTOs but no _ViewImports.cshtml exists to import namespaces

**Error**:
```
Views\Product\_List.cshtml(1,13): error CS0246: The type or namespace name 'ProductDto' could not be found
```

**Workaround**: Manually create _ViewImports.cshtml with:
```csharp
@using ECommerce.Web
@using ECommerce.Web.Models
@using ECommerce.Web.Dtos
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
```

**TODO**: CLI should auto-generate _ViewImports.cshtml if it doesn't exist

---

### ⏸️ Bug #5: Entity Constructor Mismatch
**Status**: ⏸️ NOT STARTED  
**Impact**: HIGH  
**Found**: After fixing Bugs #1-2  
**Description**: Generated Product entity has wrong constructor signature for AggregateRoot<Guid>

**Error**:
```
Product.cs(30,31): error CS1729: 'AggregateRoot<Guid>' does not contain a constructor that takes 1 arguments
```

**Root Cause**: EntityGenerator creates constructor that doesn't match base class

**TODO**: Fix EntityGenerator to use correct constructor signature

---

### ⏸️ Bug #6: HTMX Helpers Not Found in Controller
**Status**: ⏸️ NOT STARTED  
**Impact**: HIGH  
**Found**: After fixing Bugs #1-2  
**Description**: Controller uses HxTrigger, HxReswap but they're not found

**Error**:
```
CS1061: 'ProductController' does not contain a definition for 'HxTrigger'
CS0103: The name 'HtmxSwap' does not exist in the current context
```

**Root Cause**: Missing `using NetMX.AspNetCore.Mvc.Htmx;` in generated controller

**TODO**: Verify ControllerGenerator includes HTMX using statement

---

## Summary

| Bug | Status | Priority | Time to Fix |
|-----|--------|----------|-------------|
| #1: Missing packages | ✅ FIXED | 🔥 Critical | 15 min (DONE) |
| #2: Wrong namespaces | ✅ FIXED | 🔥 Critical | 45 min (DONE) |
| #3: Duplicate using in DbContext | 🔄 IN PROGRESS | 🟡 Medium | 15 min |
| #4: Missing _ViewImports | ⏸️ NOT STARTED | 🟡 Medium | 10 min |
| #5: Entity constructor | ⏸️ NOT STARTED | 🔥 High | 20 min |
| #6: HTMX helpers | ⏸️ NOT STARTED | 🔥 High | 5 min |

**Total Bugs**: 6  
**Fixed**: 2 (33%)  
**Remaining**: 4 (67%)  
**Estimated Time**: ~50 minutes remaining

---

## Next Steps

1. ⏸️ Fix Bug #5: Entity constructor
2. ⏸️ Fix Bug #6: HTMX helpers using statement
3. ⏸️ Fix Bug #4: Auto-generate _ViewImports.cshtml
4. ⏸️ Fix Bug #3: Duplicate using in DbContext
5. ✅ Verify Product feature compiles
6. ✅ Run migration
7. ✅ Test in browser

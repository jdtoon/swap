using Microsoft.AspNetCore.Mvc;
using Swap.Htmx;
using Swap.Htmx.State;
using SwapLab.Events;
using SwapLab.Models;
using SwapLab.Views;

namespace SwapLab.Controllers;

/// <summary>
/// Demonstrates all Swap.Htmx patterns with working examples.
/// </summary>
public class PatternsController : Controller
{
    #region Sample Data

    private static readonly List<Product> Products =
    [
        new(1, "Laptop Pro", "Electronics", 1299.99m, 15),
        new(2, "Wireless Mouse", "Electronics", 29.99m, 50),
        new(3, "USB-C Hub", "Electronics", 49.99m, 30),
        new(4, "Monitor 27\"", "Electronics", 399.99m, 20),
        new(5, "Keyboard", "Electronics", 79.99m, 40),
        new(6, "Desk Chair", "Furniture", 249.99m, 10),
        new(7, "Standing Desk", "Furniture", 599.99m, 5),
        new(8, "Desk Lamp", "Furniture", 39.99m, 25),
        new(9, "Notebook", "Office", 4.99m, 100),
        new(10, "Pen Set", "Office", 12.99m, 75),
    ];

    private static readonly List<TaskItem> Tasks =
    [
        new(1, "Review pull request", false, DateTime.Now.AddDays(-2)),
        new(2, "Update documentation", false, DateTime.Now.AddDays(-1)),
        new(3, "Fix login bug", true, DateTime.Now.AddDays(-3)),
        new(4, "Deploy to staging", true, DateTime.Now.AddDays(-1)),
        new(5, "Write unit tests", false, DateTime.Now),
    ];

    #endregion

    #region Basic Patterns

    /// <summary>
    /// Pattern: Basic Swap
    /// Shows the simplest HTMX + Swap.Htmx interaction.
    /// </summary>
    public IActionResult BasicSwap()
    {
        return this.SwapView();
    }

    [HttpGet]
    public IActionResult GetMessage()
    {
        // Simple response with a partial view
        return this.SwapResponse()
            .WithView("_Message", new { Message = $"Hello! The time is {DateTime.Now:HH:mm:ss}" })
            .Build();
    }

    /// <summary>
    /// Pattern: Form POST
    /// Shows form submission with response.
    /// </summary>
    public IActionResult BasicPost()
    {
        return this.SwapView();
    }

    [HttpPost]
    public IActionResult SubmitForm(string name)
    {
        return this.SwapResponse()
            .WithView("_Greeting", name ?? "Guest")
            .WithSuccessToast($"Hello, {name}!")
            .Build();
    }

    private static int _clickCount = 0;

    /// <summary>
    /// Pattern: Out-of-Band Swaps
    /// Shows updating multiple elements from one response.
    /// </summary>
    public IActionResult OobSwap()
    {
        _clickCount = 0; // Reset for demo
        return this.SwapView();
    }

    [HttpPost]
    public IActionResult DoOobSwap()
    {
        _clickCount++;
        var now = DateTime.Now;
        
        // Update primary target AND additional elements via OOB
        // Using generated constants: PatternViews.Partials for view names, PatternIds for element IDs
        return this.SwapResponse()
            .WithView(PatternViews.Partials.ClickResult, _clickCount)
            .AlsoUpdate(PatternIds.ClickCounter, PatternViews.Partials.Counter, _clickCount)
            .AlsoUpdate(PatternIds.LastClick, PatternViews.Partials.LastUpdated, now)
            .Build();
    }

    /// <summary>
    /// Pattern: Toast Notifications
    /// Shows different toast types.
    /// </summary>
    public IActionResult Toasts()
    {
        return this.SwapView();
    }

    [HttpPost]
    public IActionResult ShowSuccessToast()
    {
        return this.SwapResponse()
            .WithView(PatternViews.Partials.ToastResult, new { Message = "Success toast triggered!" })
            .WithSuccessToast("Operation completed successfully!")
            .Build();
    }

    [HttpPost]
    public IActionResult ShowErrorToast()
    {
        return this.SwapResponse()
            .WithView(PatternViews.Partials.ToastResult, new { Message = "Error toast triggered!" })
            .WithErrorToast("Something went wrong!")
            .Build();
    }

    [HttpPost]
    public IActionResult ShowInfoToast()
    {
        return this.SwapResponse()
            .WithView(PatternViews.Partials.ToastResult, new { Message = "Info toast triggered!" })
            .WithToast("Here's some information for you.")
            .Build();
    }

    #endregion

    #region State Management Patterns

    /// <summary>
    /// Pattern: Hidden Field State
    /// Shows storing state in hidden fields.
    /// </summary>
    public IActionResult HiddenFields()
    {
        var state = new ProductSearchState();
        return this.SwapView(state);
    }

    [HttpGet]
    public IActionResult SearchProducts([FromSwapState] ProductSearchState state)
    {
        var results = FilterProducts(state);
        var viewModel = new ProductGridViewModel
        {
            Products = results,
            State = state,
            TotalCount = GetFilteredCount(state)
        };
        
        // Using generated constants - PatternViews.Partials.ProductGrid instead of "_ProductGrid"
        return this.SwapResponse()
            .WithView(PatternViews.Partials.ProductGrid, viewModel)
            .WithTrigger(ProductEvents.Product.Searched)
            .WithState(state)
            .Build();
    }

    /// <summary>
    /// Pattern: URL State
    /// Shows syncing state with URL.
    /// </summary>
    public IActionResult UrlState(ProductSearchState? state)
    {
        state ??= new ProductSearchState();
        return this.SwapView(state);
    }

    [HttpGet]
    public IActionResult SearchProductsWithUrl([FromSwapState] ProductSearchState state)
    {
        var results = FilterProducts(state);
        var viewModel = new ProductGridViewModel
        {
            Products = results,
            State = state,
            TotalCount = GetFilteredCount(state)
        };
        
        // Push the URL to browser history
        Response.HxPushUrl($"/Patterns/UrlState?tab={state.Tab}&page={state.Page}&search={state.Search}");
        
        return this.SwapResponse()
            .WithView("_ProductGrid", viewModel)
            .Build();
    }

    /// <summary>
    /// Pattern: Data Attributes
    /// Shows storing state in data-* attributes.
    /// </summary>
    public IActionResult DataAttributes()
    {
        return this.SwapView();
    }

    [HttpPost]
    public IActionResult ToggleDataItem(int id, bool active)
    {
        var newActive = !active;
        var html = $@"
            <div class=""list-group-item d-flex justify-content-between align-items-center""
                 id=""item-{id}""
                 data-id=""{id}""
                 data-active=""{newActive.ToString().ToLower()}"">
                <span>Item {id} {(newActive ? "✓ Active" : "")}</span>
                <button class=""btn btn-sm {(newActive ? "btn-success" : "btn-secondary")}""
                        hx-post=""{Url.Action("ToggleDataItem", "Patterns")}""
                        hx-vals='js:{{""id"": document.getElementById(""item-{id}"").dataset.id, ""active"": document.getElementById(""item-{id}"").dataset.active}}'
                        hx-target=""#item-{id}""
                        hx-swap=""outerHTML"">
                    {(newActive ? "Active" : "Toggle")}
                </button>
            </div>";
        return Content(html, "text/html");
    }

    [HttpPost]
    public IActionResult IncrementHiddenCounter(int counter, string lastAction)
    {
        var newCounter = counter + 1;
        var html = $@"
            <div class=""alert alert-success"">
                <strong>Counter incremented!</strong><br/>
                Previous: {counter} → New: {newCounter}
            </div>
            <script>
                document.getElementById('counter-value').value = '{newCounter}';
                document.getElementById('last-action').value = 'increment';
                document.getElementById('counter-display').textContent = '{newCounter}';
                document.getElementById('action-display').textContent = 'increment';
            </script>";
        return Content(html, "text/html");
    }

    [HttpPost]
    public IActionResult DecrementHiddenCounter(int counter, string lastAction)
    {
        var newCounter = counter - 1;
        var html = $@"
            <div class=""alert alert-warning"">
                <strong>Counter decremented!</strong><br/>
                Previous: {counter} → New: {newCounter}
            </div>
            <script>
                document.getElementById('counter-value').value = '{newCounter}';
                document.getElementById('last-action').value = 'decrement';
                document.getElementById('counter-display').textContent = '{newCounter}';
                document.getElementById('action-display').textContent = 'decrement';
            </script>";
        return Content(html, "text/html");
    }

    [HttpPost]
    public IActionResult ResetHiddenCounter(int counter, string lastAction)
    {
        var html = @"
            <div class=""alert alert-info"">
                <strong>Counter reset!</strong><br/>
                Value set to 0
            </div>
            <script>
                document.getElementById('counter-value').value = '0';
                document.getElementById('last-action').value = 'reset';
                document.getElementById('counter-display').textContent = '0';
                document.getElementById('action-display').textContent = 'reset';
            </script>";
        return Content(html, "text/html");
    }

    [HttpGet]
    public IActionResult UrlStateTab(string tab)
    {
        var content = tab switch
        {
            "orders" => "<h4>Orders Tab</h4><p>Your orders would appear here.</p><p class='text-muted'>Notice the URL now includes <code>?tab=orders</code></p>",
            "settings" => "<h4>Settings Tab</h4><p>Settings configuration would go here.</p><p class='text-muted'>Notice the URL now includes <code>?tab=settings</code></p>",
            _ => "<h4>Products Tab</h4><p>Product listings would appear here.</p><p class='text-muted'>Notice the URL now includes <code>?tab=products</code></p>"
        };
        
        Response.HxPushUrl($"/Patterns/UrlState?tab={tab}");
        return Content(content, "text/html");
    }

    [HttpPost]
    public IActionResult EventTimingDemo()
    {
        var newValue = new Random().Next(1, 100);
        var html = $@"
            <div class=""p-3 bg-light rounded text-center"">
                <p class=""text-muted mb-0"">Current state: <strong id=""state-display"">{newValue}</strong></p>
            </div>
            <script>document.getElementById('demo-state').value = '{newValue}';</script>";
        return Content(html, "text/html");
    }

    #endregion

    #region Event Chain Patterns

    /// <summary>
    /// Pattern: Event Chains
    /// Shows cascading updates with ISwapEventConfiguration.
    /// </summary>
    public IActionResult EventChains()
    {
        var state = new ProductSearchState();
        var products = FilterProducts(state);
        
        return this.SwapView(new ProductGridViewModel
        {
            Products = products,
            State = state,
            TotalCount = Products.Count
        });
    }

    [HttpPost]
    public IActionResult ChangeTab([FromQuery] string tab, [FromSwapState] ProductSearchState state)
    {
        // Create a new state object with the updated tab to avoid any binding issues
        var newState = new ProductSearchState
        {
            Tab = tab,  // Use the tab from query string
            Page = 1,   // Reset page on tab change
            PageSize = state.PageSize,
            Search = state.Search,
            SortBy = state.SortBy,
            SortDesc = state.SortDesc
        };
        
        var results = FilterProducts(newState);
        var viewModel = new ProductGridViewModel
        {
            Products = results,
            State = newState,
            TotalCount = GetFilteredCount(newState)
        };
        
        // Return the entire demo area so tabs show active state
        // WithState() will automatically update the state container via OOB swap
        return this.SwapResponse()
            .WithView("_EventChainDemo", viewModel)
            .WithTrigger(ProductEvents.Product.TabChanged)
            .WithState(newState)
            .Build();
    }

    /// <summary>
    /// Pattern: Event Timing
    /// Shows the before-request vs after-request timing trap.
    /// </summary>
    public IActionResult EventTiming()
    {
        return this.SwapView();
    }

    #endregion

    #region Multi-Component Patterns

    /// <summary>
    /// Pattern: Multi-Component Coordination
    /// The most common real-world pattern: tabs + search + pagination + grid.
    /// </summary>
    public IActionResult MultiComponent()
    {
        var state = new ProductSearchState();
        var products = FilterProducts(state);
        
        return this.SwapView(new ProductGridViewModel
        {
            Products = products,
            State = state,
            TotalCount = Products.Count
        });
    }

    [HttpGet]
    public IActionResult MultiComponentSearch([FromSwapState] ProductSearchState state)
    {
        state.Page = 1; // Reset to first page on search
        var results = FilterProducts(state);
        var viewModel = new ProductGridViewModel
        {
            Products = results,
            State = state,
            TotalCount = GetFilteredCount(state)
        };
        
        // Demonstrating generated constants: PatternViews for views, PatternIds for element IDs
        return this.SwapResponse()
            .WithView(PatternViews.Partials.ProductGrid, viewModel)
            .AlsoUpdate(PatternIds.ProductCount, PatternViews.Partials.ProductCount, new { Count = GetFilteredCount(state) })
            .AlsoUpdate(PatternIds.Pagination, PatternViews.Partials.Pagination, viewModel)
            .WithState(state)
            .Build();
    }

    [HttpGet]
    public IActionResult MultiComponentPage([FromSwapState] ProductSearchState state)
    {
        var results = FilterProducts(state);
        var viewModel = new ProductGridViewModel
        {
            Products = results,
            State = state,
            TotalCount = GetFilteredCount(state)
        };
        
        return this.SwapResponse()
            .WithView("_ProductGrid", viewModel)
            .WithState(state)
            .Build();
    }

    /// <summary>
    /// Pattern: Search with Debounce
    /// Shows real-time search with input debouncing.
    /// </summary>
    public IActionResult SearchDebounce()
    {
        return this.SwapView(Products);
    }

    [HttpGet]
    public IActionResult LiveSearch(string? query)
    {
        var results = string.IsNullOrWhiteSpace(query)
            ? Products
            : Products.Where(p => 
                p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                p.Category.Contains(query, StringComparison.OrdinalIgnoreCase))
              .ToList();
        
        return this.SwapResponse()
            .WithView("_SearchResults", results)
            .Build();
    }

    /// <summary>
    /// Pattern: Infinite Scroll
    /// Shows loading more content on scroll.
    /// </summary>
    public IActionResult InfiniteScroll()
    {
        return this.SwapView();
    }

    [HttpGet]
    public async Task<IActionResult> LoadMoreItems(int page = 1)
    {
        // Simulate network delay
        await Task.Delay(500);
        
        const int pageSize = 10;
        const int totalItems = 50;
        
        var startId = (page - 1) * pageSize + 1;
        var items = Enumerable.Range(startId, pageSize)
            .Where(i => i <= totalItems)
            .Select(i => new ScrollItem { Id = i, Title = $"Item {i} - Loaded dynamically" })
            .ToList();
        
        var hasMore = page * pageSize < totalItems;
        
        var model = new InfiniteScrollViewModel
        {
            Items = items,
            CurrentPage = page,
            HasMore = hasMore
        };
        
        return PartialView("_InfiniteScrollItems", model);
    }

    #endregion

    #region Recipe Patterns

    // ==========================================
    // Sample Data for Recipes
    // ==========================================
    
    private static readonly List<RateCard> RateCards =
    [
        new(1, "Premium Package", 299.99m, "Full-featured plan with priority support"),
        new(2, "Standard Package", 199.99m, "Most popular choice for small teams"),
        new(3, "Budget Package", 99.99m, "Essential features at a great price"),
        new(4, "Enterprise", 499.99m, "Custom solutions for large organizations"),
    ];

    private static readonly List<QuoteLineItem> QuoteItems =
    [
        new(1, "Laptop Pro 15\"", 1299.99m, null, "High-performance laptop with M3 chip"),
        new(2, "Wireless Mouse", 49.99m, null, "Ergonomic design, 6-month battery"),
        new(3, "USB-C Hub", 79.99m, null, "7-in-1 connectivity solution"),
    ];

    private static List<EditableItem> EditableItems =
    [
        new() { Id = 1, Name = "Laptop Pro", Category = "Electronics", Price = 1299.99m },
        new() { Id = 2, Name = "Wireless Mouse", Category = "Electronics", Price = 29.99m },
        new() { Id = 3, Name = "USB-C Hub", Category = "Electronics", Price = 49.99m },
        new() { Id = 4, Name = "Desk Chair", Category = "Furniture", Price = 249.99m },
        new() { Id = 5, Name = "Standing Desk", Category = "Furniture", Price = 599.99m },
    ];

    // ==========================================
    // Recipes Index Page
    // ==========================================

    public IActionResult Recipes()
    {
        return this.SwapView();
    }

    // ==========================================
    // Multi-Select Picker Recipe
    // ==========================================

    public IActionResult MultiSelectPicker()
    {
        var state = new RateCardPickerState();
        return this.SwapView(BuildPickerViewModel(state));
    }

    [HttpPost]
    public IActionResult ToggleRateCard([FromSwapState] RateCardPickerState state, int id)
    {
        var selected = state.GetSelectedIdList();
        
        if (selected.Contains(id))
            selected.Remove(id);
        else
            selected.Add(id);
        
        state.SetSelectedIds(selected);
        
        return this.SwapResponse()
            .WithView("_RateCardPicker", BuildPickerViewModel(state))
            .WithState(state)
            .Build();
    }

    [HttpPost]
    public IActionResult ClearRateCardSelection([FromSwapState] RateCardPickerState state)
    {
        state.SelectedIds = "";
        
        return this.SwapResponse()
            .WithView("_RateCardPicker", BuildPickerViewModel(state))
            .WithState(state)
            .Build();
    }

    [HttpPost]
    public IActionResult ConfirmRateCardSelection([FromSwapState] RateCardPickerState state)
    {
        var selectedCards = RateCards.Where(c => state.GetSelectedIdList().Contains(c.Id)).ToList();
        
        return this.SwapResponse()
            .WithView("_RateCardPicker", BuildPickerViewModel(state))
            .WithState(state)
            .WithSuccessToast($"Selected {selectedCards.Count} cards totaling {selectedCards.Sum(c => c.Price):C}!")
            .Build();
    }

    private RateCardPickerViewModel BuildPickerViewModel(RateCardPickerState state)
    {
        var selectedIds = state.GetSelectedIdList();
        var selectedTotal = RateCards.Where(c => selectedIds.Contains(c.Id)).Sum(c => c.Price);
        
        return new RateCardPickerViewModel
        {
            State = state,
            RateCards = RateCards,
            SelectedTotal = selectedTotal
        };
    }

    // ==========================================
    // Split-View Builder Recipe
    // ==========================================

    public IActionResult SplitViewBuilder()
    {
        var state = new QuoteBuilderState();
        return this.SwapView(BuildQuoteViewModel(state));
    }

    [HttpPost]
    public IActionResult UpdateQuoteConfig(
        [FromSwapState] QuoteBuilderState state,
        string? currency, 
        decimal? markupPercent,
        string? toggleField)  // Which checkbox was toggled
    {
        if (currency != null) state.Currency = currency;
        if (markupPercent != null) state.MarkupPercent = markupPercent.Value;
        
        // Toggle the specific checkbox that was clicked
        switch (toggleField)
        {
            case "showImages": state.ShowImages = !state.ShowImages; break;
            case "showDescriptions": state.ShowDescriptions = !state.ShowDescriptions; break;
            case "includeTax": state.IncludeTax = !state.IncludeTax; break;
        }
        
        return this.SwapResponse()
            .WithView("_QuotePreview", BuildQuoteViewModel(state))
            .WithState(state)
            .Build();
    }

    private QuoteBuilderViewModel BuildQuoteViewModel(QuoteBuilderState state)
    {
        return new QuoteBuilderViewModel
        {
            State = state,
            Items = QuoteItems
        };
    }

    // ==========================================
    // Inline Edit Recipe
    // ==========================================

    public IActionResult InlineEdit()
    {
        return this.SwapView(EditableItems.ToList());
    }

    [HttpGet]
    public IActionResult EditItemName(int id)
    {
        var item = EditableItems.First(i => i.Id == id);
        return PartialView("_EditItemName", item);
    }

    [HttpGet]
    public IActionResult EditItemCategory(int id)
    {
        var item = EditableItems.First(i => i.Id == id);
        return PartialView("_EditItemCategory", item);
    }

    [HttpGet]
    public IActionResult EditItemPrice(int id)
    {
        var item = EditableItems.First(i => i.Id == id);
        return PartialView("_EditItemPrice", item);
    }

    [HttpGet]
    public IActionResult GetEditableRow(int id)
    {
        var item = EditableItems.First(i => i.Id == id);
        return PartialView("_EditableRow", item);
    }

    [HttpPost]
    public IActionResult UpdateItemName(int id, string name)
    {
        var item = EditableItems.First(i => i.Id == id);
        item.Name = name;
        
        return this.SwapResponse()
            .WithView("_EditableRow", item)
            .WithUpdatedToast("Item", name)
            .Build();
    }

    [HttpPost]
    public IActionResult UpdateItemCategory(int id, string category)
    {
        var item = EditableItems.First(i => i.Id == id);
        item.Category = category;
        
        return this.SwapResponse()
            .WithView("_EditableRow", item)
            .WithUpdatedToast("Category", category)
            .Build();
    }

    [HttpPost]
    public IActionResult UpdateItemPrice(int id, decimal price)
    {
        var item = EditableItems.First(i => i.Id == id);
        item.Price = price;
        
        return this.SwapResponse()
            .WithView("_EditableRow", item)
            .WithUpdatedToast("Price", price.ToString("C"))
            .Build();
    }

    // ==========================================
    // Wizard Form Recipe
    // ==========================================

    public IActionResult WizardForm()
    {
        var state = new CheckoutWizardState();
        return this.SwapView(new CheckoutWizardViewModel { State = state });
    }

    [HttpPost]
    public IActionResult ValidateShipping(
        [FromSwapState] CheckoutWizardState state,
        string? shippingName, 
        string? shippingAddress, 
        string? shippingCity)
    {
        state.ShippingName = shippingName ?? "";
        state.ShippingAddress = shippingAddress ?? "";
        state.ShippingCity = shippingCity ?? "";
        
        var errors = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(shippingName))
            errors["ShippingName"] = "Name is required";
        if (string.IsNullOrWhiteSpace(shippingAddress))
            errors["ShippingAddress"] = "Address is required";
        if (string.IsNullOrWhiteSpace(shippingCity))
            errors["ShippingCity"] = "City is required";
        
        if (errors.Count > 0)
        {
            return this.SwapResponse()
                .WithView("_CheckoutWizard", new CheckoutWizardViewModel { State = state, Errors = errors })
                .WithState(state)
                .Build();
        }
        
        state.CurrentStep = 2;
        return this.SwapResponse()
            .WithView("_CheckoutWizard", new CheckoutWizardViewModel { State = state })
            .WithState(state)
            .Build();
    }

    [HttpPost]
    public IActionResult ValidatePayment(
        [FromSwapState] CheckoutWizardState state,
        string? cardNumber, 
        string? cardExpiry, 
        string? cardCvv)
    {
        state.CardNumber = cardNumber ?? "";
        state.CardExpiry = cardExpiry ?? "";
        state.CardCvv = cardCvv ?? "";
        
        var errors = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(cardNumber) || cardNumber.Replace(" ", "").Length < 13)
            errors["CardNumber"] = "Valid card number is required";
        if (string.IsNullOrWhiteSpace(cardExpiry) || !cardExpiry.Contains('/'))
            errors["CardExpiry"] = "Valid expiry (MM/YY) is required";
        if (string.IsNullOrWhiteSpace(cardCvv) || cardCvv.Length < 3)
            errors["CardCvv"] = "Valid CVV is required";
        
        if (errors.Count > 0)
        {
            return this.SwapResponse()
                .WithView("_CheckoutWizard", new CheckoutWizardViewModel { State = state, Errors = errors })
                .WithState(state)
                .Build();
        }
        
        state.CurrentStep = 3;
        return this.SwapResponse()
            .WithView("_CheckoutWizard", new CheckoutWizardViewModel { State = state })
            .WithState(state)
            .Build();
    }

    [HttpPost]
    public IActionResult WizardGoBack([FromSwapState] CheckoutWizardState state)
    {
        state.CurrentStep = Math.Max(1, state.CurrentStep - 1);
        
        return this.SwapResponse()
            .WithView("_CheckoutWizard", new CheckoutWizardViewModel { State = state })
            .WithState(state)
            .Build();
    }

    [HttpPost]
    public IActionResult CompleteCheckout([FromSwapState] CheckoutWizardState state)
    {
        return this.SwapResponse()
            .WithView("_WizardComplete", new CheckoutWizardViewModel { State = state })
            .WithSuccessToast("Order placed successfully!")
            .Build();
    }

    #endregion

    #region Form Patterns

    /// <summary>
    /// Pattern: Form Validation
    /// Shows server-side validation with inline errors.
    /// </summary>
    public IActionResult FormValidation()
    {
        return this.SwapView();
    }

    [HttpPost]
    public IActionResult ValidateUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return Content("<span class='text-danger'><i class='bi bi-x-circle me-1'></i>Username is required</span>", "text/html");
        
        if (username.Length < 3)
            return Content("<span class='text-danger'><i class='bi bi-x-circle me-1'></i>Username must be at least 3 characters</span>", "text/html");
        
        if (username == "admin" || username == "test")
            return Content("<span class='text-danger'><i class='bi bi-x-circle me-1'></i>This username is already taken</span>", "text/html");
        
        return Content("<span class='text-success'><i class='bi bi-check-circle me-1'></i>Username is available</span>", "text/html");
    }

    [HttpPost]
    public IActionResult ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Content("<span class='text-danger'><i class='bi bi-x-circle me-1'></i>Email is required</span>", "text/html");
        
        if (!email.Contains('@') || !email.Contains('.'))
            return Content("<span class='text-danger'><i class='bi bi-x-circle me-1'></i>Please enter a valid email address</span>", "text/html");
        
        return Content("<span class='text-success'><i class='bi bi-check-circle me-1'></i>Email looks good</span>", "text/html");
    }

    [HttpPost]
    public IActionResult ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return Content("<span class='text-danger'><i class='bi bi-x-circle me-1'></i>Password is required</span>", "text/html");
        
        var strength = 0;
        var feedback = new List<string>();
        
        if (password.Length >= 8) strength++; else feedback.Add("8+ characters");
        if (password.Any(char.IsUpper)) strength++; else feedback.Add("uppercase letter");
        if (password.Any(char.IsLower)) strength++; else feedback.Add("lowercase letter");
        if (password.Any(char.IsDigit)) strength++; else feedback.Add("number");
        if (password.Any(c => !char.IsLetterOrDigit(c))) strength++; else feedback.Add("special character");
        
        var (color, label) = strength switch
        {
            5 => ("success", "Strong"),
            4 => ("info", "Good"),
            3 => ("warning", "Fair"),
            _ => ("danger", "Weak")
        };
        
        var html = $@"
            <div class='progress mb-1' style='height: 5px;'>
                <div class='progress-bar bg-{color}' style='width: {strength * 20}%'></div>
            </div>
            <small class='text-{color}'>{label}</small>";
        
        if (feedback.Any())
            html += $"<small class='text-muted d-block'>Add: {string.Join(", ", feedback)}</small>";
        
        return Content(html, "text/html");
    }

    [HttpPost]
    public IActionResult ValidateForm(string username, string email, string password)
    {
        var errors = new List<string>();
        
        if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
            errors.Add("Valid username is required");
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            errors.Add("Valid email is required");
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            errors.Add("Password must be at least 8 characters");
        
        if (errors.Any())
        {
            return this.SwapResponse()
                .WithView("_ValidationErrors", errors)
                .WithErrorToast("Please fix the errors above")
                .Build();
        }
        
        return this.SwapResponse()
            .WithView("_FormSuccess", username)
            .WithSuccessToast($"Welcome, {username}!")
            .Build();
    }

    /// <summary>
    /// Pattern: Loading States
    /// Shows feedback during slow operations.
    /// </summary>
    public IActionResult LoadingStates()
    {
        return this.SwapView();
    }

    [HttpPost]
    public async Task<IActionResult> SlowAction()
    {
        // Simulate slow operation
        await Task.Delay(1500);
        return PartialView("_Message", new { Message = "Operation completed successfully after 1.5 seconds!" });
    }

    /// <summary>
    /// Pattern: Modal Forms
    /// Shows edit forms in modal dialogs.
    /// </summary>
    public IActionResult ModalForms()
    {
        return this.SwapView(Products);
    }

    [HttpGet]
    public IActionResult EditProductModal(int id)
    {
        var product = Products.FirstOrDefault(p => p.Id == id);
        if (product == null) return NotFound();
        
        return this.SwapResponse()
            .WithView("_EditProductModal", product)
            .Build();
    }

    [HttpPost]
    public IActionResult SaveProduct(int id, string name, decimal price, int stock)
    {
        // Find and update the product in our demo list
        var index = Products.FindIndex(p => p.Id == id);
        if (index >= 0)
        {
            var existingProduct = Products[index];
            var updatedProduct = existingProduct with { Name = name, Price = price, Stock = stock };
            Products[index] = updatedProduct;
            
            return this.SwapResponse()
                .WithView("_ModalClosed")
                .AlsoUpdate($"product-row-{id}", "_ProductRow", updatedProduct)
                .WithSuccessToast($"Product '{name}' updated!")
                .Build();
        }
        
        return this.SwapResponse()
            .WithView("_ModalClosed")
            .WithErrorToast("Product not found!")
            .Build();
    }

    #endregion

    #region Helpers

    private List<Product> FilterProducts(ProductSearchState state)
    {
        var query = Products.AsEnumerable();
        
        // Filter by tab/category
        if (state.Tab != "all")
            query = query.Where(p => p.Category.Equals(state.Tab, StringComparison.OrdinalIgnoreCase));
        
        // Filter by search
        if (!string.IsNullOrWhiteSpace(state.Search))
            query = query.Where(p => p.Name.Contains(state.Search, StringComparison.OrdinalIgnoreCase));
        
        // Sort
        query = state.SortBy.ToLower() switch
        {
            "price" => state.SortDesc ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price),
            "stock" => state.SortDesc ? query.OrderByDescending(p => p.Stock) : query.OrderBy(p => p.Stock),
            _ => state.SortDesc ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name)
        };
        
        // Paginate
        return query
            .Skip((state.Page - 1) * state.PageSize)
            .Take(state.PageSize)
            .ToList();
    }

    private int GetFilteredCount(ProductSearchState state)
    {
        var query = Products.AsEnumerable();
        
        if (state.Tab != "all")
            query = query.Where(p => p.Category.Equals(state.Tab, StringComparison.OrdinalIgnoreCase));
        
        if (!string.IsNullOrWhiteSpace(state.Search))
            query = query.Where(p => p.Name.Contains(state.Search, StringComparison.OrdinalIgnoreCase));
        
        return query.Count();
    }

    private List<Product> FilterProductsForUrlSync(UrlSyncState state)
    {
        var query = Products.AsEnumerable();
        
        // Filter by category
        if (state.Category != "all")
            query = query.Where(p => p.Category.Equals(state.Category, StringComparison.OrdinalIgnoreCase));
        
        // Filter by price range
        query = state.PriceRange switch
        {
            "low" => query.Where(p => p.Price < 50),
            "mid" => query.Where(p => p.Price >= 50 && p.Price <= 200),
            "high" => query.Where(p => p.Price > 200),
            _ => query
        };
        
        // Sort
        query = state.SortBy.ToLower() switch
        {
            "price" => query.OrderBy(p => p.Price),
            "stock" => query.OrderBy(p => p.Stock),
            _ => query.OrderBy(p => p.Name)
        };
        
        return query.ToList();
    }

    #endregion

    #region URL Sync Pattern

    /// <summary>
    /// Pattern: URL Sync
    /// Shows SwapState with SyncToUrl for bookmarkable, shareable URLs.
    /// </summary>
    public IActionResult UrlSync([FromSwapState] UrlSyncState? state)
    {
        state ??= new UrlSyncState();
        var products = FilterProductsForUrlSync(state);
        
        return this.SwapView(new UrlSyncViewModel
        {
            State = state,
            Products = products
        });
    }

    [HttpGet]
    public IActionResult UrlSyncSearch([FromSwapState] UrlSyncState state)
    {
        var products = FilterProductsForUrlSync(state);
        
        // Push URL with state - generates query string automatically
        Response.HxPushUrl($"/Patterns/UrlSync?{state.ToQueryString()}");
        
        return this.SwapResponse()
            .WithView("_UrlSyncResults", new UrlSyncViewModel
            {
                State = state,
                Products = products
            })
            .WithState(state)
            .Build();
    }

    #endregion

    #region Conditional Swaps Pattern

    /// <summary>
    /// Pattern: Conditional Swaps
    /// Shows AlsoUpdateIfExists() and AlsoUpdateIf() for conditional OOB swaps.
    /// </summary>
    public IActionResult ConditionalSwaps()
    {
        return this.SwapView(new ConditionalSwapViewModel());
    }

    [HttpPost]
    public IActionResult TriggerWithNotification()
    {
        // AlsoUpdateIfExists - gracefully skips if #notification-content doesn't exist
        return this.SwapResponse()
            .WithView("_ActionResult", new { Message = "Action triggered successfully!" })
            .AlsoUpdateIfExists("notification-content", "_Notification", new { Message = "New notification received!" })
            .WithSuccessToast("Action completed!")
            .Build();
    }

    private static int _orderCounter = 1000;

    [HttpPost]
    public IActionResult ConditionalUpdate(string role, decimal orderValue)
    {
        var orderId = ++_orderCounter;
        var isManager = role == "manager" || role == "admin";
        var isAdmin = role == "admin";
        var isHighValue = orderValue > 500;
        
        var response = this.SwapResponse()
            .WithView("_OrderResult", new { OrderId = orderId, OrderValue = orderValue });
        
        // AlsoUpdateIf - conditionally include OOB swaps based on server-side logic
        if (isManager)
        {
            response.AlsoUpdateIf(isManager, "manager-content", "_PanelContent", new { Label = "Manager view updated" });
        }
        
        if (isAdmin)
        {
            response.AlsoUpdateIf(isAdmin, "admin-content", "_PanelContent", new { Label = "Admin audit logged" });
        }
        
        if (isHighValue)
        {
            response.AlsoUpdateIf(isHighValue, "high-value-content", "_PanelContent", new { Label = $"High-value order: {orderValue:C}" });
        }
        
        // Also show/hide panels via script
        var showManagerPanel = isManager ? "block" : "none";
        var showAdminPanel = isAdmin ? "block" : "none";
        var showHighValueAlert = isHighValue ? "block" : "none";
        
        response.WithTrigger("updatePanels", new { showManagerPanel, showAdminPanel, showHighValueAlert });
        
        return response.Build();
    }

    #endregion
}

public class ProductFormModel
{
    public string? Name { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string? Category { get; set; }
}

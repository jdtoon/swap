using Microsoft.AspNetCore.Mvc;
using NetMX.AspNetCore.Mvc.Htmx;
using System.ComponentModel.DataAnnotations;

namespace ECommerceDogfood.Web.Controllers;

/// <summary>
/// Demonstrates HTMX patterns and capabilities
/// </summary>
public class DemoController : Controller
{
    #region Page Actions
    
    public IActionResult Index()
    {
        return View();
    }

    #endregion

    #region Click to Edit

    [HttpGet]
    public IActionResult Contact()
    {
        var contact = new ContactViewModel
        {
            Name = "John Doe",
            Email = "john@example.com",
            Active = true
        };
        return PartialView("_ContactDisplay", contact);
    }

    [HttpGet]
    public IActionResult EditContact()
    {
        var contact = new ContactViewModel
        {
            Name = "John Doe",
            Email = "john@example.com",
            Active = true
        };
        return PartialView("_ContactEdit", contact);
    }

    [HttpPost]
    public IActionResult SaveContact(ContactViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return PartialView("_ContactEdit", model);
        }

        // Simulate save
        return PartialView("_ContactDisplay", model);
    }

    [HttpDelete]
    public IActionResult CancelEdit()
    {
        var contact = new ContactViewModel
        {
            Name = "John Doe",
            Email = "john@example.com",
            Active = true
        };
        return PartialView("_ContactDisplay", contact);
    }

    #endregion

    #region Delete with Confirmation

    private static List<ItemViewModel> Items = new()
    {
        new ItemViewModel { Id = 1, Name = "Product Alpha", Price = 29.99m, InStock = true },
        new ItemViewModel { Id = 2, Name = "Product Beta", Price = 49.99m, InStock = true },
        new ItemViewModel { Id = 3, Name = "Product Gamma", Price = 19.99m, InStock = false },
    };

    [HttpGet]
    public IActionResult ItemList()
    {
        return PartialView("_ItemList", Items);
    }

    [HttpDelete]
    public IActionResult DeleteItem(int id)
    {
        var item = Items.FirstOrDefault(x => x.Id == id);
        if (item != null)
        {
            Items.Remove(item);
        }

        this.HxReswap(HtmxSwap.Delete);
        
        return Ok();
    }

    #endregion

    #region Infinite Scroll

    private static readonly string[] ActivityItems = new[]
    {
        "User john.doe logged in",
        "Order #1234 was placed",
        "Payment received for invoice #5678",
        "New user registered: jane.smith",
        "Product 'Widget' stock updated",
        "Customer support ticket #999 closed",
        "Newsletter sent to 1,500 subscribers",
        "Database backup completed",
        "System health check passed",
        "New feature deployed to production",
    };

    [HttpGet]
    public IActionResult LoadMoreActivities(int page = 1)
    {
        const int pageSize = 3;
        var skip = (page - 1) * pageSize;
        
        var activities = ActivityItems
            .Skip(skip)
            .Take(pageSize)
            .Select((item, index) => new ActivityViewModel
            {
                Id = skip + index + 1,
                Message = item,
                Timestamp = DateTime.Now.AddMinutes(-(skip + index) * 5)
            })
            .ToList();

        var hasMore = skip + pageSize < ActivityItems.Length;

        ViewBag.Page = page;
        ViewBag.HasMore = hasMore;

        return PartialView("_ActivityItems", activities);
    }

    #endregion

    #region Search with Debounce

    private static readonly List<string> Products = new()
    {
        "Apple iPhone 15",
        "Apple iPad Pro",
        "Apple MacBook Air",
        "Samsung Galaxy S24",
        "Samsung Galaxy Tab",
        "Google Pixel 8",
        "Sony WH-1000XM5",
        "Bose QuietComfort",
        "Dell XPS 13",
        "HP Spectre x360",
    };

    [HttpGet]
    public IActionResult SearchProducts(string q = "")
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return PartialView("_SearchResults", new List<string>());
        }

        var results = Products
            .Where(p => p.Contains(q, StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Simulate network delay
        Thread.Sleep(300);

        return PartialView("_SearchResults", results);
    }

    #endregion

    #region Tab Switching

    [HttpGet]
    public IActionResult TabContent(string tab)
    {
        return tab switch
        {
            "overview" => PartialView("_TabOverview"),
            "features" => PartialView("_TabFeatures"),
            "pricing" => PartialView("_TabPricing"),
            _ => Content("Unknown tab")
        };
    }

    #endregion

    #region Form Validation

    [HttpPost]
    public IActionResult ValidateForm(SignupViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return PartialView("_SignupForm", model);
        }

        // Success! Trigger an event and show success message
        this.HxTrigger("signup-success");
        
        return PartialView("_SignupSuccess", model);
    }

    #endregion

    #region Active Search (Type-ahead)

    [HttpPost]
    public IActionResult ActiveSearch(string search)
    {
        var results = Products
            .Where(p => p.Contains(search, StringComparison.OrdinalIgnoreCase))
            .Take(5)
            .ToList();

        return PartialView("_ActiveSearchResults", results);
    }

    #endregion

    #region Lazy Loading

    [HttpGet]
    public IActionResult LazyImage()
    {
        // Simulate slow image loading
        Thread.Sleep(1000);
        
        return PartialView("_LazyImage");
    }

    [HttpGet]
    public IActionResult LazyChart()
    {
        Thread.Sleep(800);
        return PartialView("_LazyChart");
    }

    #endregion

    #region Progress Indicator

    private static int _uploadProgress = 0;

    [HttpPost]
    public IActionResult StartUpload()
    {
        _uploadProgress = 0;
        return PartialView("_UploadProgress");
    }

    [HttpGet]
    public IActionResult GetUploadProgress()
    {
        _uploadProgress += Random.Shared.Next(10, 25);
        
        if (_uploadProgress >= 100)
        {
            _uploadProgress = 100;
            // Stop polling with status code 286
            return StatusCode(286, PartialView("_UploadComplete").ToString());
        }

        ViewBag.Progress = _uploadProgress;
        return PartialView("_UploadProgress");
    }

    #endregion

    #region Inline Messages

    [HttpPost]
    public IActionResult ShowMessage(string type)
    {
        var message = type switch
        {
            "success" => "Operation completed successfully!",
            "error" => "An error occurred. Please try again.",
            "warning" => "Warning: This action cannot be undone.",
            _ => "Information message"
        };

        ViewBag.Type = type;
        ViewBag.Message = message;

        return PartialView("_InlineMessage");
    }

    #endregion

    #region Out of Band Updates

    [HttpPost]
    public IActionResult UpdateMultiple()
    {
        // Update multiple parts of the page using Out-of-Band swaps
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        
        var header = $"<span class='tag is-success'>Updated at {timestamp}</span>";
        var content = $"<p class='has-text-success'>Main content updated at {timestamp}</p>";
        var footer = $"<small class='has-text-grey'>Footer refreshed at {timestamp}</small>";

        // TODO: Fix HtmxResponseExtensions not being found in package
        // return this.HxOutOfBandSwaps(
        //     ("oob-header", header, HtmxSwap.InnerHTML),
        //     ("oob-content", content, HtmxSwap.InnerHTML),
        //     ("oob-footer", footer, HtmxSwap.InnerHTML)
        // );
        
        var html = $@"
            <div id=""oob-header"" hx-swap-oob=""innerHTML"">{header}</div>
            <div id=""oob-content"" hx-swap-oob=""innerHTML"">{content}</div>
            <div id=""oob-footer"" hx-swap-oob=""innerHTML"">{footer}</div>
        ";
        
        return Content(html, "text/html");
    }

    #endregion
}

#region View Models

public class ContactViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool Active { get; set; }
}

public class ItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool InStock { get; set; }
}

public class ActivityViewModel
{
    public int Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class SignupViewModel
{
    [Required(ErrorMessage = "Username is required")]
    [MinLength(3, ErrorMessage = "Username must be at least 3 characters")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    public string Password { get; set; } = string.Empty;
}

#endregion

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using ttw.Data;
using ttw.Data.Models;
using ttw.Dtos;
using ttw.Services;

namespace ttw.Controllers
{
    [Authorize(Roles = "owner,agent")]
    public class RateCardController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly IViewEngine _viewEngine;
        private readonly BrowserService _browserService;

        public RateCardController(ApplicationDbContext context, 
                                  IViewEngine viewEngine,
                                  BrowserService browserService)
        {
            db = context;
            _viewEngine = viewEngine;
            _browserService = browserService;
        }

        public ActionResult Index()
        {
            ViewBag.IsHTMXRequest = Request.Headers.ContainsKey("HX-Request");
            ViewBag.CityList = new SelectList(db.City, "Id", "Name");
            ViewBag.Suppliers = new SelectList(db.Supplier, "Id", "Name");

            if (Request.Headers["HX-Request"].ToString() == "true")
                return PartialView();

            return View();
        }

        public ActionResult List(int page = 1, int pageSize = 10)
        {
            ViewBag.Currencies = new SelectList(db.Currency.ToList(), "Id", "Name");

            var query = db.RateCard.AsNoTracking();
            var totalItems = query.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var rateCards = query
                .OrderBy(x => x.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;

            if (Request.Headers["HX-Request"].ToString() == "true")
                return PartialView(rateCards);

            return View(rateCards);
        }

        public ActionResult RateCardsList(int page = 1, int pageSize = 10, string? search = null)
        {
            var query = db.RateCard.AsNoTracking();
            
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x => EF.Functions.Like(x.Name, $"%{search}%"));
            }

            var totalItems = query.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var rateCards = query
                .OrderBy(x => x.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.CurrentSearch = search;

            return PartialView("_RateCardsList", rateCards);
        }

        public async Task<ActionResult> Edit(int? id)
        {
            ViewBag.IsHTMXRequest = Request.Headers.ContainsKey("HX-Request");

            if (id == null)
            {
                return BadRequest("Rate Card ID is required."); // Add specific error
            }

            // Use Include if you need related data from RateCard itself, otherwise FindAsync is fine
            RateCard? rateCard = await db.RateCard.FindAsync(id);
            if (rateCard == null)
            {
                return NotFound($"Rate Card with ID {id} not found.");
            }

            // --- Prepare the Main Model ---
            var model = new RateCardEditModel
            {
                Id = rateCard.Id,
                Name = rateCard.Name,
                // Handle potential null JSON and deserialization errors gracefully
                Model = new List<RateCardViewModel>() // Initialize empty list
            };

            try
            {
                if (!string.IsNullOrEmpty(rateCard.Json))
                {
                    // Use null forgiving operator only if you are absolutely sure it won't be null after check
                    // Consider try-catch block if deserialization might fail
                    model.Model = JsonConvert.DeserializeObject<List<RateCardViewModel>>(rateCard.Json)!
                                             .OrderBy(x => x.placementOrder).ToList();
                }
            }
            catch (JsonException ex)
            {
                // Log the error, maybe return a view with an error message
                // For now, model.Model remains an empty list
                Console.WriteLine($"Error deserializing RateCard JSON for ID {id}: {ex.Message}");
                // Optionally: Add a model state error: ModelState.AddModelError("Json", "Error reading rate card details.");
            }


            // --- Populate Select Lists (More efficiently if possible) ---

            // 1. Get data needed globally first to minimize DB calls
            var allSuppliers = await db.Supplier.ToListAsync();
            var allCities = await db.City.ToListAsync();
            var allRoomTypes = await db.RoomType.ToListAsync();
            // Consider fetching all relevant Hotels once if feasible, maybe filtered by distinct cities in model.Model?
            // var relevantCityIds = model.Model.Select(m => m.selectedHotel).Distinct()... get city ids ...
            // var relevantHotels = await db.Hotel.Where(h => relevantCityIds.Contains(h.CityId)).ToListAsync();


            // 2. Populate lists for each card in the model
            foreach (var item in model.Model)
            {
                // Populate Hotels (Example: Filter pre-fetched list or query DB)
                // This example still queries DB per item, optimization might be needed for performance
                var hotelForCity = await db.Hotel.Include(h => h.City) // Include City if needed
                                               .FirstOrDefaultAsync(h => h.Id == item.selectedHotel);
                int? cityId = hotelForCity?.City.Id;

                if (cityId.HasValue)
                {
                    item.Hotels = new SelectList(
                        await db.Hotel.Include(h => h.City).Where(x => x.City.Id == cityId.Value).ToListAsync(), // Query hotels in the specific city
                        "Id", // Data value field from Hotel entity
                        "Name", // Data text field from Hotel entity
                        item.selectedHotel); // Selected value
                }
                else
                {
                    // Hotel or its city not found, provide empty list
                    item.Hotels = new SelectList(Enumerable.Empty<SelectListItem>(), "Value", "Text");
                    Console.WriteLine($"Warning: Could not determine city for selected hotel ID {item.selectedHotel} in card.");
                }

                // Populate Suppliers using the pre-fetched list
                item.Suppliers = new SelectList(allSuppliers, "Id", "Name", item.selectedSupplier);
            }

            // 3. Populate Global ViewBag items needed by modals or shared elements
            ViewBag.CityList = new SelectList(allCities, "Id", "Name");

            // --- CORRECTED VIEWBAG.ROOMTYPES ---
            // Create SelectList using the pre-fetched list and specifying correct properties
            // **REPLACE "Id" and "Name" with the ACTUAL property names in your RoomType class**
            ViewBag.RoomTypes = new SelectList(allRoomTypes, "Id", "Name");
            // --- END CORRECTION ---


            // --- Return View or PartialView ---
            // Correctly check HTMX header value
            if (Request.Headers.TryGetValue("HX-Request", out var hxValue) && hxValue == "true")
            {
                return PartialView(model); // Return partial for HTMX requests
            }

            return View(model); // Return full view otherwise
        }

        [AllowAnonymous]
        public async Task<ActionResult> Download(int id, int currencyId, bool includeSupplier)
        {
            try
            {
                // Fetch the rate card
                RateCard? rateCard = await db.RateCard.FindAsync(id);
                if (rateCard == null)
                {
                    return NotFound();
                }

                // Prepare the model
                var model = GetRateCardData(rateCard, currencyId, includeSupplier);

                // Render the view to a string
                var html = await RenderViewToStringAsync("Download", model);
                if (string.IsNullOrEmpty(html))
                {
                    throw new InvalidOperationException("Failed to render the PDF template");
                }

                // Generate the PDF
                var pdfContent = await GeneratePdfFromHtmlAsync(html);
                if (pdfContent == null || pdfContent.Length == 0)
                {
                    throw new InvalidOperationException("Failed to generate PDF content");
                }

                // Return the PDF as a file
                return File(pdfContent, "application/pdf", $"ratecard_{id}_{DateTime.Now:yyyyMMdd}.pdf");
            }
            catch (Exception)
            {
                // Log the error here if you have logging configured
                return StatusCode(500, new { error = "Failed to generate PDF. Please try again later." });
            }
        }

        private async Task<byte[]> GeneratePdfFromHtmlAsync(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                throw new ArgumentException("HTML content cannot be empty", nameof(html));
            }

            var browser = await _browserService.GetBrowserAsync();
            using var page = await browser.NewPageAsync();

            try
            {
                // Set content and wait for network resources to load
                await page.SetContentAsync(html, new NavigationOptions
                {
                    WaitUntil = new[] { WaitUntilNavigation.Networkidle0 },
                    Timeout = 60000
                });

                // Wait for content and fonts
                await page.WaitForTimeoutAsync(2000);
                await page.EvaluateExpressionAsync("document.fonts.ready");

                // Generate PDF with specific settings
                var pdfContent = await page.PdfDataAsync(new PdfOptions
                {
                    Format = PaperFormat.A4,
                    PrintBackground = true,
                    PreferCSSPageSize = true,
                    MarginOptions = new MarginOptions
                    {
                        Top = "0mm",
                        Bottom = "0mm",
                        Left = "0mm",
                        Right = "0mm"
                    },
                    Scale = 1.00m
                });

                return pdfContent;
            }
            finally
            {
                await page.CloseAsync();
            }
        }

        // Helper method to render view to string
        private async Task<string> RenderViewToStringAsync<TModel>(string viewName, TModel model)
        {
            var actionContext = new ActionContext(HttpContext, RouteData, ControllerContext.ActionDescriptor);
            var view = _viewEngine.FindView(actionContext, viewName, false).View;

            using var writer = new StringWriter();
            var viewContext = new ViewContext(
                actionContext,
                view!,
                new ViewDataDictionary<TModel>(ViewData, model),
                TempData,
                writer,
                new HtmlHelperOptions()
            );

            await view!.RenderAsync(viewContext);
            return writer.ToString();
        }

        public async Task<ActionResult> View(int id, int currencyId, bool includeSupplier)
        {
            RateCard? rateCard = await db.RateCard.FindAsync(id);
            if (rateCard == null)
            {
                return NotFound();
            }

            return View("Download", GetRateCardData(rateCard, currencyId, includeSupplier));
        }

        [HttpPost]
        public ActionResult AddCard(
            [FromForm] int noOfDates,
            [FromForm] int noOfRoomTypes,
            [FromForm] int cityId,
            [FromForm] string rowRates,
            [FromForm] int cardId,
            [FromForm] bool mealPerRow)
        {
            var rowRatesList = JsonConvert.DeserializeObject<List<RowRateSelectViewModel>>(rowRates);

            var model = new RateCardPartialViewModel
            {
                NoOfDates = noOfDates,
                NoOfRoomTypes = noOfRoomTypes,
                RowRates = rowRatesList!,
                CardId = cardId,
                MealPerRow = mealPerRow,
            };

            ViewBag.Hotels = new SelectList(db.Hotel
                .Include(x => x.City)
                .Where(x => x.City.Id == cityId), "Id", "Name", 1);
            ViewBag.RoomTypes = new SelectList(db.RoomType, "Id", "Name", 1);
            ViewBag.Suppliers = new SelectList(db.Supplier, "Id", "Name", 1);

            if (mealPerRow)
            {
                return PartialView("RateCardPartial_WithAllMeals", model);
            }

            return PartialView("RateCardPartial", model);
        }

        [HttpPost]
        public async Task<ActionResult> SaveProgress(
            [FromForm] string model,
            [FromForm] bool redirect,
            [FromForm] string name,
            [FromForm] int rateCardId)
        {
            try
            {
                if (string.IsNullOrEmpty(name))
                {
                    return BadRequest("Name is required");
                }

                if (rateCardId == 0)
                {
                    var rateCard = new RateCard
                    {
                        Json = model,
                        Name = name,
                    };
                    db.RateCard.Add(rateCard);
                    await db.SaveChangesAsync();

                    rateCardId = rateCard.Id;
                }
                else
                {
                    var rateCard = await db.RateCard.FindAsync(rateCardId);
                    if (rateCard == null)
                    {
                        return NotFound($"RateCard with ID {rateCardId} not found");
                    }

                    rateCard.Name = name;
                    rateCard.Json = model;
                    db.Entry(rateCard).State = EntityState.Modified;
                    await db.SaveChangesAsync();
                }

                return Json(new { value = redirect, id = rateCardId });
            }
            catch (Exception)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while saving the rate card");
            }
        }

        private RateCardEditModel GetRateCardData(RateCard rateCard, int currencyId, bool includeSupplier)
        {
            var model = new RateCardEditModel
            {
                Id = rateCard.Id,
                Name = rateCard.Name,
                Model = JsonConvert.DeserializeObject<List<RateCardViewModel>>(rateCard.Json)!,
                Currency = db.Currency.FirstOrDefault(c => c.Id == currencyId)!,
                IncludeSupplier = includeSupplier
            };

            foreach (var item in model.Model)
            {
                item.SelectedHotelName = db.Hotel.FirstOrDefault(h => h.Id == item.selectedHotel)?.Name ?? string.Empty;
                item.SelectedSupplierName = db.Supplier.FirstOrDefault(h => h.Id == item.selectedSupplier)?.Name ?? string.Empty;
                item.SelectedRoomNames = new List<string>();
                item.selectedRooms.ForEach(room => item.SelectedRoomNames.Add(db.RoomType.FirstOrDefault(rt => rt.Id == room)!.Name));
                var hotel = db.Hotel.Include(h => h.City).FirstOrDefault(h => h.Id == item.selectedHotel);
                item.City = hotel?.City!;

                if (!includeSupplier)
                {
                    foreach (var row in item.rows)
                    {
                        foreach (var rate in row.rates)
                        {
                            rate.rate1 = Convert.ToInt32(Math.Ceiling((((rate.rate1 * model.Currency.Markup / 100) + rate.rate1)
                                * model.Currency.Rate) / model.Currency.RoundOff) * model.Currency.RoundOff);
                            rate.rate2 = Convert.ToInt32(Math.Ceiling((((rate.rate2 * model.Currency.Markup / 100) + rate.rate2)
                                * model.Currency.Rate) / model.Currency.RoundOff) * model.Currency.RoundOff);
                        }
                    }
                }
            }

            model.Model = model.Model.OrderByDescending(x => x.City.Name).ToList();

            return model;
        }

        public ActionResult DownloadModal(int id)
        {
            var currencies = db.Currency.ToList().Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name });
            return PartialView("_DownloadModal", (id, currencies));
        }

        public ActionResult ViewModal(int id)
        {
            var currencies = db.Currency.ToList().Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name });
            return PartialView("_ViewModal", (id, currencies));
        }
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ttw.Data;
using ttw.Data.Models;
using ttw.Dtos.Data;
using System.Globalization;

namespace ttw.Controllers
{
    [Authorize(Roles = "owner")]
    public class CurrencyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const int PageSize = 10;

        public CurrencyController(ApplicationDbContext context)
        {
            // Ensure decimal points are handled correctly regardless of culture
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

            _context = context;
        }

        public IActionResult Index(int take = 10, string? search = null)
        {
            if (Request.Headers["HX-Request"].ToString() == "true")
                return PartialView();

            return View();
        }

        public async Task<IActionResult> GetCurrencyList(int take = 10, string? search = null)
        {
            var query = _context.Currency.AsNoTracking();
            
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x => EF.Functions.Like(x.Name, $"%{search}%") || 
                                       EF.Functions.Like(x.LongName, $"%{search}%"));
            }

            var totalRecords = await query.CountAsync();
            var currencies = await query
                .OrderBy(c => c.Name)
                .Take(take)
                .ToListAsync();

            var model = new PagedResult<Currency>
            {
                Data = currencies,
                TotalRecords = totalRecords,
                CurrentPage = take,
                HasMore = take < totalRecords
            };

            ViewBag.CurrentSearch = search;
            return PartialView("_CurrencyList", model);
        }

        public IActionResult CreateModal()
        {
            return PartialView("_CreateModal");
        }

        public async Task<IActionResult> EditModal(int id)
        {
            var currency = await _context.Currency.FindAsync(id);
            if (currency == null) return NotFound();

            return PartialView("_EditModal", currency);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,LongName,Rate,RoundOff,Markup")] Currency currency)
        {
            // Parse the Rate manually if needed
            if (Request.Form.TryGetValue("Rate", out var rateString))
            {
                if (decimal.TryParse(rateString.ToString().Replace(',', '.'),
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out decimal rate))
                {
                    currency.Rate = rate;
                    ModelState.Remove("Rate"); // Remove any existing validation errors for Rate
                }
            }

            if (ModelState.IsValid)
            {
                _context.Add(currency);
                await _context.SaveChangesAsync();
                return await GetCurrencyList();
            }
            return PartialView("_CreateModal", currency);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([Bind("Id,Name,LongName,Rate,RoundOff,Markup")] Currency currency)
        {
            // Parse the Rate manually if needed
            if (Request.Form.TryGetValue("Rate", out var rateString))
            {
                if (decimal.TryParse(rateString.ToString().Replace(',', '.'),
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out decimal rate))
                {
                    currency.Rate = rate;
                    ModelState.Remove("Rate"); // Remove any existing validation errors for Rate
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(currency);
                    await _context.SaveChangesAsync();
                    return await GetCurrencyList();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CurrencyExists(currency.Id))
                        return NotFound();
                    throw;
                }
            }
            return PartialView("_EditModal", currency);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var currency = await _context.Currency.FindAsync(id);
            if (currency == null) return NotFound();

            _context.Currency.Remove(currency);
            await _context.SaveChangesAsync();
            return new EmptyResult();
        }

        private bool CurrencyExists(int id)
        {
            return _context.Currency.Any(e => e.Id == id);
        }
    }
}
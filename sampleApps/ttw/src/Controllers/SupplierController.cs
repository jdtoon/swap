using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ttw.Data;
using ttw.Data.Models;
using ttw.Dtos;
using ttw.Dtos.Data;

namespace ttw.Controllers
{
    [Authorize(Roles = "owner")]
    public class SupplierController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SupplierController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(int take = 10, string? search = null)
        {
            if (Request.Headers["HX-Request"].ToString() == "true")
                return PartialView();

            return View();
        }

        public async Task<IActionResult> GetSupplierList(int take = 10, string? search = null)
        {
            var query = _context.Supplier.AsNoTracking();
            
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x => EF.Functions.Like(x.Name, $"%{search}%"));
            }

            var totalRecords = await query.CountAsync();
            var suppliers = await query
                .OrderBy(s => s.Name)
                .Take(take)
                .ToListAsync();

            var model = new PagedResult<Supplier>
            {
                Data = suppliers,
                TotalRecords = totalRecords,
                CurrentPage = take,
                HasMore = take < totalRecords
            };

            ViewBag.CurrentSearch = search;
            return PartialView("_SupplierList", model);
        }

        public IActionResult CreateModal()
        {
            return PartialView("_CreateModal");
        }

        public async Task<IActionResult> EditModal(int id)
        {
            var supplier = await _context.Supplier.FindAsync(id);
            if (supplier == null) return NotFound();

            return PartialView("_EditModal", supplier);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name")] Supplier supplier)
        {
            if (ModelState.IsValid)
            {
                _context.Add(supplier);
                await _context.SaveChangesAsync();
                return await GetSupplierList();
            }
            return PartialView("_CreateModal", supplier);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([Bind("Id,Name")] Supplier supplier)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(supplier);
                    await _context.SaveChangesAsync();
                    return await GetSupplierList();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SupplierExists(supplier.Id))
                        return NotFound();
                    throw;
                }
            }
            return PartialView("_EditModal", supplier);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var supplier = await _context.Supplier.FindAsync(id);
            if (supplier == null) return NotFound();

            _context.Supplier.Remove(supplier);
            await _context.SaveChangesAsync();
            return new EmptyResult();
        }

        private bool SupplierExists(int id)
        {
            return _context.Supplier.Any(e => e.Id == id);
        }
    }
}
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
    public class CityController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CityController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: City
        public IActionResult Index(int take = 10, string? search = null)
        {
            if (Request.Headers["HX-Request"].ToString() == "true")
                return PartialView();

            return View();
        }

        // Partial view for the city list
        public async Task<IActionResult> GetCityList(int take = 10, string? search = null)
        {
            var query = _context.City.AsNoTracking();
            
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x => EF.Functions.Like(x.Name, $"%{search}%"));
            }

            var totalRecords = await query.CountAsync();
            var cities = await query
                .Take(take)
                .ToListAsync();

            var model = new PagedResult<City>
            {
                Data = cities,
                TotalRecords = totalRecords,
                CurrentPage = take,
                HasMore = take < totalRecords
            };

            ViewBag.CurrentSearch = search;
            return PartialView("_CityList", model);
        }

        // GET: City modal forms
        public IActionResult CreateModal()
        {
            ViewData["images"] = GetImageSelectList();
            return PartialView("_CreateModal");
        }

        public async Task<IActionResult> EditModal(int id)
        {
            var city = await _context.City.FindAsync(id);
            if (city == null) return NotFound();

            ViewData["images"] = GetImageSelectList();
            return PartialView("_EditModal", city);
        }

        // POST: City/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Image")] City city)
        {
            if (ModelState.IsValid)
            {
                _context.City.Add(city);
                await _context.SaveChangesAsync();
                return await GetCityList();
            }

            ViewData["images"] = GetImageSelectList();
            return PartialView("_CreateModal", city);
        }

        // POST: City/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([Bind("Id,Name,Image")] City city)
        {
            if (ModelState.IsValid)
            {
                _context.Entry(city).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return await GetCityList();
            }

            ViewData["images"] = GetImageSelectList();
            return PartialView("_EditModal", city);
        }

        // DELETE: City/Delete/5
        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var city = await _context.City.FindAsync(id);
            if (city == null) return NotFound();

            _context.City.Remove(city);
            await _context.SaveChangesAsync();
            return new EmptyResult();
        }

        private SelectList GetImageSelectList()
        {
            var list = Directory.EnumerateFiles(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images"))
                .Select(item => new
                {
                    Id = MapPathReverse(item),
                    Name = Path.GetFileName(item).Split('.')[0]
                })
                .ToList();

            return new SelectList(list, "Id", "Name");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            base.Dispose(disposing);
        }

        private string MapPathReverse(string path)
        {
            return path.Replace(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), string.Empty).Replace("\\", "/");
        }
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ttw.Data;
using ttw.Data.Models;
using ttw.Dtos.Data;


namespace ttw.Controllers
{
    [Authorize(Roles = "owner")]
    public class HotelController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HotelController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(int take = 10, string? search = null)
        {
            if (Request.Headers["HX-Request"].ToString() == "true")
                return PartialView();

            return View();
        }

        public async Task<IActionResult> GetHotelList(int take = 10, string? search = null)
        {
            var query = _context.Hotel.AsNoTracking();
            
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x => EF.Functions.Like(x.Name, $"%{search}%"));
            }

            var totalRecords = await query.CountAsync();
            var hotels = await query
                .Include(h => h.City)
                .OrderBy(r => r.Name)
                .Take(take)
                .ToListAsync();

            var model = new PagedResult<Hotel>
            {
                Data = hotels,
                TotalRecords = totalRecords,
                CurrentPage = take,
                HasMore = take < totalRecords
            };

            ViewBag.CurrentSearch = search;
            return PartialView("_HotelList", model);
        }

        public IActionResult CreateModal()
        {
            ViewData["Cities"] = new SelectList(_context.City.OrderBy(c => c.Name), "Id", "Name");
            return PartialView("_CreateModal");
        }

        public async Task<IActionResult> EditModal(int id)
        {
            var hotel = await _context.Hotel
                .Include(h => h.City)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (hotel == null) return NotFound();

            ViewData["Cities"] = new SelectList(_context.City.OrderBy(c => c.Name), "Id", "Name", hotel.City?.Id);
            return PartialView("_EditModal", hotel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,MapCityId")] Hotel hotel)
        {
            ModelState.Remove("City");

            if (ModelState.IsValid)
            {
                var city = await _context.City.FindAsync(hotel.MapCityId);
                if (city == null)
                {
                    ModelState.AddModelError("MapCityId", "Invalid city selected.");
                    ViewData["Cities"] = new SelectList(_context.City.OrderBy(c => c.Name), "Id", "Name");
                    return PartialView("_CreateModal", hotel);
                }

                hotel.City = city;
                _context.Hotel.Add(hotel);
                await _context.SaveChangesAsync();
                return await GetHotelList();
            }

            ViewData["Cities"] = new SelectList(_context.City.OrderBy(c => c.Name), "Id", "Name");
            return PartialView("_CreateModal", hotel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([Bind("Id,Name,MapCityId")] Hotel hotel)
        {
            ModelState.Remove("City");

            if (ModelState.IsValid)
            {
                var existingHotel = await _context.Hotel
                    .Include(h => h.City)
                    .FirstOrDefaultAsync(h => h.Id == hotel.Id);

                if (existingHotel == null)
                    return NotFound();

                var city = await _context.City.FindAsync(hotel.MapCityId);
                if (city == null)
                {
                    ModelState.AddModelError("MapCityId", "Invalid city selected.");
                    ViewData["Cities"] = new SelectList(_context.City.OrderBy(c => c.Name), "Id", "Name");
                    return PartialView("_EditModal", hotel);
                }

                existingHotel.Name = hotel.Name;
                existingHotel.City = city;
                await _context.SaveChangesAsync();
                return await GetHotelList();
            }

            ViewData["Cities"] = new SelectList(_context.City.OrderBy(c => c.Name), "Id", "Name");
            return PartialView("_EditModal", hotel);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var hotel = await _context.Hotel.FindAsync(id);
            if (hotel == null) return NotFound();

            _context.Hotel.Remove(hotel);
            await _context.SaveChangesAsync();
            return new EmptyResult();
        }
    }
}
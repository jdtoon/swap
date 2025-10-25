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
    public class RoomTypeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RoomTypeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(int take = 10, string? search = null)
        {
            if (Request.Headers["HX-Request"].ToString() == "true")
                return PartialView();

            return View();
        }

        public async Task<IActionResult> GetRoomTypeList(int take = 10, string? search = null)
        {
            var query = _context.RoomType.AsNoTracking();
            
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x => EF.Functions.Like(x.Name, $"%{search}%"));
            }

            var totalRecords = await query.CountAsync();
            var roomTypes = await query
                .OrderBy(r => r.Name)
                .Take(take)
                .ToListAsync();

            var model = new PagedResult<RoomType>
            {
                Data = roomTypes,
                TotalRecords = totalRecords,
                CurrentPage = take,
                HasMore = take < totalRecords
            };

            ViewBag.CurrentSearch = search;
            return PartialView("_RoomTypeList", model);
        }

        public IActionResult CreateModal()
        {
            return PartialView("_CreateModal");
        }

        public async Task<IActionResult> EditModal(int id)
        {
            var roomType = await _context.RoomType.FindAsync(id);
            if (roomType == null) return NotFound();

            return PartialView("_EditModal", roomType);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name")] RoomType roomType)
        {
            if (ModelState.IsValid)
            {
                _context.Add(roomType);
                await _context.SaveChangesAsync();
                return await GetRoomTypeList();
            }
            return PartialView("_CreateModal", roomType);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([Bind("Id,Name")] RoomType roomType)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(roomType);
                    await _context.SaveChangesAsync();
                    return await GetRoomTypeList();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RoomTypeExists(roomType.Id))
                        return NotFound();
                    throw;
                }
            }
            return PartialView("_EditModal", roomType);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var roomType = await _context.RoomType.FindAsync(id);
            if (roomType == null) return NotFound();

            _context.RoomType.Remove(roomType);
            await _context.SaveChangesAsync();
            return new EmptyResult();
        }

        private bool RoomTypeExists(int id)
        {
            return _context.RoomType.Any(e => e.Id == id);
        }
    }
}
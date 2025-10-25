using habits.Data.Models;
using habits.Data;
using habits.Dtos.Data;
using habits.Dtos;
using Microsoft.EntityFrameworkCore;

namespace habits.Services.Calendar
{
    public class CalendarEventTypeService : ICalendarEventTypeService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CalendarEventTypeService> _logger;

        public CalendarEventTypeService(ApplicationDbContext context,
                                      ILogger<CalendarEventTypeService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public PagedResult<CalendarEventTypeDto> GetEventTypes(string search, int page, int pageSize)
        {
            var query = _context.CalendarEventTypes.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(t => t.Name.ToLower().Contains(search));
            }

            int totalRecords = query.Count();
            var types = query.OrderBy(t => t.Name)
                            .Skip((page - 1) * pageSize)
                            .Take(pageSize)
                            .ToList();

            return new PagedResult<CalendarEventTypeDto>
            {
                Data = types.Select(CalendarEventTypeDto.FromModel).ToList(),
                HasMore = (page * pageSize) < totalRecords,
                TotalRecords = totalRecords,
                CurrentPage = page
            };
        }

        public async Task<CalendarEventTypeDto> AddEventTypeAsync(string name, string color, string iconPath)
        {
            var eventType = new CalendarEventType
            {
                Name = name,
                Color = color,
                IconPath = iconPath.Substring(1)
            };

            _context.CalendarEventTypes.Add(eventType);
            await _context.SaveChangesAsync();

            return CalendarEventTypeDto.FromModel(eventType);
        }

        public async Task<CalendarEventTypeDto> UpdateEventTypeAsync(int id, string name, string color, string iconPath)
        {
            var eventType = await _context.CalendarEventTypes.FindAsync(id);
            if (eventType == null) return null!;

            eventType.Name = name;
            eventType.Color = color;
            eventType.IconPath = iconPath;

            await _context.SaveChangesAsync();
            return CalendarEventTypeDto.FromModel(eventType);
        }

        public async Task<bool> DeleteEventTypeAsync(int id)
        {
            var eventType = await _context.CalendarEventTypes.FindAsync(id);
            if (eventType == null) return false;

            _context.CalendarEventTypes.Remove(eventType);
            await _context.SaveChangesAsync();
            return true;
        }

        public List<CalendarEventTypeDto> GetAllEventTypes()
        {
            return _context.CalendarEventTypes
                .AsNoTracking()
                .OrderBy(t => t.Name)
                .Select(t => CalendarEventTypeDto.FromModel(t))
                .ToList();
        }
    }
}

using habits.Dtos.Data;
using habits.Dtos;

namespace habits.Services.Calendar
{
    public interface ICalendarEventTypeService
    {
        PagedResult<CalendarEventTypeDto> GetEventTypes(string search, int page, int pageSize);
        Task<CalendarEventTypeDto> AddEventTypeAsync(string name, string color, string iconPath);
        Task<CalendarEventTypeDto> UpdateEventTypeAsync(int id, string name, string color, string iconPath);
        Task<bool> DeleteEventTypeAsync(int id);
        List<CalendarEventTypeDto> GetAllEventTypes();
    }
}

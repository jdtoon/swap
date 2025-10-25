using habits.Dtos;

namespace habits.Services.Calendar
{
    public interface ICalendarService
    {
        CalendarMonthDto GetMonthData(DateTime date);
        CalendarDayDto GetDayData(DateTime date);
        string GetIslamicDate(DateTime gregorianDate);
        string GetIslamicMonthAbbreviation(DateTime gregorianDate);
        bool IsFirstDayOfIslamicMonth(DateTime gregorianDate);
        Task<CalendarEventDto> AddEventAsync(CreateCalendarEventDto dto);
        List<CalendarEventDto> GetUpcomingEvents();
        List<CalendarEventDto> GetEventsForDate(DateTime date);
        CalendarEventDto? GetEvent(int id);
        Task<CalendarEventDto> UpdateEventAsync(int id, UpdateCalendarEventDto dto);
        Task DeleteEventAsync(int id);
    }
}

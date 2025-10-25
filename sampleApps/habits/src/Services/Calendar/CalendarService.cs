using Humanizer;
using Microsoft.EntityFrameworkCore;
using habits.Data;
using habits.Data.Models;
using habits.Dtos;
using System.Globalization;
using Microsoft.Extensions.Logging;

namespace habits.Services.Calendar
{
    public class CalendarService : ICalendarService
    {
        private readonly HijriCalendar _hijriCalendar;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CalendarService> _logger;

        public CalendarService(ApplicationDbContext context, ILogger<CalendarService> logger)
        {
            _hijriCalendar = new HijriCalendar();
            _context = context;
            _logger = logger;
        }

        public CalendarMonthDto GetMonthData(DateTime date)
        {
            var firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
            var daysInMonth = DateTime.DaysInMonth(date.Year, date.Month);

            var calendarMonth = new CalendarMonthDto
            {
                FirstDay = firstDayOfMonth,
                DaysInMonth = daysInMonth,
                FirstDayOfWeek = (int)firstDayOfMonth.DayOfWeek,
                MonthName = firstDayOfMonth.ToString("MMMM"),
                Year = firstDayOfMonth.Year,
                Days = new List<CalendarDayDto>()
            };

            // Generate days for the month
            for (int day = 1; day <= daysInMonth; day++)
            {
                var currentDate = new DateTime(date.Year, date.Month, day);
                calendarMonth.Days.Add(GetDayData(currentDate));
            }

            return calendarMonth;
        }

        public CalendarDayDto GetDayData(DateTime date)
        {
            var hijriDay = _hijriCalendar.GetDayOfMonth(date);
            var isFirstDayOfHijriMonth = hijriDay == 1;

            return new CalendarDayDto
            {
                DayNumber = date.Day,
                IsCurrentMonth = true,
                Date = date,
                IslamicDate = hijriDay.ToString(),
                IsFirstDayOfIslamicMonth = isFirstDayOfHijriMonth,
                IslamicMonthAbbreviation = isFirstDayOfHijriMonth ? GetIslamicMonthAbbreviation(date) : null!,
                Events = GetEventsForDay(date)
            };
        }

        public string GetIslamicDate(DateTime gregorianDate)
        {
            return _hijriCalendar.GetDayOfMonth(gregorianDate).ToString();
        }

        public string GetIslamicMonthAbbreviation(DateTime gregorianDate)
        {
            var monthNumber = _hijriCalendar.GetMonth(gregorianDate);
            var monthNames = new[]
            {
                "Muh", "Saf", "Rab I", "Rab II", "Jum I", "Jum II",
                "Raj", "Sha", "Ram", "Shaw", "DhulQ", "DhulH"
            };
            return monthNames[monthNumber - 1];
        }

        public bool IsFirstDayOfIslamicMonth(DateTime gregorianDate)
        {
            return _hijriCalendar.GetDayOfMonth(gregorianDate) == 1;
        }

        public async Task<CalendarEventDto> AddEventAsync(CreateCalendarEventDto dto)
        {
            _logger.LogInformation("Creating new calendar event: {Title} starting at {StartDate}", dto.Title, dto.StartDate);

            var calendarEvent = new CalendarEvent
            {
                Title = dto.Title,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                ReminderDateTime = dto.ReminderDateTime,
                StartTime = dto.IsFullDay ? null : dto.StartTime,
                EndTime = dto.IsFullDay ? null : dto.EndTime,
                Description = dto.Description,
                CalendarEventTypeId = dto.CalendarEventTypeId
            };

            _context.CalendarEvent.Add(calendarEvent);
            await _context.SaveChangesAsync();

            return new CalendarEventDto
            {
                Id = calendarEvent.Id,
                Title = calendarEvent.Title,
                StartDate = calendarEvent.StartDate,
                EndDate = calendarEvent.EndDate,
                StartTime = calendarEvent.StartTime,
                EndTime = calendarEvent.EndTime,
                Description = calendarEvent.Description,
                CalendarEventTypeId = calendarEvent.CalendarEventTypeId,
                EventType = CalendarEventTypeDto.FromModel(_context.CalendarEventTypes.FirstOrDefault(x => x.Id == dto.CalendarEventTypeId)!)
            };
        }

        public List<CalendarEventDto> GetUpcomingEvents()
        {
            _logger.LogDebug("Retrieving upcoming events. Current time: {Now}", DateTime.Now);

            var today = DateTime.Today;
            var events = _context.CalendarEvent
                .AsNoTracking()
                .Include(e => e.EventType)
                .Where(e => e.StartDate >= today)
                .OrderBy(e => e.StartDate)
                .Take(2)
                .ToList();

            if (events.Count == 0)
            {
                return [];
            }

            return events.Select(e => new CalendarEventDto
            {
                Id = e.Id,
                Title = e.Title,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                StartTime = e.StartTime,
                EndTime = e.EndTime,
                Description = e.Description,
                CalendarEventTypeId = e.CalendarEventTypeId,
                EventType = CalendarEventTypeDto.FromModel(e.EventType)
            }).ToList();
        }

        public List<CalendarEventDto> GetEventsForDate(DateTime date)
        {
            _logger.LogDebug("Retrieving events for date: {Date}", date.ToShortDateString());

            return _context.CalendarEvent
                .AsNoTracking()
                .Include(e => e.EventType)
                .Where(e => e.StartDate.Date == date.Date)
                .Select(e => new CalendarEventDto
                {
                    Id = e.Id,
                    Title = e.Title,
                    StartDate = e.StartDate,
                    EndDate = e.EndDate,
                    ReminderDateTime = e.ReminderDateTime,
                    StartTime = e.StartTime,
                    EndTime = e.EndTime,
                    Description = e.Description,
                    CalendarEventTypeId = e.CalendarEventTypeId,
                    EventType = CalendarEventTypeDto.FromModel(e.EventType)
                })
                .AsEnumerable()  // Switch to in-memory
                .OrderBy(e => e.StartTime)
                .ToList();
        }

        public CalendarEventDto? GetEvent(int id)
        {
            var evt = _context.CalendarEvent
                .AsNoTracking()
                .Include(e => e.EventType)
                .FirstOrDefault(e => e.Id == id);

            return evt == null ? null : new CalendarEventDto
            {
                Id = evt.Id,
                Title = evt.Title,
                StartDate = evt.StartDate,
                EndDate = evt.EndDate,
                ReminderDateTime = evt.ReminderDateTime,
                StartTime = evt.StartTime,
                EndTime = evt.EndTime,
                Description = evt.Description,
                CalendarEventTypeId = evt.CalendarEventTypeId,
                EventType = CalendarEventTypeDto.FromModel(evt.EventType)
            };
        }

        public async Task<CalendarEventDto> UpdateEventAsync(int id, UpdateCalendarEventDto dto)
        {
            _logger.LogInformation("Updating calendar event {EventId}: {Title}", id, dto.Title);

            var evt = await _context.CalendarEvent
                .Include(e => e.EventType)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (evt == null)
                throw new KeyNotFoundException("Event not found");

            evt.Title = dto.Title;
            evt.EndDate = dto.EndDate;
            evt.ReminderDateTime = dto.ReminderDateTime;
            evt.StartTime = dto.IsFullDay ? null : dto.StartTime;
            evt.EndTime = dto.IsFullDay ? null : dto.EndTime;
            evt.Description = dto.Description;
            evt.CalendarEventTypeId = dto.CalendarEventTypeId;

            await _context.SaveChangesAsync();

            return new CalendarEventDto
            {
                Id = evt.Id,
                Title = evt.Title,
                StartDate = evt.StartDate,
                EndDate = evt.EndDate,
                ReminderDateTime = evt.ReminderDateTime,
                StartTime = evt.StartTime,
                EndTime = evt.EndTime,
                Description = evt.Description,
                CalendarEventTypeId = evt.CalendarEventTypeId,
                EventType = CalendarEventTypeDto.FromModel(evt.EventType)
            };
        }

        public async Task DeleteEventAsync(int id)
        {
            _logger.LogInformation("Deleting calendar event {EventId}", id);

            var evt = await _context.CalendarEvent.FindAsync(id);
            if (evt == null)
                throw new KeyNotFoundException("Event not found");

            _context.CalendarEvent.Remove(evt);
            await _context.SaveChangesAsync();
        }

        private List<CalendarEventTypeDto> GetEventsForDay(DateTime date)
        {
            return _context.CalendarEvent
                .AsNoTracking()
                .Include(e => e.EventType)
                .Where(e => e.StartDate.Date == date.Date)
                .Select(e => e.EventType)
                .Select(t => CalendarEventTypeDto.FromModel(t))
                .ToList();
        }
    }
}
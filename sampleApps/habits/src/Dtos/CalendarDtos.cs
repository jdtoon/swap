using habits.Data.Models;
using System.ComponentModel.DataAnnotations;

namespace habits.Dtos
{
    public class CalendarEventTypeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string IconPath { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public static CalendarEventTypeDto FromModel(CalendarEventType model)
        {
            return new CalendarEventTypeDto
            {
                Id = model.Id,
                Name = model.Name,
                Color = model.Color,
                IconPath = model.IconPath,
                CreatedAt = model.CreatedAt
            };
        }
    }

    public class CalendarDto
    {
        public CalendarMonthDto MonthData { get; set; } = null!;
        public List<CalendarEventTypeDto> EventTypes { get; set; } = null!;
    }

    public class CalendarMonthDto
    {
        public DateTime FirstDay { get; set; }
        public int DaysInMonth { get; set; }
        public int FirstDayOfWeek { get; set; } // 0 = Sunday
        public string MonthName { get; set; } = null!;
        public int Year { get; set; }
        public List<CalendarDayDto> Days { get; set; } = new List<CalendarDayDto>();
    }

    public class CalendarDayDto
    {
        public int DayNumber { get; set; }
        public bool IsCurrentMonth { get; set; }
        public DateTime Date { get; set; }
        public string IslamicDate { get; set; } = null!;
        public bool IsFirstDayOfIslamicMonth { get; set; }
        public string IslamicMonthAbbreviation { get; set; } = null!;
        public List<CalendarEventTypeDto> Events { get; set; } = new();
        public bool IsToday => Date.Date == DateTime.Today;
    }

    public class CalendarEventDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? ReminderDateTime { get; set; }
        public string? Description { get; set; }
        public int CalendarEventTypeId { get; set; }
        public CalendarEventTypeDto EventType { get; set; } = null!;
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
    }

    public class CreateCalendarEventDto
    {
        public string Title { get; set; } = null!;
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? ReminderDateTime { get; set; }

        [DataType(DataType.Time)]
        public TimeSpan? StartTime { get; set; }

        [DataType(DataType.Time)]
        public TimeSpan? EndTime { get; set; }
        public string? Description { get; set; }
        public int CalendarEventTypeId { get; set; }
        public bool IsFullDay { get; set; }
    }

    public class UpdateCalendarEventDto
    {
        public string Title { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? ReminderDateTime { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public string? Description { get; set; }
        public int CalendarEventTypeId { get; set; }
        public bool IsFullDay { get; set; }
    }

    public class DayViewerDto
    {
        public DateTime Date { get; set; }
        public List<CalendarEventDto> Events { get; set; } = new();
    }
}

using habits.Data.Models;

public interface ICalendarNotificationProcessor
{
    Task<string> RenderEmailTemplateAsync(CalendarEvent calendarEvent);
    Task SendNotificationsAsync(CalendarEvent calendarEvent, IEnumerable<AppUser> users);
    Task<IEnumerable<CalendarEvent>> GetUpcomingEventsAsync(DateTime now, CancellationToken cancellationToken);
    Task<IEnumerable<AppUser>> GetSubscribedUsersAsync(CancellationToken cancellationToken);
    Task MarkEventAsNotifiedAsync(CalendarEvent calendarEvent, CancellationToken cancellationToken);
} 
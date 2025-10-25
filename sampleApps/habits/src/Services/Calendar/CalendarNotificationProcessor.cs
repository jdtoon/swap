using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Razor;
using habits.Services.Notifications;
using habits.Data.Models;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using habits.Data;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace Services.Calendar
{
    public class CalendarNotificationProcessor : ICalendarNotificationProcessor
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IRazorViewEngine _razorViewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IEmailSender _emailSender;
        private readonly IFcmNotificationService _fcmNotificationService;
        private readonly ILogger<CalendarNotificationProcessor> _logger;

        public CalendarNotificationProcessor(
            IServiceScopeFactory serviceScopeFactory,
            IRazorViewEngine razorViewEngine,
            ITempDataProvider tempDataProvider,
            IEmailSender emailSender,
            IFcmNotificationService fcmNotificationService,
            ILogger<CalendarNotificationProcessor> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _razorViewEngine = razorViewEngine;
            _tempDataProvider = tempDataProvider;
            _emailSender = emailSender;
            _fcmNotificationService = fcmNotificationService;
            _logger = logger;
        }

        public async Task<string> RenderEmailTemplateAsync(CalendarEvent calendarEvent)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var httpContext = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            
            using var sw = new StringWriter();
            var viewResult = _razorViewEngine.FindView(actionContext, "Emails/EventReminder", false);

            if (viewResult.View == null)
            {
                throw new ArgumentNullException($"Emails/EventReminder does not match any available view");
            }

            var viewDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
            {
                Model = calendarEvent
            };

            var viewContext = new ViewContext(
                actionContext,
                viewResult.View,
                viewDictionary,
                new TempDataDictionary(actionContext.HttpContext, _tempDataProvider),
                sw,
                new HtmlHelperOptions()
            );

            await viewResult.View.RenderAsync(viewContext);
            return sw.ToString();
        }

        public async Task SendNotificationsAsync(CalendarEvent calendarEvent, IEnumerable<AppUser> users)
        {
            var emailHtml = await RenderEmailTemplateAsync(calendarEvent);

            foreach (var user in users)
            {
                await _emailSender.SendEmailAsync(
                    user.Email!,
                    "Upcoming Event Reminder",
                    emailHtml
                );
            }

            // TODO: Fix push notifications
            // var tokens = users.Select(u => u.FcmToken!).ToList();
            // await _fcmNotificationService.SendNotificationToMultipleTokensAsync(
            //     tokens,
            //     "Upcoming Event",
            //     $"Your event '{calendarEvent.Title}' starts in 1 hour"
            // );
        }

        public async Task<IEnumerable<CalendarEvent>> GetUpcomingEventsAsync(DateTime now, CancellationToken cancellationToken)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            return await dbContext.CalendarEvent
                .Where(e => !e.NotificationSent &&
                           e.ReminderDateTime.HasValue &&
                           e.ReminderDateTime < now)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<AppUser>> GetSubscribedUsersAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            return await dbContext.Users
                .AsNoTracking()
                .Where(u => u.ReceiveNotifications)
                .ToListAsync(cancellationToken);
        }

        public async Task MarkEventAsNotifiedAsync(CalendarEvent calendarEvent, CancellationToken cancellationToken)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var eventToUpdate = await dbContext.CalendarEvent.FindAsync(new object[] { calendarEvent.Id }, cancellationToken);
            if (eventToUpdate != null)
            {
                eventToUpdate.NotificationSent = true;
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }
} 
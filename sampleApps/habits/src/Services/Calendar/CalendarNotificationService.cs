public class CalendarNotificationService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<CalendarNotificationService> _logger;

    public CalendarNotificationService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<CalendarNotificationService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<ICalendarNotificationProcessor>();

                var now = DateTime.UtcNow;
                var upcomingEvents = await processor.GetUpcomingEventsAsync(now, stoppingToken);

                if (upcomingEvents.Any())
                {
                    var subscribedUsers = await processor.GetSubscribedUsersAsync(stoppingToken);

                    _logger.LogInformation("Starting calendar notification check at {Time}", DateTime.Now);
                    _logger.LogDebug("Found {Count} upcoming events requiring notifications", upcomingEvents.Count());

                    foreach (var evt in upcomingEvents)
                    {
                        try
                        {
                            await processor.SendNotificationsAsync(evt, subscribedUsers);
                            await processor.MarkEventAsNotifiedAsync(evt, stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to send notifications for event {EventId}", evt.Id);
                            continue;
                        }
                    }

                    _logger.LogInformation("Found {Count} subscribed users for notifications", subscribedUsers.Count());
                    _logger.LogInformation("Completed notification cycle. Processed {EventCount} events for {UserCount} users",
                        upcomingEvents.Count(), subscribedUsers.Count());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error during notification processing cycle at {Time}", DateTime.Now);
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
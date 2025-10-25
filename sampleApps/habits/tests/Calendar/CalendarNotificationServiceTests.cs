using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using habits.Data.Models;
using habits.Services.Calendar;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc;
using Services.Calendar;
using Microsoft.AspNetCore.Identity.UI.Services;
using habits.Services.Notifications;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;

namespace habits.Tests.Services.Calendar
{
    [TestClass]
    public class CalendarNotificationServiceTests
    {
        private Mock<ICalendarNotificationProcessor> _processorMock;
        private Mock<ILogger<CalendarNotificationService>> _loggerMock;
        private Mock<IServiceScopeFactory> _serviceScopeFactory;
        private Mock<IServiceScope> _serviceScopeMock;
        private Mock<IServiceProvider> _serviceProviderMock;
        private CancellationTokenSource _cts;

        [TestInitialize]
        public void TestInitialize()
        {
            _processorMock = new Mock<ICalendarNotificationProcessor>();
            _loggerMock = new Mock<ILogger<CalendarNotificationService>>();
            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _serviceScopeMock = new Mock<IServiceScope>();
            _serviceProviderMock = new Mock<IServiceProvider>();
            _cts = new CancellationTokenSource();

            // Setup service scope
            _serviceProviderMock
                .Setup(x => x.GetService(typeof(ICalendarNotificationProcessor)))
                .Returns(_processorMock.Object);
            _serviceScopeMock
                .Setup(x => x.ServiceProvider)
                .Returns(_serviceProviderMock.Object);
            _serviceScopeFactory
                .Setup(x => x.CreateScope())
                .Returns(_serviceScopeMock.Object);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _cts?.Dispose();
        }

        [TestMethod]
        public async Task ExecuteAsync_NoUpcomingEvents_DoesNotProcessNotifications()
        {
            // Arrange
            _processorMock.Setup(x => x.GetUpcomingEventsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<CalendarEvent>());

            var service = new CalendarNotificationService(_serviceScopeFactory.Object, _loggerMock.Object);

            // Act
            _cts.CancelAfter(TimeSpan.FromMilliseconds(100));
            await service.StartAsync(_cts.Token);

            // Assert
            _processorMock.Verify(x => x.GetSubscribedUsersAsync(It.IsAny<CancellationToken>()), Times.Never);
            _processorMock.Verify(x => x.SendNotificationsAsync(It.IsAny<CalendarEvent>(), It.IsAny<IEnumerable<AppUser>>()), Times.Never);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithUpcomingEvents_ProcessesNotifications()
        {
            // Arrange
            var events = new List<CalendarEvent>
            {
                new CalendarEvent { Id = 1, Title = "Test Event" }
            };
            var users = new List<AppUser>
            {
                new AppUser { Email = "test@example.com" }
            };

            _processorMock.Setup(x => x.GetUpcomingEventsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(events);
            _processorMock.Setup(x => x.GetSubscribedUsersAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(users);

            var service = new CalendarNotificationService(_serviceScopeFactory.Object, _loggerMock.Object);

            // Act
            _cts.CancelAfter(TimeSpan.FromMilliseconds(100));
            await service.StartAsync(_cts.Token);

            // Assert
            _processorMock.Verify(x => x.SendNotificationsAsync(
                It.Is<CalendarEvent>(e => e.Id == events[0].Id),
                It.Is<IEnumerable<AppUser>>(u => u.First().Email == users[0].Email)), 
                Times.Once);
            _processorMock.Verify(x => x.MarkEventAsNotifiedAsync(
                It.Is<CalendarEvent>(e => e.Id == events[0].Id), 
                It.IsAny<CancellationToken>()), 
                Times.Once);
        }

        [TestMethod]
        public async Task ExecuteAsync_NotificationFails_ContinuesProcessing()
        {
            // Arrange
            var events = new List<CalendarEvent>
            {
                new CalendarEvent { Id = 1, Title = "Test Event 1" },
                new CalendarEvent { Id = 2, Title = "Test Event 2" }
            };
            var users = new List<AppUser>
            {
                new AppUser { Email = "test@example.com" }
            };

            _processorMock.Setup(x => x.GetUpcomingEventsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(events);
            _processorMock.Setup(x => x.GetSubscribedUsersAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(users);
            _processorMock.Setup(x => x.SendNotificationsAsync(
                It.Is<CalendarEvent>(e => e.Id == events[0].Id),
                It.IsAny<IEnumerable<AppUser>>()))
                .ThrowsAsync(new Exception("Test exception"));

            var service = new CalendarNotificationService(_serviceScopeFactory.Object, _loggerMock.Object);

            // Act
            _cts.CancelAfter(TimeSpan.FromMilliseconds(100));
            await service.StartAsync(_cts.Token);

            // Assert
            _processorMock.Verify(x => x.SendNotificationsAsync(
                It.Is<CalendarEvent>(e => e.Id == events[1].Id),
                It.Is<IEnumerable<AppUser>>(u => u.First().Email == users[0].Email)), 
                Times.Once);
            _processorMock.Verify(x => x.MarkEventAsNotifiedAsync(
                It.Is<CalendarEvent>(e => e.Id == events[1].Id), 
                It.IsAny<CancellationToken>()), 
                Times.Once);
        }

        [TestMethod]
        public async Task ExecuteAsync_ProcessorThrowsException_LogsErrorAndContinues()
        {
            // Arrange
            _processorMock.Setup(x => x.GetUpcomingEventsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Test critical error"));

            var service = new CalendarNotificationService(_serviceScopeFactory.Object, _loggerMock.Object);

            // Act
            _cts.CancelAfter(TimeSpan.FromMilliseconds(100));
            await service.StartAsync(_cts.Token);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.AtLeastOnce);
        }

        [TestMethod]
        public async Task RenderEmailTemplateAsync_ValidEvent_RendersCorrectTemplate()
        {
            // Arrange
            var calendarEvent = new CalendarEvent
            {
                Id = 1,
                Title = "Test Event",
                StartDate = DateTime.Parse("2025-08-02"),
                StartTime = TimeSpan.Parse("11:00:00"),
                Description = "ZMS"
            };

            var mockViewEngine = new Mock<IRazorViewEngine>();
            var mockTempDataProvider = new Mock<ITempDataProvider>();
            var mockView = new Mock<IView>();

            // Setup service scope with HttpContext culture
            var mockServiceProvider = new Mock<IServiceProvider>();
            var mockServiceScope = new Mock<IServiceScope>();
            mockServiceScope.Setup(x => x.ServiceProvider).Returns(mockServiceProvider.Object);
            _serviceScopeFactory.Setup(x => x.CreateScope()).Returns(mockServiceScope.Object);

            mockView.Setup(x => x.RenderAsync(It.IsAny<ViewContext>()))
                .Callback<ViewContext>(context =>
                {
                    var writer = context.Writer;
                    writer.Write(GetExpectedHtmlTemplate(calendarEvent));
                })
                .Returns(Task.CompletedTask);

            var viewResult = ViewEngineResult.Found("Emails/EventReminder", mockView.Object);
            mockViewEngine.Setup(x => x.FindView(It.IsAny<ActionContext>(), "Emails/EventReminder", false))
                .Returns(viewResult);

            var processor = new CalendarNotificationProcessor(
                _serviceScopeFactory.Object,
                mockViewEngine.Object,
                mockTempDataProvider.Object,
                Mock.Of<IEmailSender>(),
                Mock.Of<IFcmNotificationService>(),
                Mock.Of<ILogger<CalendarNotificationProcessor>>()
            );

            // Act
            var result = await processor.RenderEmailTemplateAsync(calendarEvent);

            // Assert
            Assert.IsNotNull(result);
            StringAssert.Contains(result, calendarEvent.Title);
            StringAssert.Contains(result, calendarEvent.StartDate.ToString("MM/dd/yy", CultureInfo.InvariantCulture));
            StringAssert.Contains(result, calendarEvent.StartTime?.ToString());
            StringAssert.Contains(result, calendarEvent.Description);
            mockViewEngine.Verify(x => x.FindView(It.IsAny<ActionContext>(), "Emails/EventReminder", false), Times.Once);
            mockView.Verify(x => x.RenderAsync(It.IsAny<ViewContext>()), Times.Once);
        }

        private string GetExpectedHtmlTemplate(CalendarEvent evt)
        {
            return $@"
                <div class='reminder-card'>
                    <div class='reminder-header'>
                        <h1 class='reminder-title'>Reminder</h1>
                    </div>
                    <div class='reminder-subtitle'>
                        Upcoming Habits Calendar Event
                    </div>
                    <div class='event-details'>
                        <div class='detail-row'>
                            <span class='detail-icon'>⭐</span>
                            <span>{evt.Title}</span>
                        </div>
                        <div class='detail-row'>
                            <span class='detail-icon'>📅</span>
                            <span>{evt.StartDate.ToString("MM/dd/yy", CultureInfo.InvariantCulture)}</span>
                        </div>
                        <div class='detail-row'>
                            <span class='detail-icon'>⏰</span>
                            <span>{evt.StartTime?.ToString() ?? "All Day"}</span>
                        </div>
                        <div class='detail-row'>
                            <span class='detail-icon'>📍</span>
                            <span>{evt.Description}</span>
                        </div>
                    </div>
                </div>";
        }
    }
}
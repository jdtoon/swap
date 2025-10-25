using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using habits.Data;
using habits.Data.Models;
using habits.Dtos;
using habits.Services.Calendar;
using System.Globalization;

namespace habits.Tests.Services.Calendar
{
    [TestClass]
    public class CalendarServiceTests
    {
        private Mock<ILogger<CalendarService>> _loggerMock;
        private ApplicationDbContext _context;
        private CalendarService _service;

        [TestInitialize]
        public void TestInitialize()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .EnableSensitiveDataLogging()
                .Options;

            _context = new ApplicationDbContext(options)
            {
                TaskUser = new Mock<DbSet<TaskUser>>().Object,
                TaskList = new Mock<DbSet<TaskList>>().Object,
                TaskListItem = new Mock<DbSet<TaskListItem>>().Object,
                CalendarEvent = new Mock<DbSet<CalendarEvent>>().Object,
                MealPlan = new Mock<DbSet<MealPlan>>().Object,
                CalendarEventTypes = new Mock<DbSet<CalendarEventType>>().Object,
                Document = new Mock<DbSet<Document>>().Object
            };

            _loggerMock = new Mock<ILogger<CalendarService>>();
            _service = new CalendarService(_context, _loggerMock.Object);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [TestMethod]
        public void GetMonthData_ReturnsCorrectNumberOfDays()
        {
            // Arrange
            var testDate = new DateTime(2024, 2, 1); // February 2024 (leap year)

            // Act
            var result = _service.GetMonthData(testDate);

            // Assert
            Assert.AreEqual(29, result.DaysInMonth); // February 2024 has 29 days
            Assert.AreEqual(29, result.Days.Count);
            Assert.AreEqual("February", result.MonthName);
            Assert.AreEqual(2024, result.Year);
            Assert.AreEqual(4, result.FirstDayOfWeek); // Feb 1, 2024 is a Thursday
        }

        [TestMethod]
        public void GetDayData_ReturnsCorrectIslamicDate()
        {
            // Arrange
            var testDate = new DateTime(2024, 2, 1);

            // Act
            var result = _service.GetDayData(testDate);

            // Assert
            Assert.IsNotNull(result.IslamicDate);
            Assert.AreEqual(testDate.Day, result.DayNumber);
            Assert.IsTrue(result.IsCurrentMonth);
        }

        [TestMethod]
        public async Task AddEventAsync_CreatesNewEvent()
        {
            // Arrange
            var eventType = new CalendarEventType { Id = 1, Name = "Test Type" };
            _context.CalendarEventTypes.Add(eventType);
            await _context.SaveChangesAsync();

            var dto = new CreateCalendarEventDto
            {
                Title = "Test Event",
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(1),
                Description = "Test Description",
                CalendarEventTypeId = 1,
                IsFullDay = true
            };

            // Act
            var result = await _service.AddEventAsync(dto);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(dto.Title, result.Title);
            Assert.AreEqual(dto.StartDate, result.StartDate);
            Assert.AreEqual(dto.EndDate, result.EndDate);
            Assert.AreEqual(dto.Description, result.Description);
            Assert.AreEqual(dto.CalendarEventTypeId, result.CalendarEventTypeId);

            var savedEvent = await _context.CalendarEvent.FindAsync(result.Id);
            Assert.IsNotNull(savedEvent);
            Assert.AreEqual(dto.Title, savedEvent.Title);
        }

        [TestMethod]
        public void GetUpcomingEvents_ReturnsCorrectNumberOfEvents()
        {
            // Arrange
            var eventType = new CalendarEventType { Id = 1, Name = "Test Type" };
            _context.CalendarEventTypes.Add(eventType);

            var events = new List<CalendarEvent>
            {
                new CalendarEvent { Title = "Event 1", StartDate = DateTime.Today.AddDays(1), EventType = eventType },
                new CalendarEvent { Title = "Event 2", StartDate = DateTime.Today.AddDays(2), EventType = eventType },
                new CalendarEvent { Title = "Event 3", StartDate = DateTime.Today.AddDays(3), EventType = eventType }
            };
            _context.CalendarEvent.AddRange(events);
            _context.SaveChanges();

            // Act
            var result = _service.GetUpcomingEvents();

            // Assert
            Assert.AreEqual(2, result.Count); // Should only return 2 upcoming events
            Assert.AreEqual("Event 1", result[0].Title);
            Assert.AreEqual("Event 2", result[1].Title);
        }

        [TestMethod]
        public async Task UpdateEventAsync_UpdatesExistingEvent()
        {
            // Arrange
            var eventType = new CalendarEventType { Id = 1, Name = "Test Type" };
            _context.CalendarEventTypes.Add(eventType);

            var existingEvent = new CalendarEvent
            {
                Title = "Original Title",
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(1),
                Description = "Original Description",
                CalendarEventTypeId = 1,
                EventType = eventType
            };
            _context.CalendarEvent.Add(existingEvent);
            await _context.SaveChangesAsync();

            var updateDto = new UpdateCalendarEventDto
            {
                Title = "Updated Title",
                EndDate = DateTime.Today.AddDays(2),
                Description = "Updated Description",
                CalendarEventTypeId = 1,
                IsFullDay = true
            };

            // Act
            var result = await _service.UpdateEventAsync(existingEvent.Id, updateDto);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(updateDto.Title, result.Title);
            Assert.AreEqual(updateDto.Description, result.Description);

            var updatedEvent = await _context.CalendarEvent.FindAsync(existingEvent.Id);
            Assert.IsNotNull(updatedEvent);
            Assert.AreEqual(updateDto.Title, updatedEvent.Title);
        }

        [TestMethod]
        public async Task DeleteEventAsync_RemovesEvent()
        {
            // Arrange
            var eventType = new CalendarEventType { Id = 1, Name = "Test Type" };
            _context.CalendarEventTypes.Add(eventType);

            var existingEvent = new CalendarEvent
            {
                Title = "Test Event",
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(1),
                CalendarEventTypeId = 1,
                EventType = eventType
            };
            _context.CalendarEvent.Add(existingEvent);
            await _context.SaveChangesAsync();

            // Act
            await _service.DeleteEventAsync(existingEvent.Id);

            // Assert
            var deletedEvent = await _context.CalendarEvent.FindAsync(existingEvent.Id);
            Assert.IsNull(deletedEvent);
        }

        [TestMethod]
        [ExpectedException(typeof(KeyNotFoundException))]
        public async Task DeleteEventAsync_ThrowsException_WhenEventNotFound()
        {
            // Act
            await _service.DeleteEventAsync(999);
        }
    }
} 
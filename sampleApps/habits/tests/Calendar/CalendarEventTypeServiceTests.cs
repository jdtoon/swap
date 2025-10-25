using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using habits.Data;
using habits.Data.Models;
using habits.Services.Calendar;

namespace habits.Tests.Services.Calendar
{
    [TestClass]
    public class CalendarEventTypeServiceTests
    {
        private Mock<ILogger<CalendarEventTypeService>> _loggerMock;
        private ApplicationDbContext _context;
        private CalendarEventTypeService _service;

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

            _loggerMock = new Mock<ILogger<CalendarEventTypeService>>();
            _service = new CalendarEventTypeService(_context, _loggerMock.Object);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [TestMethod]
        public void GetEventTypes_WithNoSearch_ReturnsAllTypesPaged()
        {
            // Arrange
            var types = new List<CalendarEventType>
            {
                new CalendarEventType { Name = "Type 1", Color = "#000000", IconPath = "path1" },
                new CalendarEventType { Name = "Type 2", Color = "#111111", IconPath = "path2" },
                new CalendarEventType { Name = "Type 3", Color = "#222222", IconPath = "path3" }
            };
            _context.CalendarEventTypes.AddRange(types);
            _context.SaveChanges();

            // Act
            var result = _service.GetEventTypes(search: "", page: 1, pageSize: 2);

            // Assert
            Assert.AreEqual(3, result.TotalRecords);
            Assert.AreEqual(2, result.Data.Count);
            Assert.IsTrue(result.HasMore);
            Assert.AreEqual(1, result.CurrentPage);
        }

        [TestMethod]
        public void GetEventTypes_WithSearch_ReturnsFilteredResults()
        {
            // Arrange
            var types = new List<CalendarEventType>
            {
                new CalendarEventType { Name = "Meeting Type", Color = "#000000", IconPath = "path1" },
                new CalendarEventType { Name = "Holiday Type", Color = "#111111", IconPath = "path2" },
                new CalendarEventType { Name = "Meeting Special", Color = "#222222", IconPath = "path3" }
            };
            _context.CalendarEventTypes.AddRange(types);
            _context.SaveChanges();

            // Act
            var result = _service.GetEventTypes(search: "meeting", page: 1, pageSize: 10);

            // Assert
            Assert.AreEqual(2, result.TotalRecords);
            Assert.AreEqual(2, result.Data.Count);
            Assert.IsFalse(result.HasMore);
        }

        [TestMethod]
        public async Task AddEventTypeAsync_CreatesNewEventType()
        {
            // Arrange
            string name = "Test Type";
            string color = "#FF0000";
            string iconPath = "/test/path";

            // Act
            var result = await _service.AddEventTypeAsync(name, color, iconPath);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(name, result.Name);
            Assert.AreEqual(color, result.Color);
            Assert.AreEqual("test/path", result.IconPath); // Note: Service removes leading slash

            var savedType = await _context.CalendarEventTypes.FirstOrDefaultAsync();
            Assert.IsNotNull(savedType);
            Assert.AreEqual(name, savedType.Name);
        }

        [TestMethod]
        public async Task UpdateEventTypeAsync_UpdatesExistingType()
        {
            // Arrange
            var eventType = new CalendarEventType
            {
                Name = "Original Name",
                Color = "#000000",
                IconPath = "original/path"
            };
            _context.CalendarEventTypes.Add(eventType);
            await _context.SaveChangesAsync();

            string newName = "Updated Name";
            string newColor = "#FF0000";
            string newIconPath = "new/path";

            // Act
            var result = await _service.UpdateEventTypeAsync(eventType.Id, newName, newColor, newIconPath);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(newName, result.Name);
            Assert.AreEqual(newColor, result.Color);
            Assert.AreEqual(newIconPath, result.IconPath);

            var updatedType = await _context.CalendarEventTypes.FindAsync(eventType.Id);
            Assert.IsNotNull(updatedType);
            Assert.AreEqual(newName, updatedType.Name);
        }

        [TestMethod]
        public async Task UpdateEventTypeAsync_ReturnsNull_WhenTypeNotFound()
        {
            // Act
            var result = await _service.UpdateEventTypeAsync(999, "Test", "#000000", "path");

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task DeleteEventTypeAsync_RemovesEventType()
        {
            // Arrange
            var eventType = new CalendarEventType
            {
                Name = "Test Type",
                Color = "#000000",
                IconPath = "path"
            };
            _context.CalendarEventTypes.Add(eventType);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.DeleteEventTypeAsync(eventType.Id);

            // Assert
            Assert.IsTrue(result);
            var deletedType = await _context.CalendarEventTypes.FindAsync(eventType.Id);
            Assert.IsNull(deletedType);
        }

        [TestMethod]
        public async Task DeleteEventTypeAsync_ReturnsFalse_WhenTypeNotFound()
        {
            // Act
            var result = await _service.DeleteEventTypeAsync(999);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void GetAllEventTypes_ReturnsAllTypesOrdered()
        {
            // Arrange
            var types = new List<CalendarEventType>
            {
                new CalendarEventType { Name = "Type C", Color = "#000000", IconPath = "path1" },
                new CalendarEventType { Name = "Type A", Color = "#111111", IconPath = "path2" },
                new CalendarEventType { Name = "Type B", Color = "#222222", IconPath = "path3" }
            };
            _context.CalendarEventTypes.AddRange(types);
            _context.SaveChanges();

            // Act
            var result = _service.GetAllEventTypes();

            // Assert
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual("Type A", result[0].Name);
            Assert.AreEqual("Type B", result[1].Name);
            Assert.AreEqual("Type C", result[2].Name);
        }
    }
} 
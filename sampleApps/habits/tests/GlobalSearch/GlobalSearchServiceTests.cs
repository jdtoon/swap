using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using habits.Data;
using habits.Data.Models;

namespace habits.Tests.Services.GlobalSearch
{
    [TestClass]
    public class GlobalSearchServiceTests
    {
        private ApplicationDbContext _context;
        private GlobalSearchService _service;
        private AppUser _testUser;

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
            _context.Database.EnsureCreated();

            // Setup test user
            _testUser = new AppUser
            {
                Id = "testuser1",
                Name = "John",
                Surname = "Doe",
                Email = "john@example.com"
            };
            _context.Users.Add(_testUser);
            _context.SaveChanges();

            _service = new GlobalSearchService(_context);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [TestMethod]
        public async Task Search_EmptyTerm_ReturnsEmptyResults()
        {
            // Act
            var result = await _service.Search("");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("", result.SearchTerm);
            Assert.AreEqual(0, result.Results.Count);
        }

        [TestMethod]
        public async Task Search_FindsCalendarEvents()
        {
            // Arrange
            var eventType = new CalendarEventType { Name = "Test Type" };
            var calendarEvent = new CalendarEvent
            {
                Title = "Test Event",
                StartDate = DateTime.Today.AddDays(1),
                EventType = eventType
            };
            _context.CalendarEventTypes.Add(eventType);
            _context.CalendarEvent.Add(calendarEvent);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.Search("test");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Results.Count);
            var searchResult = result.Results[0];
            Assert.AreEqual("Event", searchResult.Type);
            Assert.AreEqual("Test Event", searchResult.Title);
            Assert.IsTrue(searchResult.NavigateUrl.Contains(calendarEvent.Id.ToString()));
        }

        [TestMethod]
        public async Task Search_FindsTaskLists()
        {
            // Arrange
            var taskList = new TaskList
            {
                Name = "Test List",
                CreatedBy = _testUser,
                UpdatedBy = _testUser,
                CreatedDateUTC = DateTime.Today,
            };
            _context.TaskList.Add(taskList);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.Search("test");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Results.Count);
            var searchResult = result.Results[0];
            Assert.AreEqual("List", searchResult.Type);
            Assert.AreEqual("Test List", searchResult.Title);
            Assert.IsTrue(searchResult.NavigateUrl.Contains(taskList.Id.ToString()));
        }

        [TestMethod]
        public async Task Search_FindsTaskItems()
        {
            // Arrange
            var taskList = new TaskList
            {
                Name = "List",
                CreatedBy = _testUser,
                UpdatedBy = _testUser,
                CreatedDateUTC = DateTime.Today
            };
            var taskItem = new TaskListItem
            {
                Task = "Test Item",
                TaskList = taskList,
                CreatedBy = _testUser,
                UpdatedBy = _testUser,
                CreatedDateUTC = DateTime.Today
            };
            _context.TaskList.Add(taskList);
            _context.TaskListItem.Add(taskItem);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.Search("test");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Results.Count);
            var searchResult = result.Results[0];
            Assert.AreEqual("To Do", searchResult.Type);
            Assert.AreEqual("Test Item", searchResult.Title);
            Assert.IsTrue(searchResult.NavigateUrl.Contains(taskList.Id.ToString()));
        }

        [TestMethod]
        public async Task Search_FindsDocuments()
        {
            // Arrange
            var document = new Document
            {
                Name = "Test Document.pdf",
            };
            _context.Document.Add(document);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.Search("test");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Results.Count);
            var searchResult = result.Results[0];
            Assert.AreEqual("File", searchResult.Type);
            Assert.AreEqual("Test Document.pdf", searchResult.Title);
            Assert.IsTrue(searchResult.NavigateUrl.Contains(Uri.EscapeDataString("Test Document.pdf")));
        }

        [TestMethod]
        public async Task Search_FindsMembers()
        {
            // Arrange
            var member = new AppUser
            {
                Name = "Test",
                Surname = "User",
                Email = "test@example.com"
            };
            _context.Users.Add(member);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.Search("test");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Results.Count);
            var searchResult = result.Results[0];
            Assert.AreEqual("Member", searchResult.Type);
            Assert.AreEqual("Test User", searchResult.Title);
            Assert.IsTrue(searchResult.NavigateUrl.Contains(Uri.EscapeDataString("Test User")));
        }

        [TestMethod]
        public async Task Search_OrdersByPriorityAndTitle()
        {
            // Arrange
            var eventType = new CalendarEventType { Name = "Type" };
            var events = new[]
            {
                new CalendarEvent { Title = "B Test Event", StartDate = DateTime.Today.AddDays(1), EventType = eventType },
                new CalendarEvent { Title = "A Test Event", StartDate = DateTime.Today.AddDays(1), EventType = eventType }
            };
            _context.CalendarEventTypes.Add(eventType);
            _context.CalendarEvent.AddRange(events);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.Search("test");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Results.Count);
            Assert.AreEqual("A Test Event", result.Results[0].Title);
            Assert.AreEqual("B Test Event", result.Results[1].Title);
        }
    }
} 
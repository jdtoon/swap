using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using habits.Data;
using habits.Data.Models;
using habits.Dtos;
using habits.Services.Tasks;
using System.Security.Claims;

namespace habits.Tests.Services.Tasks
{
    [TestClass]
    public class TaskServiceTests
    {
        private ApplicationDbContext _context;
        private Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private TaskService _service;
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
                MealPlan = new Mock<DbSet<MealPlan>>().Object,
                CalendarEvent = new Mock<DbSet<CalendarEvent>>().Object,
                CalendarEventTypes = new Mock<DbSet<CalendarEventType>>().Object,
                Document = new Mock<DbSet<Document>>().Object
            };
            _context.Database.EnsureCreated();

            // Setup test user
            _testUser = new AppUser
            {
                Id = "testuser1",
                Email = "test@example.com",
                UserName = "test@example.com"
            };
            _context.Users.Add(_testUser);
            _context.SaveChanges();

            // Setup HttpContextAccessor mock
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            var context = new DefaultHttpContext();
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "test@example.com")
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            context.User = principal;
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);

            _service = new TaskService(_context, _httpContextAccessorMock.Object);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [TestMethod]
        public void GetTaskLists_ReturnsAllLists()
        {
            // Arrange
            var lists = new List<TaskList>
            {
                new TaskList { Name = "List 1", Order = 1, CreatedBy = _testUser, UpdatedBy = _testUser, CreatedDateUTC = DateTime.Today },
                new TaskList { Name = "List 2", Order = 2, CreatedBy = _testUser, UpdatedBy = _testUser, CreatedDateUTC = DateTime.Today }
            };
            _context.TaskList.AddRange(lists);
            _context.SaveChanges();

            // Act
            var result = _service.GetTaskLists();

            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("List 1", result[0].Name);
            Assert.AreEqual("List 2", result[1].Name);
        }

        [TestMethod]
        public void GetTaskListById_ValidId_ReturnsList()
        {
            // Arrange
            var taskList = new TaskList 
            { 
                Name = "Test List", 
                Description = "Test Description",
                Order = 1,
                CreatedBy = _testUser,
                UpdatedBy = _testUser,
                CreatedDateUTC = DateTime.Today
            };
            _context.TaskList.Add(taskList);
            _context.SaveChanges();

            // Act
            var result = _service.GetTaskListById(taskList.Id);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Test List", result.Name);
            Assert.AreEqual("Test Description", result.Description);
        }

        [TestMethod]
        public void GetTaskListById_InvalidId_ReturnsEmptyDto()
        {
            // Act
            var result = _service.GetTaskListById(999);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNull(result.Name);
        }

        [TestMethod]
        public void CreateTaskList_ValidData_CreatesNewList()
        {
            // Arrange
            var dto = new TaskListDto
            {
                Name = "New List",
                Description = "New Description"
            };

            // Act
            var result = _service.CreateTaskList(dto);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(dto.Name, result.Name);
            Assert.AreEqual(dto.Description, result.Description);

            var savedList = _context.TaskList.FirstOrDefault();
            Assert.IsNotNull(savedList);
            Assert.AreEqual(dto.Name, savedList.Name);
            Assert.AreEqual(_testUser.Id, savedList.CreatedBy.Id);
            Assert.AreEqual(1, savedList.Order); // Should be first item
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void CreateTaskList_EmptyName_ThrowsException()
        {
            // Arrange
            var dto = new TaskListDto { Name = "" };

            // Act
            _service.CreateTaskList(dto);
        }

        [TestMethod]
        public void UpdateTaskList_ValidData_UpdatesList()
        {
            // Arrange
            var taskList = new TaskList
            {
                Name = "Original Name",
                Description = "Original Description",
                Order = 1,
                CreatedBy = _testUser,
                UpdatedBy = _testUser,
                CreatedDateUTC = DateTime.Today
            };
            _context.TaskList.Add(taskList);
            _context.SaveChanges();

            // Act
            var result = _service.UpdateTaskList(taskList.Id, "Updated Name", "Updated Description");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Updated Name", result.Name);
            Assert.AreEqual("Updated Description", result.Description);

            var updatedList = _context.TaskList.Find(taskList.Id);
            Assert.IsNotNull(updatedList);
            Assert.AreEqual("Updated Name", updatedList.Name);
            Assert.AreEqual(_testUser.Id, updatedList.UpdatedBy.Id);
        }

        [TestMethod]
        public void UpdateTaskList_InvalidId_ReturnsEmptyDto()
        {
            // Act
            var result = _service.UpdateTaskList(999, "Name", "Description");

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNull(result.Name);
        }

        [TestMethod]
        public void DeleteTaskList_ValidId_DeletesList()
        {
            // Arrange
            var taskList = new TaskList
            {
                Name = "Test List",
                Order = 1,
                CreatedBy = _testUser,
                UpdatedBy = _testUser,
                CreatedDateUTC = DateTime.Today
            };
            _context.TaskList.Add(taskList);
            _context.SaveChanges();

            // Act
            var result = _service.DeleteTaskList(taskList.Id);

            // Assert
            Assert.IsTrue(result);
            var deletedList = _context.TaskList.Find(taskList.Id);
            Assert.IsNull(deletedList);
        }

        [TestMethod]
        public void DeleteTaskList_InvalidId_ReturnsFalse()
        {
            // Act
            var result = _service.DeleteTaskList(999);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task UpdateTaskListOrderAsync_ValidMove_UpdatesOrder()
        {
            // Arrange
            var lists = new List<TaskList>
            {
                new TaskList { Name = "List 1", Order = 1, CreatedBy = _testUser, UpdatedBy = _testUser, CreatedDateUTC = DateTime.Today },
                new TaskList { Name = "List 2", Order = 2, CreatedBy = _testUser, UpdatedBy = _testUser , CreatedDateUTC = DateTime.Today},
                new TaskList { Name = "List 3", Order = 3, CreatedBy = _testUser, UpdatedBy = _testUser , CreatedDateUTC = DateTime.Today}
            };
            _context.TaskList.AddRange(lists);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.UpdateTaskListOrderAsync(lists[0].Id, 3);

            // Assert
            Assert.IsTrue(result);
            var movedList = await _context.TaskList.FindAsync(lists[0].Id);
            Assert.AreEqual(3, movedList!.Order);

            // Verify other lists were reordered
            var list2 = await _context.TaskList.FindAsync(lists[1].Id);
            var list3 = await _context.TaskList.FindAsync(lists[2].Id);
            Assert.AreEqual(1, list2!.Order);
            Assert.AreEqual(2, list3!.Order);
        }

        [TestMethod]
        public async Task UpdateTaskListOrderAsync_InvalidId_ReturnsFalse()
        {
            // Act
            var result = await _service.UpdateTaskListOrderAsync(999, 1);

            // Assert
            Assert.IsFalse(result);
        }
    }
} 
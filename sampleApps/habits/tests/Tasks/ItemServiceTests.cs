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
    public class ItemServiceTests
    {
        private ApplicationDbContext _context;
        private Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private ItemService _service;
        private AppUser _testUser;
        private TaskList _testTaskList;

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

            // Setup test task list
            _testTaskList = new TaskList
            {
                Name = "Test List",
                CreatedBy = _testUser,
                UpdatedBy = _testUser,
                CreatedDateUTC = DateTime.UtcNow,
            };
            _context.TaskList.Add(_testTaskList);
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

            _service = new ItemService(_context, _httpContextAccessorMock.Object);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [TestMethod]
        public void GetItems_ReturnsItemsForTaskList()
        {
            // Arrange
            var items = new List<TaskListItem>
            {
                new TaskListItem { Task = "Item 1", Order = 1, TaskList = _testTaskList, CreatedBy = _testUser, UpdatedBy = _testUser, CreatedDateUTC = DateTime.Today },
                new TaskListItem { Task = "Item 2", Order = 2, TaskList = _testTaskList, CreatedBy = _testUser, UpdatedBy = _testUser, CreatedDateUTC = DateTime.Today }
            };
            _context.TaskListItem.AddRange(items);
            _context.SaveChanges();

            // Act
            var result = _service.GetItems(_testTaskList.Id);

            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("Item 1", result[0].Task);
            Assert.AreEqual("Item 2", result[1].Task);
        }

        [TestMethod]
        public void GetItem_ValidId_ReturnsItem()
        {
            // Arrange
            var item = new TaskListItem
            {
                Task = "Test Item",
                Order = 1,
                TaskList = _testTaskList,
                CreatedBy = _testUser,
                UpdatedBy = _testUser,
                CreatedDateUTC = DateTime.Today
            };
            _context.TaskListItem.Add(item);
            _context.SaveChanges();

            // Act
            var result = _service.GetItem(item.Id);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Test Item", result.Task);
            Assert.AreEqual(_testTaskList.Id, result.TaskListId);
        }

        [TestMethod]
        public void GetItem_InvalidId_ReturnsEmptyDto()
        {
            // Act
            var result = _service.GetItem(999);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNull(result.Task);
        }

        [TestMethod]
        public void CreateItem_ValidData_CreatesNewItem()
        {
            // Arrange
            var dto = new ItemDto
            {
                Task = "New Item",
                TaskListId = _testTaskList.Id
            };

            // Act
            var result = _service.CreateItem(dto);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(dto.Task, result.Task);
            Assert.AreEqual(_testTaskList.Id, result.TaskListId);

            var savedItem = _context.TaskListItem.FirstOrDefault();
            Assert.IsNotNull(savedItem);
            Assert.AreEqual(dto.Task, savedItem.Task);
            Assert.AreEqual(_testUser.Id, savedItem.CreatedBy.Id);
            Assert.AreEqual(1, savedItem.Order); // Should be first item
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void CreateItem_EmptyTask_ThrowsException()
        {
            // Arrange
            var dto = new ItemDto { Task = "", TaskListId = _testTaskList.Id };

            // Act
            _service.CreateItem(dto);
        }

        [TestMethod]
        public void DeleteItem_ValidId_DeletesItem()
        {
            // Arrange
            var item = new TaskListItem
            {
                Task = "Test Item",
                Order = 1,
                TaskList = _testTaskList,
                CreatedBy = _testUser,
                UpdatedBy = _testUser,
                CreatedDateUTC = DateTime.Today
            };
            _context.TaskListItem.Add(item);
            _context.SaveChanges();

            // Act
            var result = _service.DeleteItem(item.Id);

            // Assert
            Assert.IsTrue(result);
            var deletedItem = _context.TaskListItem.Find(item.Id);
            Assert.IsNull(deletedItem);
        }

        [TestMethod]
        public void DeleteItem_InvalidId_ReturnsFalse()
        {
            // Act
            var result = _service.DeleteItem(999);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void SetAsHeader_ValidId_UpdatesHeader()
        {
            // Arrange
            var item = new TaskListItem
            {
                Task = "Test Item",
                IsHeader = false,
                TaskList = _testTaskList,
                CreatedBy = _testUser,
                UpdatedBy = _testUser,
                CreatedDateUTC = DateTime.Today
            };
            _context.TaskListItem.Add(item);
            _context.SaveChanges();

            // Act
            var result = _service.SetAsHeader(item.Id, true);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsHeader);
            Assert.IsFalse(result.IsCompleted); // Headers can't be completed

            var updatedItem = _context.TaskListItem.Find(item.Id);
            Assert.IsNotNull(updatedItem);
            Assert.IsTrue(updatedItem.IsHeader);
            Assert.IsFalse(updatedItem.IsCompleted);
        }

        [TestMethod]
        public void SetCompleted_ValidId_UpdatesCompletionStatus()
        {
            // Arrange
            var item = new TaskListItem
            {
                Task = "Test Item",
                IsCompleted = false,
                TaskList = _testTaskList,
                CreatedBy = _testUser,
                UpdatedBy = _testUser,
                CreatedDateUTC = DateTime.Today
            };
            _context.TaskListItem.Add(item);
            _context.SaveChanges();

            // Act
            var result = _service.SetCompleted(item.Id, true);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsCompleted);

            var updatedItem = _context.TaskListItem.Find(item.Id);
            Assert.IsNotNull(updatedItem);
            Assert.IsTrue(updatedItem.IsCompleted);
        }

        [TestMethod]
        public void UpdateItem_ValidData_UpdatesItem()
        {
            // Arrange
            var item = new TaskListItem
            {
                Task = "Original Task",
                IsHeader = false,
                TaskList = _testTaskList,
                CreatedBy = _testUser,
                UpdatedBy = _testUser,
                CreatedDateUTC = DateTime.Today
            };
            _context.TaskListItem.Add(item);
            _context.SaveChanges();

            var updateDto = new ItemDto
            {
                Id = item.Id,
                Task = "Updated Task",
                IsHeader = true
            };

            // Act
            var result = _service.UpdateItem(updateDto);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(updateDto.Task, result.Task);
            Assert.AreEqual(updateDto.IsHeader, result.IsHeader);

            var updatedItem = _context.TaskListItem.Find(item.Id);
            Assert.IsNotNull(updatedItem);
            Assert.AreEqual(updateDto.Task, updatedItem.Task);
            Assert.AreEqual(updateDto.IsHeader, updatedItem.IsHeader);
        }

        [TestMethod]
        public async Task UpdateItemsOrderAsync_ValidMove_UpdatesOrder()
        {
            // Arrange
            var items = new List<TaskListItem>
            {
                new TaskListItem { Task = "Item 1", Order = 1, TaskList = _testTaskList, CreatedBy = _testUser, UpdatedBy = _testUser , CreatedDateUTC = DateTime.Today},
                new TaskListItem { Task = "Item 2", Order = 2, TaskList = _testTaskList, CreatedBy = _testUser, UpdatedBy = _testUser , CreatedDateUTC = DateTime.Today},
                new TaskListItem { Task = "Item 3", Order = 3, TaskList = _testTaskList, CreatedBy = _testUser, UpdatedBy = _testUser , CreatedDateUTC = DateTime.Today}
            };
            _context.TaskListItem.AddRange(items);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.UpdateItemsOrderAsync(items[0].Id, _testTaskList.Id, 3);

            // Assert
            Assert.IsTrue(result);
            var movedItem = await _context.TaskListItem.FindAsync(items[0].Id);
            Assert.AreEqual(3, movedItem!.Order);

            // Verify other items were reordered
            var item2 = await _context.TaskListItem.FindAsync(items[1].Id);
            var item3 = await _context.TaskListItem.FindAsync(items[2].Id);
            Assert.AreEqual(1, item2!.Order);
            Assert.AreEqual(2, item3!.Order);
        }

        [TestMethod]
        public void GetAllUsers_ReturnsAllUsers()
        {
            // Arrange
            var additionalUser = new AppUser
            {
                Id = "testuser2",
                Email = "test2@example.com",
                UserName = "test2@example.com",
                Name = "john"
            };
            _context.Users.Add(additionalUser);
            _context.SaveChanges();

            // Act
            var result = _service.GetAllUsers();

            // Assert
            Assert.AreEqual(2, result.Count); // Including the test user created in TestInitialize
            Assert.IsTrue(result.Any(u => u.Name == "john"));
            Assert.IsTrue(result.Any(u => u.Name == "john"));
        }

        [TestMethod]
        public void AssignUsersToItem_ValidData_AssignsUsers()
        {
            // Arrange
            var item = new TaskListItem
            {
                Task = "Test Item",
                TaskList = _testTaskList,
                CreatedBy = _testUser,
                UpdatedBy = _testUser,
                CreatedDateUTC = DateTime.Today
            };
            _context.TaskListItem.Add(item);

            var additionalUser = new AppUser
            {
                Id = "testuser2",
                Email = "test2@example.com",
                UserName = "test2@example.com"
            };
            _context.Users.Add(additionalUser);
            _context.SaveChanges();

            var userIds = new List<string> { _testUser.Id, additionalUser.Id };

            // Act
            var result = _service.AssignUsersToItem(item.Id, userIds);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.AssignedUsers.Count);
            Assert.IsTrue(result.AssignedUsers.Any(u => u.Email == "test@example.com"));
            Assert.IsTrue(result.AssignedUsers.Any(u => u.Email == "test2@example.com"));

            var updatedItem = _context.TaskListItem
                .Include(x => x.AssignedUsers)
                .ThenInclude(x => x.User)
                .FirstOrDefault(x => x.Id == item.Id);
            Assert.IsNotNull(updatedItem);
            Assert.AreEqual(2, updatedItem.AssignedUsers.Count);
        }
    }
} 
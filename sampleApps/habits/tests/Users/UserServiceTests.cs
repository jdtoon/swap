using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using habits.Data;
using habits.Data.Models;
using habits.Dtos;
using habits.Services.Users;

namespace habits.Tests.Services.Users
{
    [TestClass]
    public class UserServiceTests
    {
        private ApplicationDbContext _context;
        private UserService _service;

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
            _service = new UserService(_context);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [TestMethod]
        public void GetMembers_WithNoFilters_ReturnsAllMembers()
        {
            // Arrange
            var users = new List<AppUser>
            {
                new AppUser { Id = "1", Name = "John", Surname = "Doe", Email = "john@example.com", IsActive = true },
                new AppUser { Id = "2", Name = "Jane", Surname = "Smith", Email = "jane@example.com", IsActive = true }
            };
            _context.Users.AddRange(users);
            _context.SaveChanges();

            // Act
            var result = _service.GetMembers(search: "", status: "", page: 1, pageSize: 10);

            // Assert
            Assert.AreEqual(2, result.TotalRecords);
            Assert.AreEqual(2, result.Data.Count);
            Assert.IsFalse(result.HasMore);
            Assert.AreEqual(1, result.CurrentPage);
        }

        [TestMethod]
        public void GetMembers_WithSearch_ReturnsFilteredMembers()
        {
            // Arrange
            var users = new List<AppUser>
            {
                new AppUser { Id = "1", Name = "John", Surname = "Doe", Email = "john@example.com", IsActive = true },
                new AppUser { Id = "2", Name = "Jane", Surname = "Smith", Email = "jane@example.com", IsActive = true },
                new AppUser { Id = "3", Name = "Bob", Surname = "Johnson", Email = "bob@example.com", IsActive = true }
            };
            _context.Users.AddRange(users);
            _context.SaveChanges();

            // Act
            var result = _service.GetMembers(search: "john", status: "", page: 1, pageSize: 10);

            // Assert
            Assert.AreEqual(1, result.TotalRecords);
            Assert.AreEqual("John", result.Data[0].Name);
        }

        [TestMethod]
        public void GetMembers_WithStatusFilter_ReturnsFilteredMembers()
        {
            // Arrange
            var users = new List<AppUser>
            {
                new AppUser { Id = "1", Name = "John", Surname = "Doe", Email = "john@example.com", IsActive = true },
                new AppUser { Id = "2", Name = "Jane", Surname = "Smith", Email = "jane@example.com", IsActive = false }
            };
            _context.Users.AddRange(users);
            _context.SaveChanges();

            // Act
            var result = _service.GetMembers(search: "", status: "active", page: 1, pageSize: 10);

            // Assert
            Assert.AreEqual(1, result.TotalRecords);
            Assert.IsTrue(result.Data[0].IsActive);
        }

        [TestMethod]
        public void GetMembers_WithPagination_ReturnsPaginatedResults()
        {
            // Arrange
            var users = new List<AppUser>
            {
                new AppUser { Id = "1", Name = "User1", Email = "user1@example.com", IsActive = true },
                new AppUser { Id = "2", Name = "User2", Email = "user2@example.com", IsActive = true },
                new AppUser { Id = "3", Name = "User3", Email = "user3@example.com", IsActive = true }
            };
            _context.Users.AddRange(users);
            _context.SaveChanges();

            // Act
            var result = _service.GetMembers(search: "", status: "", page: 1, pageSize: 2);

            // Assert
            Assert.AreEqual(3, result.TotalRecords);
            Assert.AreEqual(2, result.Data.Count);
            Assert.IsTrue(result.HasMore);
        }

        [TestMethod]
        public void ToggleMemberStatus_ValidId_TogglesStatus()
        {
            // Arrange
            var user = new AppUser { Id = "1", Name = "John", Email = "john@example.com", IsActive = true };
            _context.Users.Add(user);
            _context.SaveChanges();

            // Act
            var result = _service.ToggleMemberStatus("1");

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsActive);

            var updatedUser = _context.Users.Find("1");
            Assert.IsFalse(updatedUser!.IsActive);
        }

        [TestMethod]
        public void ToggleMemberStatus_InvalidId_ReturnsNull()
        {
            // Act
            var result = _service.ToggleMemberStatus("nonexistent");

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetUserDisplay_ValidUsername_ReturnsUserDisplay()
        {
            // Arrange
            var user = new AppUser 
            { 
                Id = "1", 
                Name = "John", 
                Surname = "Doe",
                Email = "john@example.com", 
                IsActive = true,
                Color = "#000000"
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            // Act
            var result = _service.GetUserDisplay("john@example.com");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("John", result.Name);
            Assert.AreEqual("#000000", result.Color);
        }

        [TestMethod]
        public void GetUserDisplay_InvalidUsername_ReturnsEmptyDisplay()
        {
            // Act
            var result = _service.GetUserDisplay("nonexistent@example.com");

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNull(result.Name);
        }

        [TestMethod]
        public void GetMemberById_ValidId_ReturnsMember()
        {
            // Arrange
            var user = new AppUser 
            { 
                Id = "1", 
                Name = "John", 
                Surname = "Doe",
                Email = "john@example.com" 
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            // Act
            var result = _service.GetMemberById("1");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("John", result.Name);
            Assert.AreEqual("Doe", result.Surname);
        }

        [TestMethod]
        public void GetMemberById_WithRole_ReturnsMemberWithRole()
        {
            // Arrange
            var user = new AppUser { Id = "1", Name = "John", Email = "john@example.com" };
            var role = new AppRole { Id = "role1", Name = "Admin" };
            var userRole = new IdentityUserRole<string> { UserId = "1", RoleId = "role1" };

            _context.Users.Add(user);
            _context.Roles.Add(role);
            _context.UserRoles.Add(userRole);
            _context.SaveChanges();

            // Act
            var result = _service.GetMemberById("1");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Admin", result.Role);
        }

        [TestMethod]
        public void UpdateMember_ValidMember_UpdatesSuccessfully()
        {
            // Arrange
            var user = new AppUser 
            { 
                Id = "1", 
                Name = "John", 
                Surname = "Doe",
                Email = "john@example.com",
                PhoneNumber = "1234567890",
                Color = "#000000"
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            var updateDto = new MemberDto
            {
                Id = "1",
                Name = "John Updated",
                Surname = "Doe Updated",
                PhoneNumber = "0987654321",
                Color = "#FFFFFF"
            };

            // Act
            var result = _service.UpdateMember(updateDto);

            // Assert
            Assert.IsTrue(result);
            var updatedUser = _context.Users.Find("1");
            Assert.AreEqual("John Updated", updatedUser!.Name);
            Assert.AreEqual("Doe Updated", updatedUser.Surname);
            Assert.AreEqual("0987654321", updatedUser.PhoneNumber);
            Assert.AreEqual("#FFFFFF", updatedUser.Color);
        }

        [TestMethod]
        public void UpdateMember_InvalidId_ReturnsFalse()
        {
            // Arrange
            var updateDto = new MemberDto
            {
                Id = "nonexistent",
                Name = "John Updated"
            };

            // Act
            var result = _service.UpdateMember(updateDto);

            // Assert
            Assert.IsFalse(result);
        }
    }
} 
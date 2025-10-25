using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using habits.Data;
using habits.Data.Models;
using habits.Services.Documents;
using habits.Services.Storage;
using System.Security.Claims;

namespace habits.Tests.Services.Documents
{
    [TestClass]
    public class DocumentServiceTests
    {
        private ApplicationDbContext _context;
        private Mock<IR2StorageService> _r2StorageServiceMock;
        private Mock<ILogger<DocumentService>> _loggerMock;
        private Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private DocumentService _service;
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
                Email = "test@example.com",
                UserName = "test@example.com"
            };
            _context.Users.Add(_testUser);
            _context.SaveChanges();

            // Setup mocks
            _r2StorageServiceMock = new Mock<IR2StorageService>();
            _loggerMock = new Mock<ILogger<DocumentService>>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

            // Setup HttpContext
            var context = new DefaultHttpContext();
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "test@example.com")
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            context.User = principal;
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);

            _service = new DocumentService(
                _context,
                _r2StorageServiceMock.Object,
                _loggerMock.Object,
                _httpContextAccessorMock.Object);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [TestMethod]
        public void GetDocuments_WithNoSearch_ReturnsAllDocuments()
        {
            // Arrange
            var documents = new List<Document>
            {
                new Document { Name = "Doc1", StorageKey = "key1", UploadedAt = DateTime.UtcNow, UploadedBy = _testUser.Id },
                new Document { Name = "Doc2", StorageKey = "key2", UploadedAt = DateTime.UtcNow, UploadedBy = _testUser.Id }
            };
            _context.Document.AddRange(documents);
            _context.SaveChanges();

            // Act
            var result = _service.GetDocuments(search: "", page: 1, pageSize: 10);

            // Assert
            Assert.AreEqual(2, result.TotalRecords);
            Assert.AreEqual(2, result.Data.Count);
            Assert.IsFalse(result.HasMore);
            Assert.AreEqual(1, result.CurrentPage);
        }

        [TestMethod]
        public void GetDocuments_WithSearch_ReturnsFilteredDocuments()
        {
            // Arrange
            var documents = new List<Document>
            {
                new Document { Name = "Test Doc", StorageKey = "key1", UploadedAt = DateTime.UtcNow, UploadedBy = _testUser.Id },
                new Document { Name = "Other Doc", StorageKey = "key2", UploadedAt = DateTime.UtcNow, UploadedBy = _testUser.Id }
            };
            _context.Document.AddRange(documents);
            _context.SaveChanges();

            // Act
            var result = _service.GetDocuments(search: "test", page: 1, pageSize: 10);

            // Assert
            Assert.AreEqual(1, result.TotalRecords);
            Assert.AreEqual("Test Doc", result.Data[0].Name);
        }

        [TestMethod]
        public async Task AddDocumentAsync_UploadsAndSavesDocument()
        {
            // Arrange
            var fileName = "test.pdf";
            var content = new byte[] { 1, 2, 3 };
            var formFile = new FormFile(
                baseStream: new MemoryStream(content),
                baseStreamOffset: 0,
                length: content.Length,
                name: "file",
                fileName: fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };

            var uploadResult = new R2UploadResult
            {
                Key = "documents/test.pdf",
                FileName = fileName,
                ContentType = "application/pdf",
                Size = content.Length
            };

            _r2StorageServiceMock.Setup(x => x.UploadFileAsync(
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
                .ReturnsAsync(uploadResult);

            // Act
            var result = await _service.AddDocumentAsync("Test Document", formFile);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Test Document", result.Name);
            Assert.AreEqual(uploadResult.Key, result.StorageKey);
            Assert.AreEqual(formFile.ContentType, result.ContentType);
            Assert.AreEqual(formFile.Length, result.Size);
            Assert.AreEqual(_testUser.Id, result.UploadedBy);

            var savedDoc = await _context.Document.FirstOrDefaultAsync();
            Assert.IsNotNull(savedDoc);
            Assert.AreEqual("Test Document", savedDoc.Name);
        }

        [TestMethod]
        public async Task UpdateDocumentNameAsync_UpdatesName()
        {
            // Arrange
            var document = new Document
            {
                Name = "Original Name",
                StorageKey = "key1",
                UploadedAt = DateTime.UtcNow,
                UploadedBy = _testUser.Id
            };
            _context.Document.Add(document);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.UpdateDocumentNameAsync(document.Id, "New Name");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("New Name", result.Name);

            var updatedDoc = await _context.Document.FindAsync(document.Id);
            Assert.IsNotNull(updatedDoc);
            Assert.AreEqual("New Name", updatedDoc.Name);
        }

        [TestMethod]
        public async Task DeleteDocumentAsync_DeletesFromStorageAndDatabase()
        {
            // Arrange
            var document = new Document
            {
                Name = "Test Doc",
                StorageKey = "key1",
                UploadedAt = DateTime.UtcNow,
                UploadedBy = _testUser.Id
            };
            _context.Document.Add(document);
            await _context.SaveChangesAsync();

            _r2StorageServiceMock.Setup(x => x.DeleteFileAsync(document.StorageKey))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.DeleteDocumentAsync(document.Id);

            // Assert
            Assert.IsTrue(result);
            _r2StorageServiceMock.Verify(x => x.DeleteFileAsync(document.StorageKey), Times.Once);

            var deletedDoc = await _context.Document.FindAsync(document.Id);
            Assert.IsNull(deletedDoc);
        }

        [TestMethod]
        public async Task DownloadDocumentAsync_ReturnsStreamAndMetadata()
        {
            // Arrange
            var document = new Document
            {
                Name = "Test",
                StorageKey = "key1.pdf",
                ContentType = "application/pdf",
                UploadedAt = DateTime.UtcNow,
                UploadedBy = _testUser.Id
            };
            _context.Document.Add(document);
            await _context.SaveChangesAsync();

            var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            _r2StorageServiceMock.Setup(x => x.DownloadFileAsync(document.StorageKey))
                .ReturnsAsync(stream);

            // Act
            var result = await _service.DownloadDocumentAsync(document.Id);

            // Assert
            Assert.IsNotNull(result.Stream);
            Assert.AreEqual(document.ContentType, result.ContentType);
            Assert.AreEqual("Test.pdf", result.FileName);
            _r2StorageServiceMock.Verify(x => x.DownloadFileAsync(document.StorageKey), Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(KeyNotFoundException))]
        public async Task DownloadDocumentAsync_InvalidId_ThrowsException()
        {
            // Act
            await _service.DownloadDocumentAsync(999);
        }

        [TestMethod]
        public async Task DeleteDocumentAsync_InvalidId_ReturnsFalse()
        {
            // Act
            var result = await _service.DeleteDocumentAsync(999);

            // Assert
            Assert.IsFalse(result);
            _r2StorageServiceMock.Verify(x => x.DeleteFileAsync(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task UpdateDocumentNameAsync_InvalidId_ReturnsNull()
        {
            // Act
            var result = await _service.UpdateDocumentNameAsync(999, "New Name");

            // Assert
            Assert.IsNull(result);
        }
    }
} 
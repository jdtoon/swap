using Microsoft.AspNetCore.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using habits.Services.FileSystem;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace habits.Tests.Services.FileSystem
{
    [TestClass]
    public class FileSystemServiceTests
    {
        private Mock<IWebHostEnvironment> _webHostEnvironmentMock;
        private FileSystemService _service;
        private MockFileSystem _mockFileSystem;
        private string _webRootPath;

        [TestInitialize]
        public void TestInitialize()
        {
            // Setup mock file system
            _mockFileSystem = new MockFileSystem();
            _webRootPath = @"c:\webroot";
            _mockFileSystem.AddDirectory(_webRootPath);
            _mockFileSystem.AddDirectory(Path.Combine(_webRootPath, "svg"));

            // Setup web host environment mock
            _webHostEnvironmentMock = new Mock<IWebHostEnvironment>();
            _webHostEnvironmentMock.Setup(x => x.WebRootPath).Returns(_webRootPath);

            _service = new FileSystemService(_webHostEnvironmentMock.Object);
        }

        [TestMethod]
        public void GetCalendarTypeFiles_DirectoryExists_ReturnsFiles()
        {
            // Arrange
            var svgPath = Path.Combine(_webRootPath, "svg");
            var files = new[]
            {
                Path.Combine(svgPath, "type_calendar.svg"),
                Path.Combine(svgPath, "type_meeting.svg"),
                Path.Combine(svgPath, "other_file.svg") // Should not be included
            };

            foreach (var file in files)
            {
                _mockFileSystem.AddFile(file, new MockFileData(""));
            }

            // Act
            var result = _service.GetCalendarTypeFiles();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(result.All(f => f.StartsWith("~/svg/")));
            Assert.IsTrue(result.Any(f => f.EndsWith("type_calendar.svg")));
            Assert.IsTrue(result.Any(f => f.EndsWith("type_meeting.svg")));
            Assert.IsFalse(result.Any(f => f.EndsWith("other_file.svg")));
        }

        [TestMethod]
        public void GetCalendarTypeFiles_DirectoryDoesNotExist_ReturnsEmptyList()
        {
            // Arrange
            _mockFileSystem.RemoveFile(Path.Combine(_webRootPath, "svg"));

            // Act
            var result = _service.GetCalendarTypeFiles();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        public void GetCalendarTypeFiles_NoMatchingFiles_ReturnsEmptyList()
        {
            // Arrange
            var svgPath = Path.Combine(_webRootPath, "svg");
            var files = new[]
            {
                Path.Combine(svgPath, "other1.svg"),
                Path.Combine(svgPath, "other2.svg")
            };

            foreach (var file in files)
            {
                _mockFileSystem.AddFile(file, new MockFileData(""));
            }

            // Act
            var result = _service.GetCalendarTypeFiles();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        public void GetCalendarTypeFiles_ReturnsCorrectPaths()
        {
            // Arrange
            var svgPath = Path.Combine(_webRootPath, "svg");
            var fileName = "type_test.svg";
            var filePath = Path.Combine(svgPath, fileName);
            _mockFileSystem.AddFile(filePath, new MockFileData(""));

            // Act
            var result = _service.GetCalendarTypeFiles();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual($"~/svg/{fileName}", result.First());
        }
    }
} 
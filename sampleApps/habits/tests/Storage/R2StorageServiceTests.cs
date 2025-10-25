using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using habits.Services.Storage;
using habits.Settings;

namespace habits.Tests.Services.Storage
{
    [TestClass]
    public class R2StorageServiceTests
    {
        private Mock<IAmazonS3> _s3ClientMock;
        private Mock<ILogger<R2StorageService>> _loggerMock;
        private Mock<IOptions<CloudflareR2Settings>> _settingsMock;
        private CloudflareR2Settings _settings;
        private R2StorageService _service;

        [TestInitialize]
        public void TestInitialize()
        {
            // Setup mocks
            _s3ClientMock = new Mock<IAmazonS3>();
            _loggerMock = new Mock<ILogger<R2StorageService>>();
            _settingsMock = new Mock<IOptions<CloudflareR2Settings>>();

            // Setup settings
            _settings = new CloudflareR2Settings
            {
                AccessKey = "test-access-key",
                SecretKey = "test-secret-key",
                BucketName = "test-bucket",
                Endpoint = "http://test.endpoint"
            };

            _settingsMock.Setup(x => x.Value).Returns(_settings);

            _service = new R2StorageService(_loggerMock.Object, _settingsMock.Object);
        }

        [TestMethod]
        public async Task UploadFileAsync_WithFilePath_UploadsSuccessfully()
        {
            // Arrange
            var fileName = "test.txt";
            var filePath = Path.GetTempFileName();
            File.WriteAllText(filePath, "test content");

            _s3ClientMock.Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), default))
                .ReturnsAsync(new PutObjectResponse());

            try
            {
                // Act
                var result = await _service.UploadFileAsync(fileName, filePath);

                // Assert
                Assert.IsNotNull(result);
                Assert.AreEqual(fileName, result.FileName);
                Assert.IsTrue(result.Key.EndsWith(fileName));
                Assert.AreEqual("application/octet-stream", result.ContentType);
                Assert.IsTrue(result.Size > 0);
            }
            finally
            {
                // Cleanup
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
        }

        [TestMethod]
        public async Task UploadFileAsync_WithStream_UploadsSuccessfully()
        {
            // Arrange
            var fileName = "test.txt";
            var content = "test content";
            var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));

            _s3ClientMock.Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), default))
                .ReturnsAsync(new PutObjectResponse());

            // Act
            var result = await _service.UploadFileAsync(fileName, stream);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(fileName, result.FileName);
            Assert.IsTrue(result.Key.EndsWith(fileName));
            Assert.AreEqual("application/octet-stream", result.ContentType);
            Assert.AreEqual(content.Length, result.Size);
        }

        [TestMethod]
        public async Task DownloadFileAsync_ExistingFile_ReturnsStream()
        {
            // Arrange
            var key = "test.txt";
            var content = "test content";
            var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));

            var response = new GetObjectResponse
            {
                ResponseStream = stream
            };

            _s3ClientMock.Setup(x => x.GetObjectAsync(It.Is<GetObjectRequest>(r => 
                r.BucketName == _settings.BucketName && r.Key == key), default))
                .ReturnsAsync(response);

            // Act
            var result = await _service.DownloadFileAsync(key);

            // Assert
            Assert.IsNotNull(result);
            using var reader = new StreamReader(result);
            var downloadedContent = await reader.ReadToEndAsync();
            Assert.AreEqual(content, downloadedContent);
        }

        [TestMethod]
        public async Task DeleteFileAsync_ExistingFile_DeletesSuccessfully()
        {
            // Arrange
            var key = "test.txt";

            _s3ClientMock.Setup(x => x.DeleteObjectAsync(It.Is<DeleteObjectRequest>(r =>
                r.BucketName == _settings.BucketName && r.Key == key), default))
                .ReturnsAsync(new DeleteObjectResponse());

            // Act & Assert
            await _service.DeleteFileAsync(key); // Should not throw

            _s3ClientMock.Verify(x => x.DeleteObjectAsync(
                It.Is<DeleteObjectRequest>(r => r.Key == key), 
                default), 
                Times.Once);
        }

        [TestMethod]
        public async Task ListFilesAsync_ReturnsFileList()
        {
            // Arrange
            var prefix = "test/";
            var files = new List<S3Object>
            {
                new S3Object { Key = "test/file1.txt" },
                new S3Object { Key = "test/file2.txt" }
            };

            var response = new ListObjectsV2Response
            {
                S3Objects = files
            };

            _s3ClientMock.Setup(x => x.ListObjectsV2Async(It.Is<ListObjectsV2Request>(r =>
                r.BucketName == _settings.BucketName && r.Prefix == prefix), default))
                .ReturnsAsync(response);

            // Act
            var result = await _service.ListFilesAsync(prefix);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("test/file1.txt", result[0]);
            Assert.AreEqual("test/file2.txt", result[1]);
        }

        [TestMethod]
        public async Task ManageBackupsAsync_DeletesOldBackups()
        {
            // Arrange
            var prefix = "backup/";
            var keepCount = 2;
            var files = new List<S3Object>
            {
                new S3Object { Key = "backup/file1.txt", LastModified = DateTime.UtcNow },
                new S3Object { Key = "backup/file2.txt", LastModified = DateTime.UtcNow.AddDays(-1) },
                new S3Object { Key = "backup/file3.txt", LastModified = DateTime.UtcNow.AddDays(-2) }
            };

            var response = new ListObjectsV2Response
            {
                S3Objects = files
            };

            _s3ClientMock.Setup(x => x.ListObjectsV2Async(It.Is<ListObjectsV2Request>(r =>
                r.BucketName == _settings.BucketName && r.Prefix == prefix), default))
                .ReturnsAsync(response);

            _s3ClientMock.Setup(x => x.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), default))
                .ReturnsAsync(new DeleteObjectResponse());

            // Act
            await _service.ManageBackupsAsync(prefix, keepCount);

            // Assert
            _s3ClientMock.Verify(x => x.DeleteObjectAsync(
                It.Is<DeleteObjectRequest>(r => r.Key == "backup/file3.txt"),
                default),
                Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(AmazonS3Exception))]
        public async Task UploadFileAsync_WhenS3Fails_ThrowsException()
        {
            // Arrange
            var fileName = "test.txt";
            var stream = new MemoryStream();

            _s3ClientMock.Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), default))
                .ThrowsAsync(new AmazonS3Exception("Test error"));

            // Act
            await _service.UploadFileAsync(fileName, stream);
        }

        [TestMethod]
        [ExpectedException(typeof(AmazonS3Exception))]
        public async Task DownloadFileAsync_WhenS3Fails_ThrowsException()
        {
            // Arrange
            var key = "test.txt";

            _s3ClientMock.Setup(x => x.GetObjectAsync(It.IsAny<GetObjectRequest>(), default))
                .ThrowsAsync(new AmazonS3Exception("Test error"));

            // Act
            await _service.DownloadFileAsync(key);
        }

        [TestMethod]
        [ExpectedException(typeof(AmazonS3Exception))]
        public async Task DeleteFileAsync_WhenS3Fails_ThrowsException()
        {
            // Arrange
            var key = "test.txt";

            _s3ClientMock.Setup(x => x.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), default))
                .ThrowsAsync(new AmazonS3Exception("Test error"));

            // Act
            await _service.DeleteFileAsync(key);
        }
    }
} 
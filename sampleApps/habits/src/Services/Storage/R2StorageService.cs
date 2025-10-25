using Amazon.S3.Model;
using Amazon.S3;
using Microsoft.Extensions.Options;
using habits.Settings;

namespace habits.Services.Storage
{
    public class R2StorageService : IR2StorageService
    {
        private readonly ILogger<R2StorageService> _logger;
        private readonly CloudflareR2Settings _r2Settings;
        private readonly IAmazonS3 _s3Client;

        public R2StorageService(
            ILogger<R2StorageService> logger,
            IOptions<CloudflareR2Settings> r2Settings)
        {
            _logger = logger;
            _r2Settings = r2Settings.Value;

            var config = new AmazonS3Config
            {
                ServiceURL = _r2Settings.Endpoint,
                SignatureVersion = "4",
                ForcePathStyle = true,
                UseHttp = true,
                SignatureMethod = Amazon.Runtime.SigningAlgorithm.HmacSHA256
            };

            _s3Client = new AmazonS3Client(_r2Settings.AccessKey, _r2Settings.SecretKey, config);
        }

        public async Task<R2UploadResult> UploadFileAsync(string fileName, string filePath, string prefix = "")
        {
            try
            {
                // Generate a unique key
                string uniqueFileName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Path.GetFileName(fileName)}";
                string key = string.IsNullOrEmpty(prefix) ? uniqueFileName : $"{prefix.TrimEnd('/')}/{uniqueFileName}";

                // Get file info before opening the stream
                var fileInfo = new FileInfo(filePath);
                long fileSize = fileInfo.Length;

                // Upload using the file path directly
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    var putRequest = new PutObjectRequest
                    {
                        BucketName = _r2Settings.BucketName,
                        Key = key,
                        ContentType = "application/octet-stream",
                        UseChunkEncoding = false,
                        InputStream = fileStream
                    };

                    await _s3Client.PutObjectAsync(putRequest);
                }

                _logger.LogInformation($"Successfully uploaded {key}");

                return new R2UploadResult
                {
                    Key = key,
                    FileName = fileName,
                    Size = fileSize,
                    ContentType = "application/octet-stream",
                    UploadedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading file {fileName}");
                throw;
            }
        }

        public async Task<R2UploadResult> UploadFileAsync(string fileName, Stream fileStream, string prefix = "", string contentType = "application/octet-stream")
        {
            try
            {
                // Generate a unique key
                string uniqueFileName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Path.GetFileName(fileName)}";
                string key = string.IsNullOrEmpty(prefix) ? uniqueFileName : $"{prefix.TrimEnd('/')}/{uniqueFileName}";

                // Store the length before uploading
                long streamLength = fileStream.Length;

                var putRequest = new PutObjectRequest
                {
                    BucketName = _r2Settings.BucketName,
                    Key = key,
                    ContentType = contentType,
                    UseChunkEncoding = false,
                    InputStream = fileStream
                };

                await _s3Client.PutObjectAsync(putRequest);
                _logger.LogInformation($"Successfully uploaded {key}");

                return new R2UploadResult
                {
                    Key = key,
                    FileName = fileName,
                    Size = streamLength,
                    ContentType = contentType,
                    UploadedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading file {fileName}");
                throw;
            }
        }

        public async Task<Stream> DownloadFileAsync(string key)
        {
            try
            {
                var getRequest = new GetObjectRequest
                {
                    BucketName = _r2Settings.BucketName,
                    Key = key
                };

                var response = await _s3Client.GetObjectAsync(getRequest);
                var memoryStream = new MemoryStream();
                await response.ResponseStream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;
                return memoryStream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error downloading file {key}");
                throw;
            }
        }

        public async Task DeleteFileAsync(string key)
        {
            try
            {
                var deleteRequest = new DeleteObjectRequest
                {
                    BucketName = _r2Settings.BucketName,
                    Key = key
                };

                await _s3Client.DeleteObjectAsync(deleteRequest);
                _logger.LogInformation($"Successfully deleted {key}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting file {key}");
                throw;
            }
        }

        public async Task<List<string>> ListFilesAsync(string prefix)
        {
            try
            {
                var listRequest = new ListObjectsV2Request
                {
                    BucketName = _r2Settings.BucketName,
                    Prefix = prefix
                };

                var response = await _s3Client.ListObjectsV2Async(listRequest);
                return response.S3Objects.Select(x => x.Key).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error listing files with prefix {prefix}");
                throw;
            }
        }

        public async Task ManageBackupsAsync(string prefix, int keepCount)
        {
            try
            {
                var listRequest = new ListObjectsV2Request
                {
                    BucketName = _r2Settings.BucketName,
                    Prefix = prefix
                };

                var response = await _s3Client.ListObjectsV2Async(listRequest);
                var sortedBackups = response.S3Objects
                    .OrderByDescending(o => o.LastModified)
                    .ToList();

                if (sortedBackups.Count > keepCount)
                {
                    var backupsToDelete = sortedBackups.Skip(keepCount).ToList();
                    foreach (var backup in backupsToDelete)
                    {
                        await DeleteFileAsync(backup.Key);
                    }
                }

                _logger.LogInformation($"Backup management completed. Current backup count: {Math.Min(sortedBackups.Count, keepCount)}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error managing backups");
                throw;
            }
        }
    }
}

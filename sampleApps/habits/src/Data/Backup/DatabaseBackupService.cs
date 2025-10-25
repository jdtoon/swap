using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using habits.Services.Storage;
using habits.Settings;

namespace habits.Data.Backup
{
    public class DatabaseBackupService : BackgroundService
    {
        private readonly ILogger<DatabaseBackupService> _logger;
        private readonly CloudflareR2Settings _r2Settings;
        private readonly IConfiguration _configuration;
        private readonly IR2StorageService _r2StorageService;

        public DatabaseBackupService(
            ILogger<DatabaseBackupService> logger,
            IOptions<CloudflareR2Settings> r2Settings,
            IConfiguration configuration,
            IR2StorageService r2StorageService)
        {
            _logger = logger;
            _r2Settings = r2Settings.Value;
            _configuration = configuration;
            _r2StorageService = r2StorageService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Starting database backup...");
                    await BackupDatabaseToCloudflareR2();
                    _logger.LogInformation("Database backup completed successfully.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during database backup.");
                }

                await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
            }
        }

        private async Task BackupDatabaseToCloudflareR2()
        {
            var isDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
            var connectionString = _configuration.GetConnectionString(isDocker ? "DefaultConnectionDocker" : "DefaultConnection");
            var databaseFilePath = ExtractDatabaseFilePath(connectionString!);

            var backupFileName = $"backup-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.db";
            var tempBackupPath = Path.Combine(Path.GetTempPath(), backupFileName);

            try
            {
                // Create a copy of the database
                File.Copy(databaseFilePath, tempBackupPath, overwrite: true);

                // Ensure the file exists and is accessible
                if (!File.Exists(tempBackupPath))
                {
                    throw new FileNotFoundException("Backup file was not created successfully", tempBackupPath);
                }

                // Upload the backup
                var uploadResult = await _r2StorageService.UploadFileAsync(
                    backupFileName,
                    tempBackupPath,
                    _r2Settings.BackupPrefix);

                _logger.LogInformation($"Database backed up with key {uploadResult.Key} to Cloudflare R2.");

                // Manage backups (keep only 5)
                await _r2StorageService.ManageBackupsAsync(_r2Settings.BackupPrefix!, 5);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during backup process");
                throw;
            }
            finally
            {
                // Clean up the temporary file
                try
                {
                    if (File.Exists(tempBackupPath))
                    {
                        File.Delete(tempBackupPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temporary backup file");
                }
            }
        }

        private string ExtractDatabaseFilePath(string connectionString)
        {
            var match = System.Text.RegularExpressions.Regex.Match(connectionString, @"Data Source=([^;]+)");

            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            throw new InvalidOperationException("Invalid connection string format.");
        }
    }
}
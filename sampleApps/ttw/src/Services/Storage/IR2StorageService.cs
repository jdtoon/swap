namespace ttw.Services.Storage
{
    public interface IR2StorageService
    {
        Task<R2UploadResult> UploadFileAsync(string fileName, string filePath, string prefix = "");

        Task<R2UploadResult> UploadFileAsync(string fileName, Stream fileStream, string prefix = "", string contentType = "application/octet-stream");

        Task<Stream> DownloadFileAsync(string key);

        Task DeleteFileAsync(string key);

        Task<List<string>> ListFilesAsync(string prefix);

        Task ManageBackupsAsync(string prefix, int keepCount);
    }
}
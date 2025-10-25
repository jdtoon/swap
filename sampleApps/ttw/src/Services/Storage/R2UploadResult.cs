namespace ttw.Services.Storage
{
    public class R2UploadResult
    {
        public string? Key { get; set; }
        public string? FileName { get; set; }
        public long Size { get; set; }
        public string? ContentType { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
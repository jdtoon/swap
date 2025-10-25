namespace ttw.Settings
{
    public class CloudflareR2Settings
    {
        public string AccessKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string BucketName { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public string BackupPrefix { get; set; } = string.Empty;
    }
}
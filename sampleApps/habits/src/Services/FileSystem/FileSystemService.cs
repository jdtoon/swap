namespace habits.Services.FileSystem
{
    public class FileSystemService : IFileSystemService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;

        public FileSystemService(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        public IEnumerable<string> GetCalendarTypeFiles()
        {
            var svgPath = Path.Combine(_webHostEnvironment.WebRootPath, "svg");
            if (!Directory.Exists(svgPath))
                return [];

            var files = Directory.GetFiles(svgPath, "type_*.svg");

            return files
                .Select(file => Path.GetFileName(file))
                .Select(fileName => $"~/svg/{fileName}");
        }
    }
}

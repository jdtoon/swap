using Microsoft.AspNetCore.Mvc;

namespace habits.Controllers
{
    [Route("api/service-worker")]
    [ApiController]
    public class ServiceWorkerController : ControllerBase
    {
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ServiceWorkerController(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet("directory-contents")]
        public IActionResult GetDirectoryContents([FromQuery] string[] paths)
        {
            try
            {
                var allFiles = new HashSet<string>(); // Using HashSet to avoid duplicates

                foreach (var path in paths)
                {
                    // Sanitize the path to prevent directory traversal
                    var sanitizedPath = path.TrimStart('/');
                    var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, sanitizedPath);

                    // Verify the path is within wwwroot
                    if (!fullPath.StartsWith(_webHostEnvironment.WebRootPath))
                    {
                        continue; // Skip invalid paths
                    }

                    if (!Directory.Exists(fullPath))
                    {
                        continue; // Skip non-existent directories
                    }

                    // Get all files in the directory and its subdirectories
                    var files = Directory.GetFiles(fullPath, "*.*", SearchOption.AllDirectories)
                        .Select(f => "/" + Path.GetRelativePath(_webHostEnvironment.WebRootPath, f)
                            .Replace("\\", "/")); // Ensure forward slashes for web paths

                    allFiles.UnionWith(files);
                }

                return Ok(allFiles);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}

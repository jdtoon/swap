using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using habits.Services.Documents;

namespace habits.Controllers
{
    [Authorize]
    public class DocumentsController : Controller
    {
        private readonly ILogger<DocumentsController> _logger;
        private readonly IDocumentService _documentService;

        public DocumentsController(ILogger<DocumentsController> logger, 
                                   IDocumentService documentService)
        {
            _logger = logger;
            _documentService = documentService;
        }

        public IActionResult Index(string? search = null)
        {
            ViewBag.IsHTMXRequest = HttpContext.Request.Headers.ContainsKey("HX-Request");

            if (!string.IsNullOrWhiteSpace(search))
            {
                HttpContext.Session.SetString("SelectedSearchDocuments", search);
            }

            if (HttpContext.Request.Headers.ContainsKey("HX-Request"))
                return PartialView();
            return View();
        }

        public IActionResult GetDocuments(string search = "", int page = 1, int pageSize = 10)
        {
            if (HttpContext?.Session.Keys.Contains("SelectedSearchDocuments") == true)
            {
                HttpContext.Session.Remove("SelectedSearchDocuments");
            }

            var documents = _documentService.GetDocuments(search, page, pageSize);
            return PartialView("_DocumentsList", documents);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDocument([FromForm] string name, IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("No file uploaded");

                if (string.IsNullOrWhiteSpace(name))
                    return BadRequest("Document name is required");

                if (file.Length > 30 * 1024 * 1024) // 30MB
                    return BadRequest("File size exceeds maximum limit of 30MB");

                using var stream = file.OpenReadStream();
                var document = await _documentService.AddDocumentAsync(name, file);

                return PartialView("_DocumentItem", document);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document");
                return StatusCode(500, "An error occurred while uploading the document.");
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateDocumentName(int id, string name)
        {
            var updatedDocument = await _documentService.UpdateDocumentNameAsync(id, name);
            return PartialView("_DocumentItem", updatedDocument);
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            var success = await _documentService.DeleteDocumentAsync(id);
            return success ? Ok() : BadRequest();
        }

        public async Task<IActionResult> DownloadDocument(int id)
        {
            try
            {
                var (stream, contentType, fileName) = await _documentService.DownloadDocumentAsync(id);
                return new FileStreamResult(stream, contentType)
                {
                    FileDownloadName = fileName
                };
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        public async Task<IActionResult> ViewDocument(int id)
        {
            try
            {
                var (stream, contentType, fileName) = await _documentService.DownloadDocumentAsync(id);
                return File(stream, contentType);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }
    }
}
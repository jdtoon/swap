using habits.Data;
using habits.Dtos.Data;
using habits.Dtos;
using habits.Services.Storage;
using habits.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace habits.Services.Documents
{
    public class DocumentService : IDocumentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IR2StorageService _r2StorageService;
        private readonly ILogger<DocumentService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DocumentService(ApplicationDbContext context,
                               IR2StorageService r2StorageService,
                               ILogger<DocumentService> logger,
                               IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _r2StorageService = r2StorageService;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public PagedResult<DocumentDto> GetDocuments(string search, int page, int pageSize)
        {
            var query = _context.Document.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(d => d.Name.ToLower().Contains(search));
            }

            int totalRecords = query.Count();
            var documents = query.OrderByDescending(d => d.UploadedAt)
                               .Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .ToList();

            return new PagedResult<DocumentDto>
            {
                Data = documents.Select(DocumentDto.FromModel).ToList(),
                HasMore = (page * pageSize) <= totalRecords,
                TotalRecords = totalRecords,
                CurrentPage = page
            };
        }

        public async Task<DocumentDto> AddDocumentAsync(string name, IFormFile file)
        {
            try
            {
                using var stream = file.OpenReadStream();
                var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                var uploadResult = await _r2StorageService.UploadFileAsync(
                    file.FileName,
                    memoryStream,
                    "documents",
                    file.ContentType);

                var document = new Document
                {
                    Name = name,
                    StorageKey = uploadResult.Key!,
                    ContentType = file.ContentType,
                    Size = file.Length,
                    UploadedAt = DateTime.UtcNow,
                    UploadedBy = _context.Users.FirstOrDefault(x => x.Email == _httpContextAccessor.HttpContext!.User.Identity!.Name)!.Id
                };

                _context.Document.Add(document);
                await _context.SaveChangesAsync();

                return DocumentDto.FromModel(document);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding document: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<DocumentDto> UpdateDocumentNameAsync(int id, string newName)
        {
            try
            {
                var document = await _context.Document.FindAsync(id);
                if (document == null)
                    throw new Exception();

                document.Name = newName;
                await _context.SaveChangesAsync();
                return DocumentDto.FromModel(document);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating document name");
                return null!;
            }
        }

        public async Task<bool> DeleteDocumentAsync(int id)
        {
            try
            {
                var document = await _context.Document.FindAsync(id);
                if (document == null) return false;

                await _r2StorageService.DeleteFileAsync(document.StorageKey);
                _context.Document.Remove(document);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document");
                return false;
            }
        }

        public async Task<(Stream Stream, string ContentType, string FileName)> DownloadDocumentAsync(int id)
        {
            var document = await _context.Document.FindAsync(id);
            if (document == null) throw new KeyNotFoundException("Document not found");

            var stream = await _r2StorageService.DownloadFileAsync(document.StorageKey);

            // Ensure filename has the correct extension
            string extension = Path.GetExtension(document.StorageKey);
            string downloadFileName = !document.Name.EndsWith(extension, StringComparison.OrdinalIgnoreCase)
                ? $"{document.Name}{extension}"
                : document.Name;

            return (stream, document.ContentType, downloadFileName);
        }
    }
}

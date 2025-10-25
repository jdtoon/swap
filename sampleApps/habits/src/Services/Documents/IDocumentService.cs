using habits.Dtos.Data;
using habits.Dtos;

namespace habits.Services.Documents
{
    public interface IDocumentService
    {
        PagedResult<DocumentDto> GetDocuments(string search, int page, int pageSize);
        Task<DocumentDto> AddDocumentAsync(string name, IFormFile file);
        Task<DocumentDto> UpdateDocumentNameAsync(int id, string newName);
        Task<bool> DeleteDocumentAsync(int id);
        Task<(Stream Stream, string ContentType, string FileName)> DownloadDocumentAsync(int id);
    }
}

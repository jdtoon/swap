using habits.Data.Models;
using System.ComponentModel.DataAnnotations;

namespace habits.Dtos
{
    public class DocumentDto
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = null!;

        public string StorageKey { get; set; } = null!;

        public string ContentType { get; set; } = null!;

        public long Size { get; set; }

        public DateTime UploadedAt { get; set; }

        public string UploadedBy { get; set; } = null!;

        public static DocumentDto FromModel(Document document)
        {
            return new DocumentDto
            {
                Id = document.Id,
                Name = document.Name,
                StorageKey = document.StorageKey,
                ContentType = document.ContentType,
                Size = document.Size,
                UploadedAt = document.UploadedAt,
                UploadedBy = document.UploadedBy
            };
        }

        public static Document ToModel(DocumentDto dto)
        {
            return new Document
            {
                Name = dto.Name,
                StorageKey = dto.StorageKey,
                ContentType = dto.ContentType,
                Size = dto.Size,
                UploadedAt = dto.UploadedAt,
                UploadedBy = dto.UploadedBy
            };
        }
    }
}

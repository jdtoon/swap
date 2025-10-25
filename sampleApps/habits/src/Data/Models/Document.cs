using System.ComponentModel.DataAnnotations;

namespace habits.Data.Models
{
    public class Document
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = null!;

        [Required]
        public string StorageKey { get; set; } = null!;

        public string ContentType { get; set; } = null!;

        public long Size { get; set; }

        public DateTime UploadedAt { get; set; }

        public string UploadedBy { get; set; } = null!;
    }
}

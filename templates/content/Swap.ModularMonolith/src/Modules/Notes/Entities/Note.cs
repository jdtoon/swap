using SwapModularMonolith.Data;

namespace SwapModularMonolith.Modules.Notes.Entities;

public class Note : IAuditableEntity
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public string? Content { get; set; }
    public string Color { get; set; } = "gray";
    public bool IsPinned { get; set; }

    // Audit fields
    public string? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ModifiedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

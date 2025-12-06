namespace SwapSmallPartials.Data;

public interface IAuditableEntity
{
    string? CreatedByUserId { get; set; }
    DateTime CreatedAt { get; set; }
    string? ModifiedByUserId { get; set; }
    DateTime? UpdatedAt { get; set; }
}

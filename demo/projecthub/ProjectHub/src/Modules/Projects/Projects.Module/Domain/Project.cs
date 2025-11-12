namespace ProjectHub.Modules.Projects.Module.Domain;

public class Project
{
    public int Id { get; set; }
    public int WorkspaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "Planning"; // Planning, Active, OnHold, Completed
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

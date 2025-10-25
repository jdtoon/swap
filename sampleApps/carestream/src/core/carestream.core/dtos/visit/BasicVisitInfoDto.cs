public class BasicVisitInfoDto
{
    public int VisitId { get; set; }
    public int PatientId { get; set; }
    public string? BriefReason { get; set; }
    public DateTime VisitTimestamp { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? AssignedOfficerUserId { get; set; }
}
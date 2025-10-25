namespace carestream.core.dtos.dashboard
{
    public class RecentPatientDto
    {
        public string Name { get; set; } = string.Empty;
        public DateTime VisitTimestamp { get; set; }
        public string BriefReason { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // e.g., "Discharged", "In Treatment", "Pending"
        public int? PatientId { get; set; } // Optional ID for linking later
    }
}

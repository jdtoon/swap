namespace carestream.core.dtos.dashboard
{
    public class RecentStaffReportDto
    {
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty; // e.g., "High", "Medium", "Low"
        public DateTime Timestamp { get; set; }
        public int? ReportId { get; set; } // Optional ID for linking later
    }
}

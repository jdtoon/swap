namespace carestream.core.dtos.dashboard
{
    public class PatientAdminDashboardViewModel
    {
        public DashboardStatsDto? Stats { get; set; }
        public List<RecentPatientDto> RecentPatients { get; set; } = new List<RecentPatientDto>();
        public List<RecentStaffReportDto> RecentStaffReports { get; set; } = new List<RecentStaffReportDto>();
    }
}

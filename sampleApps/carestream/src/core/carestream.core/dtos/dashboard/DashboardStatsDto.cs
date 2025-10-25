namespace carestream.core.dtos.dashboard
{
    public class DashboardStatsDto
    {
        public int TotalSickBayVisits { get; set; }
        public int CurrentlyInTreatment { get; set; }
        public int PendingCheckin { get; set; } // Or whatever 'Pending' represents
    }
}

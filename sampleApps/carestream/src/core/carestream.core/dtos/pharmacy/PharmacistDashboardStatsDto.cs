namespace carestream.core.dtos.pharmacy
{
    public class PharmacistDashboardStatsDto
    {
        public int PendingPrescriptionsCount { get; set; }
        public int PatientsWaitingCollection { get; set; } // Or "Prescriptions Ready for Collection"
        public int DispensedTodayCount { get; set; }
        public string AveragePreparationTime { get; set; } = "N/A"; // Placeholder
    }
}
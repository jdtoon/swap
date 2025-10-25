namespace carestream.core.dtos.doctor
{
    /// <summary>
    /// Data Transfer Object for displaying key statistics on the Doctor's dashboard.
    /// </summary>
    public class DoctorDashboardStatsDto
    {
        /// <summary>
        /// Gets or sets the total number of patients currently waiting for a doctor (status 'ReadyForDoctor').
        /// </summary>
        public int TotalWaitingForDoctor { get; set; }

        /// <summary>
        /// Gets or sets the count of urgent cases waiting for a doctor. (Future implementation)
        /// </summary>
        public int UrgentCasesCount { get; set; } // Placeholder for FR-DN-001

        /// <summary>
        /// Gets or sets the count of high priority cases waiting for a doctor. (Future implementation)
        /// </summary>
        public int HighPriorityCasesCount { get; set; } // Placeholder for FR-DN-001

        /// <summary>
        /// Gets or sets the average wait time for patients waiting for a doctor. (Future implementation)
        /// </summary>
        public string AverageWaitTime { get; set; } = "N/A"; // Placeholder for FR-DN-001
    }
}
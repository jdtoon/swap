namespace carestream.core.dtos.doctor
{
    /// <summary>
    /// View model for the Doctor's dashboard.
    /// </summary>
    public class DoctorDashboardViewModel
    {
        /// <summary>
        /// Gets or sets the dashboard statistics.
        /// </summary>
        public DoctorDashboardStatsDto? Stats { get; set; }

        /// <summary>
        /// Gets or sets the list of patients waiting for consultation.
        /// </summary>
        public List<DoctorQueueItemDto> PatientQueue { get; set; } = new List<DoctorQueueItemDto>();

        /// <summary>
        /// Gets or sets the list of patients in progress for consultation.
        /// </summary>
        public List<DoctorQueueItemDto> InProgressConsultations { get; set; } = new List<DoctorQueueItemDto>();
    }
}
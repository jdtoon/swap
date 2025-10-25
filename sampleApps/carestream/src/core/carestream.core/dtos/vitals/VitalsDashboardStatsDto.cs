namespace carestream.core.dtos.vitals
{
    public class VitalsDashboardStatsDto
    {
        public int WaitingForVitals { get; set; }
        public int VitalsInProgress { get; set; } // Patients currently having vitals taken
        public int ReadyForDoctor { get; set; }   // Patients who have completed vitals
    }
}
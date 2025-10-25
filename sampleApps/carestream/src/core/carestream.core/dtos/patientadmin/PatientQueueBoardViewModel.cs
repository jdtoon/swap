using carestream.core.enums;
using System.Collections.Generic;
using System.Linq; // For .Any() in property defaults

namespace carestream.core.dtos.patientadmin
{
    /// <summary>
    /// Represents the view model for the patient queue displayed in a board (Kanban) format.
    /// </summary>
    public class PatientQueueBoardViewModel
    {
        /// <summary>
        /// Gets or sets a dictionary where keys are patient status categories (e.g., "Waiting for Vitals", "Waiting for Doctor")
        /// and values are lists of <see cref="PatientQueueItemDto"/> for that status.
        /// </summary>
        public Dictionary<string, List<PatientQueueItemDto>> PatientsByStatus { get; set; } = new Dictionary<string, List<PatientQueueItemDto>>();

        /// <summary>
        /// Gets or sets the total number of patients across all statuses in the queue.
        /// </summary>
        public int TotalPatientsInQueue { get; set; }

        /// <summary>
        /// Gets or sets the overall average wait time across all patients in the queue (e.g., "25 mins").
        /// </summary>
        public string OverallAverageWaitTime { get; set; } = "N/A";

        /// <summary>
        /// Gets or sets the current active view type ("list" or "board").
        /// </summary>
        public string CurrentViewType { get; set; } = "board"; // Default to board view for this model

        /// <summary>
        /// Gets or sets a list of predefined status columns to ensure order and display even empty columns.
        /// These should ideally match the statuses used in your system.
        /// </summary>
        public List<string> StatusColumnOrder { get; set; } = new List<string>
        {
            VisitStatus.WaitingForVitals.ToString(),
            VisitStatus.VitalsInProgress.ToString(),
            VisitStatus.ReadyForDoctor.ToString(),
            VisitStatus.ConsultationInProgress.ToString(),
            VisitStatus.InTreatment.ToString(),
            VisitStatus.Discharged.ToString(),
            VisitStatus.AdministrativelyClosed.ToString(),
            VisitStatus.PendingPrescription.ToString(),
        };
    }
}
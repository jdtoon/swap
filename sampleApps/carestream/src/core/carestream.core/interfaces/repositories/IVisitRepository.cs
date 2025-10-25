using carestream.core.dtos.doctor;
using carestream.core.dtos.patientadmin;
using carestream.core.dtos.shared;
using carestream.core.dtos.visit;
using carestream.core.dtos.vitals;
using carestream.core.enums;
using System.Data;
using carestream.core.dtos.consultation;

namespace carestream.core.interfaces.repositories
{
    public interface IVisitRepository
    {
        /// <summary>
        /// Updates the status of a visit.
        /// </summary>
        /// <param name="visitId"></param>
        /// <param name="newStatus">The new status for the visit, using VisitStatus enum.</param>
        /// <param name="assignedOfficerUserId"></param>
        /// <returns></returns>
        Task<bool> UpdateVisitStatusAsync(int visitId, VisitStatus newStatus, int? assignedOfficerUserId = null, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Creates a new visit record.
        /// </summary>
        /// <param name="patientId">The ID of the patient.</param>
        /// <param name="initialStatus">The starting status (e.g., WaitingForVitals), using VisitStatus enum.</param>
        /// <param name="checkedInByUserId">The ID of the user performing the check-in.</param>
        /// <returns>The ID of the newly created visit.</returns>
        Task<int> CreateVisitAsync(int patientId, VisitStatus initialStatus, int checkedInByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves the current status of the vitals dashboard.
        /// </summary>
        /// <returns></returns>
        Task<VitalsDashboardStatsDto> GetVitalsDashboardStatsAsync(IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves a list of patients waiting for vitals.
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<VitalsQueueItemDto>> GetVitalsQueueAsync(IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Gets the statistics for the Doctor's dashboard.
        /// </summary>
        /// <returns>A DTO containing Doctor dashboard statistics.</returns>
        Task<DoctorDashboardStatsDto> GetDoctorDashboardStatsAsync(IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Gets the list of patients currently in the queue for doctor consultation
        /// (e.g., status 'ReadyForDoctor').
        /// </summary>
        /// <returns>An enumerable of <see cref="DoctorQueueItemDto"/>.</returns>
        Task<IEnumerable<DoctorQueueItemDto>> GetDoctorPatientQueueAsync(IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves the basic visit information by visit ID.
        /// </summary>
        /// <param name="visitId"></param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        Task<BasicVisitInfoDto?> GetBasicVisitInfoByIdAsync(int visitId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Gets a list of visits currently in 'ConsultationInProgress' status
        /// assigned to a specific doctor.
        /// </summary>
        /// <param name="doctorUserId">The internal user ID of the doctor.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>An enumerable of <see cref="DoctorQueueItemDto"/> representing in-progress consultations.</returns>
        Task<IEnumerable<DoctorQueueItemDto>> GetInProgressConsultationsForDoctorAsync(int doctorUserId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves the doctor notes for a specific visit.
        /// </summary>
        /// <param name="visitId"></param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        Task<string?> GetDoctorNotesAsync(int visitId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Updates the doctor notes for a specific visit.
        /// </summary>
        /// <param name="visitId"></param>
        /// <param name="notes"></param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        Task<bool> UpdateDoctorNotesAsync(int visitId, string? notes, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves the list of patients in the admin queue.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        Task<(IEnumerable<PatientQueueItemDto> Items, int TotalCount)> GetPatientAdminQueueAsync(
            FilterAndPaginationOptions options,
            IDbConnection? connection = null,
            IDbTransaction? transaction = null);

        /// <summary>
        /// Finds the latest active (non-terminal status) visit for a specific patient.
        /// </summary>
        /// <param name="patientId">The unique identifier of the patient.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>An <see cref="ActiveVisitDto"/> representing the active visit, or null if none found.</returns>
        Task<ActiveVisitDto?> FindLatestActiveVisitAsync(int patientId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Creates a new visit record in the database.
        /// </summary>
        /// <param name="patientId">The ID of the patient associated with this visit.</param>
        /// <param name="status">The initial status of the visit (e.g., 'WaitingForVitals'), using VisitStatus enum.</param>
        /// <param name="checkedInByUserId">The ID of the user who performed the check-in.</param>
        /// <param name="briefReason">A brief reason for the visit.</param>
        /// <param name="additionalNotes">Optional additional notes for the visit.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>The ID of the newly created visit.</returns>
        Task<int> CreateVisitAsync(int patientId, VisitStatus status, int checkedInByUserId, string? briefReason = null, string? additionalNotes = null, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Updates the status of an existing visit.
        /// </summary>
        /// <param name="visitId">The ID of the visit to update.</param>
        /// <param name="newStatus">The new status for the visit, using VisitStatus enum.</param>
        /// <param name="actionedByUserId">The ID of the user performing the status update.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>True if the update was successful, false otherwise.</returns>
        Task<bool> UpdateVisitStatusAsync(int visitId, VisitStatus newStatus, int actionedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Updates the visit status and the assigned officer for a visit.
        /// </summary>
        /// <param name="visitId">The ID of the visit to update.</param>
        /// <param name="newStatus">The new status for the visit, using VisitStatus enum.</param>
        /// <param name="assignedOfficerUserId">The ID of the user to assign as the officer.</param>
        /// <param name="actionedByUserId">The ID of the user performing the update.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>True if the update was successful, false otherwise.</returns>
        Task<bool> UpdateVisitStatusAndAssignedOfficerAsync(int visitId, VisitStatus newStatus, int? assignedOfficerUserId, int actionedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves comprehensive data for a DD50 Medical Examination Report for a given visit.
        /// This involves joining data from visits, patients, vital_signs, visit_assessments,
        /// patient_medical_history, visit_diagnoses, visit_procedures.
        /// </summary>
        /// <param name="visitId">The ID of the visit for which to generate the DD50.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>A <see cref="DD50ReportDto"/> containing all relevant data, or null if not found.</returns>
        Task<DD50ReportDto?> GetDD50ReportDataAsync(int visitId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        // NEW METHODS BELOW
        /// <summary>
        /// Links one or more ICD-10 diagnosis codes to a specific patient visit.
        /// </summary>
        /// <param name="visitId">The ID of the visit.</param>
        /// <param name="patientId">The ID of the patient.</param>
        /// <param name="icd10CodeIds">A collection of ICD-10 code IDs to link.</param>
        /// <param name="recordedByUserId">The ID of the user who recorded the diagnosis.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>True if the linking was successful, false otherwise.</returns>
        Task<bool> LinkVisitToDiagnosisAsync(int visitId, int patientId, IEnumerable<int> icd10CodeIds, int recordedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Links one or more medical procedures to a specific patient visit.
        /// </summary>
        /// <param name="visitId">The ID of the visit.</param>
        /// <param name="patientId">The ID of the patient.</param>
        /// <param name="procedureIds">A collection of procedure IDs to link.</param>
        /// <param name="performedByUserId">The ID of the user who performed/recorded the procedure.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>True if the linking was successful, false otherwise.</returns>
        Task<bool> LinkVisitToProcedureAsync(int visitId, int patientId, IEnumerable<int> procedureIds, int performedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves the ICD-10 diagnosis codes linked to a specific patient visit.
        /// </summary>
        /// <param name="visitId">The ID of the visit.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>An enumerable of <see cref="Icd10CodeDto"/> linked to the visit.</returns>
        Task<IEnumerable<Icd10CodeDto>> GetIcd10CodesForVisitAsync(int visitId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves the medical procedures linked to a specific patient visit.
        /// </summary>
        /// <param name="visitId">The ID of the visit.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>An enumerable of <see cref="ProcedureDto"/> linked to the visit.</returns>
        Task<IEnumerable<ProcedureDto>> GetProceduresForVisitAsync(int visitId, IDbConnection? connection = null, IDbTransaction? transaction = null);
    }
}
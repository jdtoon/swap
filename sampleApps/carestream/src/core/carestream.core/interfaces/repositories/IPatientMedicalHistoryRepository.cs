using carestream.core.dtos.patient; // For PatientMedicalHistoryDto, CreateUpdatePatientMedicalHistoryDto
using carestream.core.dtos.shared; // For FilterAndPaginationOptions
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic; // For IEnumerable

namespace carestream.core.interfaces.repositories
{
    /// <summary>
    /// Defines data access operations for patient medical history.
    /// </summary>
    public interface IPatientMedicalHistoryRepository
    {
        /// <summary>
        /// Retrieves all medical history entries for a specific patient.
        /// </summary>
        /// <param name="patientId">The ID of the patient.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>An enumerable of <see cref="PatientMedicalHistoryDto"/> for the patient.</returns>
        Task<IEnumerable<PatientMedicalHistoryDto>> GetMedicalHistoryByPatientIdAsync(int patientId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves a specific medical history entry by its ID.
        /// </summary>
        /// <param name="historyId">The ID of the history entry.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>A <see cref="PatientMedicalHistoryDto"/> if found; otherwise, null.</returns>
        Task<PatientMedicalHistoryDto?> GetMedicalHistoryByIdAsync(int historyId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Creates a new medical history entry for a patient.
        /// </summary>
        /// <param name="historyData">The DTO containing the medical history data.</param>
        /// <param name="patientId">The ID of the patient this history belongs to.</param>
        /// <param name="recordedByUserId">The ID of the user who recorded the entry.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>The ID of the newly created history entry, or 0 if creation failed.</returns>
        Task<int> CreateMedicalHistoryEntryAsync(CreateUpdatePatientMedicalHistoryDto historyData, int patientId, int recordedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Updates an existing medical history entry.
        /// </summary>
        /// <param name="historyData">The DTO containing the updated medical history data.</param>
        /// <param name="recordedByUserId">The ID of the user who updated the entry.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>True if the update was successful, false otherwise.</returns>
        Task<bool> UpdateMedicalHistoryEntryAsync(CreateUpdatePatientMedicalHistoryDto historyData, int recordedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Deactivates a medical history entry.
        /// </summary>
        /// <param name="historyId">The ID of the history entry to deactivate.</param>
        /// <param name="recordedByUserId">The ID of the user who deactivated the entry.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>True if deactivation was successful, false otherwise.</returns>
        Task<bool> DeactivateMedicalHistoryEntryAsync(int historyId, int recordedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Activates an inactive medical history entry.
        /// </summary>
        /// <param name="historyId">The ID of the history entry to activate.</param>
        /// <param name="recordedByUserId">The ID of the user who activated the entry.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>True if activation was successful, false otherwise.</returns>
        Task<bool> ActivateMedicalHistoryEntryAsync(int historyId, int recordedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null);
    }
}
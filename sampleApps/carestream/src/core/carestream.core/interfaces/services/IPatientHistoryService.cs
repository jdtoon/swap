using carestream.core.dtos.patient;

namespace carestream.core.interfaces.services
{
    /// <summary>
    /// Defines the business logic for managing patient medical history.
    /// </summary>
    public interface IPatientHistoryService
    {
        /// <summary>
        /// Retrieves all medical history entries for a specific patient.
        /// </summary>
        /// <param name="patientId">The unique ID of the patient.</param>
        /// <returns>An enumerable of <see cref="PatientMedicalHistoryDto"/> representing the patient's medical history.</returns>
        Task<IEnumerable<PatientMedicalHistoryDto>> GetPatientMedicalHistoryAsync(int patientId);

        /// <summary>
        /// Saves a new or updates an existing patient medical history entry.
        /// This method determines whether to create or update based on the <see cref="CreateUpdatePatientMedicalHistoryDto.HistoryId"/>.
        /// The patient ID is expected to be part of the <paramref name="historyData"/> DTO.
        /// </summary>
        /// <param name="historyData">The DTO containing the medical history data to save.</param>
        /// <param name="performingUserId">The ID of the user performing the save operation.</param>
        /// <returns>True if the history entry was successfully saved (created or updated), false otherwise.</returns>
        Task<bool> SavePatientMedicalHistoryEntryAsync(CreateUpdatePatientMedicalHistoryDto historyData, int performingUserId); // MODIFIED: Removed patientId from signature

        /// <summary>
        /// Deactivates a specific patient medical history entry.
        /// </summary>
        /// <param name="historyId">The ID of the history entry to deactivate.</param>
        /// <param name="performingUserId">The ID of the user performing the deactivation.</param>
        /// <returns>True if the history entry was successfully deactivated, false otherwise.</returns>
        Task<bool> DeactivatePatientMedicalHistoryEntryAsync(int historyId, int performingUserId);

        /// <summary>
        /// Activates a previously deactivated patient medical history entry.
        /// </summary>
        /// <param name="historyId">The ID of the history entry to activate.</param>
        /// <param name="performingUserId">The ID of the user performing the activation.</param>
        /// <returns>True if the history entry was successfully activated, false otherwise.</returns>
        Task<bool> ActivatePatientMedicalHistoryEntryAsync(int historyId, int performingUserId);
    }
}
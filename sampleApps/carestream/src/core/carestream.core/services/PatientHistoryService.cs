using carestream.core.dtos.patient;
using carestream.core.interfaces.repositories;
using carestream.core.interfaces.services;
using Microsoft.Extensions.Logging;

namespace carestream.core.services
{
    /// <summary>
    /// Implements the business logic for managing patient medical history.
    /// </summary>
    public class PatientHistoryService : IPatientHistoryService
    {
        private readonly IPatientMedicalHistoryRepository _patientMedicalHistoryRepository;
        private readonly ILogger<PatientHistoryService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PatientHistoryService"/> class.
        /// </summary>
        /// <param name="patientMedicalHistoryRepository">The patient medical history data repository.</param>
        /// <param name="logger">The logger instance.</param>
        public PatientHistoryService(IPatientMedicalHistoryRepository patientMedicalHistoryRepository, ILogger<PatientHistoryService> logger)
        {
            _patientMedicalHistoryRepository = patientMedicalHistoryRepository ?? throw new ArgumentNullException(nameof(patientMedicalHistoryRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Retrieves all medical history entries for a specific patient.
        /// </summary>
        /// <param name="patientId">The unique ID of the patient.</param>
        /// <returns>An enumerable of <see cref="PatientMedicalHistoryDto"/> representing the patient's medical history.</returns>
        public async Task<IEnumerable<PatientMedicalHistoryDto>> GetPatientMedicalHistoryAsync(int patientId)
        {
            _logger.LogInformation("Service: Getting medical history for PatientId: {PatientId}", patientId);
            if (patientId <= 0)
            {
                _logger.LogWarning("Service: GetPatientMedicalHistoryAsync called with invalid PatientId: {PatientId}", patientId);
                return Enumerable.Empty<PatientMedicalHistoryDto>();
            }
            return await _patientMedicalHistoryRepository.GetMedicalHistoryByPatientIdAsync(patientId);
        }

        /// <summary>
        /// Saves a new or updates an existing patient medical history entry.
        /// This method determines whether to create or update based on the <see cref="CreateUpdatePatientMedicalHistoryDto.HistoryId"/>.
        /// The patient ID is expected to be part of the <paramref name="historyData"/> DTO.
        /// </summary>
        /// <param name="historyData">The DTO containing the medical history data to save.</param>
        /// <param name="patientId">The ID of the patient the history entry belongs to.</param>
        /// <param name="performingUserId">The ID of the user performing the save operation.</param>
        /// <returns>True if the history entry was successfully saved (created or updated), false otherwise.</returns>
        public async Task<bool> SavePatientMedicalHistoryEntryAsync(CreateUpdatePatientMedicalHistoryDto historyData, int performingUserId) // MODIFIED: Removed patientId from signature, now comes from DTO
        {
            _logger.LogInformation("Service: Saving patient medical history entry for PatientId: {PatientId}, HistoryId: {HistoryId}", historyData.PatientId, historyData.HistoryId);

            // Re-validate patientId from DTO
            if (historyData == null || historyData.PatientId <= 0 || performingUserId <= 0) // Updated check
            {
                _logger.LogWarning("Service: SavePatientMedicalHistoryEntryAsync called with invalid input. PatientId: {PatientId}, PerformingUserId: {PerformingUserId}", historyData?.PatientId, performingUserId);
                return false;
            }

            // Basic validation for required fields
            if (string.IsNullOrWhiteSpace(historyData.Type) || string.IsNullOrWhiteSpace(historyData.Description))
            {
                _logger.LogWarning("Service: SavePatientMedicalHistoryEntryAsync failed due to missing Type or Description.");
                return false;
            }

            try
            {
                if (historyData.HistoryId > 0)
                {
                    // Update existing entry
                    bool success = await _patientMedicalHistoryRepository.UpdateMedicalHistoryEntryAsync(historyData, performingUserId);
                    if (success)
                    {
                        _logger.LogInformation("Service: Successfully updated medical history entry {HistoryId} for PatientId {PatientId}.", historyData.HistoryId, historyData.PatientId);
                    }
                    else
                    {
                        _logger.LogError("Service: Failed to update medical history entry {HistoryId} for PatientId {PatientId}.", historyData.HistoryId, historyData.PatientId);
                    }
                    return success;
                }
                else
                {
                    // Create new entry
                    int newHistoryId = await _patientMedicalHistoryRepository.CreateMedicalHistoryEntryAsync(historyData, historyData.PatientId, performingUserId); // Pass historyData.PatientId
                    if (newHistoryId > 0)
                    {
                        _logger.LogInformation("Service: Successfully created new medical history entry {NewHistoryId} for PatientId {PatientId}.", newHistoryId, historyData.PatientId);
                        historyData.HistoryId = newHistoryId; // Update DTO with new ID if needed by caller
                        return true;
                    }
                    else
                    {
                        _logger.LogError("Service: Failed to create new medical history entry for PatientId {PatientId}.", historyData.PatientId);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Service: An error occurred while saving patient medical history for PatientId: {PatientId}, HistoryId: {HistoryId}", historyData.PatientId, historyData.HistoryId);
                return false;
            }
        }

        /// <summary>
        /// Deactivates a specific patient medical history entry.
        /// </summary>
        /// <param name="historyId">The ID of the history entry to deactivate.</param>
        /// <param name="performingUserId">The ID of the user performing the deactivation.</param>
        /// <returns>True if the history entry was successfully deactivated, false otherwise.</returns>
        public async Task<bool> DeactivatePatientMedicalHistoryEntryAsync(int historyId, int performingUserId)
        {
            _logger.LogInformation("Service: Deactivating medical history entry {HistoryId} by User: {PerformingUserId}", historyId, performingUserId);

            if (historyId <= 0 || performingUserId <= 0)
            {
                _logger.LogWarning("Service: DeactivatePatientMedicalHistoryEntryAsync called with invalid IDs. HistoryId: {HistoryId}, PerformingUserId: {PerformingUserId}", historyId, performingUserId);
                return false;
            }
            return await _patientMedicalHistoryRepository.DeactivateMedicalHistoryEntryAsync(historyId, performingUserId);
        }

        /// <summary>
        /// Activates a previously deactivated patient medical history entry.
        /// </summary>
        /// <param name="historyId">The ID of the history entry to activate.</param>
        /// <param name="performingUserId">The ID of the user performing the activation.</param>
        /// <returns>True if the history entry was successfully activated, false otherwise.</returns>
        public async Task<bool> ActivatePatientMedicalHistoryEntryAsync(int historyId, int performingUserId)
        {
            _logger.LogInformation("Service: Activating medical history entry {HistoryId} by User: {PerformingUserId}", historyId, performingUserId);

            if (historyId <= 0 || performingUserId <= 0)
            {
                _logger.LogWarning("Service: ActivatePatientMedicalHistoryEntryAsync called with invalid IDs. HistoryId: {HistoryId}, PerformingUserId: {PerformingUserId}", historyId, performingUserId);
                return false;
            }
            return await _patientMedicalHistoryRepository.ActivateMedicalHistoryEntryAsync(historyId, performingUserId);
        }
    }
}
using carestream.core.dtos.patient;
using carestream.core.dtos.checkin;
using carestream.core.dtos.visit;
using carestream.core.interfaces.repositories;
using carestream.core.interfaces.services;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using carestream.core.enums; // Added for VisitStatus enum

namespace carestream.core.services
{
    /// <summary>
    /// Provides services related to patient management, lookup, and check-in processes.
    /// </summary>
    public class PatientService : IPatientService
    {
        private readonly IPatientRepository _patientRepository;
        private readonly IVisitRepository _visitRepository;
        private readonly ILogger<PatientService> _logger;

        // Removed class-level string constants for statuses.

        /// <summary>
        /// Initializes a new instance of the <see cref="PatientService"/> class.
        /// </summary>
        /// <param name="patientRepository">The patient repository for data access.</param>
        /// <param name="visitRepository">The visit repository for data access.</param>
        /// <param name="logger">The logger for logging service operations.</param>
        public PatientService(
            IPatientRepository patientRepository,
            IVisitRepository visitRepository,
            ILogger<PatientService> logger)
        {
            _patientRepository = patientRepository ?? throw new ArgumentNullException(nameof(patientRepository));
            _visitRepository = visitRepository ?? throw new ArgumentNullException(nameof(visitRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<PatientDetailDto?> GetPatientByForceNumberAsync(string forceNumber)
        {
            _logger.LogInformation("Attempting to get patient by ForceNumber: {ForceNumber}", forceNumber);
            if (string.IsNullOrWhiteSpace(forceNumber))
            {
                _logger.LogWarning("GetPatientByForceNumberAsync called with null or whitespace ForceNumber.");
                return null;
            }
            var patient = await _patientRepository.FindByForceNumberAsync(forceNumber.Trim());
            if (patient == null)
            {
                _logger.LogInformation("No patient found for ForceNumber: {ForceNumber}", forceNumber);
            }
            else
            {
                _logger.LogInformation("Patient found for ForceNumber: {ForceNumber}, PatientId: {PatientId}", forceNumber, patient.PatientId);
            }
            return patient;
        }

        /// <inheritdoc/>
        public async Task<PatientDetailDto?> GetPatientDetailByIdAsync(int patientId)
        {
            _logger.LogInformation("Service: Fetching detailed patient info by PatientId: {PatientId}", patientId);
            return await _patientRepository.GetPatientDetailByIdAsync(patientId);
        }

        /// <inheritdoc/>
        public async Task<ActiveVisitDto?> GetActiveVisitForPatientAsync(int patientId)
        {
            _logger.LogInformation("Checking for active visit for PatientId: {PatientId}", patientId);
            var activeVisit = await _visitRepository.FindLatestActiveVisitAsync(patientId);
            if (activeVisit != null)
            {
                _logger.LogInformation("Active visit found for PatientId: {PatientId}. VisitId: {VisitId}, Status: {Status}",
                    patientId, activeVisit.VisitId, activeVisit.Status);
            }
            else
            {
                _logger.LogInformation("No active visit found for PatientId: {PatientId}", patientId);
            }
            return activeVisit;
        }

        /// <inheritdoc/>
        public async Task<CheckinConfirmationDto> CreateNewVisitAndCheckinAsync(int patientId, int performingUserId, string briefReason, string? additionalNotes)
        {
            string patientNameForLogging = $"PatientId {patientId}";
            _logger.LogInformation(
                "Attempting to create new visit and check-in for PatientId: {PatientId} by PerformingUserId: {PerformingUserId} with BriefReason: '{BriefReason}' and AdditionalNotes: '{AdditionalNotes}'",
                patientId, performingUserId, briefReason, additionalNotes);

            var activeVisit = await _visitRepository.FindLatestActiveVisitAsync(patientId);
            if (activeVisit != null)
            {
                _logger.LogWarning(
                    "CreateNewVisitAndCheckinAsync blocked: Active visit (ID: {ActiveVisitId}, Status: {ActiveVisitStatus}) already exists for PatientId: {PatientId}.",
                    activeVisit.VisitId, activeVisit.Status, patientId);
                return new CheckinConfirmationDto
                {
                    Success = false,
                    PatientName = patientNameForLogging,
                    ErrorMessage = $"Cannot create new visit; an active visit (Status: {activeVisit.Status}) already exists."
                };
            }

            int newVisitId;
            try
            {
                // Using VisitStatus enum
                newVisitId = await _visitRepository.CreateVisitAsync(patientId, VisitStatus.WaitingForVitals, performingUserId, briefReason, additionalNotes);
                _logger.LogInformation(
                    "New visit created (ID: {NewVisitId}) for PatientId: {PatientId} with status '{TargetStatus}' by PerformingUserId: {PerformingUserId}.",
                    newVisitId, patientId, VisitStatus.WaitingForVitals.ToString(), performingUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to create new visit in repository for PatientId: {PatientId} by PerformingUserId: {PerformingUserId}.",
                    patientId, performingUserId);
                return new CheckinConfirmationDto
                {
                    Success = false,
                    PatientName = patientNameForLogging,
                    ErrorMessage = "Failed to create a new visit record due to a database error."
                };
            }

            return new CheckinConfirmationDto
            {
                Success = true,
                PatientName = patientNameForLogging,
                EstimatedWaitTime = "10-15 min (New Visit)",
                NextSteps = "Please proceed to the Vitals area and wait to be called.",
                NotificationTarget = "Vitals Queue"
            };
        }

        /// <inheritdoc/>
        public async Task<CheckinConfirmationDto> ResumeActiveVisitAsync(int visitId, int patientId, int performingUserId)
        {
            _logger.LogInformation("Service: Attempting to resume active VisitId: {VisitId} for PatientId: {PatientId} by User: {PerformingUserId}",
                visitId, patientId, performingUserId);

            if (visitId <= 0 || patientId <= 0)
            {
                _logger.LogWarning("Service: Invalid parameters for ResumeActiveVisitAsync. VisitId: {VisitId}, PatientId: {PatientId}", visitId, patientId);
                return new CheckinConfirmationDto { Success = false, ErrorMessage = "Invalid visit or patient identifier." };
            }

            // Using VisitStatus enum
            VisitStatus newStatusForResumedVisit = VisitStatus.WaitingForVitals;

            bool success = await _visitRepository.UpdateVisitStatusAsync(visitId, newStatusForResumedVisit, performingUserId);

            if (success)
            {
                _logger.LogInformation("Service: Successfully resumed VisitId: {VisitId}, status set to {NewStatus}", visitId, newStatusForResumedVisit.ToString());
                var patientInfo = await _patientRepository.GetPatientBasicInfoByIdAsync(patientId);
                return new CheckinConfirmationDto
                {
                    Success = true,
                    PatientName = patientInfo?.FullName ?? $"Patient {patientId}",
                    NotificationTarget = "Vitals Queue",
                    EstimatedWaitTime = "5-10 minutes (Resumed)",
                    NextSteps = "Patient has been re-added to the queue for vitals.",
                    VisitId = visitId
                };
            }
            else
            {
                _logger.LogError("Service: Failed to update status for resuming VisitId: {VisitId}", visitId);
                return new CheckinConfirmationDto
                {
                    Success = false,
                    ErrorMessage = "Failed to resume the active visit. Please try again.",
                    VisitId = visitId
                };
            }
        }

        /// <inheritdoc/>
        public async Task<CheckinConfirmationDto> CloseAndStartNewVisitAsync(int oldVisitId, int patientId, int performingUserId)
        {
            _logger.LogInformation("Service: Attempting to close OldVisitId: {OldVisitId} and start new visit for PatientId: {PatientId} by User: {PerformingUserId}",
                oldVisitId, patientId, performingUserId);

            if (oldVisitId <= 0 || patientId <= 0)
            {
                _logger.LogWarning("Service: Invalid parameters for CloseAndStartNewVisitAsync. OldVisitId: {OldVisitId}, PatientId: {PatientId}", oldVisitId, patientId);
                return new CheckinConfirmationDto { Success = false, ErrorMessage = "Invalid visit or patient identifier." };
            }

            // Using VisitStatus enum
            VisitStatus closeStatus = VisitStatus.AdministrativelyClosed;
            bool oldVisitClosed = await _visitRepository.UpdateVisitStatusAsync(oldVisitId, closeStatus, performingUserId);

            if (!oldVisitClosed)
            {
                _logger.LogError("Service: Failed to close OldVisitId: {OldVisitId}. Cannot proceed with new check-in.", oldVisitId);
                return new CheckinConfirmationDto
                {
                    Success = false,
                    ErrorMessage = $"Failed to close the previous active visit (ID: {oldVisitId}). Please try again or contact support.",
                    VisitId = oldVisitId
                };
            }
            _logger.LogInformation("Service: Successfully closed OldVisitId: {OldVisitId} with status {Status}.", oldVisitId, closeStatus.ToString());

            _logger.LogInformation("Service: Creating new visit for PatientId: {PatientId} after closing old one.", patientId);
            // Using VisitStatus enum
            VisitStatus newVisitStatus = VisitStatus.WaitingForVitals;
            int newVisitId;
            try
            {
                newVisitId = await _visitRepository.CreateVisitAsync(patientId, newVisitStatus, performingUserId, null, null);
                if (newVisitId <= 0) throw new Exception("CreateVisitAsync returned invalid newVisitId.");
                _logger.LogInformation("Service: New visit created (NewVisitId: {NewVisitId}) with status {Status}.", newVisitId, newVisitStatus.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Service: Failed to create new visit for PatientId: {PatientId} after closing old one.", patientId);
                return new CheckinConfirmationDto { Success = false, PatientName = $"Patient {patientId}", ErrorMessage = "Closed old visit, but failed to create new visit. Please contact support immediately." };
            }

            var patientInfo = await _patientRepository.GetPatientBasicInfoByIdAsync(patientId);
            return new CheckinConfirmationDto
            {
                Success = true,
                PatientName = patientInfo?.FullName ?? $"Patient {patientId}",
                NotificationTarget = "Vitals Queue",
                EstimatedWaitTime = "10-15 minutes (New Visit)",
                NextSteps = "Patient has been checked in with a new visit. Please proceed to Vitals.",
                VisitId = newVisitId
            };
        }

        public Task<CheckinConfirmationDto> CompletePatientCheckinAsync(int patientId, int? visitId)
        {
            throw new NotImplementedException();
        }

        public Task<CheckinConfirmationDto> CheckinPatientAsync(int patientId, int performingUserId)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<EditPatientPersonalInfoDto?> GetPatientPersonalInfoForEditAsync(int patientId)
        {
            _logger.LogInformation("Service: Fetching patient personal info for edit, PatientId: {PatientId}", patientId);

            var patientDetail = await _patientRepository.GetPatientDetailByIdAsync(patientId);
            if (patientDetail == null) return null;

            return new EditPatientPersonalInfoDto
            {
                PatientId = patientDetail.PatientId,
                ForceNumber = patientDetail.ForceNumber,
                Rank = patientDetail.Rank, // Rank is string, not enum here
                FirstName = patientDetail.FirstName,
                LastName = patientDetail.LastName,
                DateOfBirth = patientDetail.DateOfBirth,
                Gender = patientDetail.Gender, // Gender is string, not enum here
                Unit = patientDetail.Unit
            };
        }

        /// <inheritdoc/>
        public async Task<bool> UpdatePatientPersonalInfoAsync(EditPatientPersonalInfoDto patientInfo)
        {
            _logger.LogInformation("Service: Updating personal info for PatientId: {PatientId}", patientInfo.PatientId);
            return await _patientRepository.UpdatePatientPersonalInfoAsync(patientInfo);
        }

        /// <inheritdoc/>
        public async Task<EditPatientContactInfoDto?> GetPatientContactInfoForEditAsync(int patientId)
        {
            _logger.LogInformation("Service: Fetching patient contact info for edit, PatientId: {PatientId}", patientId);
            return await _patientRepository.GetPatientContactInfoForEditAsync(patientId);
        }

        /// <inheritdoc/>
        public async Task<bool> UpdatePatientContactInfoAsync(EditPatientContactInfoDto contactInfo)
        {
            _logger.LogInformation("Service: Updating contact info for PatientId: {PatientId}", contactInfo.PatientId);
            return await _patientRepository.UpdatePatientContactInfoAsync(contactInfo);
        }

        /// <inheritdoc/>
        public async Task<EditPatientEmergencyContactInfoDto?> GetPatientEmergencyContactInfoForEditAsync(int patientId)
        {
            _logger.LogInformation("Service: Fetching patient emergency contact info for edit, PatientId: {PatientId}", patientId);
            return await _patientRepository.GetPatientEmergencyContactInfoForEditAsync(patientId);
        }

        /// <inheritdoc/>
        public async Task<bool> UpdatePatientEmergencyContactInfoAsync(EditPatientEmergencyContactInfoDto emergencyContactInfo)
        {
            _logger.LogInformation("Service: Updating emergency contact info for PatientId: {PatientId}", emergencyContactInfo.PatientId);
            return await _patientRepository.UpdatePatientEmergencyContactInfoAsync(emergencyContactInfo);
        }
    }
}
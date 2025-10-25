using carestream.core.interfaces.services;
using carestream.core.interfaces.repositories;
using carestream.core.dtos.vitals;
using carestream.core.dtos.patient; // Needed for PatientBasicInfoDto
using Microsoft.Extensions.Logging;
using carestream.core.enums; // Added for VisitStatus enum

namespace carestream.core.services
{
    /// <summary>
    /// Service implementation for handling vital signs capture and management.
    /// </summary>
    public class VitalsService : IVitalsService
    {
        private readonly IVitalsRepository _vitalsRepository;
        private readonly IVisitRepository _visitRepository;
        private readonly IPatientRepository _patientRepository; // Correct repository for patient info
        private readonly ILogger<VitalsService> _logger;

        // Removed status constant, using VisitStatus enum directly now.

        /// <summary>
        /// Initializes a new instance of the <see cref="VitalsService"/> class.
        /// </summary>
        /// <param name="vitalsRepository">The repository for vitals data access.</param>
        /// <param name="visitRepository">The repository for visit data access.</param>
        /// <param name="patientRepository">The repository for patient data access.</param>
        /// <param name="logger">The logger for this service.</param>
        public VitalsService(
            IVitalsRepository vitalsRepository,
            IVisitRepository visitRepository,
            IPatientRepository patientRepository, // Correctly injected
            ILogger<VitalsService> logger)
        {
            _vitalsRepository = vitalsRepository ?? throw new ArgumentNullException(nameof(vitalsRepository));
            _visitRepository = visitRepository ?? throw new ArgumentNullException(nameof(visitRepository));
            _patientRepository = patientRepository ?? throw new ArgumentNullException(nameof(patientRepository)); // Assign injected repo
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<VitalsCaptureInputDto?> GetVitalsCaptureModelAsync(int visitId, int patientId)
        {
            _logger.LogInformation("Preparing VitalsCaptureModel for VisitId: {VisitId}, PatientId: {PatientId}", visitId, patientId);

            PatientBasicInfoDto? patientInfo = null;
            try
            {
                // 1. Fetch basic patient details using the correct repository method
                patientInfo = await _patientRepository.GetPatientBasicInfoByIdAsync(patientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching patient basic info for PatientId: {PatientId} in GetVitalsCaptureModelAsync.", patientId);
                // Returning null indicates failure to prepare the essential model context.
                return null;
            }

            if (patientInfo == null)
            {
                _logger.LogWarning("Could not find patient basic info for PatientId: {PatientId} while preparing vitals model.", patientId);
                // Return null as essential patient context is missing.
                return null;
            }
            _logger.LogInformation("Found patient context for PatientId {PatientId}: Rank {Rank}, Name {FirstName} {LastName}",
                patientId, patientInfo.Rank, patientInfo.FirstName, patientInfo.LastName);


            // 2. Check if vitals already exist for this visit
            VitalsCaptureInputDto? existingVitals = null;
            try
            {
                existingVitals = await _vitalsRepository.GetVitalsForVisitAsync(visitId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching existing vitals for VisitId: {VisitId} in GetVitalsCaptureModelAsync.", visitId);
                // Decide if we should proceed with a blank form or fail completely. Proceed for now.
            }

            VitalsCaptureInputDto model;
            if (existingVitals != null)
            {
                _logger.LogInformation("Existing vitals record found for VisitId: {VisitId}. Using existing data.", visitId);
                model = existingVitals; // Use existing data
            }
            else
            {
                _logger.LogInformation("No existing vitals record found for VisitId: {VisitId}. Creating new model.", visitId);
                // Create a new DTO if no existing vitals found
                model = new VitalsCaptureInputDto
                {
                    VisitId = visitId,
                    PatientId = patientId,
                };
            }

            // 3. Populate/overwrite patient context fields from the fetched info
            // Ensure PatientName and PatientRank are set correctly based on fetched patientInfo
            model.PatientName = $"{patientInfo.FirstName} {patientInfo.LastName}";
            model.PatientRank = patientInfo.Rank; // Rank is string in DTO, not enum here

            return model;
        }

        /// <inheritdoc/>
        public async Task<bool> SaveVitalsAsync(VitalsCaptureInputDto inputDto, int performingUserId)
        {
            if (inputDto == null)
            {
                _logger.LogError("SaveVitalsAsync called with null inputDto.");
                throw new ArgumentNullException(nameof(inputDto)); // Throw exception for null input
            }

            _logger.LogInformation(
               "Attempting to save vitals for VisitId: {VisitId}, PatientId: {PatientId} by PerformingUserId: {PerformingUserId}",
               inputDto.VisitId, inputDto.PatientId, performingUserId);

            // Populate server-set fields
            inputDto.RecordedAt = DateTimeOffset.UtcNow;
            inputDto.RecordedByUserId = performingUserId;

            int newVitalsRecordId = 0;
            bool vitalsSaveSuccess = false;
            try
            {
                // 1. Save the vitals record
                newVitalsRecordId = await _vitalsRepository.CreateVitalsRecordAsync(inputDto);
                vitalsSaveSuccess = newVitalsRecordId > 0;

                if (!vitalsSaveSuccess)
                {
                    _logger.LogError("Failed to create vitals record in repository for VisitId: {VisitId} (Returned ID: {VitalsRecordId}).", inputDto.VisitId, newVitalsRecordId);
                    return false; // Exit early if vitals didn't save
                }
                _logger.LogInformation("Successfully created vitals record (ID: {VitalsRecordId}) for VisitId: {VisitId}.", newVitalsRecordId, inputDto.VisitId);

            }
            catch (Exception ex) // Catch exceptions during vitals save specifically
            {
                _logger.LogError(ex, "Exception occurred during VitalsRepository.CreateVitalsRecordAsync for VisitId: {VisitId}.", inputDto.VisitId);
                return false;
            }


            // 2. Update the visit status to "ReadyForDoctor"
            bool statusUpdateSuccess = false;
            try
            {
                // Using VisitStatus enum
                statusUpdateSuccess = await _visitRepository.UpdateVisitStatusAsync(inputDto.VisitId, VisitStatus.ReadyForDoctor, null);
                if (!statusUpdateSuccess)
                {
                    _logger.LogError(
                        "Vitals saved (ID: {VitalsRecordId}), but failed to update visit status to '{TargetStatus}' for VisitId: {VisitId}. Manual reconciliation may be needed.",
                        newVitalsRecordId, VisitStatus.ReadyForDoctor.ToString(), inputDto.VisitId);
                    // Consider this overall failure for now
                    return false;
                }
                _logger.LogInformation("Successfully updated VisitId: {VisitId} status to '{TargetStatus}'.", inputDto.VisitId, VisitStatus.ReadyForDoctor.ToString());
            }
            catch (Exception ex) // Catch exceptions during visit update
            {
                _logger.LogError(ex,
                   "Vitals saved (ID: {VitalsRecordId}), but an exception occurred updating visit status to '{TargetStatus}' for VisitId: {VisitId}.",
                       newVitalsRecordId, VisitStatus.ReadyForDoctor.ToString(), inputDto.VisitId);
                // Consider this overall failure
                return false;
            }

            // TODO: Potentially send notification to Doctor's queue/dashboard

            return true; // Overall success (both vitals saved and status updated)
        }
    }
}
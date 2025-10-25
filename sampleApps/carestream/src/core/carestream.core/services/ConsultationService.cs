using carestream.core.dtos.consultation;
using carestream.core.enums;
using carestream.core.interfaces.repositories;
using carestream.core.interfaces.services;
using Microsoft.Extensions.Logging;

namespace carestream.core.services
{
    /// <summary>
    /// Implements the business logic for the Doctor/Nurse Consultation module.
    /// </summary>
    public class ConsultationService : IConsultationService
    {
        private readonly IPatientRepository _patientRepository;
        private readonly IVisitRepository _visitRepository;
        private readonly IVitalsRepository _vitalsRepository;
        private readonly IIcd10CodeRepository _icd10CodeRepository;
        private readonly IProcedureRepository _procedureRepository;
        private readonly IPatientMedicalHistoryRepository _patientMedicalHistoryRepository;
        private readonly IVisitAssessmentRepository _visitAssessmentRepository;
        private readonly IReferralRepository _referralRepository;
        private readonly ILogger<ConsultationService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsultationService"/> class.
        /// </summary>
        /// <param name="patientRepository">The patient data repository.</param>
        /// <param name="visitRepository">The visit data repository.</param>
        /// <param name="vitalsRepository">The vital signs data repository.</param>
        /// <param name="icd10CodeRepository">The ICD-10 code data repository.</param>
        /// <param name="procedureRepository">The procedure data repository.</param>
        /// <param name="patientMedicalHistoryRepository">The patient medical history data repository.</param>
        /// <param name="visitAssessmentRepository">The visit assessment data repository.</param>
        /// <param name="referralRepository">The referral data repository.</param>
        /// <param name="logger">The logger instance.</param>
        public ConsultationService(
            IPatientRepository patientRepository,
            IVisitRepository visitRepository,
            IVitalsRepository vitalsRepository,
            IIcd10CodeRepository icd10CodeRepository,
            IProcedureRepository procedureRepository,
            IPatientMedicalHistoryRepository patientMedicalHistoryRepository,
            IVisitAssessmentRepository visitAssessmentRepository,
            IReferralRepository referralRepository,
            ILogger<ConsultationService> logger)
        {
            _patientRepository = patientRepository ?? throw new ArgumentNullException(nameof(patientRepository));
            _visitRepository = visitRepository ?? throw new ArgumentNullException(nameof(visitRepository));
            _vitalsRepository = vitalsRepository ?? throw new ArgumentNullException(nameof(vitalsRepository));
            _icd10CodeRepository = icd10CodeRepository ?? throw new ArgumentNullException(nameof(icd10CodeRepository));
            _procedureRepository = procedureRepository ?? throw new ArgumentNullException(nameof(procedureRepository));
            _patientMedicalHistoryRepository = patientMedicalHistoryRepository ?? throw new ArgumentNullException(nameof(patientMedicalHistoryRepository));
            _visitAssessmentRepository = visitAssessmentRepository ?? throw new ArgumentNullException(nameof(visitAssessmentRepository));
            _referralRepository = referralRepository ?? throw new ArgumentNullException(nameof(referralRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Initiates or resumes a consultation session for a given visit, updating its status
        /// to 'ConsultationInProgress' and assigning the performing user as the assigned officer.
        /// </summary>
        /// <param name="visitId">The ID of the visit to start/resume.</param>
        /// <param name="patientId">The ID of the patient associated with the visit (for context/validation).</param>
        /// <param name="performingUserId">The ID of the user (doctor/nurse) who is starting the consultation.</param>
        /// <returns>True if the consultation session was successfully initiated/resumed, false otherwise.</returns>
        public async Task<bool> StartConsultationSessionAsync(int visitId, int patientId, int performingUserId)
        {
            _logger.LogInformation("Service: Attempting to start/resume consultation session for VisitId: {VisitId}, PatientId: {PatientId} by User: {PerformingUserId}", visitId, patientId, performingUserId);

            if (visitId <= 0 || patientId <= 0 || performingUserId <= 0)
            {
                _logger.LogWarning("Service: StartConsultationSessionAsync called with invalid IDs. VisitId: {VisitId}, PatientId: {PatientId}, PerformingUserId: {PerformingUserId}", visitId, patientId, performingUserId);
                return false;
            }

            var currentVisitInfo = await _visitRepository.GetBasicVisitInfoByIdAsync(visitId);
            if (currentVisitInfo == null)
            {
                _logger.LogWarning("Service: Visit ID {VisitId} not found for starting consultation session.", visitId);
                return false;
            }

            VisitStatus newStatus = VisitStatus.ConsultationInProgress;

            // Check if status update is actually needed or if already in desired state
            // and assigned to the same user.
            if (Enum.TryParse(currentVisitInfo.Status, out VisitStatus currentStatusEnum) &&
                currentStatusEnum == newStatus &&
                currentVisitInfo.AssignedOfficerUserId == performingUserId)
            {
                _logger.LogInformation("Service: VisitId {VisitId} is already in '{Status}' status and assigned to User {PerformingUserId}. No status update needed.", visitId, newStatus.ToString(), performingUserId);
                return true; // Already in desired state, consider it successful
            }

            // Update status and assign doctor/nurse
            bool success = await _visitRepository.UpdateVisitStatusAndAssignedOfficerAsync(
                visitId, newStatus, performingUserId, performingUserId); // Last param is actionedByUserId

            if (success)
            {
                _logger.LogInformation("Service: Successfully updated VisitId {VisitId} status to '{NewStatus}' and assigned to User {PerformingUserId}.", visitId, newStatus.ToString(), performingUserId);
                // TODO: Future - Trigger a real-time update event for dashboards (e.g., DoctorDashboard queue)
            }
            else
            {
                _logger.LogError("Service: Failed to update VisitId {VisitId} status to '{NewStatus}' and assign to User {PerformingUserId}.", visitId, newStatus.ToString(), performingUserId);
            }
            return success;
        }

        /// <summary>
        /// Prepares and retrieves the complete view model for the main consultation screen.
        /// This method aggregates data from various repositories.
        /// </summary>
        /// <param name="visitId">The unique ID of the patient's visit.</param>
        /// <param name="patientId">The unique ID of the patient.</param>
        /// <returns>A <see cref="ConsultationViewModel"/> containing all necessary data for the consultation UI.</returns>
        public async Task<ConsultationViewModel> GetConsultationViewModelAsync(int visitId, int patientId)
        {
            _logger.LogInformation("Service: Getting ConsultationViewModel for VisitId: {VisitId}, PatientId: {PatientId}", visitId, patientId);

            var patientBasicInfoTask = _patientRepository.GetPatientBasicInfoByIdAsync(patientId);
            var basicVisitInfoTask = _visitRepository.GetBasicVisitInfoByIdAsync(visitId);
            var vitalsDataTask = _vitalsRepository.GetVitalsForVisitAsync(visitId);
            var doctorNotesTask = _visitRepository.GetDoctorNotesAsync(visitId);
            // NEW: Fetch saved ICD-10 codes and Procedures
            var savedIcd10CodesTask = _visitRepository.GetIcd10CodesForVisitAsync(visitId);
            var savedProceduresTask = _visitRepository.GetProceduresForVisitAsync(visitId);


            await Task.WhenAll(
                patientBasicInfoTask,
                basicVisitInfoTask,
                vitalsDataTask,
                doctorNotesTask,
                savedIcd10CodesTask, // Await new tasks
                savedProceduresTask // Await new tasks
            );

            var patientBasicInfo = patientBasicInfoTask.Result;
            var basicVisitInfo = basicVisitInfoTask.Result;
            var vitalsData = vitalsDataTask.Result;
            var doctorNotes = doctorNotesTask.Result;
            // NEW: Get results of new tasks
            var savedIcd10Codes = savedIcd10CodesTask.Result;
            var savedProcedures = savedProceduresTask.Result;


            if (patientBasicInfo == null || basicVisitInfo == null)
            {
                _logger.LogWarning("Service: Patient or Visit not found for ConsultationViewModel. PatientId: {PatientId}, VisitId: {VisitId}", patientId, visitId);
                // Return a default/empty view model
                return new ConsultationViewModel();
            }

            // Populate PatientBannerDto
            var patientBanner = new PatientBannerDto
            {
                PatientId = patientId,
                VisitId = visitId,
                PatientName = $"{patientBasicInfo.FirstName} {patientBasicInfo.LastName}".Trim(),
                Rank = patientBasicInfo.Rank,
                Age = patientBasicInfo.DateOfBirth.HasValue ? (int?)(DateTime.Today.Year - patientBasicInfo.DateOfBirth.Value.Year) : null,
                Gender = patientBasicInfo.Gender,
                ForceNumber = patientBasicInfo.ForceNumber,
                BriefReasonForVisit = basicVisitInfo.BriefReason,
                VisitTimestamp = basicVisitInfo.VisitTimestamp
            };

            // Populate ConsultationVitalsDisplayDto
            var consultationVitalsDisplay = vitalsData != null ? new ConsultationVitalsDisplayDto
            {
                BloodPressureSystolic = vitalsData.BloodPressureSystolic,
                BloodPressureDiastolic = vitalsData.BloodPressureDiastolic,
                HeartRate = vitalsData.HeartRate,
                Temperature = vitalsData.Temperature,
                RespiratoryRate = vitalsData.RespiratoryRate,
                OxygenSaturation = vitalsData.OxygenSaturation,
                PainLevel = vitalsData.PainLevel,
                UrinalysisColor = vitalsData.UrinalysisColor,
                UrinalysisClarity = vitalsData.UrinalysisClarity,
                UrinalysisSpecificGravity = vitalsData.UrinalysisSpecificGravity,
                UrinalysisPh = vitalsData.UrinalysisPh,
                UrinalysisProtein = vitalsData.UrinalysisProtein,
                UrinalysisGlucose = vitalsData.UrinalysisGlucose,
                ClinicalNotesFromVitals = vitalsData.ClinicalNotes,
                RequiresFollowUp = vitalsData.RequiresFollowUp,
                MarkAsUrgent = vitalsData.MarkAsUrgent,
                RecordedAt = vitalsData.RecordedAt ?? DateTime.UtcNow,
                RecordedByUserName = vitalsData.RecordedByUserName // Assumes this is populated by repo or from user table
            } : null;

            var viewModel = new ConsultationViewModel
            {
                PatientBanner = patientBanner,
                VitalsData = consultationVitalsDisplay,
                DoctorNotes = doctorNotes,
                ActiveTab = "VitalSigns", // Default tab on load
                MedicationsData = null, // No medication data populated here currently
                SavedIcd10Codes = savedIcd10Codes.ToList(), // NEW: Assign saved codes to the view model
                SavedProcedures = savedProcedures.ToList() // NEW: Assign saved procedures to the view model
            };

            return viewModel;
        }

        /// <summary>
        /// Updates the doctor's consultation notes for a specific visit.
        /// </summary>
        /// <param name="visitId">The ID of the visit for which to update notes.</param>
        /// <param name="notes">The new content of the doctor's notes.</param>
        /// <returns>True if the notes were successfully updated, false otherwise.</returns>
        public async Task<bool> UpdateDoctorNotesAsync(int visitId, string? notes)
        {
            _logger.LogInformation("Service: Updating doctor's notes for VisitId: {VisitId}", visitId);
            if (visitId <= 0)
            {
                _logger.LogWarning("Service: UpdateDoctorNotesAsync called with invalid VisitId: {VisitId}", visitId);
                return false;
            }
            return await _visitRepository.UpdateDoctorNotesAsync(visitId, notes);
        }

        /// <summary>
        /// Searches for ICD-10 diagnosis codes based on a search term.
        /// </summary>
        /// <param name="searchTerm">The term to search for (code or description).</param>
        /// <param name="limit">The maximum number of results to return (default is 10).</param>
        /// <returns>An enumerable of <see cref="Icd10CodeDto"/> matching the search criteria.</returns>
        public async Task<IEnumerable<Icd10CodeDto>> SearchIcd10CodesAsync(string searchTerm, int limit = 10)
        {
            _logger.LogInformation("Service: Searching ICD-10 codes for term: '{SearchTerm}' with limit: {Limit}", searchTerm, limit);
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return new List<Icd10CodeDto>();
            }
            return await _icd10CodeRepository.SearchIcd10CodesAsync(searchTerm, limit);
        }

        /// <summary>
        /// Searches for medical procedures based on a search term.
        /// </summary>
        /// <param name="searchTerm">The term to search for (code or name).</param>
        /// <param name="limit">The maximum number of results to return (default is 10).</param>
        /// <returns>An enumerable of <see cref="ProcedureDto"/> matching the search criteria.</returns>
        public async Task<IEnumerable<ProcedureDto>> SearchProceduresAsync(string searchTerm, int limit = 10)
        {
            _logger.LogInformation("Service: Searching procedures for term: '{SearchTerm}' with limit: {Limit}", searchTerm, limit);
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return new List<ProcedureDto>();
            }
            return await _procedureRepository.SearchProceduresAsync(searchTerm, limit);
        }

        /// <summary>
        /// Saves or links one or more ICD-10 diagnosis codes to a specific patient visit.
        /// </summary>
        /// <param name="visitId">The ID of the visit to link diagnoses to.</param>
        /// <param name="patientId">The ID of the patient associated with the visit.</param>
        /// <param name="icd10CodeIds">A collection of unique identifiers for the ICD-10 codes.</param>
        /// <param name="recordedByUserId">The ID of the user who recorded the diagnoses.</param>
        /// <returns>True if the diagnoses were successfully linked, false otherwise.</returns>
        public async Task<bool> SaveVisitDiagnosisAsync(int visitId, int patientId, IEnumerable<int> icd10CodeIds, int recordedByUserId)
        {
            _logger.LogInformation("Service: Saving diagnosis for VisitId: {VisitId}, PatientId: {PatientId} with {Count} codes.", visitId, patientId, icd10CodeIds?.Count() ?? 0);
            if (visitId <= 0 || patientId <= 0 || recordedByUserId <= 0)
            {
                _logger.LogWarning("Service: SaveVisitDiagnosisAsync called with invalid IDs. VisitId: {VisitId}, PatientId: {PatientId}, RecordedByUserId: {RecordedByUserId}", visitId, patientId, recordedByUserId);
                return false;
            }
            return await _visitRepository.LinkVisitToDiagnosisAsync(visitId, patientId, icd10CodeIds, recordedByUserId);
        }

        /// <summary>
        /// Saves or links one or more medical procedures to a specific patient visit.
        /// </summary>
        /// <param name="visitId">The ID of the visit to link procedures to.</param>
        /// <param name="patientId">The ID of the patient associated with the visit.</param>
        /// <param name="procedureIds">A collection of unique identifiers for the procedures.</param>
        /// <param name="performedByUserId">The ID of the user who performed/recorded the procedures.</param>
        /// <returns>True if the procedures were successfully linked, false otherwise.</returns>
        public async Task<bool> SaveVisitProceduresAsync(int visitId, int patientId, IEnumerable<int> procedureIds, int performedByUserId)
        {
            _logger.LogInformation("Service: Saving procedures for VisitId: {VisitId}, PatientId: {PatientId} with {Count} procedures.", visitId, patientId, procedureIds?.Count() ?? 0);
            if (visitId <= 0 || patientId <= 0 || performedByUserId <= 0)
            {
                _logger.LogWarning("Service: SaveVisitProceduresAsync called with invalid IDs. VisitId: {VisitId}, PatientId: {PatientId}, PerformedByUserId: {PerformedByUserId}", visitId, patientId, performedByUserId);
                return false;
            }
            return await _visitRepository.LinkVisitToProcedureAsync(visitId, patientId, procedureIds, performedByUserId);
        }

        /// <summary>
        /// Finalizes a patient consultation, typically by updating the visit status to 'Discharged'.
        /// </summary>
        /// <param name="visitId">The ID of the visit to finalize.</param>
        /// <param name="performingUserId">The ID of the user performing the finalization.</param>
        /// <returns>True if the consultation was successfully finalized, false otherwise.</returns>
        public async Task<bool> FinalizeConsultationAsync(int visitId, int performingUserId)
        {
            _logger.LogInformation("Service: Finalizing consultation for VisitId: {VisitId} by User: {PerformingUserId}", visitId, performingUserId);

            if (visitId <= 0 || performingUserId <= 0)
            {
                _logger.LogWarning("Service: FinalizeConsultationAsync called with invalid IDs. VisitId: {VisitId}, PerformingUserId: {PerformingUserId}", visitId, performingUserId);
                return false;
            }

            // Update the visit status to Discharged
            bool success = await _visitRepository.UpdateVisitStatusAsync(visitId, VisitStatus.Discharged, performingUserId);

            if (success)
            {
                _logger.LogInformation("Service: Successfully finalized VisitId {VisitId} and set status to Discharged.", visitId);
                // TODO: Future - Trigger a notification to pharmacy if there are pending prescriptions
            }
            else
            {
                _logger.LogError("Service: Failed to finalize consultation for VisitId {VisitId}.", visitId);
            }
            return success;
        }
    }
}
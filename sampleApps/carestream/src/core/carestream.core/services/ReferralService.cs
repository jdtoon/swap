using carestream.core.dtos.consultation;
using carestream.core.interfaces.repositories;
using carestream.core.interfaces.services;
using Microsoft.Extensions.Logging;

namespace carestream.core.services
{
    /// <summary>
    /// Implements the business logic for managing patient referrals.
    /// </summary>
    public class ReferralService : IReferralService
    {
        private readonly IReferralRepository _referralRepository;
        private readonly ILogger<ReferralService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferralService"/> class.
        /// </summary>
        /// <param name="referralRepository">The referral data repository.</param>
        /// <param name="logger">The logger instance.</param>
        public ReferralService(IReferralRepository referralRepository, ILogger<ReferralService> logger)
        {
            _referralRepository = referralRepository ?? throw new ArgumentNullException(nameof(referralRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a new patient referral record.
        /// </summary>
        /// <param name="referralData">The DTO containing the referral data to create.</param>
        /// <returns>The ID of the newly created referral, or 0 if creation failed.</returns>
        public async Task<int> CreateReferralAsync(CreateUpdateReferralDto referralData)
        {
            _logger.LogInformation("Service: Creating new referral for PatientId: {PatientId} to Department: {ReferringDepartment}", referralData.PatientId, referralData.ReferredToDepartmentId);

            if (referralData == null || referralData.PatientId <= 0 || referralData.ReferredToDepartmentId == null)
            {
                _logger.LogWarning("Service: CreateReferralAsync called with invalid input. PatientId: {PatientId}, ReferringDepartment: {ReferringDepartment}", referralData?.PatientId, referralData?.ReferredToDepartmentId);
                return 0;
            }

            try
            {
                int newReferralId = await _referralRepository.CreateReferralAsync(referralData);
                if (newReferralId > 0)
                {
                    _logger.LogInformation("Service: Successfully created new referral with ID: {ReferralId}", newReferralId);
                    referralData.ReferralId = newReferralId; // Update DTO with new ID if needed by caller
                }
                else
                {
                    _logger.LogError("Service: Failed to create new referral.");
                }
                return newReferralId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Service: An error occurred while creating referral for PatientId: {PatientId}", referralData.PatientId);
                return 0;
            }
        }

        /// <summary>
        /// Updates the status of an existing patient referral.
        /// </summary>
        /// <param name="referralId">The ID of the referral to update.</param>
        /// <param name="newStatus">The new status for the referral (e.g., "Pending", "Accepted", "Completed").</param>
        /// <param name="completedByUserId">The ID of the user who completed the referral (optional).</param>
        /// <returns>True if the referral status was successfully updated, false otherwise.</returns>
        public async Task<bool> UpdateReferralStatusAsync(int referralId, string newStatus, int? completedByUserId)
        {
            _logger.LogInformation("Service: Updating referral status for ReferralId: {ReferralId} to '{NewStatus}' by User: {CompletedByUserId}", referralId, newStatus, completedByUserId);

            if (referralId <= 0 || string.IsNullOrWhiteSpace(newStatus))
            {
                _logger.LogWarning("Service: UpdateReferralStatusAsync called with invalid input. ReferralId: {ReferralId}, NewStatus: '{NewStatus}'", referralId, newStatus);
                return false;
            }
            return await _referralRepository.UpdateReferralStatusAsync(referralId, newStatus, completedByUserId);
        }

        /// <summary>
        /// Retrieves all referral records associated with a specific patient visit.
        /// </summary>
        /// <param name="visitId">The unique ID of the visit.</param>
        /// <returns>An enumerable of <see cref="ReferralDto"/> representing the referrals for the visit.</returns>
        public async Task<IEnumerable<ReferralDto>> GetReferralsForVisitAsync(int visitId)
        {
            _logger.LogInformation("Service: Getting referrals for VisitId: {VisitId}", visitId);
            if (visitId <= 0)
            {
                _logger.LogWarning("Service: GetReferralsForVisitAsync called with invalid VisitId: {VisitId}", visitId);
                return Enumerable.Empty<ReferralDto>();
            }
            return await _referralRepository.GetReferralsByVisitIdAsync(visitId);
        }

        /// <summary>
        /// Retrieves all referral records for a specific patient, representing their referral history.
        /// </summary>
        /// <param name="patientId">The unique ID of the patient.</param>
        /// <returns>An enumerable of <see cref="ReferralDto"/> representing the patient's referral history.</returns>
        public async Task<IEnumerable<ReferralDto>> GetPatientReferralHistoryAsync(int patientId)
        {
            _logger.LogInformation("Service: Getting patient referral history for PatientId: {PatientId}", patientId);
            if (patientId <= 0)
            {
                _logger.LogWarning("Service: GetPatientReferralHistoryAsync called with invalid PatientId: {PatientId}", patientId);
                return Enumerable.Empty<ReferralDto>();
            }
            return await _referralRepository.GetPatientReferralHistoryAsync(patientId);
        }
    }
}
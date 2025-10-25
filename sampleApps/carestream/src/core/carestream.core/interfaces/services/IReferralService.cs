using carestream.core.dtos.consultation;

namespace carestream.core.interfaces.services
{
    /// <summary>
    /// Defines the business logic for managing patient referrals.
    /// </summary>
    public interface IReferralService
    {
        /// <summary>
        /// Creates a new patient referral record.
        /// </summary>
        /// <param name="referralData">The DTO containing the referral data to create.</param>
        /// <returns>The ID of the newly created referral, or 0 if creation failed.</returns>
        Task<int> CreateReferralAsync(CreateUpdateReferralDto referralData);

        /// <summary>
        /// Updates the status of an existing patient referral.
        /// </summary>
        /// <param name="referralId">The ID of the referral to update.</param>
        /// <param name="newStatus">The new status for the referral (e.g., "Pending", "Accepted", "Completed").</param>
        /// <param name="completedByUserId">The ID of the user who completed the referral (optional).</param>
        /// <returns>True if the referral status was successfully updated, false otherwise.</returns>
        Task<bool> UpdateReferralStatusAsync(int referralId, string newStatus, int? completedByUserId);

        /// <summary>
        /// Retrieves all referral records associated with a specific patient visit.
        /// </summary>
        /// <param name="visitId">The unique ID of the visit.</param>
        /// <returns>An enumerable of <see cref="ReferralDto"/> representing the referrals for the visit.</returns>
        Task<IEnumerable<ReferralDto>> GetReferralsForVisitAsync(int visitId);

        /// <summary>
        /// Retrieves all referral records for a specific patient, representing their referral history.
        /// </summary>
        /// <param name="patientId">The unique ID of the patient.</param>
        /// <returns>An enumerable of <see cref="ReferralDto"/> representing the patient's referral history.</returns>
        Task<IEnumerable<ReferralDto>> GetPatientReferralHistoryAsync(int patientId);
    }
}
using carestream.core.dtos.consultation; // For ReferralDto, CreateUpdateReferralDto
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic; // For IEnumerable

namespace carestream.core.interfaces.repositories
{
    /// <summary>
    /// Defines data access operations for patient referrals.
    /// </summary>
    public interface IReferralRepository
    {
        /// <summary>
        /// Retrieves a referral by its unique ID.
        /// </summary>
        /// <param name="referralId">The ID of the referral.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>A <see cref="ReferralDto"/> if found; otherwise, null.</returns>
        Task<ReferralDto?> GetReferralByIdAsync(int referralId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves all referrals associated with a specific visit.
        /// </summary>
        /// <param name="visitId">The ID of the visit.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>An enumerable of <see cref="ReferralDto"/> for the visit.</returns>
        Task<IEnumerable<ReferralDto>> GetReferralsByVisitIdAsync(int visitId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves all referrals for a specific patient.
        /// </summary>
        /// <param name="patientId">The ID of the patient.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>An enumerable of <see cref="ReferralDto"/> for the patient.</returns>
        Task<IEnumerable<ReferralDto>> GetPatientReferralHistoryAsync(int patientId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Creates a new referral record.
        /// </summary>
        /// <param name="referralData">The DTO containing the referral data.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>The ID of the newly created referral, or 0 if creation failed.</returns>
        Task<int> CreateReferralAsync(CreateUpdateReferralDto referralData, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Updates the status of an existing referral record.
        /// </summary>
        /// <param name="referralId">The ID of the referral to update.</param>
        /// <param name="newStatus">The new status for the referral (e.g., "Pending", "Accepted", "Completed").</param>
        /// <param name="completedByUserId">The ID of the user completing the referral (optional).</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>True if the update was successful, false otherwise.</returns>
        Task<bool> UpdateReferralStatusAsync(int referralId, string newStatus, int? completedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Updates an existing referral record (general fields).
        /// </summary>
        /// <param name="referralData">The DTO containing the updated referral data.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>True if the update was successful, false otherwise.</returns>
        Task<bool> UpdateReferralAsync(CreateUpdateReferralDto referralData, IDbConnection? connection = null, IDbTransaction? transaction = null);
    }
}
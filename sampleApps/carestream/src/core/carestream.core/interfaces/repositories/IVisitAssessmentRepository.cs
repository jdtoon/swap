using carestream.core.dtos.consultation; // For VisitAssessmentDto, CreateUpdateVisitAssessmentDto
using System.Data;
using System.Threading.Tasks;

namespace carestream.core.interfaces.repositories
{
    /// <summary>
    /// Defines data access operations for visit assessments.
    /// </summary>
    public interface IVisitAssessmentRepository
    {
        /// <summary>
        /// Retrieves a visit assessment by its unique ID.
        /// </summary>
        /// <param name="assessmentId">The ID of the assessment.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>A <see cref="VisitAssessmentDto"/> if found; otherwise, null.</returns>
        Task<VisitAssessmentDto?> GetVisitAssessmentByIdAsync(int assessmentId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves the latest assessment for a specific visit.
        /// </summary>
        /// <param name="visitId">The ID of the visit.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>A <see cref="VisitAssessmentDto"/> if found; otherwise, null.</returns>
        Task<VisitAssessmentDto?> GetLatestAssessmentForVisitAsync(int visitId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Creates a new visit assessment record.
        /// </summary>
        /// <param name="assessmentData">The DTO containing the assessment data.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>The ID of the newly created assessment, or 0 if creation failed.</returns>
        Task<int> CreateVisitAssessmentAsync(CreateUpdateVisitAssessmentDto assessmentData, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Updates an existing visit assessment record.
        /// </summary>
        /// <param name="assessmentData">The DTO containing the updated assessment data.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>True if the update was successful, false otherwise.</returns>
        Task<bool> UpdateVisitAssessmentAsync(CreateUpdateVisitAssessmentDto assessmentData, IDbConnection? connection = null, IDbTransaction? transaction = null);
    }
}
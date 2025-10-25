using carestream.core.dtos.consultation;

namespace carestream.core.interfaces.services
{
    /// <summary>
    /// Defines the business logic for managing patient visit assessments.
    /// </summary>
    public interface IVisitAssessmentService
    {
        /// <summary>
        /// Retrieves the latest visit assessment for a given visit.
        /// </summary>
        /// <param name="visitId">The unique ID of the visit.</param>
        /// <returns>A <see cref="VisitAssessmentDto"/> representing the latest assessment, or null if no assessment is found for the visit.</returns>
        Task<VisitAssessmentDto?> GetVisitAssessmentAsync(int visitId);

        /// <summary>
        /// Saves a new or updates an existing patient visit assessment.
        /// This method determines whether to create or update based on the <see cref="CreateUpdateVisitAssessmentDto.AssessmentId"/>.
        /// </summary>
        /// <param name="assessmentData">The DTO containing the assessment data to save.</param>
        /// <returns>True if the assessment was successfully saved (created or updated), false otherwise.</returns>
        Task<bool> SaveVisitAssessmentAsync(CreateUpdateVisitAssessmentDto assessmentData);
    }
}
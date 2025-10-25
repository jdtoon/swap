using carestream.core.dtos.consultation;
using carestream.core.interfaces.repositories;
using carestream.core.interfaces.services;
using Microsoft.Extensions.Logging;

namespace carestream.core.services
{
    /// <summary>
    /// Implements the business logic for managing patient visit assessments.
    /// </summary>
    public class VisitAssessmentService : IVisitAssessmentService
    {
        private readonly IVisitAssessmentRepository _visitAssessmentRepository;
        private readonly ILogger<VisitAssessmentService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="VisitAssessmentService"/> class.
        /// </summary>
        /// <param name="visitAssessmentRepository">The visit assessment data repository.</param>
        /// <param name="logger">The logger instance.</param>
        public VisitAssessmentService(IVisitAssessmentRepository visitAssessmentRepository, ILogger<VisitAssessmentService> logger)
        {
            _visitAssessmentRepository = visitAssessmentRepository ?? throw new ArgumentNullException(nameof(visitAssessmentRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Retrieves the latest visit assessment for a given visit.
        /// </summary>
        /// <param name="visitId">The unique ID of the visit.</param>
        /// <returns>A <see cref="VisitAssessmentDto"/> representing the latest assessment, or null if no assessment is found for the visit.</returns>
        public async Task<VisitAssessmentDto?> GetVisitAssessmentAsync(int visitId)
        {
            _logger.LogInformation("Service: Getting latest visit assessment for VisitId: {VisitId}", visitId);
            if (visitId <= 0)
            {
                _logger.LogWarning("Service: GetVisitAssessmentAsync called with invalid VisitId: {VisitId}", visitId);
                return null;
            }
            return await _visitAssessmentRepository.GetLatestAssessmentForVisitAsync(visitId);
        }

        /// <summary>
        /// Saves a new or updates an existing patient visit assessment.
        /// This method determines whether to create or update based on the <see cref="CreateUpdateVisitAssessmentDto.AssessmentId"/>.
        /// </summary>
        /// <param name="assessmentData">The DTO containing the assessment data to save.</param>
        /// <returns>True if the assessment was successfully saved (created or updated), false otherwise.</returns>
        public async Task<bool> SaveVisitAssessmentAsync(CreateUpdateVisitAssessmentDto assessmentData)
        {
            _logger.LogInformation("Service: Saving visit assessment. AssessmentId: {AssessmentId}, VisitId: {VisitId}", assessmentData.VisitAssessmentId, assessmentData.VisitId);

            if (assessmentData == null || assessmentData.VisitId <= 0)
            {
                _logger.LogWarning("Service: SaveVisitAssessmentAsync called with invalid input. VisitId: {VisitId}", assessmentData?.VisitId);
                return false;
            }

            try
            {
                if (assessmentData.VisitAssessmentId > 0)
                {
                    // Update existing entry
                    bool success = await _visitAssessmentRepository.UpdateVisitAssessmentAsync(assessmentData);
                    if (success)
                    {
                        _logger.LogInformation("Service: Successfully updated visit assessment {AssessmentId} for VisitId {VisitId}.", assessmentData.VisitAssessmentId, assessmentData.VisitId);
                    }
                    else
                    {
                        _logger.LogError("Service: Failed to update visit assessment {AssessmentId} for VisitId {VisitId}.", assessmentData.VisitAssessmentId, assessmentData.VisitId);
                    }
                    return success;
                }
                else
                {
                    // Create new entry
                    int newAssessmentId = await _visitAssessmentRepository.CreateVisitAssessmentAsync(assessmentData);
                    if (newAssessmentId > 0)
                    {
                        _logger.LogInformation("Service: Successfully created new visit assessment {NewAssessmentId} for VisitId {VisitId}.", newAssessmentId, assessmentData.VisitId);
                        assessmentData.VisitAssessmentId = newAssessmentId; // Update DTO with new ID if needed by caller
                        return true;
                    }
                    else
                    {
                        _logger.LogError("Service: Failed to create new visit assessment for VisitId {VisitId}.", assessmentData.VisitId);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Service: An error occurred while saving visit assessment for VisitId: {VisitId}, AssessmentId: {AssessmentId}", assessmentData.VisitId, assessmentData.VisitAssessmentId);
                return false;
            }
        }
    }
}
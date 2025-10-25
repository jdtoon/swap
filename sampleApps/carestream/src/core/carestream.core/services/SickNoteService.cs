using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using carestream.core.interfaces.services;
using carestream.core.interfaces.repositories;
using carestream.core.dtos.consultation;

namespace carestream.core.services
{
    public class SickNoteService : ISickNoteService
    {
        private readonly ISickNoteRepository _sickNoteRepository;
        private readonly ILogger<SickNoteService> _logger;

        public SickNoteService(
            ISickNoteRepository sickNoteRepository,
            ILogger<SickNoteService> logger)
        {
            _sickNoteRepository = sickNoteRepository ?? throw new ArgumentNullException(nameof(sickNoteRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<SickNoteInputDto?> GetSickNoteForVisitAsync(int visitId)
        {
            _logger.LogInformation("Service: Fetching sick note for VisitId: {VisitId}", visitId);
            return await _sickNoteRepository.GetSickNoteByVisitIdAsync(visitId);
        }

        public async Task<SickNoteInputDto?> SaveSickNoteAsync(SickNoteInputDto sickNoteData, int performingUserId)
        {
            _logger.LogInformation("Service: Attempting to save sick note for VisitId: {VisitId} by User: {PerformingUserId}",
                sickNoteData.VisitId, performingUserId);

            // Basic validation (more can be added)
            if (sickNoteData.StartDate.HasValue && sickNoteData.EndDate.HasValue && sickNoteData.EndDate < sickNoteData.StartDate)
            {
                _logger.LogWarning("Service: Invalid sick note dates - EndDate cannot be before StartDate for VisitId: {VisitId}", sickNoteData.VisitId);
                // Optionally throw a specific validation exception or return a DTO indicating failure
                // For now, let repository handle if it has similar checks or let controller catch
                // For a service, it's better to return a result object or throw.
                // For simplicity, we'll let it proceed to repo for now, but this is a point for refinement.
            }

            SickNoteInputDto? result;
            if (sickNoteData.SickNoteId.HasValue && sickNoteData.SickNoteId.Value > 0)
            {
                // Update existing sick note
                _logger.LogInformation("Service: Updating existing SickNoteId: {SickNoteId}", sickNoteData.SickNoteId.Value);
                result = await _sickNoteRepository.UpdateSickNoteAsync(sickNoteData, performingUserId);
            }
            else
            {
                // Create new sick note
                _logger.LogInformation("Service: Creating new sick note for VisitId: {VisitId}", sickNoteData.VisitId);
                result = await _sickNoteRepository.CreateSickNoteAsync(sickNoteData, performingUserId);
            }

            if (result == null)
            {
                _logger.LogWarning("Service: SaveSickNoteAsync - Repository operation failed for VisitId: {VisitId}", sickNoteData.VisitId);
            }
            else
            {
                _logger.LogInformation("Service: Sick note successfully saved/updated for VisitId: {VisitId}, new/updated SickNoteId: {SickNoteId}", result.VisitId, result.SickNoteId);
            }
            return result;
        }
    }
}
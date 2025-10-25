using carestream.core.dtos.consultation;
using carestream.core.interfaces.repositories;
using carestream.core.interfaces.services;
using Microsoft.Extensions.Logging;

namespace carestream.core.services
{
    /// <summary>
    /// Implements the business logic for generating DD50 Medical Examination Reports.
    /// </summary>
    public class DD50ReportService : IDD50ReportService
    {
        private readonly IVisitRepository _visitRepository;
        private readonly ILogger<DD50ReportService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DD50ReportService"/> class.
        /// </summary>
        /// <param name="visitRepository">The visit data repository.</param>
        /// <param name="logger">The logger instance.</param>
        public DD50ReportService(IVisitRepository visitRepository, ILogger<DD50ReportService> logger)
        {
            _visitRepository = visitRepository ?? throw new ArgumentNullException(nameof(visitRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Generates a comprehensive DD50 Medical Examination Report for a given patient visit.
        /// This method aggregates data from various sources to compile the report.
        /// </summary>
        /// <param name="visitId">The unique ID of the visit for which to generate the report.</param>
        /// <returns>A <see cref="DD50ReportDto"/> containing all data points for the DD50 report, or null if the visit data cannot be found.</returns>
        public async Task<DD50ReportDto?> GenerateDD50ReportAsync(int visitId)
        {
            _logger.LogInformation("Service: Generating DD50 report for VisitId: {VisitId}", visitId);

            if (visitId <= 0)
            {
                _logger.LogWarning("Service: GenerateDD50ReportAsync called with invalid VisitId: {VisitId}", visitId);
                return null;
            }

            try
            {
                // The heavy lifting of data aggregation for the DD50 is expected to be in the repository.
                var reportData = await _visitRepository.GetDD50ReportDataAsync(visitId);

                if (reportData == null)
                {
                    _logger.LogWarning("Service: No DD50 report data found for VisitId: {VisitId}. Report cannot be generated.", visitId);
                }
                else
                {
                    _logger.LogInformation("Service: Successfully retrieved DD50 report data for VisitId: {VisitId}.", visitId);
                    // Additional business logic or formatting could go here if needed
                }

                return reportData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Service: An error occurred while generating DD50 report for VisitId: {VisitId}.", visitId);
                return null;
            }
        }
    }
}
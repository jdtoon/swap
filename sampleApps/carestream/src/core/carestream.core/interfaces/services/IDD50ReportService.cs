using carestream.core.dtos.consultation; // For DD50ReportDto

namespace carestream.core.interfaces.services
{
    /// <summary>
    /// Defines the business logic for generating DD50 Medical Examination Reports.
    /// </summary>
    public interface IDD50ReportService
    {
        /// <summary>
        /// Generates a comprehensive DD50 Medical Examination Report for a given patient visit.
        /// This method aggregates data from various sources to compile the report.
        /// </summary>
        /// <param name="visitId">The unique ID of the visit for which to generate the report.</param>
        /// <returns>A <see cref="DD50ReportDto"/> containing all data points for the DD50 report, or null if the visit data cannot be found.</returns>
        Task<DD50ReportDto?> GenerateDD50ReportAsync(int visitId);
    }
}
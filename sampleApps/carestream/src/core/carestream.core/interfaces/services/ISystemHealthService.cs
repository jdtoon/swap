using carestream.core.dtos.admin.systemhealth;

namespace carestream.core.interfaces.services
{
    /// <summary>
    /// Defines the business logic for providing system health status information.
    /// </summary>
    public interface ISystemHealthService
    {
        /// <summary>
        /// Retrieves a comprehensive dashboard DTO containing the health status of various system components.
        /// </summary>
        /// <returns>A <see cref="SystemHealthDashboardDto"/> with aggregated system health information.</returns>
        Task<SystemHealthDashboardDto> GetSystemHealthDashboardAsync();
    }
}
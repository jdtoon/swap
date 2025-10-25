using carestream.core.dtos.admin.systemhealth;
using carestream.core.interfaces.repositories;
using carestream.core.interfaces.services;
using Microsoft.Extensions.Logging;

namespace carestream.core.services
{
    /// <summary>
    /// Implements the business logic for providing system health status information.
    /// </summary>
    public class SystemHealthService : ISystemHealthService
    {
        private readonly ISystemHealthRepository _systemHealthRepository;
        private readonly ILogger<SystemHealthService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemHealthService"/> class.
        /// </summary>
        /// <param name="systemHealthRepository">The system health data repository.</param>
        /// <param name="logger">The logger instance.</param>
        public SystemHealthService(ISystemHealthRepository systemHealthRepository, ILogger<SystemHealthService> logger)
        {
            _systemHealthRepository = systemHealthRepository ?? throw new ArgumentNullException(nameof(systemHealthRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Retrieves a comprehensive dashboard DTO containing the health status of various system components.
        /// </summary>
        /// <returns>A <see cref="SystemHealthDashboardDto"/> with aggregated system health information.</returns>
        public async Task<SystemHealthDashboardDto> GetSystemHealthDashboardAsync()
        {
            _logger.LogInformation("Service: Getting system health dashboard information.");

            var componentStatuses = new List<SystemHealthDto>();
            string overallStatus = "Operational";
            string overallMessage = "All core systems are functioning normally.";

            try
            {
                // 1. Check Database Health
                var dbHealth = await _systemHealthRepository.GetDatabaseHealthStatusAsync();
                if (dbHealth != null)
                {
                    componentStatuses.Add(dbHealth);
                    if (dbHealth.Status != "Healthy")
                    {
                        overallStatus = "Degraded";
                        overallMessage = "Some systems are experiencing issues. Database status: " + dbHealth.Status;
                    }
                }
                else
                {
                    componentStatuses.Add(new SystemHealthDto
                    {
                        Component = "Database",
                        Status = "Unreachable",
                        LastChecked = DateTimeOffset.UtcNow,
                        Details = "Failed to connect to the database."
                    });
                    overallStatus = "Critical";
                    overallMessage = "Database is unreachable. Core functionality may be affected.";
                }

                // 2. Placeholder for External API Health (e.g., Logto, payment gateway if applicable)
                // In a real scenario, you would make HTTP calls to health endpoints or use dedicated clients.
                var apiHealth = new SystemHealthDto
                {
                    Component = "Logto Identity Provider",
                    Status = "Healthy", // Placeholder
                    LastChecked = DateTimeOffset.UtcNow,
                    Details = "External authentication service reachable."
                };
                // Example of how to simulate an unhealthy status
                // if (new Random().Next(0, 10) < 2) // 20% chance of degraded
                // {
                //     apiHealth.Status = "Degraded";
                //     apiHealth.Details = "Occasional timeouts detected.";
                //     if (overallStatus == "Operational") overallStatus = "Degraded"; // Degrade overall if not already critical
                // }
                componentStatuses.Add(apiHealth);

                // 3. Placeholder for Storage Health (e.g., Azure Blob Storage)
                var storageHealth = new SystemHealthDto
                {
                    Component = "File Storage",
                    Status = "Healthy", // Placeholder
                    LastChecked = DateTimeOffset.UtcNow,
                    Details = "Document storage service reachable."
                };
                // if (new Random().Next(0, 10) < 1) // 10% chance of unhealthy
                // {
                //     storageHealth.Status = "Unhealthy";
                //     storageHealth.Details = "Write operations are slow.";
                //     overallStatus = "Critical"; // Critical if storage is unhealthy
                // }
                componentStatuses.Add(storageHealth);

                // Re-evaluate overall status based on all components if necessary
                if (componentStatuses.Any(c => c.Status == "Critical" || c.Status == "Unreachable"))
                {
                    overallStatus = "Critical";
                    overallMessage = "One or more critical system components are unavailable.";
                }
                else if (componentStatuses.Any(c => c.Status == "Degraded" || c.Status == "Unhealthy"))
                {
                    if (overallStatus == "Operational") // Only set to degraded if not already critical
                    {
                        overallStatus = "Degraded";
                        overallMessage = "Some system components are operating with reduced performance or issues.";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Service: An unexpected error occurred while compiling system health dashboard.");
                overallStatus = "Unknown";
                overallMessage = $"An unexpected error occurred during health check: {ex.Message}";
                componentStatuses.Add(new SystemHealthDto
                {
                    Component = "System Health Service",
                    Status = "Error",
                    LastChecked = DateTimeOffset.UtcNow,
                    Details = ex.Message
                });
            }

            return new SystemHealthDashboardDto
            {
                ComponentStatuses = componentStatuses,
                OverallStatus = overallStatus,
                Message = overallMessage
            };
        }
    }
}
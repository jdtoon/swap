using carestream.core.dtos.admin.systemhealth; // For SystemHealthDto
using carestream.core.infrastructure; // For ICurrentFacilityContext
using carestream.core.interfaces.repositories;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic; // Not used in this version but included for completeness if needed later.
using System.Data;
using System.Linq; // Not used in this version but included for completeness if needed later.
using System.Text; // Not used in this version but included for completeness if needed later.
using System.Threading.Tasks;

namespace carestream.persistence.repositories
{
    /// <summary>
    /// Repository for managing system health monitoring.
    /// Does NOT include support ticketing functionality.
    /// </summary>
    public class SystemHealthRepository : BaseRepository, ISystemHealthRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SystemHealthRepository"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="logger">The logger for this repository.</param>
        /// <param name="facilityContext">The current facility context.</param>
        public SystemHealthRepository(IConfiguration configuration, ILogger<SystemHealthRepository> logger, ICurrentFacilityContext facilityContext)
            : base(configuration, logger, facilityContext)
        {
        }

        /// <inheritdoc/>
        public async Task<SystemHealthDto?> GetDatabaseHealthStatusAsync(IDbConnection? connection = null, IDbTransaction? transaction = null)
        {
            // This method checks database connectivity and potentially simple query performance.
            const string sql = "SELECT 1;"; // Simple query to check connectivity

            return await ExecuteWithConnectionAsync(async (conn, trans) =>
            {
                try
                {
                    await conn.ExecuteScalarAsync<int>(sql, transaction: trans);
                    return new SystemHealthDto { Component = "Database", Status = "Healthy", LastChecked = DateTimeOffset.UtcNow };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Database health check failed.");
                    return new SystemHealthDto { Component = "Database", Status = "Unhealthy", LastChecked = DateTimeOffset.UtcNow, Details = ex.Message };
                }
            }, connection, transaction);
        }
    }
}
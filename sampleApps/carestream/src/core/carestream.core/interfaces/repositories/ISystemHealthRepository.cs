using carestream.core.dtos.admin.systemhealth; // For SystemHealthDto
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic; // For IEnumerable (if needed, but not for current methods)

namespace carestream.core.interfaces.repositories
{
    /// <summary>
    /// Defines data access operations related to system health monitoring.
    /// Does NOT include support ticketing functionality.
    /// </summary>
    public interface ISystemHealthRepository
    {
        /// <summary>
        /// Retrieves the current status of the database component.
        /// Note: This method probes the database directly.
        /// </summary>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>A <see cref="SystemHealthDto"/> with database status, or null if check fails.</returns>
        Task<SystemHealthDto?> GetDatabaseHealthStatusAsync(IDbConnection? connection = null, IDbTransaction? transaction = null);
    }
}
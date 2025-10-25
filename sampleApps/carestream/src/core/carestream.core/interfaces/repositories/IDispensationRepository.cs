using carestream.core.dtos.pharmacy;
using carestream.core.dtos.shared; // For FilterAndPaginationOptions
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace carestream.core.interfaces.repositories
{
    /// <summary>
    /// Defines data access operations related to medication dispensation logs.
    /// </summary>
    public interface IDispensationRepository
    {
        /// <summary>
        /// Logs a new dispense action for a prescription item.
        /// </summary>
        /// <param name="logEntry">The DTO containing the dispense log entry data.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>The ID of the newly created dispensation log item.</returns>
        Task<int> LogDispenseActionAsync(DispenseLogEntryInputDto logEntry, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves a paginated list of dispensed medication history items for the current facility.
        /// </summary>
        /// <param name="options">Options for filtering, searching, and pagination.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>A tuple containing the enumerable of <see cref="DispensedHistoryItemDto"/> and the total count.</returns>
        Task<(IEnumerable<DispensedHistoryItemDto> Items, int TotalCount)> GetDispensedHistoryAsync(
            FilterAndPaginationOptions options,
            IDbConnection? connection = null,
            IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves detailed information for a specific dispensed history item.
        /// </summary>
        /// <param name="dispensationLogItemId">The ID of the dispensation log item.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>A <see cref="DispensedHistoryItemDto"/> with detailed information, or null if not found.</returns>
        Task<DispensedHistoryItemDto?> GetDispensedHistoryDetailAsync(int dispensationLogItemId, IDbConnection? connection = null, IDbTransaction? transaction = null);
    }
}
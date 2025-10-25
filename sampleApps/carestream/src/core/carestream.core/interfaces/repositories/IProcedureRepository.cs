using carestream.core.dtos.consultation; // For ProcedureDto
using carestream.core.dtos.shared; // For FilterAndPaginationOptions
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic; // For IEnumerable

namespace carestream.core.interfaces.repositories
{
    /// <summary>
    /// Defines data access operations for medical procedures.
    /// </summary>
    public interface IProcedureRepository
    {
        /// <summary>
        /// Searches for procedures by code or name.
        /// </summary>
        /// <param name="searchTerm">The term to search for.</param>
        /// <param name="limit">The maximum number of results to return.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>An enumerable of <see cref="ProcedureDto"/> matching the search term.</returns>
        Task<IEnumerable<ProcedureDto>> SearchProceduresAsync(string searchTerm, int limit = 10, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves a procedure by its unique ID.
        /// </summary>
        /// <param name="procedureId">The unique ID of the procedure.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>A <see cref="ProcedureDto"/> if found; otherwise, null.</returns>
        Task<ProcedureDto?> GetProcedureByIdAsync(int procedureId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves a procedure by its code string.
        /// </summary>
        /// <param name="code">The procedure code string.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>A <see cref="ProcedureDto"/> if found; otherwise, null.</returns>
        Task<ProcedureDto?> GetProcedureByCodeAsync(string code, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves a paginated list of all procedures, with optional search and filtering.
        /// </summary>
        /// <param name="options">Options for filtering, searching, and pagination.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>A tuple containing the enumerable of <see cref="ProcedureDto"/> and the total count.</returns>
        Task<(IEnumerable<ProcedureDto> Items, int TotalCount)> GetAllProceduresAsync(FilterAndPaginationOptions options, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Creates a new procedure record.
        /// </summary>
        /// <param name="procedureDto">The DTO containing the procedure data.</param>
        /// <param name="createdByUserId">The ID of the user who created the record.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>The ID of the newly created procedure, or 0 if creation failed.</returns>
        Task<int> CreateProcedureAsync(ProcedureDto procedureDto, int createdByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Updates an existing procedure record.
        /// </summary>
        /// <param name="procedureDto">The DTO containing the updated procedure data.</param>
        /// <param name="updatedByUserId">The ID of the user who updated the record.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>True if the update was successful, false otherwise.</returns>
        Task<bool> UpdateProcedureAsync(ProcedureDto procedureDto, int updatedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Deactivates a procedure, making it inactive.
        /// </summary>
        /// <param name="procedureId">The ID of the procedure to deactivate.</param>
        /// <param name="deactivatedByUserId">The ID of the user who deactivated the record.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>True if deactivation was successful, false otherwise.</returns>
        Task<bool> DeactivateProcedureAsync(int procedureId, int deactivatedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Activates an inactive procedure.
        /// </summary>
        /// <param name="procedureId">The ID of the procedure to activate.</param>
        /// <param name="activatedByUserId">The ID of the user who activated the record.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>True if activation was successful, false otherwise.</returns>
        Task<bool> ActivateProcedureAsync(int procedureId, int activatedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null);
    }
}
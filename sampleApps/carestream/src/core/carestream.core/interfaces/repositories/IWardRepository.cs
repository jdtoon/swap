using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data; // For IDbConnection, IDbTransaction
using carestream.core.dtos.admin.facility; // For WardDto, CreateUpdateWardDto
using carestream.core.dtos.shared; // For FilterAndPaginationOptions

namespace carestream.core.interfaces.repositories
{
    /// <summary>
    /// Defines data access operations for Wards.
    /// </summary>
    public interface IWardRepository
    {
        /// <summary>
        /// Retrieves a ward by its unique identifier.
        /// </summary>
        /// <param name="wardId">The unique ID of the ward.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>A <see cref="WardDto"/> if found, otherwise null.</returns>
        Task<WardDto?> GetWardByIdAsync(int wardId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves a ward by its name within a specific facility (and optionally department).
        /// </summary>
        /// <param name="facilityId">The ID of the facility.</param>
        /// <param name="wardName">The name of the ward.</param>
        /// <param name="departmentId">Optional ID of the department.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>A <see cref="WardDto"/> if found, otherwise null.</returns>
        Task<WardDto?> GetWardByNameAndFacilityAsync(int facilityId, string wardName, int? departmentId = null, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves a paginated and filtered list of wards for a specific facility, optionally filtered by department.
        /// </summary>
        /// <param name="facilityId">The ID of the facility.</param>
        /// <param name="options">Filtering and pagination options (e.g., SearchTerm1 for ward name, SearchTerm2 for department name).</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>A tuple containing the list of wards and the total count.</returns>
        Task<(IEnumerable<WardDto> Items, int TotalCount)> GetWardsByFacilityAsync(int facilityId, FilterAndPaginationOptions options, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves all active wards for a specific facility, optionally filtered by department.
        /// </summary>
        /// <param name="facilityId">The ID of the facility.</param>
        /// <param name="departmentId">Optional ID of the department to filter by.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>An enumerable collection of <see cref="WardDto"/>.</returns>
        Task<IEnumerable<WardDto>> GetAllActiveWardsByFacilityAsync(int facilityId, int? departmentId = null, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Creates a new ward.
        /// </summary>
        /// <param name="ward">The DTO containing the ward data.</param>
        /// <param name="createdByUserId">The ID of the user creating the ward.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>The ID of the newly created ward, or 0 if creation failed.</returns>
        Task<int> CreateWardAsync(CreateUpdateWardDto ward, int createdByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Updates an existing ward.
        /// </summary>
        /// <param name="ward">The DTO containing the updated ward data.</param>
        /// <param name="updatedByUserId">The ID of the user updating the ward.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>True if the update was successful, false otherwise.</returns>
        Task<bool> UpdateWardAsync(CreateUpdateWardDto ward, int updatedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Deactivates a ward by its unique identifier.
        /// </summary>
        /// <param name="wardId">The ID of the ward to deactivate.</param>
        /// <param name="deactivatedByUserId">The ID of the user deactivating the ward.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>True if the ward was successfully deactivated, false otherwise.</returns>
        Task<bool> DeactivateWardAsync(int wardId, int deactivatedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null);
    }
}
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data; // For IDbConnection, IDbTransaction
using carestream.core.dtos.facility; // For FacilityDto
using carestream.core.dtos.admin.facility; // For CreateUpdateFacilityDto, FacilityDetailWithChildrenDto
using carestream.core.dtos.shared; // For FilterAndPaginationOptions

namespace carestream.core.interfaces.repositories
{
    /// <summary>
    /// Defines data access operations for Facilities.
    /// </summary>
    public interface IFacilityRepository
    {
        /// <summary>
        /// Retrieves a facility by its unique identifier.
        /// </summary>
        /// <param name="facilityId">The unique ID of the facility.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>A <see cref="FacilityDto"/> if found, otherwise null.</returns>
        Task<FacilityDto?> GetFacilityByIdAsync(int facilityId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves a facility by its name.
        /// </summary>
        /// <param name="name">The name of the facility.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>A <see cref="FacilityDto"/> if found, otherwise null.</returns>
        Task<FacilityDto?> GetFacilityByNameAsync(string name, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves a facility by its short code.
        /// </summary>
        /// <param name="shortCode">The short code of the facility.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>A <see cref="FacilityDto"/> if found, otherwise null.</returns>
        Task<FacilityDto?> GetFacilityByShortCodeAsync(string shortCode, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves all active facilities.
        /// </summary>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>An enumerable collection of <see cref="FacilityDto"/>.</returns>
        Task<IEnumerable<FacilityDto>> GetAllActiveFacilitiesAsync(IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves a paginated and filtered list of facilities for the admin panel.
        /// </summary>
        /// <param name="options">Filtering and pagination options.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>A tuple containing the list of facilities and the total count.</returns>
        Task<(IEnumerable<FacilityDto> Items, int TotalCount)> GetFacilitiesForAdminAsync(FilterAndPaginationOptions options, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Creates a new facility.
        /// </summary>
        /// <param name="facility">The DTO containing the facility data.</param>
        /// <param name="createdByUserId">The ID of the user creating the facility.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>The ID of the newly created facility, or 0 if creation failed.</returns>
        Task<int> CreateFacilityAsync(CreateUpdateFacilityDto facility, int createdByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Updates an existing facility.
        /// </summary>
        /// <param name="facility">The DTO containing the updated facility data.</param>
        /// <param name="updatedByUserId">The ID of the user updating the facility.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>True if the update was successful, false otherwise.</returns>
        Task<bool> UpdateFacilityAsync(CreateUpdateFacilityDto facility, int updatedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Deactivates a facility by its unique identifier.
        /// </summary>
        /// <param name="facilityId">The ID of the facility to deactivate.</param>
        /// <param name="deactivatedByUserId">The ID of the user deactivating the facility.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>True if the facility was successfully deactivated, false otherwise.</returns>
        Task<bool> DeactivateFacilityAsync(int facilityId, int deactivatedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Activates a deactivated facility.
        /// </summary>
        /// <param name="facilityId">The ID of the facility to activate.</param>
        /// <param name="activatedByUserId">The ID of the user activating the facility.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>True if the facility was successfully activated, false otherwise.</returns>
        Task<bool> ActivateFacilityAsync(int facilityId, int activatedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves comprehensive details for a facility, including its associated departments and wards.
        /// </summary>
        /// <param name="facilityId">The ID of the facility.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>A <see cref="FacilityDetailWithChildrenDto"/> if found; otherwise, null.</returns>
        Task<FacilityDetailWithChildrenDto?> GetFacilityDetailsWithChildrenAsync(int facilityId, IDbConnection? connection = null, IDbTransaction? transaction = null);
    }
}
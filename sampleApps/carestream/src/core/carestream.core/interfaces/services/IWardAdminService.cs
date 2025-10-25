using System.Collections.Generic;
using System.Threading.Tasks;
using carestream.core.dtos.admin.facility;
using carestream.core.dtos.shared;

namespace carestream.core.interfaces.services
{
    /// <summary>
    /// Defines service operations for administrative management of Wards.
    /// </summary>
    public interface IWardAdminService
    {
        /// <summary>
        /// Retrieves a paginated and filtered list of wards for a specific facility, optionally filtered by department.
        /// </summary>
        /// <param name="facilityId">The ID of the facility for which to retrieve wards.</param>
        /// <param name="options">Filtering and pagination options (e.g., search by ward name, filter by department ID).</param>
        /// <returns>A <see cref="WardListViewModel"/> containing the wards.</returns>
        Task<WardListViewModel> GetWardsViewModelAsync(int facilityId, FilterAndPaginationOptions options);

        /// <summary>
        /// Retrieves a single ward by its unique identifier for administrative purposes.
        /// </summary>
        /// <param name="wardId">The unique ID of the ward.</param>
        /// <returns>A <see cref="WardDto"/> if found, otherwise null.</returns>
        Task<WardDto?> GetWardByIdAsync(int wardId);

        /// <summary>
        /// Creates a new ward within a specified facility and optionally links it to a department.
        /// </summary>
        /// <param name="wardDto">The DTO containing the data for the new ward.</param>
        /// <param name="createdByUserId">The ID of the admin user creating the ward.</param>
        /// <returns>True if the ward was successfully created, false otherwise.</returns>
        Task<bool> CreateWardAsync(CreateUpdateWardDto wardDto, int createdByUserId);

        /// <summary>
        /// Updates an existing ward.
        /// </summary>
        /// <param name="wardDto">The DTO containing the updated ward data.</param>
        /// <param name="updatedByUserId">The ID of the admin user updating the ward.</param>
        /// <returns>True if the ward was successfully updated, false otherwise.</returns>
        Task<bool> UpdateWardAsync(CreateUpdateWardDto wardDto, int updatedByUserId);

        /// <summary>
        /// Deactivates a ward by its unique identifier.
        /// </summary>
        /// <param name="wardId">The ID of the ward to deactivate.</param>
        /// <param name="deactivatedByUserId">The ID of the admin user deactivating the ward.</param>
        /// <returns>True if the ward was successfully deactivated, false otherwise.</returns>
        Task<bool> DeactivateWardAsync(int wardId, int deactivatedByUserId);

        /// <summary>
        /// Retrieves all active wards for a specific facility, optionally filtered by department,
        /// typically for dropdowns or selection lists.
        /// </summary>
        /// <param name="facilityId">The ID of the facility to retrieve wards for.</param>
        /// <param name="departmentId">Optional ID of the department to filter by.</param>
        /// <returns>An enumerable collection of active <see cref="WardDto"/>.</returns>
        Task<IEnumerable<WardDto>> GetAllWardsForFacilityAndDepartmentAsync(int facilityId, int? departmentId = null);
    }
}
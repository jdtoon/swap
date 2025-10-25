using System.Collections.Generic;
using System.Threading.Tasks;
using carestream.core.dtos.admin.facility;
using carestream.core.dtos.facility;
using carestream.core.dtos.shared;

namespace carestream.core.interfaces.services
{
    /// <summary>
    /// Defines service operations for administrative management of Facilities.
    /// </summary>
    public interface IFacilityAdminService
    {
        /// <summary>
        /// Retrieves a paginated and filtered list of facilities for the admin panel.
        /// </summary>
        /// <param name="options">Filtering and pagination options.</param>
        /// <returns>A <see cref="FacilityListViewModel"/> containing the facilities.</returns>
        Task<FacilityListViewModel> GetFacilitiesViewModelAsync(FilterAndPaginationOptions options);

        /// <summary>
        /// Retrieves a single facility by its unique identifier for administrative purposes.
        /// </summary>
        /// <param name="facilityId">The unique ID of the facility.</param>
        /// <returns>A <see cref="FacilityDto"/> if found, otherwise null.</returns>
        Task<FacilityDto?> GetFacilityByIdAsync(int facilityId);

        /// <summary>
        /// Creates a new facility.
        /// </summary>
        /// <param name="facilityDto">The DTO containing the data for the new facility.</param>
        /// <param name="createdByUserId">The ID of the admin user creating the facility.</param>
        /// <returns>True if the facility was successfully created, false otherwise.</returns>
        Task<bool> CreateFacilityAsync(CreateUpdateFacilityDto facilityDto, int createdByUserId);

        /// <summary>
        /// Updates an existing facility.
        /// </summary>
        /// <param name="facilityDto">The DTO containing the updated facility data.</param>
        /// <param name="updatedByUserId">The ID of the admin user updating the facility.</param>
        /// <returns>True if the facility was successfully updated, false otherwise.</returns>
        Task<bool> UpdateFacilityAsync(CreateUpdateFacilityDto facilityDto, int updatedByUserId);

        /// <summary>
        /// Deactivates a facility by its unique identifier.
        /// </summary>
        /// <param name="facilityId">The ID of the facility to deactivate.</param>
        /// <param name="deactivatedByUserId">The ID of the admin user deactivating the facility.</param>
        /// <returns>True if the facility was successfully deactivated, false otherwise.</returns>
        Task<bool> DeactivateFacilityAsync(int facilityId, int deactivatedByUserId);
    }
}
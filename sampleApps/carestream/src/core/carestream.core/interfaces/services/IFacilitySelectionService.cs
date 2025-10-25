using System.Collections.Generic;
using System.Threading.Tasks;
using carestream.core.dtos.facility; // For FacilityDto
using carestream.core.dtos.user; // For UserFacilityLinkDto

namespace carestream.core.interfaces.services
{
    /// <summary>
    /// Defines service operations for managing user facility assignments and selection.
    /// </summary>
    public interface IFacilitySelectionService
    {
        /// <summary>
        /// Retrieves all facilities that a given internal user ID has access to.
        /// Only active facilities are returned.
        /// </summary>
        /// <param name="internalUserId">The internal user ID.</param>
        /// <returns>A list of <see cref="FacilityDto"/> accessible by the user.</returns>
        Task<IEnumerable<FacilityDto>> GetFacilitiesForUserAsync(int internalUserId);

        /// <summary>
        /// Sets the current active facility for the user's session.
        /// This method should validate that the user has access to the specified facility.
        /// It also updates the user's default facility preference.
        /// </summary>
        /// <param name="internalUserId">The ID of the user attempting to set the facility.</param>
        /// <param name="facilityId">The ID of the facility to set as active.</param>
        /// <returns>The <see cref="FacilityDto"/> of the newly active facility if successful, otherwise null.</returns>
        Task<FacilityDto?> SetActiveFacilityForUserAsync(int internalUserId, int facilityId);

        /// <summary>
        /// Determines the initial default facility for a user upon login.
        /// This will prioritize a previously set default, then the first accessible facility.
        /// </summary>
        /// <param name="internalUserId">The ID of the user.</param>
        /// <returns>The default <see cref="FacilityDto"/> for the user, or null if none can be determined (e.g., no access).</returns>
        Task<FacilityDto?> GetDefaultFacilityForUserAsync(int internalUserId);

        // Add admin methods for managing user-facility links later if needed, e.g.:
        // Task<bool> LinkUserToFacilityAsync(int userId, int facilityId, bool isDefault);
        // Task<bool> UnlinkUserFromFacilityAsync(int userId, int facilityId);
        // Task<bool> SetUserDefaultFacilityAsync(int userId, int facilityId);
    }
}
using System.Collections.Generic; // For IEnumerable
using carestream.core.dtos.facility; // For FacilityDto

namespace carestream.core.infrastructure
{
    /// <summary>
    /// Defines a scoped service that holds and provides the current operational facility context.
    /// This context is typically set based on user login, selection, or URL.
    /// </summary>
    public interface ICurrentFacilityContext
    {
        /// <summary>
        /// Gets the Facility ID that is active for the current request.
        /// </summary>
        int CurrentFacilityId { get; }

        /// <summary>
        /// Gets the name of the currently active facility.
        /// </summary>
        string CurrentFacilityName { get; }

        /// <summary>
        /// Gets a value indicating whether the current facility context has been set.
        /// </summary>
        bool IsFacilityContextSet { get; }

        /// <summary>
        /// Gets a list of all facilities the current user has access to.
        /// </summary>
        IEnumerable<FacilityDto> UserAccessibleFacilities { get; }

        /// <summary>
        /// Sets the current facility context for the request.
        /// This method should typically be called once per request lifecycle.
        /// </summary>
        /// <param name="facilityId">The ID of the facility to set as current.</param>
        /// <param name="facilityName">The name of the facility.</param>
        /// <param name="userAccessibleFacilities">A list of all facilities the user can access.</param>
        void SetCurrentFacility(int facilityId, string facilityName, IEnumerable<FacilityDto> userAccessibleFacilities);

        /// <summary>
        /// Clears the current facility context.
        /// </summary>
        void ClearContext();
    }
}
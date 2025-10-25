using System.Collections.Generic;
using carestream.core.dtos.facility; // For FacilityDto (for user's linked facilities)
using carestream.core.dtos.user;    // For UserFacilityLinkDto (if we want to expose it)

namespace carestream.core.dtos.admin
{
    /// <summary>
    /// Represents detailed information about a user for administrative purposes.
    /// This includes personal details and associated facilities.
    /// </summary>
    public class AdminUserDetailDto
    {
        /// <summary>
        /// Gets or sets the unique identifier of the user.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the user's force number.
        /// </summary>
        public string ForceNumber { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user's full name.
        /// </summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user's military rank.
        /// </summary>
        public string? Rank { get; set; }

        /// <summary>
        /// Gets or sets the user's department.
        /// </summary>
        public string? Department { get; set; }

        /// <summary>
        /// Gets or sets the user's Logto subject (sub) claim.
        /// </summary>
        public string? LogtoSub { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user account is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets a list of facilities the user is linked to, including whether each is default.
        /// </summary>
        public List<UserFacilityLinkDto> LinkedFacilities { get; set; } = new List<UserFacilityLinkDto>();

        /// <summary>
        /// Gets or sets a list of all active facilities in the system.
        /// Used for dropdowns to link user to new facilities.
        /// </summary>
        public List<FacilityDto> AllActiveFacilities { get; set; } = new List<FacilityDto>();
    }
}
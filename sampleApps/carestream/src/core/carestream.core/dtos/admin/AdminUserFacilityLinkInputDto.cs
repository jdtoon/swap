using System.ComponentModel.DataAnnotations;

namespace carestream.core.dtos.admin
{
    /// <summary>
    /// Data Transfer Object for managing a user's link to a specific facility from the admin panel.
    /// Used for linking/unlinking or setting default.
    /// </summary>
    public class AdminUserFacilityLinkInputDto
    {
        /// <summary>
        /// Gets or sets the unique identifier of the user.
        /// </summary>
        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier of the facility to link/unlink/set as default.
        /// </summary>
        [Required]
        public int FacilityId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this facility should be set as the user's default.
        /// </summary>
        public bool IsDefault { get; set; }
    }
}
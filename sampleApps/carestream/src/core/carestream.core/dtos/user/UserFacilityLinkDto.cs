namespace carestream.core.dtos.user
{
    /// <summary>
    /// Represents a link between a user and a facility, indicating a user's access to that facility.
    /// </summary>
    public class UserFacilityLinkDto
    {
        /// <summary>
        /// Gets or sets the ID of the user.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the facility.
        /// </summary>
        public int FacilityId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this facility is the user's default/preferred facility.
        /// </summary>
        public bool IsDefault { get; set; }

        // We can add other properties like roles specific to this facility link later if needed.
    }
}
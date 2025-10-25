using System.ComponentModel.DataAnnotations;

namespace carestream.core.dtos.facility
{
    /// <summary>
    /// Data Transfer Object for basic facility information.
    /// </summary>
    public class FacilityDto
    {
        /// <summary>
        /// Gets or sets the unique identifier for the facility.
        /// </summary>
        public int FacilityId { get; set; }

        /// <summary>
        /// Gets or sets the full name of the facility.
        /// </summary>
        [Required(ErrorMessage = "Facility name is required.")]
        [StringLength(255, ErrorMessage = "Facility name cannot exceed 255 characters.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the short code or abbreviation for the facility.
        /// </summary>
        [StringLength(50, ErrorMessage = "Short code cannot exceed 50 characters.")]
        public string? ShortCode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the facility is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the city where the facility is located.
        /// </summary>
        public string? City { get; set; }

        /// <summary>
        /// Gets or sets the address line 1 where the facility is located
        /// </summary>
        public string? AddressLine1 { get; set; }

        /// <summary>
        /// Gets or sets the address line 2 where the facilty is located
        /// </summary>
        public string? AddressLine2 { get; set; }

        /// <summary>
        /// Gets or sets the province where the facility is located
        /// </summary>
        public string? Province { get; set; }

        /// <summary>
        /// Gets or sets the country where the facility is located
        /// </summary>
        public string? Country { get; set; }

        /// <summary>
        /// Gets or sets the phone number where the facility is located
        /// </summary>
        public string? PhoneNumber { get; set; }
        
        /// <summary>
        /// Gets or sets the email address where the facility is located
        /// </summary>
        public string? EmailAddress { get; set; }
    }
}
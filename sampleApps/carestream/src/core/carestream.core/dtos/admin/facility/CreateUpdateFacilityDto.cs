using System.ComponentModel.DataAnnotations;

namespace carestream.core.dtos.admin.facility
{
    /// <summary>
    /// Data Transfer Object for creating or updating a Facility.
    /// </summary>
    public class CreateUpdateFacilityDto
    {
        /// <summary>
        /// Gets or sets the unique identifier of the facility.
        /// Required for update operations, auto-generated for create.
        /// </summary>
        public int? FacilityId { get; set; }

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
        /// Gets or sets the first line of the facility's address.
        /// </summary>
        [StringLength(255, ErrorMessage = "Address line 1 cannot exceed 255 characters.")]
        public string? AddressLine1 { get; set; }

        /// <summary>
        /// Gets or sets the second line of the facility's address.
        /// </summary>
        [StringLength(255, ErrorMessage = "Address line 2 cannot exceed 255 characters.")]
        public string? AddressLine2 { get; set; }

        /// <summary>
        /// Gets or sets the city where the facility is located.
        /// </summary>
        [StringLength(100, ErrorMessage = "City cannot exceed 100 characters.")]
        public string? City { get; set; }

        /// <summary>
        /// Gets or sets the province where the facility is located.
        /// </summary>
        [StringLength(100, ErrorMessage = "Province cannot exceed 100 characters.")]
        public string? Province { get; set; }

        /// <summary>
        /// Gets or sets the country where the facility is located.
        /// </summary>
        [StringLength(100, ErrorMessage = "Country cannot exceed 100 characters.")]
        public string Country { get; set; } = "South Africa"; // Default value

        /// <summary>
        /// Gets or sets the phone number of the facility.
        /// </summary>
        [Phone(ErrorMessage = "Invalid phone number format.")]
        [StringLength(50, ErrorMessage = "Phone number cannot exceed 50 characters.")]
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// Gets or sets the email address of the facility.
        /// </summary>
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        [StringLength(255, ErrorMessage = "Email address cannot exceed 255 characters.")]
        public string? EmailAddress { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the facility is currently active.
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}
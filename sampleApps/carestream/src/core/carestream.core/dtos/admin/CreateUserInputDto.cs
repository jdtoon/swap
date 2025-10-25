using System.ComponentModel.DataAnnotations;

namespace carestream.core.dtos.admin
{
    /// <summary>
    /// DTO for creating a new user from the admin panel.
    /// Does not include Logto Sub as it's linked separately.
    /// </summary>
    public class CreateUserInputDto
    {
        [Required(ErrorMessage = "Force Number is required.")]
        [StringLength(50, ErrorMessage = "Force Number cannot exceed 50 characters.")]
        public string ForceNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "First Name is required.")]
        [StringLength(100, ErrorMessage = "First Name cannot exceed 100 characters.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last Name is required.")]
        [StringLength(100, ErrorMessage = "Last Name cannot exceed 100 characters.")]
        public string LastName { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Rank cannot exceed 50 characters.")]
        public string? Rank { get; set; }

        [StringLength(100, ErrorMessage = "Department cannot exceed 100 characters.")]
        public string? Department { get; set; }

        public bool IsActive { get; set; } = true; // Default to active

        [Required(ErrorMessage = "Initial Facility is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid initial facility.")]
        public int InitialFacilityId { get; set; }
    }
}

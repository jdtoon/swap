using System.ComponentModel.DataAnnotations;

namespace carestream.core.dtos.admin.facility
{
    /// <summary>
    /// Data Transfer Object for creating or updating a Ward.
    /// </summary>
    public class CreateUpdateWardDto
    {
        /// <summary>
        /// Gets or sets the unique identifier of the ward.
        /// Required for update operations, auto-generated for create.
        /// </summary>
        public int? WardId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the facility this ward belongs to.
        /// </summary>
        [Required(ErrorMessage = "Facility ID is required.")]
        public int FacilityId { get; set; }

        /// <summary>
        /// Gets or sets the optional ID of the department this ward belongs to.
        /// </summary>
        public int? DepartmentId { get; set; }

        /// <summary>
        /// Gets or sets the name of the ward.
        /// </summary>
        [Required(ErrorMessage = "Ward name is required.")]
        [StringLength(255, ErrorMessage = "Ward name cannot exceed 255 characters.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the ward.
        /// </summary>
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the ward is active.
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}
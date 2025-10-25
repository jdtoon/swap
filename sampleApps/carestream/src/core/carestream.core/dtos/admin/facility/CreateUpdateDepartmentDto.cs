using System.ComponentModel.DataAnnotations;

namespace carestream.core.dtos.admin.facility
{
    /// <summary>
    /// Data Transfer Object for creating or updating a Department.
    /// </summary>
    public class CreateUpdateDepartmentDto
    {
        /// <summary>
        /// Gets or sets the unique identifier of the department.
        /// Required for update operations, auto-generated for create.
        /// </summary>
        public int? DepartmentId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the facility this department belongs to.
        /// </summary>
        [Required(ErrorMessage = "Facility ID is required.")]
        public int FacilityId { get; set; }

        /// <summary>
        /// Gets or sets the name of the department.
        /// </summary>
        [Required(ErrorMessage = "Department name is required.")]
        [StringLength(255, ErrorMessage = "Department name cannot exceed 255 characters.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the department.
        /// </summary>
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the department is active.
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}
using System.ComponentModel.DataAnnotations;

namespace carestream.core.dtos.admin.facility
{
    /// <summary>
    /// Represents a Department for administrative display.
    /// </summary>
    public class DepartmentDto
    {
        /// <summary>
        /// Gets or sets the unique identifier of the department.
        /// </summary>
        public int DepartmentId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the facility this department belongs to.
        /// </summary>
        public int FacilityId { get; set; }

        /// <summary>
        /// Gets or sets the name of the department.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the department.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the department is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the name of the facility this department belongs to.
        /// (Populated for display purposes).
        /// </summary>
        public string FacilityName { get; set; } = string.Empty;
    }
}
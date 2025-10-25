using System.ComponentModel.DataAnnotations;

namespace carestream.core.dtos.admin.facility
{
    /// <summary>
    /// Represents a Ward for administrative display.
    /// </summary>
    public class WardDto
    {
        /// <summary>
        /// Gets or sets the unique identifier of the ward.
        /// </summary>
        public int WardId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the facility this ward belongs to.
        /// </summary>
        public int FacilityId { get; set; }

        /// <summary>
        /// Gets or sets the name of the facility this ward belongs to.
        /// (Populated for display purposes).
        /// </summary>
        public string FacilityName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional ID of the department this ward belongs to.
        /// </summary>
        public int? DepartmentId { get; set; }

        /// <summary>
        /// Gets or sets the name of the department this ward belongs to.
        /// (Populated for display purposes, if linked).
        /// </summary>
        public string? DepartmentName { get; set; }

        /// <summary>
        /// Gets or sets the name of the ward.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the ward.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the ward is active.
        /// </summary>
        public bool IsActive { get; set; }
    }
}
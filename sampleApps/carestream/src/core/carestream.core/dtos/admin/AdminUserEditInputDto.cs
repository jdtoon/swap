using System.ComponentModel.DataAnnotations;

namespace carestream.core.dtos.admin
{
    /// <summary>
    /// Data Transfer Object for editing a user's basic personal information from the admin panel.
    /// </summary>
    public class AdminUserEditInputDto
    {
        /// <summary>
        /// Gets or sets the unique identifier of the user being edited.
        /// </summary>
        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the user's military rank.
        /// </summary>
        [StringLength(100, ErrorMessage = "Rank cannot exceed 100 characters.")]
        public string? Rank { get; set; }

        /// <summary>
        /// Gets or sets the user's department.
        /// </summary>
        [StringLength(100, ErrorMessage = "Department cannot exceed 100 characters.")]
        public string? Department { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user account is active.
        /// </summary>
        public bool IsActive { get; set; } // Allow admin to activate/deactivate
    }
}
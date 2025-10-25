using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using carestream.core.dtos.admin.facility; // For basic DepartmentDto properties (inheriting)

namespace carestream.core.dtos.admin.facility
{
    /// <summary>
    /// DTO for displaying detailed department information, including its associated wards.
    /// </summary>
    public class DepartmentDetailWithChildrenDto : DepartmentDto // Inherit common properties from DepartmentDto
    {
        /// <summary>
        /// Gets or sets the list of wards associated with this department.
        /// </summary>
        public List<WardDto> Wards { get; set; } = new List<WardDto>();
    }
}
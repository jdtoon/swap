using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using carestream.core.dtos.facility; // For basic FacilityDto properties (inheriting)

namespace carestream.core.dtos.admin.facility
{
    /// <summary>
    /// DTO for displaying detailed facility information, including its associated departments and wards.
    /// </summary>
    public class FacilityDetailWithChildrenDto : FacilityDto // Inherit common properties from FacilityDto
    {
        /// <summary>
        /// Gets or sets the list of departments associated with this facility.
        /// </summary>
        public List<DepartmentDto> Departments { get; set; } = new List<DepartmentDto>();

        /// <summary>
        /// Gets or sets the list of wards associated with this facility.
        /// </summary>
        public List<WardDto> Wards { get; set; } = new List<WardDto>();
    }
}
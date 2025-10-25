using System.Collections.Generic;
using carestream.core.dtos.shared; // For PaginationDto, FilterAndPaginationOptions
using carestream.core.dtos.facility; // For FacilityDto to get facility options for dropdown

namespace carestream.core.dtos.admin.facility
{
    /// <summary>
    /// Represents the view model for listing Wards in the admin panel.
    /// </summary>
    public class WardListViewModel
    {
        /// <summary>
        /// Gets or sets the ID of the facility for which wards are being listed.
        /// </summary>
        public int FacilityId { get; set; }

        /// <summary>
        /// Gets or sets the name of the facility for which wards are being listed.
        /// </summary>
        public string FacilityName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the ID of the department for which wards are being listed (optional filter).
        /// </summary>
        public int? DepartmentId { get; set; }

        /// <summary>
        /// Gets or sets the name of the department for which wards are being listed.
        /// </summary>
        public string? DepartmentName { get; set; }

        /// <summary>
        /// Gets or sets the list of wards to display.
        /// </summary>
        public List<WardDto> Wards { get; set; } = new List<WardDto>();

        /// <summary>
        /// Gets or sets the pagination information for the list.
        /// </summary>
        public PaginationDto PaginationInfo { get; set; } = new PaginationDto();

        /// <summary>
        /// Gets or sets the currently applied filters for the list.
        /// </summary>
        public FilterAndPaginationOptions CurrentFilters { get; set; } = new FilterAndPaginationOptions();

        /// <summary>
        /// Gets or sets a list of all facilities available for filtering/selection.
        /// </summary>
        public List<FacilityDto> AllFacilities { get; set; } = new List<FacilityDto>();

        /// <summary>
        /// Gets or sets a list of all departments available for filtering/selection.
        /// This list might be filtered by FacilityId.
        /// </summary>
        public List<DepartmentDto> AllDepartments { get; set; } = new List<DepartmentDto>();
    }
}
using System.Collections.Generic;
using carestream.core.dtos.shared; // For PaginationDto, FilterAndPaginationOptions

namespace carestream.core.dtos.admin.facility
{
    /// <summary>
    /// Represents the view model for listing Departments in the admin panel.
    /// </summary>
    public class DepartmentListViewModel
    {
        /// <summary>
        /// Gets or sets the ID of the facility for which departments are being listed.
        /// </summary>
        public int FacilityId { get; set; }

        /// <summary>
        /// Gets or sets the name of the facility for which departments are being listed.
        /// </summary>
        public string FacilityName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of departments to display.
        /// </summary>
        public List<DepartmentDto> Departments { get; set; } = new List<DepartmentDto>();

        /// <summary>
        /// Gets or sets the pagination information for the list.
        /// </summary>
        public PaginationDto PaginationInfo { get; set; } = new PaginationDto();

        /// <summary>
        /// Gets or sets the currently applied filters for the list.
        /// </summary>
        public FilterAndPaginationOptions CurrentFilters { get; set; } = new FilterAndPaginationOptions();
    }
}
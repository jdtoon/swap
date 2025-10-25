using System.Collections.Generic;
using carestream.core.dtos.facility;
using carestream.core.dtos.shared; // For PaginationDto, FilterAndPaginationOptions

namespace carestream.core.dtos.admin.facility
{
    /// <summary>
    /// Represents the view model for listing Facilities in the admin panel.
    /// </summary>
    public class FacilityListViewModel
    {
        /// <summary>
        /// Gets or sets the list of facilities to display.
        /// </summary>
        public List<FacilityDto> Facilities { get; set; } = new List<FacilityDto>();

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
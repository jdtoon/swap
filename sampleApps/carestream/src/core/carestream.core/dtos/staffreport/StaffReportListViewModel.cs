using carestream.core.dtos.shared; // Assuming PaginationDto and FilterAndPaginationOptions are here

namespace carestream.core.dtos.admin.staffreport
{
    /// <summary>
    /// Represents a view model for displaying a paginated list of staff reports.
    /// </summary>
    public class StaffReportListViewModel
    {
        /// <summary>
        /// Gets or sets the list of staff report details.
        /// </summary>
        public IEnumerable<StaffReportDto> Reports { get; set; } = new List<StaffReportDto>();

        /// <summary>
        /// Gets or sets the pagination details for the report list.
        /// </summary>
        public PaginationDto Pagination { get; set; } = new PaginationDto();

        /// <summary>
        /// Gets or sets the filtering and pagination options used to generate this view model.
        /// </summary>
        public FilterAndPaginationOptions Filters { get; set; } = new FilterAndPaginationOptions();

        /// <summary>
        /// Gets or sets the ID of the facility for which reports are being displayed.
        /// </summary>
        public int FacilityId { get; set; }
    }
}
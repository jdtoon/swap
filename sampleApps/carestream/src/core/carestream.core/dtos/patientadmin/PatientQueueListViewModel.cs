using System.Collections.Generic;
using carestream.core.dtos.shared; // For FilterAndPaginationOptions, PaginationDto

namespace carestream.core.dtos.patientadmin
{
    /// <summary>
    /// Represents the view model for the patient queue displayed in a list format.
    /// </summary>
    public class PatientQueueListViewModel
    {
        /// <summary>
        /// Gets or sets the list of patient queue items for the current page.
        /// </summary>
        public List<PatientQueueItemDto> QueueItems { get; set; } = new List<PatientQueueItemDto>();

        /// <summary>
        /// Gets or sets the pagination information for the queue.
        /// </summary>
        public PaginationDto PaginationInfo { get; set; } = new PaginationDto();

        /// <summary>
        /// Gets or sets the currently applied filters for the queue.
        /// </summary>
        public FilterAndPaginationOptions CurrentFilters { get; set; } = new FilterAndPaginationOptions();

        /// <summary>
        /// Gets or sets the current active view type ("list" or "board").
        /// </summary>
        public string CurrentViewType { get; set; } = "list"; // Default to list view
    }
}
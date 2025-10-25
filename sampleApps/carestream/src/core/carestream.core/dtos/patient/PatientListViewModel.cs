using System.Collections.Generic;
using carestream.core.dtos.shared; // Assuming PaginationDto is in this namespace

namespace carestream.core.dtos.patient
{
    /// <summary>
    /// Represents a view model for displaying a paginated list of patients.
    /// </summary>
    public class PatientListViewModel
    {
        /// <summary>
        /// Gets or sets the list of patient basic information.
        /// </summary>
        public IEnumerable<PatientBasicInfoDto> Patients { get; set; } = new List<PatientBasicInfoDto>();

        /// <summary>
        /// Gets or sets the pagination details for the patient list.
        /// </summary>
        public PaginationDto Pagination { get; set; } = new PaginationDto();

        /// <summary>
        /// Gets or sets the filtering and pagination options used to generate this view model.
        /// </summary>
        public FilterAndPaginationOptions Filters { get; set; } = new FilterAndPaginationOptions();
    }
}
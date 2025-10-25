using System.Collections.Generic;
using carestream.core.dtos.shared; // For FilterAndPaginationOptions
using carestream.core.dtos.pharmacy; // For PaginationInfoViewModel (or move PaginationInfoViewModel to shared)

namespace carestream.core.dtos.patientadmin
{
    public class PatientQueueViewModel
    {
        public List<PatientQueueItemDto> QueueItems { get; set; } = new List<PatientQueueItemDto>();
        public PaginationDto PaginationInfo { get; set; } = new PaginationDto();
        public FilterAndPaginationOptions CurrentFilters { get; set; } = new FilterAndPaginationOptions();
    }
}
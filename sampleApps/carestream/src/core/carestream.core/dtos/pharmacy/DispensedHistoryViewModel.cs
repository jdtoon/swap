using carestream.core.dtos.shared;
using System.Collections.Generic;

namespace carestream.core.dtos.pharmacy
{
    public class DispensedHistoryViewModel
    {
        public List<DispensedHistoryItemDto> DispensedItems { get; set; } = new List<DispensedHistoryItemDto>();
        public PaginationDto PaginationInfo { get; set; } = new PaginationDto();

        // Filters
        public DateTime? FilterStartDate { get; set; }
        public DateTime? FilterEndDate { get; set; }
        public string? FilterPatientSearch { get; set; } // Name or Force Number
        public string? FilterMedicationSearch { get; set; }
    }
}
using System.Collections.Generic;

namespace carestream.core.dtos.pharmacy
{
    public class PharmacistDashboardViewModel
    {
        public PharmacistDashboardStatsDto Stats { get; set; } = new PharmacistDashboardStatsDto();
        public List<PendingPrescriptionSummaryDto> PendingPrescriptions { get; set; } = new List<PendingPrescriptionSummaryDto>();
    }
}
using System.Collections.Generic;

namespace carestream.core.dtos.vitals
{
    public class NurseDashboardViewModel
    {
        public VitalsDashboardStatsDto? Stats { get; set; }
        public List<VitalsQueueItemDto> VitalsQueue { get; set; } = new List<VitalsQueueItemDto>();
    }
}
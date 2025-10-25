using System;

namespace carestream.core.dtos.vitals
{
    public class VitalsQueueItemDto
    {
        public int VisitId { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string? Rank { get; set; }
        public int? Age { get; set; } // Calculate from DoB
        public DateTime CheckinTimestamp { get; set; } // When they entered 'Waiting for Vitals' or initial check-in
        public string Priority { get; set; } = "Normal"; // e.g., Normal, Urgent, High
        // WaitTime can be calculated in the view or service: (DateTime.UtcNow - CheckinTimestamp)
    }
}
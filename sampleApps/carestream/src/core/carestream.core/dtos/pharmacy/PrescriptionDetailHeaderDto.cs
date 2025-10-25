using System;

namespace carestream.core.dtos.pharmacy
{
    /// <summary>
    /// Contains header information for a detailed prescription view.
    /// </summary>
    public class PrescriptionDetailHeaderDto
    {
        public int VisitId { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string? PatientRank { get; set; }
        public string? PatientForceNumber { get; set; }
        public int? PatientAge { get; set; }

        public string PrescriberName { get; set; } = string.Empty;
        public string? PrescriberRank { get; set; }
        public string? PrescriberDepartment { get; set; }
        public DateTime PrescriptionDate { get; set; } // Could be visit_timestamp or pharmacy_sent_at
        public string PrescriptionIdentifier { get; set; } = string.Empty; // e.g., "Rx-" + VisitId
    }
}
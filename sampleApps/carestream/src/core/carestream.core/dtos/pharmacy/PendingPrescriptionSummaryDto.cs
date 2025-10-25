using System;

namespace carestream.core.dtos.pharmacy
{
    public class PendingPrescriptionSummaryDto
    {
        public int VisitId { get; set; } // Key to link to the full prescription
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string? PatientRank { get; set; }
        public string? PatientForceNumber { get; set; }
        public DateTime PrescribedAt { get; set; } // When it was sent to pharmacy
        public string PrescribingDoctorName { get; set; } = string.Empty;
        public int NumberOfMedications { get; set; }
        public string Status { get; set; } = "Pending Dispense"; // Could be "Processing", etc.
    }
}
using System;

namespace carestream.core.dtos.pharmacy
{
    public class DispensedHistoryItemDto
    {
        public int DispensationLogItemId { get; set; }
        public int VisitId { get; set; }
        public DateTime DispensedAt { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string? PatientForceNumber { get; set; }
        public string MedicationName { get; set; } = string.Empty; // Incl. strength/form
        public string QuantityDispensedInTransaction { get; set; } = string.Empty;
        public string PharmacistName { get; set; } = string.Empty;
        public string? PharmacistNotes { get; set; }
        public string? BatchNumber { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int PrescriptionItemId { get; set; }
    }
}
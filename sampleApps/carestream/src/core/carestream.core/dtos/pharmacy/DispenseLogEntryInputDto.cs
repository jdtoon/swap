namespace carestream.core.dtos.pharmacy
{
    // Input DTO for logging a dispense action
    public class DispenseLogEntryInputDto
    {
        public int PrescriptionItemId { get; set; }
        public int VisitId { get; set; } // Denormalized
        public int MedicationId { get; set; } // Denormalized
        public string QuantityDispensedInTransaction { get; set; } = string.Empty;
        public int DispensedByUserId { get; set; }
        public string? PharmacistNotes { get; set; } // Specific notes for this dispense act
        public string? BatchNumber { get; set; }
        public System.DateTime? ExpiryDate { get; set; }
    }
}

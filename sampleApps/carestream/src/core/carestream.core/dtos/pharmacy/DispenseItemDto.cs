namespace carestream.core.dtos.pharmacy
{
    public class DispenseItemDto
    {
        public int PrescriptionItemId { get; set; }
        public int MedicationId { get; set; }
        public string MedicationName { get; set; } = string.Empty; // e.g., Amoxicillin 500mg Capsule
        public string QuantityPrescribed { get; set; } = string.Empty; // e.g., "21 tablets"

        // --- Fields for Pharmacist Interaction ---
        public bool IsSelectedForDispense { get; set; } = true; // Default to selected
        public string QuantityToDispense { get; set; } = string.Empty; // Pharmacist enters this

        // --- Information for Pharmacist ---
        public int StockOnHand { get; set; } = 0; // Placeholder, to be fetched from inventory later
        public string? OriginalDosage { get; set; }
        public string? OriginalFrequency { get; set; }
        public string? OriginalDuration { get; set; }
        public string? SpecialInstructions { get; set; }
    }
}
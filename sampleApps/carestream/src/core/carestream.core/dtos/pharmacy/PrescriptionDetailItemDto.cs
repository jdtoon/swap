namespace carestream.core.dtos.pharmacy
{
    public class PrescriptionDetailItemDto
    {
        public int PrescriptionItemId { get; set; } // From app.prescription_items
        public int MedicationId { get; set; }
        public string MedicationName { get; set; } = string.Empty; // e.g., "Amoxicillin 500mg Capsule"
        public string Dosage { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public string? Duration { get; set; }
        public string QuantityPrescribed { get; set; } = string.Empty; // e.g., "21 tablets"
        public string? SpecialInstructions { get; set; }

        // Pharmacy-specific fields to be added later:
        // public int StockOnHand { get; set; }
        // public string DispensedQuantity { get; set; }
        // public string BatchNumber { get; set; }
        // public bool IsDispensed { get; set; }
    }
}
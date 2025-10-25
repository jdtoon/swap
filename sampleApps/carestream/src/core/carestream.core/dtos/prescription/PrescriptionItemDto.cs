namespace carestream.core.dtos.prescription
{
    public class PrescriptionItemDto
    {
        public int PrescriptionItemId { get; set; }
        public int MedicationId { get; set; }
        public string MedicationName { get; set; } = string.Empty;
        public string Dosage { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public string? Duration { get; set; }
        public string QuantityPrescribed { get; set; } = string.Empty;
        public string? SpecialInstructions { get; set; }
        public bool IsSentToPharmacy { get; set; }
    }
}
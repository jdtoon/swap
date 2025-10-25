using System.Collections.Generic;

namespace carestream.core.dtos.pharmacy
{
    public class DispensedItemConfirmationDto
    {
        public int PrescriptionItemId { get; set; }
        public string MedicationName { get; set; } = string.Empty;
        public string QuantityPrescribed { get; set; } = string.Empty;
        public string QuantityActuallyDispensedInTransaction { get; set; } = string.Empty;
        public string TotalQuantityDispensedSoFar { get; set; } = string.Empty;
        public bool IsFullyDispensedNow { get; set; }
        public string? Notes { get; set; } // e.g., "Partial dispense", "Stock issue"
    }

    public class DispenseConfirmationDto
    {
        public bool OverallSuccess { get; set; }
        public int VisitId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string PrescriptionIdentifier { get; set; } = string.Empty;
        public List<DispensedItemConfirmationDto> DispensedItems { get; set; } = new List<DispensedItemConfirmationDto>();
        public string? ErrorMessage { get; set; } // If OverallSuccess is false
        public string? NextStepMessage { get; set; } // e.g., "Prescription fully dispensed." or "Partial dispense recorded."
    }
}
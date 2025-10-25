using System;
using System.ComponentModel.DataAnnotations;

namespace carestream.core.dtos.prescription
{
    /// <summary>
    /// DTO for displaying a single item in a patient's prescription history.
    /// </summary>
    public class PatientPrescriptionHistoryItemDto
    {
        public int PrescriptionItemId { get; set; }
        public int VisitId { get; set; }
        public int PatientId { get; set; } // From visit for context

        public DateTimeOffset PrescribedAt { get; set; } // When doctor prescribed
        public string? PrescribedByDoctorName { get; set; } // From user join

        public int MedicationId { get; set; }
        public string MedicationName { get; set; } = string.Empty; // From medication join

        public string Dosage { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public string? Duration { get; set; }
        public string QuantityPrescribed { get; set; } = string.Empty;
        public string? SpecialInstructions { get; set; }

        public string? QuantityDispensed { get; set; } // The total quantity dispensed for this item
        public bool IsFullyDispensed { get; set; }
        public DateTimeOffset? LastDispensedAt { get; set; }
        public string? LastDispensedByPharmacistName { get; set; } // From user join

        /// <summary>
        /// Calculated status like "Fully Dispensed", "Partially Dispensed", "Pending Dispense".
        /// </summary>
        public string DispenseStatus { get; set; } = string.Empty; // Derived property
    }
}
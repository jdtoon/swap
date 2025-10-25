using System.ComponentModel.DataAnnotations;

namespace carestream.core.dtos.prescription
{
    public class AddPrescriptionItemInputDto
    {
        [Required]
        public int VisitId { get; set; }
        [Required]
        public int MedicationId { get; set; }

        [Required(ErrorMessage = "Dosage is required.")]
        [StringLength(100)]
        public string Dosage { get; set; } = string.Empty;

        [Required(ErrorMessage = "Frequency is required.")]
        [StringLength(100)]
        public string Frequency { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Duration { get; set; }

        [Required(ErrorMessage = "Quantity is required.")]
        [StringLength(50)]
        public string QuantityPrescribed { get; set; } = string.Empty;

        [StringLength(500)]
        public string? SpecialInstructions { get; set; }
    }
}
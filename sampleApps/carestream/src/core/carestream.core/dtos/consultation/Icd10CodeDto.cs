using System;
using System.ComponentModel.DataAnnotations;

namespace carestream.core.dtos.consultation
{
    /// <summary>
    /// DTO for ICD-10 code information. Used for creation, update, and display.
    /// </summary>
    public class Icd10CodeDto
    {
        public int Icd10CodeId { get; set; }

        [Required]
        [StringLength(10)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)] // Assuming description can be long
        public string Description { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Category { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
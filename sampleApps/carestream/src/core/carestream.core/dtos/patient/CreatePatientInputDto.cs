using System;
using System.ComponentModel.DataAnnotations;

namespace carestream.core.dtos.patient
{
    /// <summary>
    /// DTO for creating a new patient record.
    /// </summary>
    public class CreatePatientInputDto
    {
        [Required]
        [StringLength(50)]
        public string ForceNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Rank { get; set; } = string.Empty; // Consider enum: PatientRank

        [Required]
        [StringLength(150)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string LastName { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(50)]
        public string? Gender { get; set; } // Consider enum: PatientGender

        [StringLength(100)]
        public string? Unit { get; set; }

        [StringLength(255)]
        [EmailAddress]
        public string? EmailAddress { get; set; }

        [StringLength(50)]
        [Phone]
        public string? PrimaryPhoneNumber { get; set; }

        [StringLength(255)]
        public string? EmergencyContactName { get; set; }

        [StringLength(50)]
        [Phone]
        public string? EmergencyContactPhone { get; set; }

        [StringLength(255)]
        public string? AddressLine1 { get; set; }

        [StringLength(255)]
        public string? AddressLine2 { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? Province { get; set; }

        [StringLength(20)]
        public string? PostalCode { get; set; }

        [StringLength(100)]
        public string? Country { get; set; } = "South Africa";

        [StringLength(255)]
        public string? NextOfKinName { get; set; }

        [StringLength(50)]
        public string? NextOfKinPhone { get; set; }

        [StringLength(100)]
        public string? NextOfKinRelationship { get; set; }

        [StringLength(100)]
        public string? Religion { get; set; }

        [StringLength(100)]
        public string? Occupation { get; set; }

        [StringLength(50)]
        public string? MaritalStatus { get; set; }

        [StringLength(100)]
        public string? Nationality { get; set; }
    }
}
using System;

namespace carestream.core.dtos.patient
{
    public class PatientDetailDto
    {
        public int PatientId { get; set; }
        public string ForceNumber { get; set; } = string.Empty;
        public string? Rank { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Unit { get; set; } 
        public string PrimaryPhoneNumber { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public string EmergencyContactName { get; set; } = string.Empty;
        public string EmergencyContactPhone { get; set; } = string.Empty;
        public int VisitId { get; set; } 
        public string FullName => $"{FirstName} {LastName}".Trim();
        public bool ShowActionButtons { get; set; }
    }
}
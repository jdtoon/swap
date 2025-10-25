namespace carestream.core.dtos.consultation // lowercase
{
    /// <summary>
    /// Data Transfer Object for displaying patient banner information
    /// at the top of the consultation screen.
    /// </summary>
    public class PatientBannerDto
    {
        public int PatientId { get; set; }
        public int VisitId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string? Rank { get; set; }
        public int? Age { get; set; } // Calculated age
        public string? Gender { get; set; }
        public string? ForceNumber { get; set; }
        public string? BriefReasonForVisit { get; set; } // From the visit record
        public DateTime VisitTimestamp { get; set; } // From the visit record
        // Add any other key identifiers or alerts you want in the banner
    }
}
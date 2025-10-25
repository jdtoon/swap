namespace carestream.core.dtos.medication
{
    public class MedicationSearchResultDto
    {
        public int MedicationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Strength { get; set; }
        public string? Form { get; set; }
        public string DisplayName => $"{Name} {Strength ?? ""} {Form ?? ""}".Trim();
    }
}
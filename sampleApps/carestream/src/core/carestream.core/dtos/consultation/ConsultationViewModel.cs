namespace carestream.core.dtos.consultation
{
    /// <summary>
    /// View model for the main consultation screen (_ConsultationLayout.cshtml).
    /// </summary>
    public class ConsultationViewModel
    {
        public PatientBannerDto PatientBanner { get; set; } = new PatientBannerDto();
        public ConsultationVitalsDisplayDto? VitalsData { get; set; }
        public string? DoctorNotes { get; set; }
        public string ActiveTab { get; set; } = "VitalSigns";
        public ConsultationMedicationsViewModel? MedicationsData { get; set; }

        public List<Icd10CodeDto> SavedIcd10Codes { get; set; } = new List<Icd10CodeDto>();
        public List<ProcedureDto> SavedProcedures { get; set; } = new List<ProcedureDto>();
    }
}
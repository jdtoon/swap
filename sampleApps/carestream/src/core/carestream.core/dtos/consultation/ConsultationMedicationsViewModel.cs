using carestream.core.dtos.prescription;

namespace carestream.core.dtos.consultation
{
    public class ConsultationMedicationsViewModel
    {
        public int VisitId { get; set; }
        public int PatientId { get; set; }
        public List<PrescriptionItemDto> CurrentPrescriptionItems { get; set; } = new List<PrescriptionItemDto>();
        public AddPrescriptionItemInputDto NewPrescriptionItem { get; set; } = new AddPrescriptionItemInputDto();
        public string? MedicationSearchTerm { get; set; } 
    }
}
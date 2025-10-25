using Microsoft.Extensions.Logging;
using carestream.core.interfaces.services;
using carestream.core.interfaces.repositories;
using carestream.core.dtos.medication;
using carestream.core.dtos.prescription;
using carestream.core.dtos.consultation;

namespace carestream.core.services
{
    public class PrescriptionService : IPrescriptionService
    {
        private readonly IMedicationRepository _medicationRepository;
        private readonly IPrescriptionRepository _prescriptionRepository;
        private readonly ILogger<PrescriptionService> _logger;

        public PrescriptionService(
            IMedicationRepository medicationRepository,
            IPrescriptionRepository prescriptionRepository,
            ILogger<PrescriptionService> logger)
        {
            _medicationRepository = medicationRepository ?? throw new ArgumentNullException(nameof(medicationRepository));
            _prescriptionRepository = prescriptionRepository ?? throw new ArgumentNullException(nameof(prescriptionRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<MedicationSearchResultDto>> SearchMedicationsAsync(string searchTerm, int limit = 10)
        {
            _logger.LogInformation("Service: Searching medications for term '{SearchTerm}' with limit {Limit}.", searchTerm, limit);
            if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
            {
                _logger.LogInformation("Service: Search term too short, returning empty results.");
                return Enumerable.Empty<MedicationSearchResultDto>();
            }
            return await _medicationRepository.SearchMedicationsAsync(searchTerm, limit);
        }

        public async Task<ConsultationMedicationsViewModel> GetMedicationsViewModelAsync(int visitId, int patientId)
        {
            _logger.LogInformation("Service: Getting medications view model for VisitId: {VisitId}, PatientId: {PatientId}.", visitId, patientId);
            var currentItems = await _prescriptionRepository.GetPrescriptionItemsForVisitAsync(visitId);
            return new ConsultationMedicationsViewModel
            {
                VisitId = visitId,
                PatientId = patientId,
                CurrentPrescriptionItems = currentItems.ToList(),
                NewPrescriptionItem = new AddPrescriptionItemInputDto { VisitId = visitId }
            };
        }

        public async Task<IEnumerable<PrescriptionItemDto>> AddPrescriptionItemAsync(AddPrescriptionItemInputDto inputDto, int prescribingUserId)
        {
            _logger.LogInformation("Service: Attempting to add prescription item for VisitId: {VisitId}, MedicationId: {MedicationId} by User: {PrescribingUserId}.",
                inputDto.VisitId, inputDto.MedicationId, prescribingUserId);

            var addedItem = await _prescriptionRepository.AddPrescriptionItemAsync(inputDto, prescribingUserId);
            if (addedItem == null)
            {
                _logger.LogWarning("Service: Failed to add prescription item for VisitId: {VisitId}, MedicationId: {MedicationId}.", inputDto.VisitId, inputDto.MedicationId);
            }
            else
            {
                _logger.LogInformation("Service: Successfully added prescription item ID {PrescriptionItemId} for VisitId {VisitId}.", addedItem.PrescriptionItemId, inputDto.VisitId);
            }

            return await _prescriptionRepository.GetPrescriptionItemsForVisitAsync(inputDto.VisitId)
                   ?? Enumerable.Empty<PrescriptionItemDto>();
        }

        public async Task<IEnumerable<PrescriptionItemDto>> RemovePrescriptionItemAsync(int prescriptionItemId, int visitId)
        {
            _logger.LogInformation("Service: Attempting to remove prescription item ID: {PrescriptionItemId} for VisitId: {VisitId}.", prescriptionItemId, visitId);

            bool success = await _prescriptionRepository.RemovePrescriptionItemAsync(prescriptionItemId);
            if (!success)
            {
                _logger.LogWarning("Service: Failed to remove prescription item ID: {PrescriptionItemId}. It might have already been sent or does not exist.", prescriptionItemId);
            }
            else
            {
                _logger.LogInformation("Service: Successfully removed prescription item ID: {PrescriptionItemId}.", prescriptionItemId);
            }

            return await _prescriptionRepository.GetPrescriptionItemsForVisitAsync(visitId)
                   ?? Enumerable.Empty<PrescriptionItemDto>();
        }

        public async Task<bool> SendPrescriptionToPharmacyAsync(int visitId, int sentByUserId)
        {
            _logger.LogInformation("Service: Attempting to send prescription to pharmacy for VisitId: {VisitId} by User: {SentByUserId}.", visitId, sentByUserId);

            bool success = await _prescriptionRepository.SendPrescriptionToPharmacyAsync(visitId, sentByUserId);

            if (success)
            {
                _logger.LogInformation("Service: Prescription for VisitId: {VisitId} successfully marked as sent to pharmacy.", visitId);
            }
            else
            {
                _logger.LogWarning("Service: Failed to mark prescription for VisitId: {VisitId} as sent to pharmacy, or no items were pending.", visitId);
            }
            return success;
        }
    }
}
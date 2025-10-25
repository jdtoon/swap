using carestream.core.dtos.medication;
using carestream.core.dtos.prescription;
using carestream.core.dtos.consultation;

namespace carestream.core.interfaces.services
{
    /// <summary>
    /// Defines operations related to managing prescriptions during a consultation.
    /// </summary>
    public interface IPrescriptionService
    {
        /// <summary>
        /// Searches for available medications based on a search term.
        /// </summary>
        /// <param name="searchTerm">The term to search for.</param>
        /// <param name="limit">The maximum number of results to return.</param>
        /// <returns>A collection of medication search results.</returns>
        Task<IEnumerable<MedicationSearchResultDto>> SearchMedicationsAsync(string searchTerm, int limit = 10);

        /// <summary>
        /// Gets the current state of the medication prescription for a given visit,
        /// including items already added but not yet finalized.
        /// </summary>
        /// <param name="visitId">The ID of the current visit.</param>
        /// <param name="patientId">The ID of the patient (for context).</param>
        /// <returns>A view model containing current prescription items and data for adding new ones.</returns>
        Task<ConsultationMedicationsViewModel> GetMedicationsViewModelAsync(int visitId, int patientId);

        /// <summary>
        /// Adds a new medication item to the current (pending) prescription for a visit.
        /// </summary>
        /// <param name="inputDto">The DTO containing details of the medication item to add.</param>
        /// <param name="prescribingUserId">The ID of the user performing the prescription.</param>
        /// <returns>The updated list of current prescription items for the visit, or a success/failure indicator.</returns>
        Task<IEnumerable<PrescriptionItemDto>> AddPrescriptionItemAsync(AddPrescriptionItemInputDto inputDto, int prescribingUserId);

        /// <summary>
        /// Removes a medication item from the current (pending) prescription for a visit.
        /// Only items not yet sent to pharmacy can be removed.
        /// </summary>
        /// <param name="prescriptionItemId">The ID of the prescription item to remove.</param>
        /// <param name="visitId">The ID of the visit (for reloading context).</param>
        /// <returns>The updated list of current prescription items for the visit, or a success/failure indicator.</returns>
        Task<IEnumerable<PrescriptionItemDto>> RemovePrescriptionItemAsync(int prescriptionItemId, int visitId);

        /// <summary>
        /// Finalizes the current prescription for a visit and marks it as sent to pharmacy.
        /// </summary>
        /// <param name="visitId">The ID of the visit whose prescription is being finalized.</param>
        /// <param name="sentByUserId">The ID of the user finalizing the prescription.</param>
        /// <returns>True if successful, false otherwise.</returns>
        Task<bool> SendPrescriptionToPharmacyAsync(int visitId, int sentByUserId);
    }
}
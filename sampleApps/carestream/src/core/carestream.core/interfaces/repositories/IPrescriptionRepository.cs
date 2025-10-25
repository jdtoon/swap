using carestream.core.dtos.prescription;
using carestream.core.dtos.pharmacy;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic; // For IEnumerable
using carestream.core.dtos.consultation;
using carestream.core.dtos.shared; // For PrescriptionDetailHeaderDto, PrescriptionDetailItemDto

namespace carestream.core.interfaces.repositories
{
    /// <summary>
    /// Defines data access operations related to medication prescriptions.
    /// </summary>
    public interface IPrescriptionRepository
    {
        /// <summary>
        /// Retrieves prescription items for a specific visit that are not yet sent to pharmacy.
        /// </summary>
        /// <param name="visitId"></param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        Task<IEnumerable<PrescriptionItemDto>> GetPrescriptionItemsForVisitAsync(int visitId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Adds a new prescription item to a visit.
        /// </summary>
        /// <param name="item">The DTO containing the prescription item data.</param>
        /// <param name="createdByUserId">The ID of the user who created the item.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>The created <see cref="PrescriptionItemDto"/> with its generated ID, or null if creation failed.</returns>
        Task<PrescriptionItemDto?> AddPrescriptionItemAsync(AddPrescriptionItemInputDto item, int createdByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Removes a prescription item. Can only remove if not yet sent to pharmacy.
        /// </summary>
        /// <param name="prescriptionItemId">The ID of the prescription item to remove.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>True if the item was removed, false otherwise.</returns>
        Task<bool> RemovePrescriptionItemAsync(int prescriptionItemId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Marks all prescription items for a given visit as sent to pharmacy.
        /// </summary>
        /// <param name="visitId">The ID of the visit.</param>
        /// <param name="sentByUserId">The ID of the user who sent the prescription.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>True if at least one item was marked as sent, false otherwise.</returns>
        Task<bool> SendPrescriptionToPharmacyAsync(int visitId, int sentByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves a paginated summary of prescriptions pending dispensation.
        /// </summary>
        /// <param name="limit"></param>
        /// <param name="offset"></param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        Task<IEnumerable<PendingPrescriptionSummaryDto>> GetPendingPrescriptionsSummaryAsync(int limit = 25, int offset = 0, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves statistics for the pharmacist dashboard (e.g., pending prescription count).
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        Task<PharmacistDashboardStatsDto> GetPharmacistDashboardStatsAsync(IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves detailed items for a specific prescription.
        /// </summary>
        /// <param name="visitId"></param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        Task<IEnumerable<PrescriptionDetailItemDto>> GetPrescriptionDetailItemsAsync(int visitId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves header information for a specific prescription detail view.
        /// </summary>
        /// <param name="visitId"></param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        Task<PrescriptionDetailHeaderDto?> GetPrescriptionDetailHeaderAsync(int visitId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves prescription items prepared for dispensing, including basic stock info.
        /// </summary>
        /// <param name="visitId"></param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        Task<IEnumerable<DispenseItemDto>> GetItemsForDispensingAsync(int visitId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Updates the dispense status and quantity for a prescription item.
        /// </summary>
        /// <param name="prescriptionItemId"></param>
        /// <param name="newTotalQuantityDispensed"></param>
        /// <param name="isFullyDispensed"></param>
        /// <param name="dispensedByUserId"></param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        Task<bool> UpdatePrescriptionItemDispenseStatusAsync(
            int prescriptionItemId,
            string newTotalQuantityDispensed,
            bool isFullyDispensed,
            int dispensedByUserId,
            IDbConnection? connection = null,
            IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves current dispense information for a specific prescription item.
        /// </summary>
        /// <param name="prescriptionItemId"></param>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        Task<PrescriptionItemDispenseInfoDto?> GetPrescriptionItemCurrentDispenseInfoAsync(int prescriptionItemId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves a paginated list of all prescription items for a specific patient,
        /// including fully and partially dispensed.
        /// </summary>
        /// <param name="patientId">The ID of the patient.</param>
        /// <param name="options">Options for filtering and pagination.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>A tuple containing the enumerable of <see cref="PatientPrescriptionHistoryItemDto"/> and the total count.</returns>
        Task<(IEnumerable<PatientPrescriptionHistoryItemDto> Items, int TotalCount)> GetPatientPrescriptionHistoryAsync(int patientId, FilterAndPaginationOptions options, IDbConnection? connection = null, IDbTransaction? transaction = null);
    }
}
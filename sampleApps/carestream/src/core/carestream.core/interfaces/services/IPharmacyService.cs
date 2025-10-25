using carestream.core.dtos.medication; // Added for MedicationInventoryViewModel
using carestream.core.dtos.pharmacy;
using carestream.core.dtos.shared;
using System.Data;

namespace carestream.core.interfaces.services
{
    public interface IPharmacyService
    {
        /// <summary>
        /// Gets the view model data for the Pharmacist Dashboard.
        /// </summary>
        /// <param name="pageNumber">For pagination of pending prescriptions (1-based).</param>
        /// <param name="pageSize">Number of items per page for pending prescriptions.</param>
        /// <returns>The view model for the pharmacist dashboard.</returns>
        Task<PharmacistDashboardViewModel> GetDashboardViewModelAsync(int pageNumber = 1, int pageSize = 25);

        /// <summary>
        /// Gets the detailed view model for a specific prescription (visit).
        /// </summary>
        /// <param name="visitId">The ID of the visit whose prescription details are to be fetched.</param>
        /// <returns>A view model containing the prescription header and item details, or null if not found.</returns>
        Task<ViewPrescriptionViewModel?> GetPrescriptionDetailsAsync(int visitId);

        /// <summary>
        /// Gets the view model needed to start the dispensing process for a prescription.
        /// </summary>
        /// <param name="visitId">The ID of the visit whose prescription is to be dispensed.</param>
        /// <returns>The view model for the dispensing screen, or null if prescription not found/ready.</returns>
        Task<StartDispenseViewModel?> GetStartDispenseViewModelAsync(int visitId);

        /// <summary>
        /// Processes the dispensing of medications based on pharmacist input.
        /// Logs dispense actions and updates prescription item statuses.
        /// </summary>
        /// <param name="dispenseInput">The view model containing items to dispense and quantities.</param>
        /// <param name="pharmacistUserId">The ID of the pharmacist performing the dispense.</param>
        /// <returns>A DTO confirming the dispense actions and any errors.</returns>
        Task<DispenseConfirmationDto> ProcessDispenseAsync(StartDispenseViewModel dispenseInput, int pharmacistUserId);

        /// <summary>
        /// Gets the view model for the dispensed history page, including items and pagination info.
        /// </summary>
        /// <param name="options">Filtering and pagination options.</param>
        /// <returns>The view model for the dispensed history page.</returns>
        Task<DispensedHistoryViewModel> GetDispensedHistoryViewModelAsync(FilterAndPaginationOptions options);

        /// <summary>
        /// Retrieves a view model containing a paginated list of medication stock details for the inventory UI.
        /// Includes overall stock status and low stock items count.
        /// </summary>
        /// <param name="options">Filtering and pagination options for the inventory list.</param>
        /// <returns>A <see cref="MedicationInventoryViewModel"/> containing the paginated inventory list and associated data.</returns>
        Task<MedicationInventoryViewModel> GetMedicationInventoryViewModelAsync(FilterAndPaginationOptions options);

        /// <summary>
        /// Retrieves a single medication stock detail by its ID, including current stock level and other details.
        /// </summary>
        /// <param name="medicationId">ID of the medication in question</param>
        /// <returns></returns>
        Task<MedicationStockDetailDto?> GetMedicationStockDetailAsync(int medicationId);

        /// <summary>
        /// Adjusts the stock level for a specific medication by incrementing or decrementing its quantity.
        /// </summary>
        /// <param name="medicationId">The ID of the medication whose stock is to be adjusted.</param>
        /// <param name="quantity">The amount by which to adjust the stock.</param>
        /// <param name="isIncrement">If true, stock is incremented; if false, it is decremented.</param>
        /// <param name="performingUserId">The ID of the user performing the stock adjustment.</param>
        /// <returns>True if the stock was successfully adjusted, false otherwise.</returns>
        Task<bool> AdjustMedicationStockAsync(int medicationId, int quantity, bool isIncrement, int performingUserId);
    }
}
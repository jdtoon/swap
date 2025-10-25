using carestream.core.dtos.medication;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic; // For IEnumerable
using carestream.core.dtos.shared; // For FilterAndPaginationOptions

namespace carestream.core.interfaces.repositories
{
    /// <summary>
    /// Defines data access operations related to medications and their stock.
    /// </summary>
    public interface IMedicationRepository
    {
        /// <summary>
        /// Searches for medications by name or category.
        /// </summary>
        /// <param name="searchTerm">The term to search for.</param>
        /// <param name="limit">The maximum number of results to return.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>An enumerable of <see cref="MedicationSearchResultDto"/> matching the search term.</returns>
        Task<IEnumerable<MedicationSearchResultDto>> SearchMedicationsAsync(string searchTerm, int limit = 10, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves a medication by its unique ID.
        /// </summary>
        /// <param name="medicationId">The unique ID of the medication.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>A <see cref="MedicationSearchResultDto"/> or null if not found.</returns>
        Task<MedicationSearchResultDto?> GetMedicationByIdAsync(int medicationId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves the quantity on hand for a specific medication at the current facility.
        /// </summary>
        /// <param name="medicationId">The ID of the medication.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>The quantity on hand, or null if no stock entry exists.</returns>
        Task<int?> GetStockOnHandAsync(int medicationId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Decrements the quantity on hand for a medication at the current facility.
        /// </summary>
        /// <param name="medicationId">The ID of the medication.</param>
        /// <param name="quantityToDecrement">The amount to decrement.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>True if stock was decremented successfully, false otherwise (e.g., insufficient stock).</returns>
        Task<bool> DecrementStockAsync(int medicationId, int quantityToDecrement, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves detailed medication stock information for the current facility.
        /// </summary>
        /// <param name="medicationId">The ID of the medication.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>A <see cref="MedicationStockDetailDto"/> or null if not found.</returns>
        Task<MedicationStockDetailDto?> GetMedicationStockDetailAsync(int medicationId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves a paginated list of all medication stock entries for the current facility, with optional search.
        /// </summary>
        /// <param name="options">Options for filtering, searching, and pagination (SearchTerm1 for name/category, IsActiveFilter for medication status).</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>A tuple containing the enumerable of <see cref="MedicationStockDetailDto"/> and the total count.</returns>
        Task<(IEnumerable<MedicationStockDetailDto> Items, int TotalCount)> GetAllMedicationStockAsync(FilterAndPaginationOptions options, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Increments the quantity on hand for a medication at the current facility.
        /// If no stock entry exists, a new one is created.
        /// </summary>
        /// <param name="medicationId">The ID of the medication.</param>
        /// <param name="quantityToIncrement">The amount to increment.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>True if stock was incremented successfully, false otherwise.</returns>
        Task<bool> IncrementStockAsync(int medicationId, int quantityToIncrement, int recordedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Sets the exact quantity on hand for a medication at the current facility.
        /// If no stock entry exists, a new one is created.
        /// </summary>
        /// <param name="medicationId">The ID of the medication.</param>
        /// <param name="newQuantity">The new quantity to set.</param>
        /// <param name="recordedByUserId">The ID of the user performing the update.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>True if stock was set successfully, false otherwise.</returns>
        Task<bool> SetStockLevelAsync(int medicationId, int newQuantity, int recordedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null);
    }
}
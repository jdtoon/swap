using carestream.core.dtos.consultation; // For Icd10CodeDto
using carestream.core.dtos.shared; // For FilterAndPaginationOptions
using System.Data;
using System.Threading.Tasks;

namespace carestream.core.interfaces.repositories
{
    /// <summary>
    /// Defines data access operations for ICD-10 codes.
    /// </summary>
    public interface IIcd10CodeRepository
    {
        /// <summary>
        /// Searches for ICD-10 codes by code or description.
        /// </summary>
        /// <param name="searchTerm">The term to search for.</param>
        /// <param name="limit">The maximum number of results to return.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>An enumerable of <see cref="Icd10CodeDto"/> matching the search term.</returns>
        Task<IEnumerable<Icd10CodeDto>> SearchIcd10CodesAsync(string searchTerm, int limit = 10, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves an ICD-10 code by its unique ID.
        /// </summary>
        /// <param name="icd10CodeId">The unique ID of the ICD-10 code.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>An <see cref="Icd10CodeDto"/> if found; otherwise, null.</returns>
        Task<Icd10CodeDto?> GetIcd10CodeByIdAsync(int icd10CodeId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves an ICD-10 code by its code string.
        /// </summary>
        /// <param name="code">The ICD-10 code string.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>An <see cref="Icd10CodeDto"/> if found; otherwise, null.</returns>
        Task<Icd10CodeDto?> GetIcd10CodeByCodeAsync(string code, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Retrieves a paginated list of all ICD-10 codes, with optional search and filtering.
        /// </summary>
        /// <param name="options">Options for filtering, searching, and pagination.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>A tuple containing the enumerable of <see cref="Icd10CodeDto"/> and the total count.</returns>
        Task<(IEnumerable<Icd10CodeDto> Items, int TotalCount)> GetAllIcd10CodesAsync(FilterAndPaginationOptions options, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Creates a new ICD-10 code record.
        /// </summary>
        /// <param name="icd10CodeDto">The DTO containing the ICD-10 code data.</param>
        /// <param name="createdByUserId">The ID of the user who created the record.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>The ID of the newly created ICD-10 code, or 0 if creation failed.</returns>
        Task<int> CreateIcd10CodeAsync(Icd10CodeDto icd10CodeDto, int createdByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Updates an existing ICD-10 code record.
        /// </summary>
        /// <param name="icd10CodeDto">The DTO containing the updated ICD-10 code data.</param>
        /// <param name="updatedByUserId">The ID of the user who updated the record.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>True if the update was successful, false otherwise.</returns>
        Task<bool> UpdateIcd10CodeAsync(Icd10CodeDto icd10CodeDto, int updatedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Deactivates an ICD-10 code, making it inactive.
        /// </summary>
        /// <param name="icd10CodeId">The ID of the ICD-10 code to deactivate.</param>
        /// <param name="deactivatedByUserId">The ID of the user who deactivated the record.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>True if deactivation was successful, false otherwise.</returns>
        Task<bool> DeactivateIcd10CodeAsync(int icd10CodeId, int deactivatedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null);

        /// <summary>
        /// Activates an inactive ICD-10 code.
        /// </summary>
        /// <param name="icd10CodeId">The ID of the ICD-10 code to activate.</param>
        /// <param name="activatedByUserId">The ID of the user who activated the record.</param>
        /// <param name="connection">Optional existing database connection.</param>
        /// <param name="transaction">Optional existing database transaction.</param>
        /// <returns>True if activation was successful, false otherwise.</returns>
        Task<bool> ActivateIcd10CodeAsync(int icd10CodeId, int activatedByUserId, IDbConnection? connection = null, IDbTransaction? transaction = null);
    }
}